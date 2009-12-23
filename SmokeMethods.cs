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

// Extension methods would be nice, but 'Smoke' is a struct and would be
// copied every time we use an extension method.
unsafe partial struct Smoke {
    public short idClass(string name) {
        byte[] bytes = Encoding.ASCII.GetBytes(name);
        fixed (byte* c = bytes) {
            short imax = numClasses;
            short imin = 1;
            short icur = -1;
            long icmp = -1;

            while (imax >= imin) {
                icur = (short) ((imin + imax) / 2);
                icmp = ByteArrayManager.strcmp(classes[icur].className, c);
                if (icmp == 0) {
                    return icur;
                }
                if (icmp > 0) {
                    imax = (short) (icur - 1);
                } else {
                    imin = (short) (icur + 1);
                }
            }

            return 0;
        }
    }

    // adapted from QtRuby's findAllMethods()
    public void FindAllMethods(short c, IDictionary<short, string> ret, bool searchSuperClasses) {
        short imax = numMethodMaps;
        short imin = 0, icur = -1, methmin = -1, methmax = -1;
        int icmp = -1;
        while(imax >= imin) {
            icur = (short) ((imin + imax) / 2);
            icmp = methodMaps[icur].classId - c;
            if (icmp == 0) {
                short pos = icur;
                while (icur > 0 && methodMaps[icur-1].classId == c)
                    icur--;
                methmin = icur;
                icur = pos;
                while(icur < imax && methodMaps[icur+1].classId == c)
                    icur++;
                methmax = icur;
                break;
            }
            if (icmp > 0)
                imax = (short) (icur - 1);
            else
                imin = (short) (icur + 1);
        }
        if (icmp != 0)
            return;

        for (short i = methmin; i <= methmax; i++) {
            string mungedName = ByteArrayManager.GetString(methodNames[methodMaps[i].name]);
            short methId = methodMaps[i].method;
            if (methId > 0) {
                ret[methId] = mungedName;
            } else {
                for (short *overload = ambiguousMethodList + (-methId); *overload > 0; overload++) {
                    ret[*overload] = mungedName;
                }
            }
        }
        if (searchSuperClasses) {
            for (short *parent = inheritanceList + classes[c].parents; *parent > 0; parent++) {
                FindAllMethods(*parent, ret, true);
            }
        }
    }

    // convenience overload
    public Dictionary<short, string> FindAllMethods(short classId, bool searchSuperClasses) {
        Dictionary<short, string> ret = new Dictionary<short, string>();
        FindAllMethods(classId, ret, searchSuperClasses);
        return ret;
    }

    public override string ToString() {
        return ByteArrayManager.GetString(module_name);
    }
    
    public string GetMethodSignature(short index) {
        Smoke.Method* meth = methods + index;
        return GetMethodSignature(meth);
    }
    
    public string GetMethodSignature(Smoke.Method *meth) {
        StringBuilder str = new StringBuilder();
        str.Append(ByteArrayManager.GetString(methodNames[meth->name]));
        str.Append('(');
        for (short* typeIndex = argumentList + meth->args; *typeIndex > 0;) {
            str.Append(ByteArrayManager.GetString(types[*typeIndex].name));
            if (*(++typeIndex) > 0)
                str.Append(", ");
        }
        str.Append(')');
        if ((meth->flags & (ushort) Smoke.MethodFlags.mf_const) != 0)
            str.Append(" const");
        return str.ToString();
    }
}