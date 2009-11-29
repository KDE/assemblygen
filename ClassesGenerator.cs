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

    public readonly Smoke* smoke;
    public readonly CodeCompileUnit unit;
    public readonly CodeNamespace cnDefault;

    // maps a C++ namespace to a .NET namespace
    Dictionary<string, CodeNamespace> namespaceMap = new Dictionary<string, CodeNamespace>();
    // maps a C++ class to a .NET class
    Dictionary<string, CodeTypeDeclaration> typeMap = new Dictionary<string, CodeTypeDeclaration>();

    // needed to filter out superfluous methods from base classes
    SmokeMethodEqualityComparer smokeMethodComparer;

    public ClassesGenerator(Smoke* smoke, CodeCompileUnit unit, string defaultNamespace) {
        this.smoke = smoke;
        this.unit = unit;
        this.cnDefault = new CodeNamespace(defaultNamespace);
        unit.Namespaces.Add(cnDefault);
        namespaceMap[defaultNamespace] = cnDefault;
        smokeMethodComparer = new SmokeMethodEqualityComparer(smoke);
    }

    /*
     * Returns the collection of sub-types for a given prefix (which may be a namespace or a class).
     * If 'prefix' is empty, returns the collection of the default namespace.
     */
    public IList GetTypeCollection(string prefix) {
        if (prefix == null || prefix == string.Empty)
            return cnDefault.Types;
        CodeNamespace nspace;
        CodeTypeDeclaration typeDecl;
        if (namespaceMap.TryGetValue(prefix, out nspace)) {
            return nspace.Types;
        }
        if (typeMap.TryGetValue(prefix, out typeDecl)) {
            return typeDecl.Members;
        }
        
        short id = smoke->idClass(prefix);
        Smoke.Class *klass = smoke->classes + id;
        if (id != 0 && klass->size > 0) {
            throw new Exception("Found class instead of namespace - this should not happen!");
        }
        
        IList parentCollection = unit.Namespaces;
        string name = prefix;
        int colon = name.LastIndexOf("::");
        if (colon != -1) {
            parentCollection = GetTypeCollection(name.Substring(0, colon));
            name = prefix.Substring(colon + 2);
        }

        nspace = new CodeNamespace(name);
        parentCollection.Add(nspace);
        namespaceMap[prefix] = nspace;
        return nspace.Types;
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
        if (smokeClass->size == 0) {
            // namespace
            prefix = smokeName;
            name = "Global";
            mapName = prefix + "::" + "Global";
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
        if (smokeClass->parents == 0) {
            if (smokeName == "QObject") {
                type.BaseTypes.Add(new CodeTypeReference("Qt"));
            } else {
                type.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            }
        } else {
            short *parent = smoke->inheritanceList + smokeClass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                if (firstParent) {
                    type.BaseTypes.Add(new CodeTypeReference(ByteArrayManager.GetString((smoke->classes + *parent)->className).Replace("::", ".")));
                    firstParent = false;
                    parent++;
                    continue;
                }
                // Translator.CppToCSharp() will take care of 'interfacifying' the class name
                type.BaseTypes.Add(Translator.CppToCSharp(smoke->classes + *parent));
                parent++;
            }
        }
        CodeTypeDeclaration iface;
        if (Translator.InterfaceTypeMap.TryGetValue(smokeName, out iface)) {
            type.BaseTypes.Add(new CodeTypeReference('I' + name));
        }

        typeMap[mapName] = type;
        GetTypeCollection(prefix).Add(type);
        return type;
    }

    /*
     * Loops through all wrapped methods. Any class that is found is converted to a .NET class (see DefineClass()).
     * A MethodGenerator is then created to generate the methods for that class.
     */
    public void Run() {
        ClassInterfacesGenerator cig = new ClassInterfacesGenerator(this);
        cig.Run();
        MethodsGenerator methgen = null;
        short currentClassId = 0;
        Smoke.Class *klass = (Smoke.Class*) IntPtr.Zero;
        CodeTypeDeclaration type = null;

        // Contains inherited methods that have to be implemented by the current class.
        // We use our custom comparer, so we don't end up with the same method multiple times.
        IDictionary<short, string> implementMethods = new Dictionary<short, string>(smokeMethodComparer);

        for (short i = 1; i < smoke->numMethodMaps; i++) {
            Smoke.MethodMap *map = smoke->methodMaps + i;

            if (currentClassId != map->classId) {
                // we encountered a new class
                currentClassId = map->classId;
                klass = smoke->classes + currentClassId;
                type = DefineClass(klass);

                methgen = new MethodsGenerator(smoke, type);

                implementMethods.Clear();

                bool firstParent = true;
                for (short *parent = smoke->inheritanceList + klass->parents; *parent > 0; parent++) {
                    if (firstParent) {
                        // we're only interested in parents implemented as interfaces
                        firstParent = false;
                        continue;
                    }
                    // collect all methods (+ inherited ones) and add them to the implementMethods Dictionary
                    smoke->FindAllMethods(*parent, implementMethods, true);
                }

                foreach (KeyValuePair<short, string> pair in implementMethods) {
                    methgen.Generate(pair.Key, pair.Value);
                }
            }

            string mungedName = ByteArrayManager.GetString(smoke->methodNames[map->name]);
            if (map->method > 0) {
                Smoke.Method *meth = smoke->methods + map->method;
                if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
                    continue;   // don't process enums here

                // already implemented?
                if (implementMethods.ContainsKey(map->method))
                    continue;

                methgen.Generate(map->method, mungedName);
            } else if (map->method < 0) {
                for (short *overload = smoke->ambiguousMethodList + (-map->method); *overload > 0; overload++) {
                    Smoke.Method *meth = smoke->methods + *overload;
                    if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
                        continue;   // don't process enums here

                    // already implemented?
                    if (implementMethods.ContainsKey(*overload))
                        continue;

                    methgen.Generate(*overload, mungedName);
                }
            }
        }
    }
}
