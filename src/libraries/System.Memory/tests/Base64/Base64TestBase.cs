// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.utf8Bytes, utf8Bytes.Length

using System.Collections.Generic;
using System.Text;

namespace System.Buffers.Text.Tests
{
    public class Base64TestBase
    {
        public static IEnumerable<object[]> ValidBase64Strings_WithCharsThatMustBeIgnored()
        {
            // Create a Base64 string
            string text = "a b c";
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
            string base64Utf8String = Convert.ToBase64String(utf8Bytes);

            // Split the base64 string in half
            int stringLength = base64Utf8String.Length / 2;
            string firstSegment = base64Utf8String.Substring(0, stringLength);
            string secondSegment = base64Utf8String.Substring(stringLength, stringLength);

            // Insert ignored chars between the base 64 string
            // One will have 1 char, another will have 3

            // Line feed
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(9), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(9), 3), utf8Bytes };

            // Horizontal tab
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(10), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(10), 3), utf8Bytes };

            // Carriage return
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(13), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(13), 3), utf8Bytes };

            // Space
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(32), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedInTheMiddle(Convert.ToChar(32), 3), utf8Bytes };

            string GetBase64StringWithPassedCharInsertedInTheMiddle(char charToInsert, int numberOfTimesToInsert) => $"{firstSegment}{new string(charToInsert, numberOfTimesToInsert)}{secondSegment}";

            // Insert ignored chars at the start of the base 64 string
            // One will have 1 char, another will have 3

            // Line feed
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(9), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(9), 3), utf8Bytes };

            // Horizontal tab
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(10), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(10), 3), utf8Bytes };

            // Carriage return
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(13), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(13), 3), utf8Bytes };

            // Space
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(32), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheStart(Convert.ToChar(32), 3), utf8Bytes };

            string GetBase64StringWithPassedCharInsertedAtTheStart(char charToInsert, int numberOfTimesToInsert) => $"{new string(charToInsert, numberOfTimesToInsert)}{firstSegment}{secondSegment}";

            // Insert ignored chars at the end of the base 64 string
            // One will have 1 char, another will have 3
            // Whitespace after end/padding is not included in consumed bytes

            // Line feed
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(9), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(9), 3), utf8Bytes };

            // Horizontal tab
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(10), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(10), 3), utf8Bytes };

            // Carriage return
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(13), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(13), 3), utf8Bytes };

            // Space
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(32), 1), utf8Bytes };
            yield return new object[] { GetBase64StringWithPassedCharInsertedAtTheEnd(Convert.ToChar(32), 3), utf8Bytes };

            string GetBase64StringWithPassedCharInsertedAtTheEnd(char charToInsert, int numberOfTimesToInsert) => $"{firstSegment}{secondSegment}{new string(charToInsert, numberOfTimesToInsert)}";
        }

        public static IEnumerable<object[]> StringsOnlyWithCharsToBeIgnored()
        {
            // One will have 1 char, another will have 3

            // Line feed
            yield return new object[] { GetRepeatedChar(Convert.ToChar(9), 1) };
            yield return new object[] { GetRepeatedChar(Convert.ToChar(9), 3) };

            // Horizontal tab
            yield return new object[] { GetRepeatedChar(Convert.ToChar(10), 1) };
            yield return new object[] { GetRepeatedChar(Convert.ToChar(10), 3) };

            // Carriage return
            yield return new object[] { GetRepeatedChar(Convert.ToChar(13), 1) };
            yield return new object[] { GetRepeatedChar(Convert.ToChar(13), 3) };

            // Space
            yield return new object[] { GetRepeatedChar(Convert.ToChar(32), 1) };
            yield return new object[] { GetRepeatedChar(Convert.ToChar(32), 3) };

            string GetRepeatedChar(char charToInsert, int numberOfTimesToInsert) => new string(charToInsert, numberOfTimesToInsert);
        }

        public static IEnumerable<object[]> BasicDecodingWithExtraWhitespaceShouldBeCountedInConsumedBytes_MemberData()
        {
            var r = new Random(42);
            for (int i = 0; i < 5; i++)
            {
                yield return new object[] { "AQ==" + new string(GetChars(r, i)), 4 + i, 1 };
            }

            foreach (string s in new[] { "MTIz", "M TIz", "MT Iz", "MTI z", "MTIz ", "M    TI   z", "M T I Z " })
            {
                yield return new object[] { s + s + s + s, s.Length * 4, 12 };
            }
        }

        private static char[] GetChars(Random r, int i)
        {
#if NET
            return r.GetItems<char>(" \n\t\r", i);
#else
            byte[] bytes = new byte[i];
            char[] chars = new char[i];
            r.NextBytes(bytes);
            for (int j = 0; j < bytes.Length; j++)
            {
                switch (bytes[j] % 4)
                {
                    case 0: chars[j] = ' '; break;
                    case 1: chars[j] = '\n'; break;
                    case 2: chars[j] = '\t'; break;
                    case 3: chars[j] = '\r'; break;
                }
            }
            return chars;
#endif
        }
    }
}
