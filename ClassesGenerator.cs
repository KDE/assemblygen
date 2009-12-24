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
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

unsafe class ClassesGenerator {

    GeneratorData data;
    Translator translator;
    EnumGenerator eg;

    // needed to filter out superfluous methods from base classes
    SmokeMethodEqualityComparer smokeMethodComparer;

    public ClassesGenerator(GeneratorData data, Translator translator) {
        this.data = data;
        this.translator = translator;
        smokeMethodComparer = new SmokeMethodEqualityComparer(data.Smoke);
        eg = new EnumGenerator(data);
    }

    /*
     * Create a .NET class from a smoke class.
     * A class Namespace::Foo is mapped to Namespace.Foo. Classes that are not in any namespace go into the default namespace.
     * For namespaces that contain functions, a Namespace.Global class is created which holds the functions as methods.
     */
    CodeTypeDeclaration DefineClass(Smoke.Class* smokeClass) {
        string smokeName = ByteArrayManager.GetString(smokeClass->className);
        string mapName = smokeName;
        string name;
        string prefix = string.Empty;
        if (smokeClass->size == 0 && !data.NamespacesAsClasses.Contains(smokeName)) {
            if (smokeName == "QGlobalSpace") {  // global space
                name = data.GlobalSpaceClassName;
            } else {
                // namespace
                prefix = smokeName;
                name = "Global";
                mapName = prefix + "::Global";
            }
        } else {
            int colon = smokeName.LastIndexOf("::");
            prefix = (colon != -1) ? smokeName.Substring(0, colon) : string.Empty;
            name = (colon != -1) ? smokeName.Substring(colon + 2) : smokeName;
        }

        // define the .NET class
        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeClass",
            new CodeAttributeArgument(new CodePrimitiveExpression(smokeName)));
        CodeTypeDeclaration type = new CodeTypeDeclaration(name);
        type.CustomAttributes.Add(attr);
        type.IsPartial = true;

        if (smokeClass->parents == 0) {
            if (smokeName == "QObject") {
                type.BaseTypes.Add(new CodeTypeReference("Qt"));
            } else {
                type.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            }
        } else {
            short *parent = data.Smoke->inheritanceList + smokeClass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                if (firstParent) {
                    type.BaseTypes.Add(new CodeTypeReference(ByteArrayManager.GetString((data.Smoke->classes + *parent)->className).Replace("::", ".")));
                    firstParent = false;
                    parent++;
                    continue;
                }
                // Translator.CppToCSharp() will take care of 'interfacifying' the class name
                type.BaseTypes.Add(translator.CppToCSharp(data.Smoke->classes + *parent));
                parent++;
            }
        }
        CodeTypeDeclaration iface;
        if (data.InterfaceTypeMap.TryGetValue(smokeName, out iface)) {
            type.BaseTypes.Add(new CodeTypeReference('I' + name));
        }

        data.CSharpTypeMap[mapName] = type;
        data.SmokeTypeMap[(IntPtr) smokeClass] = type;
        data.GetTypeCollection(prefix).Add(type);
        return type;
    }

    /*
     * Loops through all wrapped methods. Any class that is found is converted to a .NET class (see DefineClass()).
     * A MethodGenerator is then created to generate the methods for that class.
     */
    public void Run() {
        // create interfaces if necessary
        ClassInterfacesGenerator cig = new ClassInterfacesGenerator(data, translator);
        cig.Run();

        for (short i = 1; i <= data.Smoke->numClasses; i++) {
            Smoke.Class* klass = data.Smoke->classes + i;
            if (klass->external)
                continue;

            DefineClass(klass);
        }

        eg.DefineEnums();

        GenerateMethods();
    }

    /*
     * Adds the methods to the classes created by Run()
     */
    void GenerateMethods() {
        short currentClassId = 0;
        Smoke.Class *klass = (Smoke.Class*) IntPtr.Zero;
        MethodsGenerator methgen = null;
        CodeTypeDeclaration type = null;

        // Contains inherited methods that have to be implemented by the current class.
        // We use our custom comparer, so we don't end up with the same method multiple times.
        IDictionary<short, string> implementMethods = new Dictionary<short, string>(smokeMethodComparer);

        for (short i = 1; i < data.Smoke->numMethodMaps; i++) {
            Smoke.MethodMap *map = data.Smoke->methodMaps + i;

            if (currentClassId != map->classId) {
                // we encountered a new class
                currentClassId = map->classId;
                klass = data.Smoke->classes + currentClassId;
                type = data.SmokeTypeMap[(IntPtr) klass];

                methgen = new MethodsGenerator(data, translator, type);

                implementMethods.Clear();

                bool firstParent = true;
                for (short *parent = data.Smoke->inheritanceList + klass->parents; *parent > 0; parent++) {
                    if (firstParent) {
                        // we're only interested in parents implemented as interfaces
                        firstParent = false;
                        continue;
                    }
                    // collect all methods (+ inherited ones) and add them to the implementMethods Dictionary
                    data.Smoke->FindAllMethods(*parent, implementMethods, true);
                }

                foreach (KeyValuePair<short, string> pair in implementMethods) {
                    Smoke.Method *meth = data.Smoke->methods + pair.Key;
                    if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_ctor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_copyctor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_dtor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_internal) > 0)
                    {
                        continue;
                    }

                    methgen.Generate(meth, pair.Value);
                }
            }

            string mungedName = ByteArrayManager.GetString(data.Smoke->methodNames[map->name]);
            if (map->method > 0) {
                Smoke.Method *meth = data.Smoke->methods + map->method;
                if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0) {
                    eg.DefineMember(meth);
                    continue;
                }

                // already implemented?
                if (implementMethods.ContainsKey(map->method))
                    continue;

                methgen.Generate(map->method, mungedName);
            } else if (map->method < 0) {
                for (short *overload = data.Smoke->ambiguousMethodList + (-map->method); *overload > 0; overload++) {
                    Smoke.Method *meth = data.Smoke->methods + *overload;
                    if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0) {
                        eg.DefineMember(meth);
                        continue;
                    }

                    // if the methods only differ by const, we will generate special code
                    bool nextDiffersByConst = false;
                    if (*(overload + 1) > 0) {
                        if (SmokeMethodEqualityComparer.EqualExceptConstness(meth, data.Smoke->methods + *(overload + 1)))
                            nextDiffersByConst = true;
                    }

                    // already implemented?
                    if (implementMethods.ContainsKey(*overload))
                        continue;

                    methgen.Generate(*overload, mungedName);
                    if (nextDiffersByConst)
                        overload++;
                }
            }
        }
    }
}
