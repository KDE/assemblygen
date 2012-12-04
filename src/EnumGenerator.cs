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
using System.Runtime.InteropServices;
using System.CodeDom;

public unsafe delegate void EnumMemberHook(Smoke* smoke, Smoke.Method* smokeMethod, CodeMemberField cmm, CodeTypeDeclaration typeDecl);

// Generates C# enums from enums found in the smoke lib.
public unsafe class EnumGenerator
{
	[DllImport("assemblygen-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
	private static extern long GetEnumValue(Smoke* smoke, Smoke.Method* meth);

	public static event EnumMemberHook PostEnumMemberHook;

	private readonly GeneratorData data;

	public EnumGenerator(GeneratorData data)
	{
		this.data = data;
	}

	/*
	 * Defines an Enum.
	 */

	private CodeTypeDeclaration DefineEnum(string cppName)
	{
		int colon = cppName.LastIndexOf("::", StringComparison.Ordinal);
		string prefix = string.Empty;
		if (colon != -1)
		{
			prefix = cppName.Substring(0, colon);
		}

		string name = cppName;
		if (colon != -1)
		{
			name = cppName.Substring(colon + 2);
		}
		// HACK: qprinter.h - typedef PageSize PaperSize but PageSize is obsolete; typedefs don't help here: this is the only case where the type def and not the real type, should be used
		if (cppName == "QPrinter::PageSize")
		{
			name = "PaperSize";
		}
		CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(name);
		typeDecl.IsEnum = true;
		if (!data.ReferencedTypeMap.ContainsKey(name) ||
		    data.ReferencedTypeMap[name].FullName != data.DefaultNamespace.Name + "." + cppName.Replace("::", "+"))
		{
			data.GetTypeCollection(prefix).Add(typeDecl);
			typeDecl.UserData.Add("parent", prefix);
			data.EnumTypeMap[cppName] = typeDecl;
		}
		return typeDecl;
	}

	/*
	 * convenience overload
	 */

	private void DefineEnum(Smoke.Type* type)
	{
		// we want the exact combination: t_enum | tf_stack
		if (type->flags != ((uint) Smoke.TypeId.t_enum | (uint) Smoke.TypeFlags.tf_stack))
		{
			// not an enum type
			return;
		}

		if (type->classId == 0 || data.Smoke->classes[type->classId].external)
		{
			// defined elsewhere
			return;
		}

		string enumName = ByteArrayManager.GetString(type->name);

		this.DefineEnum(enumName);
	}

	/*
	 * Loops through the 'types' table and defines .NET Enums for t_enums
	 */

	public void DefineEnums()
	{
		for (short i = 1; i <= data.Smoke->numTypes; i++)
		{
			DefineEnum(data.Smoke->types + i);
		}
	}

	/*
	 * Generates an Enum member, creating the Enum if necessary.
	 */

	public void DefineMember(Smoke.Method* meth)
	{
		if ((meth->flags & (uint) Smoke.MethodFlags.mf_enum) == 0)
			return;

		string typeName = ByteArrayManager.GetString(data.Smoke->types[meth->ret].name);
		if (typeName == "long") // unnamed enum
			return;

		CodeTypeDeclaration enumType;
		if (!data.EnumTypeMap.TryGetValue(typeName, out enumType))
		{
			enumType = DefineEnum(typeName);
		}
		CodeMemberField member = new CodeMemberField();
		member.Name = ByteArrayManager.GetString(data.Smoke->methodNames[meth->name]);
		long value = GetEnumValue(data.Smoke, meth);

		if (value > int.MaxValue && enumType.BaseTypes.Count == 0)
		{
			// make the enum derive from 'long' if necessary
			enumType.BaseTypes.Add(new CodeTypeReference(typeof(long)));
		}

		member.InitExpression = new CodePrimitiveExpression(value);
		if (PostEnumMemberHook != null)
		{
			PostEnumMemberHook(data.Smoke, meth, member, enumType);
		}
		enumType.Members.Add(member);
	}
}
