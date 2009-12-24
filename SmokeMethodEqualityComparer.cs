using System;
using System.Collections.Generic;

unsafe class SmokeMethodEqualityComparer : IEqualityComparer<short> {
    Smoke* smoke;

    public SmokeMethodEqualityComparer(Smoke* smoke) {
        this.smoke = smoke;
    }

    public bool Equals(short first, short second) {
        Smoke.Method* firstMeth = smoke->methods + first;
        Smoke.Method* secondMeth = smoke->methods + second;

        bool firstConst = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);
        bool secondConst = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);

        bool firstStatic = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);
        bool secondStatic = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        if (firstMeth->name == secondMeth->name && firstMeth->args == secondMeth->args && firstConst == secondConst && firstStatic == secondStatic)
            return true;

        return false;
    }

    public int GetHashCode(short index) {
        Smoke.Method *method = smoke->methods + index;
        bool isConst = ((method->flags & (ushort) Smoke.MethodFlags.mf_const) > 0);
        bool isStatic = ((method->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        long hash = method->name * 10000000 + method->args * 100 + (isConst? 10 : 0) + (isStatic? 1 : 0);
        return hash.GetHashCode();
    }

    public static bool EqualExceptConstness(Smoke.Method* firstMeth, Smoke.Method* secondMeth) {
        bool firstStatic = ((firstMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);
        bool secondStatic = ((secondMeth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0);

        if (firstMeth->name == secondMeth->name && firstMeth->args == secondMeth->args && firstStatic == secondStatic)
            return true;

        return false;
    }
}
