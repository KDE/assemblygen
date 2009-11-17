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
    void AddBaseClassesToHashSet(Smoke.Class *klass, HashSet<IntPtr> set) {
        short *parent = smoke->inheritanceList + klass->parents;
        while (*parent > 0) {
            Smoke.Class *baseClass = smoke->classes + *parent;
            set.Add((IntPtr) baseClass);
            AddBaseClassesToHashSet(baseClass, set);
            parent++;
        }
    }

    /*
     * Returns a list of classes for which we need to generate interfaces.
     * IntPtr is not type-safe, but we can't have pointers as generic parameters. :(
     */
    HashSet<IntPtr> GetClassList() {
        HashSet<IntPtr> set = new HashSet<IntPtr>();
        for (short i = 1; i <= smoke->numClasses; i++) {
            Smoke.Class *klass = smoke->classes + i;
            short *parent = smoke->inheritanceList + klass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                Smoke.Class *baseClass = smoke->classes + *parent;
                if (firstParent) {
                    // don't generate interfaces for the first base class
                    firstParent = false;
                    parent++;
                    continue;
                }

                set.Add((IntPtr) baseClass);
                // also generate interfaces for the base classes of the base classes ;)
                AddBaseClassesToHashSet(baseClass, set);
                parent++;
            }
        }
        return set;
    }

    public void Run() {
        foreach (IntPtr ptr in GetClassList()) {
            Smoke.Class* klass = (Smoke.Class*) ptr;
            string className = ByteArrayManager.GetString(klass->className);
            Console.WriteLine("Generate interface for: 0x{0:x8} {1}", (int) ptr, className);
        }
    }
}
