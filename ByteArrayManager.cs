/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009 Arno Rehn <arno@arnorehn.de>

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/

using System;
using System.Collections;
using System.Runtime.InteropServices;

unsafe class ByteArrayManager {
    static Hashtable maps = new Hashtable();
    
/*    public static unsafe long strlen(byte *str) {
        if (str == (void*) 0) return 0;
        byte* s = str;
        while (*s > 0) { s++; }
        return s - str;
    }*/

    /*
     * Use this custom method because Encoding.GetBytes() won't append the '\0' at the end.
     */
    public static byte[] GetCString(string input) {
        byte[] bytes = new byte[input.Length + 1];
        for (int i = 0; i < input.Length; i++) {
            bytes[i] = (byte) input[i];
        }
        bytes[input.Length] = 0;
        return bytes;
    }

    public static long strcmp(byte* str1, byte* str2) {
        while (*str1 != 0 && *str1 == *str2) {
            str1++; str2++;
        }
        return *str1 - *str2;
    }

    public static string GetString(byte* ptr) {
        IntPtr intptr = (IntPtr) ptr;
        if (maps.Contains(intptr)) {
            return (string) maps[intptr];
        }
        string str = Marshal.PtrToStringAnsi(intptr);
        maps[intptr] = str;
        return str;
    }
}
