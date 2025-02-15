// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Collections.Frozen.String.SubstringEquality
{
    internal static class Slicers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Full(string s, int _, int __) => s.AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Left(string s, int index, int count) => s.AsSpan(index, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<char> Right(string s, int index, int count) => s.AsSpan(s.Length + index, count);
    }

    internal static class OrdinalEquality
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.SequenceEqual(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinal(s);
    }

    internal static class OrdinalInsensitiveEquality
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinalIgnoreCase(s);
    }

    internal static class CaseInsensitiveAsciiEquality
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y) => x.Equals(y, StringComparison.OrdinalIgnoreCase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(ReadOnlySpan<char> s) => Hashing.GetHashCodeOrdinalIgnoreCaseAscii(s);
    }

    /// <inheritdoc/>
    internal sealed class LeftJustifiedSubstringComparer : SubstringEqualityComparerBase<LeftJustifiedSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftJustifiedSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (LeftJustifiedSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Left(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class RightJustifiedSubstringComparer : SubstringEqualityComparerBase<RightJustifiedSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightJustifiedSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (RightJustifiedSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Right(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class LeftJustifiedCaseInsensitiveSubstringComparer : SubstringEqualityComparerBase<LeftJustifiedCaseInsensitiveSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftJustifiedCaseInsensitiveSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (LeftJustifiedCaseInsensitiveSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Left(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalInsensitiveEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalInsensitiveEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class RightJustifiedCaseInsensitiveSubstringComparer : SubstringEqualityComparerBase<RightJustifiedCaseInsensitiveSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightJustifiedCaseInsensitiveSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (RightJustifiedCaseInsensitiveSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Right(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => OrdinalInsensitiveEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => OrdinalInsensitiveEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class LeftJustifiedCaseInsensitiveAsciiSubstringComparer : SubstringEqualityComparerBase<LeftJustifiedCaseInsensitiveAsciiSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private LeftJustifiedCaseInsensitiveAsciiSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (LeftJustifiedCaseInsensitiveAsciiSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Left(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => CaseInsensitiveAsciiEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => CaseInsensitiveAsciiEquality.GetHashCode(Slice(s));
        }
    }

    /// <inheritdoc/>
    internal sealed class RightJustifiedCaseInsensitiveAsciiSubstringComparer : SubstringEqualityComparerBase<RightJustifiedCaseInsensitiveAsciiSubstringComparer.GSW>
    {
        internal struct GSW : IGenericSpecializedWrapper
        {
            private RightJustifiedCaseInsensitiveAsciiSubstringComparer _this;
            public void Store(ISubstringEqualityComparer @this) => _this = (RightJustifiedCaseInsensitiveAsciiSubstringComparer)@this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ReadOnlySpan<char> Slice(string s) => Slicers.Right(s, _this.Index, _this.Count);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(string? x, string? y) => CaseInsensitiveAsciiEquality.Equals(Slice(x!), Slice(y!));
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int GetHashCode(string s) => CaseInsensitiveAsciiEquality.GetHashCode(Slice(s));
        }
    }
}
