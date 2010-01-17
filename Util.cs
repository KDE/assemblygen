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
using System.Collections.Generic;
using System.Text;

static class Util {
    public static bool IsPrimitiveType(string type) {
        type = type.Replace("unsigned ", "u");
        if (   type == "char" || type == "uchar" || type == "short" || type == "ushort" || type == "int" || type == "uint"
            || type == "long" || type == "long long" || type == "ulong" || type == "ulong long" || type == "float" || type == "double"
            || type == "bool" || type == "void" || type == "qreal" || type == "QString")
        {
            return true;
        }
        return false;
    }

    public static List<string> SplitUnenclosed(string input, char delimeter, char open, char close) {
        int enclosed = 0;
        int lastDelimeter = -1;
        List<string> ret = new List<string>();
        for (int i = 0; i < input.Length; i++) {
            char c = input[i];
            if (c == open) {
                enclosed++;
            } else if (c == close) {
                enclosed--;
            } else if (c == delimeter && enclosed == 0) {
                ret.Add(input.Substring(lastDelimeter + 1, i - lastDelimeter - 1));
                lastDelimeter = i;
            }
        }
        ret.Add(input.Substring(lastDelimeter + 1));
        return ret;
    }
}
