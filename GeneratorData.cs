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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

unsafe class GeneratorData {

    public Smoke* Smoke = (Smoke*) IntPtr.Zero;

    public CodeCompileUnit CompileUnit = null;
    public CodeNamespace DefaultNamespace = null;

    public GeneratorData(Smoke* smoke, string defaultNamespace) : this(smoke, defaultNamespace, new CodeCompileUnit()) {}

    public GeneratorData(Smoke* smoke, string defaultNamespace, CodeCompileUnit unit) {
        Smoke = smoke;
        CompileUnit = unit;

        DefaultNamespace = new CodeNamespace(defaultNamespace);
        CompileUnit.Namespaces.Add(DefaultNamespace);
        NamespaceMap[defaultNamespace] = DefaultNamespace;
    }

    // maps a C++ class to a .NET interface (needed for multiple inheritance), populated by ClassInterfacesGenerator
    public readonly Dictionary<string, CodeTypeDeclaration> InterfaceTypeMap = new Dictionary<string, CodeTypeDeclaration>();
    // maps a C++ namespace to a .NET namespace
    public readonly Dictionary<string, CodeNamespace> NamespaceMap = new Dictionary<string, CodeNamespace>();
    // maps a Smoke class to a .NET class
    public readonly Dictionary<IntPtr, CodeTypeDeclaration> SmokeTypeMap = new Dictionary<IntPtr, CodeTypeDeclaration>();
    // maps a binding class name to a .NET class
    public readonly Dictionary<string, CodeTypeDeclaration> CSharpTypeMap = new Dictionary<string, CodeTypeDeclaration>();

    // C++ namespaces that should be mapped to .NET classes
    public static List<string> NamespacesAsClasses = new List<string>()
    {
        "Qt",
        "KDE"
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
        parentCollection.Add(nspace);
        NamespaceMap[prefix] = nspace;
        return nspace.Types;
    }

}
