﻿/*
 * Derived from https://github.com/google/google-authenticator-android/blob/master/AuthenticatorApp/src/main/java/com/google/android/apps/authenticator/Base32String.java
 * 
 * Copyright (C) 2016 BravoTango86
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DnmpNetworkClient.Util
{
    public static class Base32Util
    {

        private static readonly char[] digits;
        private static readonly int mask;
        private static readonly int shift;
        private static readonly Dictionary<char, int> charMap = new Dictionary<char, int>();
        private const string separator = "-";

        static Base32Util()
        {
            digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();
            mask = digits.Length - 1;
            shift = NumberOfTrailingZeros(digits.Length);
            for (var i = 0; i < digits.Length; i++)
                charMap[digits[i]] = i;
        }

        private static int NumberOfTrailingZeros(int i)
        {
            // HD, Figure 5-14
            if (i == 0) return 32;
            var n = 31;
            var y = i << 16;
            if (y != 0)
            {
                n = n - 16;
                i = y;
            }

            y = i << 8;
            if (y != 0)
            {
                n = n - 8;
                i = y;
            }

            y = i << 4;
            if (y != 0)
            {
                n = n - 4;
                i = y;
            }

            y = i << 2;
            if (y != 0)
            {
                n = n - 2;
                i = y;
            }

            return n - (int) ((uint) (i << 1) >> 31);
        }

        public static byte[] Decode(string encoded)
        {
            // Remove whitespace and separators
            encoded = encoded.Trim().Replace(separator, "");

            // Remove padding. Note: the padding is used as hint to determine how many
            // bits to decode from the last incomplete chunk (which is commented out
            // below, so this may have been wrong to start with).
            encoded = Regex.Replace(encoded, "[0]*$", "");

            // Canonicalize to all upper case
            encoded = encoded.ToUpper();
            if (encoded.Length == 0)
            {
                return new byte[0];
            }

            var encodedLength = encoded.Length;
            var outLength = encodedLength * shift / 8;
            var result = new byte[outLength];
            var buffer = 0;
            var next = 0;
            var bitsLeft = 0;
            foreach (var c in encoded.ToCharArray())
            {
                if (!charMap.ContainsKey(c))
                {
                    throw new DecodingException("Illegal character: " + c);
                }

                buffer <<= shift;
                buffer |= charMap[c] & mask;
                bitsLeft += shift;
                if (bitsLeft < 8)
                    continue;
                result[next++] = (byte) (buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }

            // We'll ignore leftover bits for now.
            //
            // if (next != outLength || bitsLeft >= SHIFT) {
            //  throw new DecodingException("Bits left: " + bitsLeft);
            // }
            return result;
        }


        public static string Encode(byte[] data, bool padOutput = false)
        {
            if (data.Length == 0)
            {
                return "";
            }

            // SHIFT is the number of bits per output character, so the length of the
            // output is the length of the input multiplied by 8/SHIFT, rounded up.
            if (data.Length >= (1 << 28))
            {
                // The computation below will fail, so don't do it.
                throw new ArgumentOutOfRangeException(nameof(data));
            }

            var outputLength = (data.Length * 8 + shift - 1) / shift;
            var result = new StringBuilder(outputLength);

            int buffer = data[0];
            var next = 1;
            var bitsLeft = 8;
            while (bitsLeft > 0 || next < data.Length)
            {
                if (bitsLeft < shift)
                {
                    if (next < data.Length)
                    {
                        buffer <<= 8;
                        buffer |= (data[next++] & 0xff);
                        bitsLeft += 8;
                    }
                    else
                    {
                        var pad = shift - bitsLeft;
                        buffer <<= pad;
                        bitsLeft += pad;
                    }
                }

                var index = mask & (buffer >> (bitsLeft - shift));
                bitsLeft -= shift;
                result.Append(digits[index]);
            }

            if (!padOutput)
                return result.ToString();

            var padding = 8 - (result.Length % 8);
            if (padding > 0) result.Append(new string('0', padding == 8 ? 0 : padding));

            return result.ToString();
        }

        private class DecodingException : Exception
        {
            public DecodingException(string message) : base(message) { }
        }
    }
}