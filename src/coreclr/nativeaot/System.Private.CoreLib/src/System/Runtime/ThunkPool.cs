// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is an implementation of a general purpose thunk pool manager. Each thunk consists of:
//      1- A thunk stub, typically consisting of a lea + jmp instructions (slightly different
//         on ARM, but semantically equivalent)
//      2- A thunk common stub: the implementation of the common stub depends on
//         the usage scenario of the thunk
//      3- Thunk data: each thunk has two pointer-sized data values that can be stored.
//         The first data value is called the thunk's 'context', and the second value is
//         the thunk's jump target typically.
//
// Without FEATURE_RX_THUNKS, thunks are allocated by mapping a thunks template into memory. The template
// consists of a number of pairs of sections called thunk blocks (typically 8 pairs per mapping). Each pair
// has 2 page-long sections (4096 bytes):
//      1- The first section has RX permissions, and contains the thunk stubs (lea's + jmp's),
//         and the thunk common stubs.
//      2- The second section has RW permissions and contains the thunks data (context + target).
//         The last pointer-sized block in this section is special: it stores the address of
//         the common stub that each thunk stub will jump to (the jump instruction in each thunk
//         jumps to the address stored in that block). Therefore, whenever a new thunks template
//         gets mapped into memory, the value of that last pointer cell in the data section is updated
//         to the common stub address passed in by the caller
//
// With FEATURE_RX_THUNKS, thunks are created by allocating new virtual memory space, where the first half of
// that space is filled with thunk stubs, and gets RX permissions, and the second half is for the thunks data,
// and gets RW permissions. The thunk stubs and data blocks are not grouped in pairs:
// all the thunk stubs blocks are groupped at the beginning of the allocated virtual memory space, and all the
// thunk data blocks are groupped in the second half of the virtual space.
//
// Available thunks are tracked using a linked list. The first cell in the data block of each thunk is
// used as the nodes of the linked list. The cell will point to the data block of the next available thunk,
// if one is available, or point to null. When thunks are freed, they are added to the beginning of the list.
//

using System.Diagnostics;
using System.Numerics;

namespace System.Runtime
{
    internal static class Constants
    {
        public static readonly int ThunkDataSize = 2 * IntPtr.Size;
        public static readonly int ThunkCodeSize = RuntimeImports.RhpGetThunkSize();
        public static readonly int NumThunksPerBlock = RuntimeImports.RhpGetNumThunksPerBlock();
        public static readonly int NumThunkBlocksPerMapping = RuntimeImports.RhpGetNumThunkBlocksPerMapping();
        public static readonly uint PageSize = BitOperations.RoundUpToPowerOf2((uint)Math.Max(ThunkCodeSize * NumThunksPerBlock, ThunkDataSize * NumThunksPerBlock + IntPtr.Size));
        public static readonly nuint PageSizeMask = PageSize - 1;
    }

    internal class ThunksHeap
    {
        private class AllocatedBlock
        {
            internal IntPtr _blockBaseAddress;
            internal AllocatedBlock _nextBlock;
        }

        private IntPtr _commonStubAddress;
        private IntPtr _nextAvailableThunkPtr;
        private IntPtr _lastThunkPtr;

        private AllocatedBlock _allocatedBlocks;

        // Helper functions to set/clear the lowest bit for ARM instruction pointers
        private static IntPtr ClearThumbBit(IntPtr value)
        {
#if TARGET_ARM
            Debug.Assert(((nint)value & 1) == 1);
            value = (IntPtr)((nint)value - 1);
#endif
            return value;
        }
        private static IntPtr SetThumbBit(IntPtr value)
        {
#if TARGET_ARM
            Debug.Assert(((nint)value & 1) == 0);
            value = (IntPtr)((nint)value + 1);
#endif
            return value;
        }

        private unsafe ThunksHeap(IntPtr commonStubAddress)
        {
            _commonStubAddress = commonStubAddress;

            _allocatedBlocks = new AllocatedBlock();

            IntPtr thunkStubsBlock = ThunkBlocks.GetNewThunksBlock();
            IntPtr thunkDataBlock = RuntimeImports.RhpGetThunkDataBlockAddress(thunkStubsBlock);

            // Address of the first thunk data cell should be at the beginning of the thunks data block (page-aligned)
            Debug.Assert(((nuint)(nint)thunkDataBlock % Constants.PageSize) == 0);

            // Update the last pointer value in the thunks data section with the value of the common stub address
            *(IntPtr*)(thunkDataBlock + (int)(Constants.PageSize - IntPtr.Size)) = commonStubAddress;
            Debug.Assert(*(IntPtr*)(thunkDataBlock + (int)(Constants.PageSize - IntPtr.Size)) == commonStubAddress);

            // Set the head and end of the linked list
            _nextAvailableThunkPtr = thunkDataBlock;
            _lastThunkPtr = _nextAvailableThunkPtr + Constants.ThunkDataSize * (Constants.NumThunksPerBlock - 1);

            _allocatedBlocks._blockBaseAddress = thunkStubsBlock;
        }

        public static unsafe ThunksHeap CreateThunksHeap(IntPtr commonStubAddress)
        {
            return new ThunksHeap(commonStubAddress);
        }

        // TODO: Feature
        // public static ThunksHeap DestroyThunksHeap(ThunksHeap heapToDestroy)
        // {
        // }

        //
        // Note: Expected to be called under lock
        //
        private unsafe void ExpandHeap()
        {
            AllocatedBlock newBlockInfo = new AllocatedBlock();

            IntPtr thunkStubsBlock = ThunkBlocks.GetNewThunksBlock();
            IntPtr thunkDataBlock = RuntimeImports.RhpGetThunkDataBlockAddress(thunkStubsBlock);

            // Address of the first thunk data cell should be at the beginning of the thunks data block (page-aligned)
            Debug.Assert(((nuint)(nint)thunkDataBlock % Constants.PageSize) == 0);

            // Update the last pointer value in the thunks data section with the value of the common stub address
            *(IntPtr*)(thunkDataBlock + (int)(Constants.PageSize - IntPtr.Size)) = _commonStubAddress;
            Debug.Assert(*(IntPtr*)(thunkDataBlock + (int)(Constants.PageSize - IntPtr.Size)) == _commonStubAddress);

            // Link the last entry in the old list to the first entry in the new list
            *((IntPtr*)_lastThunkPtr) = thunkDataBlock;

            // Update the pointer to the last entry in the list
            _lastThunkPtr = *((IntPtr*)_lastThunkPtr) + Constants.ThunkDataSize * (Constants.NumThunksPerBlock - 1);

            newBlockInfo._blockBaseAddress = thunkStubsBlock;
            newBlockInfo._nextBlock = _allocatedBlocks;

            _allocatedBlocks = newBlockInfo;
        }

        public unsafe IntPtr AllocateThunk()
        {
            // TODO: optimize the implementation and make it lock-free
            // or at least change it to a per-heap lock instead of a global lock.

            Debug.Assert(_nextAvailableThunkPtr != IntPtr.Zero);

            IntPtr nextAvailableThunkPtr;
            lock (this)
            {
                nextAvailableThunkPtr = _nextAvailableThunkPtr;
                IntPtr nextNextAvailableThunkPtr = *((IntPtr*)(nextAvailableThunkPtr));

                if (nextNextAvailableThunkPtr == IntPtr.Zero)
                {
                    ExpandHeap();

                    nextAvailableThunkPtr = _nextAvailableThunkPtr;
                    nextNextAvailableThunkPtr = *((IntPtr*)(nextAvailableThunkPtr));
                    Debug.Assert(nextNextAvailableThunkPtr != IntPtr.Zero);
                }

                _nextAvailableThunkPtr = nextNextAvailableThunkPtr;
            }
            Debug.Assert(nextAvailableThunkPtr != IntPtr.Zero);

#if DEBUG
            // Reset debug flag indicating the thunk is now in use
            *((IntPtr*)(nextAvailableThunkPtr + IntPtr.Size)) = IntPtr.Zero;
#endif

            int thunkIndex = (int)(((nuint)(nint)nextAvailableThunkPtr) - ((nuint)(nint)nextAvailableThunkPtr & ~Constants.PageSizeMask));
            Debug.Assert((thunkIndex % Constants.ThunkDataSize) == 0);
            thunkIndex /= Constants.ThunkDataSize;

            IntPtr thunkAddress = RuntimeImports.RhpGetThunkStubsBlockAddress(nextAvailableThunkPtr) + thunkIndex * Constants.ThunkCodeSize;

            return SetThumbBit(thunkAddress);
        }

        public unsafe void FreeThunk(IntPtr thunkAddress)
        {
            // TODO: optimize the implementation and make it lock-free
            // or at least change it to a per-heap lock instead of a global lock.

            IntPtr dataAddress = TryGetThunkDataAddress(thunkAddress);
            Debug.Assert(dataAddress != IntPtr.Zero);

#if DEBUG
            Debug.Assert(IsThunkInHeap(thunkAddress));

            // Debug flag indicating the thunk is no longer used
            *((IntPtr*)(dataAddress + IntPtr.Size)) = new IntPtr(-1);
#endif

            lock (this)
            {
                *((IntPtr*)(dataAddress)) = _nextAvailableThunkPtr;
                _nextAvailableThunkPtr = dataAddress;
            }
        }

        private bool IsThunkInHeap(IntPtr thunkAddress)
        {
            nuint thunkAddressValue = (nuint)(nint)ClearThumbBit(thunkAddress);

            AllocatedBlock currentBlock = _allocatedBlocks;

            while (currentBlock != null)
            {
                if (thunkAddressValue >= (nuint)(nint)currentBlock._blockBaseAddress &&
                    thunkAddressValue < (nuint)(nint)currentBlock._blockBaseAddress + (nuint)(Constants.NumThunksPerBlock * Constants.ThunkCodeSize))
                {
                    return true;
                }

                currentBlock = currentBlock._nextBlock;
            }

            return false;
        }

        private static IntPtr TryGetThunkDataAddress(IntPtr thunkAddress)
        {
            nuint thunkAddressValue = (nuint)(nint)ClearThumbBit(thunkAddress);

            // Compute the base address of the thunk's mapping
            nuint currentThunksBlockAddress = thunkAddressValue & ~Constants.PageSizeMask;

            // Make sure the thunk address is valid by checking alignment
            if ((thunkAddressValue - currentThunksBlockAddress) % (nuint)Constants.ThunkCodeSize != 0)
                return IntPtr.Zero;

            // Compute the thunk's index
            int thunkIndex = (int)((thunkAddressValue - currentThunksBlockAddress) / (nuint)Constants.ThunkCodeSize);

            // Compute the address of the data block that corresponds to the current thunk
            IntPtr thunkDataBlockAddress = RuntimeImports.RhpGetThunkDataBlockAddress((IntPtr)((nint)thunkAddressValue));

            return thunkDataBlockAddress + thunkIndex * Constants.ThunkDataSize;
        }

        /// <summary>
        /// This method retrieves the two data fields for a thunk.
        /// Caution: No checks are made to verify that the thunk address is that of a
        /// valid thunk in use. The caller of this API is responsible for providing a valid
        /// address of a thunk that was not previously freed.
        /// </summary>
        /// <returns>True if the thunk's data was successfully retrieved.</returns>
        public unsafe bool TryGetThunkData(IntPtr thunkAddress, out IntPtr context, out IntPtr target)
        {
            context = IntPtr.Zero;
            target = IntPtr.Zero;

            IntPtr dataAddress = TryGetThunkDataAddress(thunkAddress);
            if (dataAddress == IntPtr.Zero)
                return false;

            if (!IsThunkInHeap(thunkAddress))
                return false;

            // Update the data that will be used by the thunk that was allocated
            context = *((IntPtr*)(dataAddress));
            target = *((IntPtr*)(dataAddress + IntPtr.Size));

            return true;
        }

        /// <summary>
        /// This method sets the two data fields for a thunk.
        /// Caution: No checks are made to verify that the thunk address is that of a
        /// valid thunk in use. The caller of this API is responsible for providing a valid
        /// address of a thunk that was not previously freed.
        /// </summary>
        public unsafe void SetThunkData(IntPtr thunkAddress, IntPtr context, IntPtr target)
        {
            IntPtr dataAddress = TryGetThunkDataAddress(thunkAddress);
            Debug.Assert(dataAddress != IntPtr.Zero);
            Debug.Assert(IsThunkInHeap(thunkAddress));

            // Update the data that will be used by the thunk that was allocated
            *((IntPtr*)(dataAddress)) = context;
            *((IntPtr*)(dataAddress + IntPtr.Size)) = target;
        }
    }

    internal static class ThunkBlocks
    {
        private static IntPtr[] s_currentlyMappedThunkBlocks = new IntPtr[Constants.NumThunkBlocksPerMapping];
        private static int s_currentlyMappedThunkBlocksIndex = Constants.NumThunkBlocksPerMapping;

        public static unsafe IntPtr GetNewThunksBlock()
        {
            IntPtr nextThunksBlock;

            // Check the most recently mapped thunks block. Each mapping consists of multiple
            // thunk stubs pages, and multiple thunk data pages (typically 8 pages of each in a single mapping)
            if (s_currentlyMappedThunkBlocksIndex < Constants.NumThunkBlocksPerMapping)
            {
                nextThunksBlock = s_currentlyMappedThunkBlocks[s_currentlyMappedThunkBlocksIndex++];
#if DEBUG
                s_currentlyMappedThunkBlocks[s_currentlyMappedThunkBlocksIndex - 1] = IntPtr.Zero;
                Debug.Assert(nextThunksBlock != IntPtr.Zero);
#endif
            }
            else
            {
                nextThunksBlock = IntPtr.Zero;
                int result = RuntimeImports.RhAllocateThunksMapping(&nextThunksBlock);
                if (result == HResults.E_OUTOFMEMORY)
                    throw new OutOfMemoryException();
                else if (result != HResults.S_OK)
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_DynamicEntrypoint);

                // Each mapping consists of multiple blocks of thunk stubs/data pairs. Keep track of those
                // so that we do not create a new mapping until all blocks in the sections we just mapped are consumed
                IntPtr currentThunksBlock = nextThunksBlock;
                int thunkBlockSize = RuntimeImports.RhpGetThunkBlockSize();
                for (int i = 0; i < Constants.NumThunkBlocksPerMapping; i++)
                {
                    s_currentlyMappedThunkBlocks[i] = currentThunksBlock;
                    currentThunksBlock += thunkBlockSize;
                }
                s_currentlyMappedThunkBlocksIndex = 1;
            }

            Debug.Assert(nextThunksBlock != IntPtr.Zero);

            // Setup the thunks in the new block as a linked list of thunks.
            // Use the first data field of the thunk to build the linked list.
            IntPtr dataAddress = RuntimeImports.RhpGetThunkDataBlockAddress(nextThunksBlock);

            for (int i = 0; i < Constants.NumThunksPerBlock; i++)
            {
                if (i == (Constants.NumThunksPerBlock - 1))
                    *((IntPtr*)(dataAddress)) = IntPtr.Zero;
                else
                    *((IntPtr*)(dataAddress)) = dataAddress + Constants.ThunkDataSize;

#if DEBUG
                // Debug flag in the second data cell indicating the thunk is not used
                *((IntPtr*)(dataAddress + IntPtr.Size)) = new IntPtr(-1);
#endif

                dataAddress += Constants.ThunkDataSize;
            }

            return nextThunksBlock;
        }

        // TODO: [Feature] Keep track of mapped sections and free them if we need to.
        // public static unsafe void FreeThunksBlock()
        // {
        // }
    }
}
