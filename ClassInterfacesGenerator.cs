using System;
using System.CodeDom;
using System.Collections.Generic;

unsafe class ClassInterfacesGenerator {
    ClassesGenerator classesGenerator;
    Smoke *smoke;
    // maps a C++ class to a .NET interface (needed for multiple inheritance)
    Dictionary<string, CodeTypeDeclaration> interfaceTypeMap = new Dictionary<string, CodeTypeDeclaration>();

    public ClassInterfacesGenerator(ClassesGenerator cg) {
        classesGenerator = cg;
        smoke = cg.smoke;
    }

    public Dictionary<string, CodeTypeDeclaration> InterfaceTypeMap {
        get { return interfaceTypeMap; }
    }

    // Recursively adds base classes to a hash set.
    void AddBaseClassesToHashSet(Smoke.Class *klass, HashSet<short> set) {
        short *parent = smoke->inheritanceList + klass->parents;
        while (*parent > 0) {
            Smoke.Class *baseClass = smoke->classes + *parent;
            set.Add(*parent);
            AddBaseClassesToHashSet(baseClass, set);
            parent++;
        }
    }

    /*
     * Returns a list of classes for which we need to generate interfaces.
     * IntPtr is not type-safe, but we can't have pointers as generic parameters. :(
     */
    HashSet<short> GetClassList() {
        HashSet<short> set = new HashSet<short>();
        for (short i = 1; i <= smoke->numClasses; i++) {
            Smoke.Class *klass = smoke->classes + i;
            short *parent = smoke->inheritanceList + klass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                if (firstParent) {
                    // don't generate interfaces for the first base class
                    firstParent = false;
                    parent++;
                    continue;
                }

                set.Add(*parent);
                Smoke.Class *baseClass = smoke->classes + *parent;
                // also generate interfaces for the base classes of the base classes ;)
                AddBaseClassesToHashSet(baseClass, set);
                parent++;
            }
        }
        return set;
    }

    public void Run() {
        MethodsGenerator mg = null;
        foreach (short idx in GetClassList()) {
            Smoke.Class* klass = smoke->classes + idx;
            string className = ByteArrayManager.GetString(klass->className);
            string prefix;
            string name;
            int colon = className.LastIndexOf("::");
            prefix = (colon != -1) ? className.Substring(0, colon) : string.Empty;
            name = (colon != -1) ? className.Substring(colon + 2) : className;

            CodeTypeDeclaration ifaceDecl = new CodeTypeDeclaration('I' + name);
            ifaceDecl.IsInterface = true;
            mg = new MethodsGenerator(smoke, ifaceDecl);

            // TODO: replace this algorithm, it's highly inefficient
            for (short i = 0; i <= smoke->numMethods && smoke->methods[i].classId <= idx; i++) {
                Smoke.Method *meth = smoke->methods + i;
                if (meth->classId != idx)
                    continue;

                // we don't want anything except protected, const or empty flags
                if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_ctor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_copyctor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_dtor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_internal) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0)
                {
                    continue;
                }

                CodeMemberMethod cmm = mg.GenerateBasicMethodDefinition(meth);
                ifaceDecl.Members.Add(cmm);
            }

            classesGenerator.GetTypeCollection(prefix).Add(ifaceDecl);
        }
    }
}
