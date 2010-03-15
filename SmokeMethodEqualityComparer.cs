using System;
using System.Collections.Generic;

unsafe class SmokeMethodEqualityComparer : IEqualityComparer<Smoke.ModuleIndex> {

    public bool Equals(Smoke.ModuleIndex first, Smoke.ModuleIndex second) {
        Smoke.Method* firstMeth = first.smoke->methods + first.index;
        Smoke.Method* secondMeth = first.smoke->methods + second.index;

        bool firstConst = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);
        bool secondConst = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);

        bool firstStatic = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);
        bool secondStatic = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        if (firstConst != secondConst || firstStatic != secondStatic) {
            return false;
        }

        if (first.smoke == second.smoke) {
            // when the methods are in the same module, we can be rather quick
            if (firstMeth->name == secondMeth->name && firstMeth->args == secondMeth->args)
                return true;
            return false;
        } else {
            if (ByteArrayManager.strcmp(first.smoke->methodNames[firstMeth->name], second.smoke->methodNames[secondMeth->name]) == 0 &&
                firstMeth->numArgs == secondMeth->numArgs)
            {
                // name and number of arguments match, now compare the arguments individually
                short *firstMethodArgPtr = first.smoke->argumentList + firstMeth->args;
                short *secondMethodArgPtr = second.smoke->argumentList + secondMeth->args;

                while ((*firstMethodArgPtr) > 0) {
                    if (ByteArrayManager.strcmp(first.smoke->types[*firstMethodArgPtr].name, second.smoke->types[*secondMethodArgPtr].name) != 0) {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }

    public int GetHashCode(Smoke.ModuleIndex mi) {
        Smoke.Method *method = mi.smoke->methods + mi.index;
        bool isConst = ((method->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);
        bool isStatic = ((method->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        long hash = method->name * 10000000 + method->args * 100 + (isConst? 10 : 0) + (isStatic? 1 : 0);
        return (hash ^ (long) mi.smoke).GetHashCode();
    }

    // this only has to work within the module boundaries
    public static bool EqualExceptConstness(Smoke.Method* firstMeth, Smoke.Method* secondMeth) {
        bool firstStatic = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);
        bool secondStatic = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        if (firstMeth->name == secondMeth->name && firstMeth->args == secondMeth->args && firstStatic == secondStatic)
            return true;

        return false;
    }
}
