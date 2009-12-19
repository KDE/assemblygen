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
using System.CodeDom;
using System.Collections.Generic;

unsafe class ClassInterfacesGenerator {
    ClassesGenerator classesGenerator;
    Smoke *smoke;

    public ClassInterfacesGenerator(ClassesGenerator cg) {
        classesGenerator = cg;
        smoke = cg.smoke;
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
                string methName = ByteArrayManager.GetString(smoke->methodNames[meth->name]);

                // we don't want anything except protected, const or empty flags
                if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_ctor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_copyctor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_dtor) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_internal) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0
                    || (meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0
                    || methName.StartsWith("operator"))
                {
                    continue;
                }

                CodeMemberMethod cmm = mg.GenerateBasicMethodDefinition(meth);
                ifaceDecl.Members.Add(cmm);
            }

            classesGenerator.GetTypeCollection(prefix).Add(ifaceDecl);
            Translator.InterfaceTypeMap[className] = ifaceDecl;
        }
    }
}
