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

enum MemberType {
    Class,
    Method,
    Field,
    Property,
}

unsafe class GeneratorData {

    public Smoke* Smoke = (Smoke*) IntPtr.Zero;

    public CodeCompileUnit CompileUnit = null;
    public CodeNamespace DefaultNamespace = null;
    public string GlobalSpaceClassName = "Global";
    public List<Assembly> References;

    public GeneratorData(Smoke* smoke, string defaultNamespace, List<Assembly> references) : this(smoke, defaultNamespace, references, new CodeCompileUnit()) {}

    public GeneratorData(Smoke* smoke, string defaultNamespace, List<Assembly> references, CodeCompileUnit unit) {
        Smoke = smoke;
        CompileUnit = unit;

        DefaultNamespace = new CodeNamespace(defaultNamespace);
        DefaultNamespace.Imports.Add(new CodeNamespaceImport("System"));
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
                referencedTypeMap[smokeClassName] = type;
            }
        }

        CompileUnit.Namespaces.Add(DefaultNamespace);
        NamespaceMap[defaultNamespace] = DefaultNamespace;
    }

    public Dictionary<string, Type> referencedTypeMap = new Dictionary<string, Type>();
    Type smokeClassAttribute = null;
    MethodInfo smokeClassGetSignature = null;

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

    // C++ namespaces that should be mapped to .NET classes
    public List<string> NamespacesAsClasses = new List<string>()
    {
        "Qt",
        "KDE"
    };

    // C++ method signatures (without return type) that should be excluded
    public List<Regex> ExcludedMethods = new List<Regex>()
    {
        new Regex(@"^qt_.*\("),
    };

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
        nspace.Imports.Add(new CodeNamespaceImport(DefaultNamespace.Name));
        parentCollection.Add(nspace);
        NamespaceMap[prefix] = nspace;
        return nspace.Types;
    }

    public class InternalMemberInfo {
        public MemberType Type;
        public string Name;

        public InternalMemberInfo(MemberType type, string name) {
            Type = type;
            Name = name;
        }
    }

    Dictionary<IntPtr, List<InternalMemberInfo>> membersCache = new Dictionary<IntPtr, List<InternalMemberInfo>>();
    /*
     * Returns a list of accessible members from class 'klass' and superclasses (just nested classes and properties for now).
     */
    public List<InternalMemberInfo> GetAccessibleMembers(Smoke.Class* klass) {
        List<InternalMemberInfo> members;
        if (membersCache.TryGetValue((IntPtr) klass, out members)) {
            return members;
        }

        members = new List<InternalMemberInfo>();
        for (; klass->className != (char*) IntPtr.Zero;
               klass = Smoke->classes + Smoke->inheritanceList[klass->parents])
        {
            // loop through superclasses (only the first ones - others are only implemented as interfaces)
            try {
                foreach (CodeTypeMember member in SmokeTypeMap[(IntPtr) klass].Members) {
                    if (member is CodeTypeDeclaration) {
                        members.Add(new InternalMemberInfo(MemberType.Class, member.Name));
                    } else if (member is CodeMemberProperty) {
                        members.Add(new InternalMemberInfo(MemberType.Property, member.Name));
                    }
                }
            } catch (KeyNotFoundException) {
                try {
                    Type type = referencedTypeMap[ByteArrayManager.GetString(klass->className)];
                    while (type != null && type.GetCustomAttributes(smokeClassAttribute, false).Length > 0) {
                        foreach (Type nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)) {
                            members.Add(new InternalMemberInfo(MemberType.Class, nested.Name));
                        }

                        foreach (PropertyInfo pi in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic
                                                                       | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                        {
                            members.Add(new InternalMemberInfo(MemberType.Property, pi.Name));
                        }

                        type = type.BaseType;
                    }
                    break;
                } catch (KeyNotFoundException) {
                    // don't use Debug.Print here - this is important!
                    Console.Error.WriteLine("  |--Can't find class: {0}", ByteArrayManager.GetString(klass->className));
                }
            }
        }
        return members;
    }
}
