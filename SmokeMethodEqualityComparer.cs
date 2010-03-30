using System;
using System.Collections.Generic;

unsafe class SmokeMethodEqualityComparer : IEqualityComparer<Smoke.ModuleIndex> {

    public static SmokeMethodEqualityComparer DefaultEqualityComparer = new SmokeMethodEqualityComparer();
    public static SmokeMethodEqualityComparer AbstractRespectingComparer = new SmokeMethodEqualityComparer(true);

    readonly bool m_abstractIsDifference;

    private SmokeMethodEqualityComparer() : this(false) {}

    private SmokeMethodEqualityComparer(bool abstractIsDifference) {
        m_abstractIsDifference = abstractIsDifference;
    }

    public bool Equals(Smoke.ModuleIndex first, Smoke.ModuleIndex second) {
        Smoke.Method* firstMeth = first.smoke->methods + first.index;
        Smoke.Method* secondMeth = second.smoke->methods + second.index;

        bool firstConst = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);
        bool secondConst = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);

        bool firstStatic = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);
        bool secondStatic = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        bool firstAbstract = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0);
        bool secondAbstract = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0);

        if (firstConst != secondConst || firstStatic != secondStatic ||
            (m_abstractIsDifference && (firstAbstract != secondAbstract)) )
        {
            return false;
        }

        if (first.smoke == second.smoke) {
            // when the methods are in the same module, we can be rather quick
            if (firstMeth->name == secondMeth->name && firstMeth->args == secondMeth->args && firstMeth->ret == secondMeth->ret)
                return true;
            return false;
        } else {
            if (ByteArrayManager.strcmp(first.smoke->methodNames[firstMeth->name], second.smoke->methodNames[secondMeth->name]) == 0 &&
                ByteArrayManager.strcmp(first.smoke->types[firstMeth->ret].name, second.smoke->types[secondMeth->ret].name) == 0 &&
                firstMeth->numArgs == secondMeth->numArgs)
            {
                // name and number of arguments match, now compare the arguments individually
                short *firstMethodArgPtr = first.smoke->argumentList + firstMeth->args;
                short *secondMethodArgPtr = second.smoke->argumentList + secondMeth->args;

                while ((*firstMethodArgPtr) > 0) {
                    if (ByteArrayManager.strcmp(first.smoke->types[*firstMethodArgPtr].name, second.smoke->types[*secondMethodArgPtr].name) != 0) {
                        return false;
                    }
                    firstMethodArgPtr++;
                    secondMethodArgPtr++;
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

        int hash = method->name << 18 + method->args << 2 + (isConst? 1 : 0) << 1 + (isStatic? 1 : 0);
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
