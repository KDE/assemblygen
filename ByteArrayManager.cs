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

    public static long strcmp(byte* str1, byte* str2) {
        while (*str1 == *str2) {
            if (*str1 == 0)
                return 0;
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
