/*
    Generator for .NET assemblies utilizing SMOKE libraries
    Copyright (C) 2009, 2010 Arno Rehn <arno@arnorehn.de>

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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;

public unsafe class GeneratorData
{
	public string Destination { get; set; }
	public string Docs { get; set; }

	public Smoke* Smoke = (Smoke*) IntPtr.Zero;

	public CodeCompileUnit CompileUnit;
	public CodeNamespace DefaultNamespace;
	public string GlobalSpaceClassName = "Global";
	public List<Assembly> References;
	public List<string> Imports;
	public IDictionary<string, string[]> ArgumentNames = new Dictionary<string, string[]>();
	public Dictionary<string, string> TypeDefs = new Dictionary<string, string>();
	public Dictionary<string, IList<string>> TypeDefsPerType = new Dictionary<string, IList<string>>();

	private readonly Type smokeClassAttribute;
	private readonly MethodInfo smokeClassGetSignature;

	public Dictionary<string, Type> ReferencedTypeMap = new Dictionary<string, Type>();

	// maps a C++ class to a .NET interface (needed for multiple inheritance), populated by ClassInterfacesGenerator
	public readonly Dictionary<string, CodeTypeDeclaration> InterfaceTypeMap =
		new Dictionary<string, CodeTypeDeclaration>();

	// maps a C++ namespace to a .NET namespace
	public readonly Dictionary<string, CodeNamespace> NamespaceMap = new Dictionary<string, CodeNamespace>();
	// maps a Smoke class to a .NET class
	public readonly Dictionary<IntPtr, CodeTypeDeclaration> SmokeTypeMap = new Dictionary<IntPtr, CodeTypeDeclaration>();
	// maps a binding class name to a .NET class
	public readonly Dictionary<string, CodeTypeDeclaration> CSharpTypeMap = new Dictionary<string, CodeTypeDeclaration>();
	// maps a smoke enum type to a .NET enum
	public readonly Dictionary<string, CodeTypeDeclaration> EnumTypeMap = new Dictionary<string, CodeTypeDeclaration>();
	// maps public abstract classes to their internal implemented types
	public readonly Dictionary<CodeTypeDeclaration, CodeTypeDeclaration> InternalTypeMap =
		new Dictionary<CodeTypeDeclaration, CodeTypeDeclaration>();

	public bool Debug;

	public GeneratorData(Smoke* smoke, string defaultNamespace, List<string> imports, List<Assembly> references,
	                     string destination, string docs)
		: this(smoke, defaultNamespace, imports, references, new CodeCompileUnit(), destination, docs)
	{
	}

	public GeneratorData(Smoke* smoke, string defaultNamespace, List<string> imports, List<Assembly> references,
	                     CodeCompileUnit unit, string destination, string docs)
	{
		Destination = destination;
		Docs = docs;
		Smoke = smoke;
		this.GetArgNames();
		this.GetTypeDefs();
		CompileUnit = unit;
		Imports = imports;

		References = references;
		DefaultNamespace = new CodeNamespace(defaultNamespace);
		this.AddUsings(DefaultNamespace);

		foreach (Assembly assembly in References)
		{
			smokeClassAttribute = assembly.GetType("QtCore.SmokeClass", false);
			if (smokeClassAttribute != null)
			{
				smokeClassGetSignature = smokeClassAttribute.GetProperty("Signature").GetGetMethod();
				break;
			}
		}
		foreach (Assembly assembly in References)
		{
			foreach (Type type in assembly.GetTypes())
			{
				object[] attributes = type.GetCustomAttributes(smokeClassAttribute, false);
				if (attributes.Length != 0)
				{
					string smokeClassName = (string) smokeClassGetSignature.Invoke(attributes[0], null);
					Type t;
					if (ReferencedTypeMap.TryGetValue(smokeClassName, out t) && t.IsInterface)
					{
						continue;
					}
					ReferencedTypeMap[smokeClassName] = type;
				}
				else
				{
					ReferencedTypeMap[type.Name] = type;
				}
			}
		}

		CompileUnit.Namespaces.Add(DefaultNamespace);
		NamespaceMap[defaultNamespace] = DefaultNamespace;
	}

	private void GetTypeDefs()
	{
		string typeDefsFile = this.GetTypeDefsFile(this.Smoke);
		if (File.Exists(typeDefsFile))
		{
			foreach (string[] strings in File.ReadAllLines(typeDefsFile).Select(line => line.Split(';')))
			{
				string key = strings[0];
				string value = strings[1];
				this.TypeDefs[key] = value;
				IList<string> typedefs;
				if (this.TypeDefsPerType.ContainsKey(value))
				{
					typedefs = this.TypeDefsPerType[value];
				}
				else
				{
					this.TypeDefsPerType[value] = typedefs = new List<string>();
				}
				typedefs.Add(key);
			}
		}
	}

	private void GetArgNames()
	{
		string argNamesFile = this.GetArgNamesFile(this.Smoke);
		if (File.Exists(argNamesFile))
		{
			foreach (string[] strings in File.ReadAllLines(argNamesFile).Select(line => line.Split(';')))
			{
				this.ArgumentNames[strings[0]] = strings[1].Split(',');
			}
		}
	}

	/*
	 * Returns the collection of sub-types for a given prefix (which may be a namespace or a class).
	 * If 'prefix' is empty, returns the collection of the default namespace.
	 */

	public IList GetTypeCollection(string prefix)
	{
		if (string.IsNullOrEmpty(prefix))
			return DefaultNamespace.Types;
		CodeNamespace nspace;
		CodeTypeDeclaration typeDecl;

		// Did we already define the class or namespace?
		if (NamespaceMap.TryGetValue(prefix, out nspace))
		{
			return nspace.Types;
		}
		if (CSharpTypeMap.TryGetValue(prefix, out typeDecl))
		{
			return typeDecl.Members;
		}

		// Make sure that we don't define a namespace where a class should actually be.
		// This shouldn't happen, but to be sure we check it again.
		short id = Smoke->IDClass(prefix);
		Smoke.Class* klass = Smoke->classes + id;
		if (id != 0 && klass->size > 0)
		{
			throw new Exception("Found class instead of namespace - this should not happen!");
		}

		// Define a new namespace.
		nspace = new CodeNamespace(prefix.Replace("::", "."));
		this.AddUsings(nspace);

		CompileUnit.Namespaces.Add(nspace);
		NamespaceMap[prefix] = nspace;
		return nspace.Types;
	}

	private void AddUsings(CodeNamespace nspace)
	{
		nspace.Imports.Add(new CodeNamespaceImport("System"));
		nspace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
		nspace.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
		nspace.Imports.Add(new CodeNamespaceImport(this.DefaultNamespace.Name));
		foreach (string @namespace in from Type type in this.References.SelectMany(r => r.GetTypes())
		                              where type.Namespace != this.DefaultNamespace.Name && !string.IsNullOrEmpty(type.Namespace)
		                              select type.Namespace)
		{
			nspace.Imports.Add(new CodeNamespaceImport(@namespace));
		}
		foreach (string import in this.Imports)
		{
			nspace.Imports.Add(new CodeNamespaceImport(import));
		}
	}

	public class InternalMemberInfo
	{
		public MemberTypes Type;
		public string Name;

		public InternalMemberInfo(MemberTypes type, string name)
		{
			Type = type;
			Name = name;
		}
	}

	/*
	 * Returns a list of accessible members from class 'klass' and superclasses (just nested classes and properties for now).
	 */

	public List<InternalMemberInfo> GetAccessibleMembers(Smoke.Class* klass)
	{
		List<InternalMemberInfo> members = new List<InternalMemberInfo>();
		GetAccessibleMembers(klass, members);
		return members;
	}

	private void GetAccessibleMembers(Smoke.Class* klass, List<InternalMemberInfo> list)
	{
		if (Debug)
		{
			Console.Error.WriteLine("members from class {0}", ByteArrayManager.GetString(klass->className));
		}
		if (klass->external)
		{
			AddReferencedMembers(klass, list);
			return;
		}

		CodeTypeDeclaration typeDecl;
		if (!SmokeTypeMap.TryGetValue((IntPtr) klass, out typeDecl))
		{
			AddReferencedMembers(klass, list);
			return;
		}
		foreach (CodeTypeMember member in typeDecl.Members)
		{
			if (member is CodeMemberProperty)
			{
				list.Add(new InternalMemberInfo(MemberTypes.Property, member.Name));
			}
			else if (member is CodeMemberMethod)
			{
				list.Add(new InternalMemberInfo(MemberTypes.Method, member.Name));
			}
			else if (member is CodeMemberField)
			{
				list.Add(new InternalMemberInfo(MemberTypes.Field, member.Name));
			}
			else if (member is CodeTypeDeclaration)
			{
				list.Add(new InternalMemberInfo(MemberTypes.NestedType, member.Name));
			}
		}

		for (short* parent = Smoke->inheritanceList + klass->parents; *parent > 0; parent++)
		{
			Smoke.Class* parentClass = Smoke->classes + *parent;
			GetAccessibleMembers(parentClass, list);
		}
	}

	private void AddReferencedMembers(Smoke.Class* klass, ICollection<InternalMemberInfo> list)
	{
		string smokeClassName = ByteArrayManager.GetString(klass->className);
		Type type;

		if (!ReferencedTypeMap.TryGetValue(smokeClassName, out type))
		{
			Console.Error.WriteLine("Couldn't find referenced class {0}", smokeClassName);
			return;
		}

		foreach (
			MemberInfo member in
				type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
		{
			list.Add(new InternalMemberInfo(member.MemberType, member.Name));
		}
	}

	public string GetArgNamesFile(Smoke* smoke)
	{
		return this.GetFile(smoke, "argnames");
	}

	private string GetTypeDefsFile(Smoke* smoke)
	{
		return this.GetFile(smoke, "typedefs");
	}

	public string GetFile(Smoke* smoke, string name)
	{
		string dest = this.Destination;
		if (string.IsNullOrEmpty(dest))
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
			{
				dest = Path.GetDirectoryName(Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.System)));
			}
			else
			{
				dest = Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture);
			}
		}
		dest = Path.Combine(dest, "share");
		dest = Path.Combine(dest, "smoke");
		return Path.Combine(dest, string.Format("{0}.{1}.txt", ByteArrayManager.GetString(smoke->module_name), name));
	}
}
