using System;
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
