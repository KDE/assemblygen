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
using System.Reflection;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

public unsafe class GeneratorData {

    public Smoke* Smoke = (Smoke*) IntPtr.Zero;

    public CodeCompileUnit CompileUnit = null;
    public CodeNamespace DefaultNamespace = null;
    public string GlobalSpaceClassName = "Global";
    public List<Assembly> References;
    public List<string> Imports;

    public GeneratorData(Smoke* smoke, string defaultNamespace, List<Assembly> references)
        : this(smoke, defaultNamespace, new List<string>(), references, new CodeCompileUnit()) {}

    public GeneratorData(Smoke* smoke, string defaultNamespace, List<string> imports, List<Assembly> references)
        : this(smoke, defaultNamespace, imports, references, new CodeCompileUnit()) {}

    public GeneratorData(Smoke* smoke, string defaultNamespace, List<string> imports, List<Assembly> references, CodeCompileUnit unit) {
        Smoke = smoke;
        CompileUnit = unit;
        Imports = imports;

        DefaultNamespace = new CodeNamespace(defaultNamespace);
        DefaultNamespace.Imports.Add(new CodeNamespaceImport("System"));
        DefaultNamespace.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
        foreach (string import in imports) {
            DefaultNamespace.Imports.Add(new CodeNamespaceImport(import));
        }

        References = references;
        foreach (Assembly assembly in References) {
            smokeClassAttribute = assembly.GetType("Qyoto.SmokeClass", false);
            if (smokeClassAttribute != null) {
                smokeClassGetSignature = smokeClassAttribute.GetProperty("Signature").GetGetMethod();
                break;
            }
        }
        foreach (Assembly assembly in References) {
            foreach (Type type in assembly.GetTypes()) {
                object[] attributes = type.GetCustomAttributes(smokeClassAttribute, false);
                if (attributes.Length == 0)
                    continue;
                string smokeClassName = (string) smokeClassGetSignature.Invoke(attributes[0], null);
                Type t;
                if (ReferencedTypeMap.TryGetValue(smokeClassName, out t) && t.IsInterface) {
                    continue;
                }
                ReferencedTypeMap[smokeClassName] = type;
            }
        }

        CompileUnit.Namespaces.Add(DefaultNamespace);
        NamespaceMap[defaultNamespace] = DefaultNamespace;
    }

    Type smokeClassAttribute = null;
    MethodInfo smokeClassGetSignature = null;

    public Dictionary<string, Type> ReferencedTypeMap = new Dictionary<string, Type>();

    // maps a C++ class to a .NET interface (needed for multiple inheritance), populated by ClassInterfacesGenerator
    public readonly Dictionary<string, CodeTypeDeclaration> InterfaceTypeMap = new Dictionary<string, CodeTypeDeclaration>();
    // maps a C++ namespace to a .NET namespace
    public readonly Dictionary<string, CodeNamespace> NamespaceMap = new Dictionary<string, CodeNamespace>();
    // maps a Smoke class to a .NET class
    public readonly Dictionary<IntPtr, CodeTypeDeclaration> SmokeTypeMap = new Dictionary<IntPtr, CodeTypeDeclaration>();
    // maps a binding class name to a .NET class
    public readonly Dictionary<string, CodeTypeDeclaration> CSharpTypeMap = new Dictionary<string, CodeTypeDeclaration>();
    // maps a smoke enum type to a .NET enum
    public readonly Dictionary<string, CodeTypeDeclaration> EnumTypeMap = new Dictionary<string, CodeTypeDeclaration>();

    public bool Debug = false;

    /*
     * Returns the collection of sub-types for a given prefix (which may be a namespace or a class).
     * If 'prefix' is empty, returns the collection of the default namespace.
     */
    public IList GetTypeCollection(string prefix) {
        if (prefix == null || prefix == string.Empty)
            return DefaultNamespace.Types;
        CodeNamespace nspace;
        CodeTypeDeclaration typeDecl;

        // Did we already define the class or namespace?
        if (NamespaceMap.TryGetValue(prefix, out nspace)) {
            return nspace.Types;
        }
        if (CSharpTypeMap.TryGetValue(prefix, out typeDecl)) {
            return typeDecl.Members;
        }

        // Make sure that we don't define a namespace where a class should actually be.
        // This shouldn't happen, but to be sure we check it again.
        short id = Smoke->idClass(prefix);
        Smoke.Class *klass = Smoke->classes + id;
        if (id != 0 && klass->size > 0) {
            throw new Exception("Found class instead of namespace - this should not happen!");
        }

        // Get the collection of the parent namespace
        IList parentCollection = CompileUnit.Namespaces;
        string name = prefix;
        int colon = name.LastIndexOf("::");
        if (colon != -1) {
            parentCollection = GetTypeCollection(name.Substring(0, colon));
            name = prefix.Substring(colon + 2);
        }

        // Define a new namespace.
        nspace = new CodeNamespace(name);
        nspace.Imports.Add(new CodeNamespaceImport("System"));
        nspace.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
        nspace.Imports.Add(new CodeNamespaceImport(DefaultNamespace.Name));
        foreach (string import in Imports) {
            nspace.Imports.Add(new CodeNamespaceImport(import));
        }

        parentCollection.Add(nspace);
        NamespaceMap[prefix] = nspace;
        return nspace.Types;
    }

    public class InternalMemberInfo {
        public MemberTypes Type;
        public string Name;

        public InternalMemberInfo(MemberTypes type, string name) {
            Type = type;
            Name = name;
        }
    }

    /*
     * Returns a list of accessible members from class 'klass' and superclasses (just nested classes and properties for now).
     */
    public List<InternalMemberInfo> GetAccessibleMembers(Smoke.Class* klass) {
        List<InternalMemberInfo> members = new List<InternalMemberInfo>();
        GetAccessibleMembers(klass, members);
        return members;
    }

    void GetAccessibleMembers(Smoke.Class* klass, List<InternalMemberInfo> list) {
        if (Debug) {
            Console.Error.WriteLine("members from class {0}", ByteArrayManager.GetString(klass->className));
        }
        if (klass->external) {
            AddReferencedMembers(klass, list);
            return;
        }

        CodeTypeDeclaration typeDecl = null;
        if (!SmokeTypeMap.TryGetValue((IntPtr) klass, out typeDecl)) {
            AddReferencedMembers(klass, list);
            return;
        } else {
            foreach (CodeTypeMember member in typeDecl.Members) {
                if (member is CodeMemberProperty) {
                    if (Debug) {
                        Console.Error.WriteLine("Adding property {0}", member.Name);
                    }
                    list.Add(new InternalMemberInfo(MemberTypes.Property, member.Name));
                } else if (member is CodeMemberMethod) {
                    list.Add(new InternalMemberInfo(MemberTypes.Method, member.Name));
                } else if (member is CodeMemberField) {
                    list.Add(new InternalMemberInfo(MemberTypes.Field, member.Name));
                } else if (member is CodeTypeDeclaration) {
                    list.Add(new InternalMemberInfo(MemberTypes.NestedType, member.Name));
                }
            }
        }

        for (short *parent = Smoke->inheritanceList + klass->parents; *parent > 0; parent++) {
            Smoke.Class *parentClass = Smoke->classes + *parent;
            GetAccessibleMembers(parentClass, list);
        }
    }

    void AddReferencedMembers(Smoke.Class *klass, List<InternalMemberInfo> list) {
//         Console.WriteLine("Add referenced members for class {0}", ByteArrayManager.GetString(klass->className));
    }
}