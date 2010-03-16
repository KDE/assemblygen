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
using System.Runtime.InteropServices;

// Extension methods would be nice, but 'Smoke' is a struct and would be
// copied every time we use an extension method.
unsafe partial struct Smoke {

    [DllImport("smokeloader", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern unsafe bool GetModuleIndexFromClassName(byte* name, ref Smoke* smoke, ref short index);

    public short idClass(string name) {
        byte[] bytes = ByteArrayManager.GetCString(name);
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

    public short idMethodName(string name) {
        byte[] bytes = ByteArrayManager.GetCString(name);
        fixed (byte* m = bytes) {
            short imax = numMethodNames;
            short imin = 1;
            short icur = -1;
            long icmp = -1;

            while (imax >= imin) {
                icur = (short) ((imin + imax) / 2);
                icmp = ByteArrayManager.strcmp(methodNames[icur], m);
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

    public short idMethod(short c, short name) {
        short imax = numMethodMaps;
        short imin = 1;
        short icur = -1;
        int icmp = -1;

        while (imax >= imin) {
            icur = (short) ((imin + imax) / 2);
            icmp = methodMaps[icur].classId - c;
            if (icmp == 0) {
                icmp = methodMaps[icur].name - name;
                if (icmp == 0) {
                    return icur;
                }
            }
            if (icmp > 0) {
                imax = (short) (icur - 1);
            } else {
                imin = (short) (icur + 1);
            }
        }

        return 0;
    }

    public short FindMungedName(Smoke.Method* meth) {
        short imax = numMethodMaps;
        short imin = 1;
        short icur = -1;
        int icmp = -1;

        short c = meth->classId;
        short searchId = (short) (meth - methods);

        while (imax >= imin) {
            icur = (short) ((imin + imax) / 2);
            icmp = methodMaps[icur].classId - c;
            if (icmp == 0) {
                // move to the beginning of this class
                while (methodMaps[icur - 1].classId == c) {
                    icur--;
                }
                // we found the class, let's go hunt for the munged name
                while (methodMaps[icur].classId == c) {
                    if (methodMaps[icur].method == searchId) {
                        return methodMaps[icur].name;
                    } else if (methodMaps[icur].method < 0) {
                        for (short *id = ambiguousMethodList + (-methodMaps[icur].method); *id > 0; id++) {
                            if (*id == searchId)
                                return methodMaps[icur].name;
                        }
                    }
                    icur++;
                }
            }
            if (icmp > 0) {
                imax = (short) (icur - 1);
            } else {
                imin = (short) (icur + 1);
            }
        }

        return 0;
    }

    // adapted from QtRuby's findAllMethods()
    public void FindAllMethods(short c, IDictionary<Smoke.ModuleIndex, string> ret, bool searchSuperClasses) {
        Smoke *thisPtr;
        fixed (Smoke *smoke = &this) {
            thisPtr = smoke;    // hackish, but this is the cleanest way that I know to get the 'this' pointer
        }

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

        if (icmp != 0) {
            Console.Error.WriteLine("WARNING: FindAllMethods failed for class {0}", ByteArrayManager.GetString(classes[c].className));
            return;
        }

        for (short i = methmin; i <= methmax; i++) {
            string mungedName = ByteArrayManager.GetString(methodNames[methodMaps[i].name]);
            short methId = methodMaps[i].method;
            if (methId > 0) {
                ret[new Smoke.ModuleIndex(thisPtr, methId)] = mungedName;
            } else {
                for (short *overload = ambiguousMethodList + (-methId); *overload > 0; overload++) {
                    ret[new Smoke.ModuleIndex(thisPtr, *overload)] = mungedName;
                }
            }
        }
        if (searchSuperClasses) {
            for (short *parent = inheritanceList + classes[c].parents; *parent > 0; parent++) {
                Smoke.Class* klass = classes + *parent;
                if (klass->external) {
                    Smoke* smoke = (Smoke*) 0;
                    short index = 0;
                    if (!GetModuleIndexFromClassName(klass->className, ref smoke, ref index)) {
                        Console.Error.WriteLine("  |--Failed resolving external class {0}", ByteArrayManager.GetString(klass->className));
                        continue;
                    }
                    smoke->FindAllMethods(index, ret, true);
                } else {
                    FindAllMethods(*parent, ret, true);
                }
            }
        }
    }

    // convenience overload
    public Dictionary<Smoke.ModuleIndex, string> FindAllMethods(short classId, bool searchSuperClasses) {
        Dictionary<Smoke.ModuleIndex, string> ret = new Dictionary<Smoke.ModuleIndex, string>();
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
