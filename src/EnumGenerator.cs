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
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

// Generates C# enums from enums found in the smoke lib.
public unsafe class EnumGenerator {

    [DllImport("smokeloader", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    static extern unsafe long GetEnumValue(Smoke* smoke, Smoke.Method* meth);

    readonly GeneratorData data;

    public EnumGenerator(GeneratorData data) {
        this.data = data;
    }

    /*
     * Defines an Enum.
     */
    CodeTypeDeclaration DefineEnum(string cppName) {
        int colon = cppName.LastIndexOf("::");
        string prefix = string.Empty;
        if (colon != -1) {
            prefix = cppName.Substring(0,  colon);
        }

        string name = cppName;
        if (colon != -1) {
            name = cppName.Substring(colon + 2);
        }

        CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(name);
        typeDecl.IsEnum = true;
        data.GetTypeCollection(prefix).Add(typeDecl);
        data.EnumTypeMap[cppName] = typeDecl;
        return typeDecl;
    }

    /*
     * convenience overload
     */
    CodeTypeDeclaration DefineEnum(Smoke.Type* type) {
        // we want the exact combination: t_enum | tf_stack
        if (type->flags != ((uint) Smoke.TypeId.t_enum | (uint) Smoke.TypeFlags.tf_stack)) {
            // not an enum type
            return null;
        }

        if (type->classId == 0 || data.Smoke->classes[type->classId].external) {
            // defined elsewhere
            return null;
        }

        string enumName = ByteArrayManager.GetString(type->name);

        return DefineEnum(enumName);
    }

    /*
     * Loops through the 'types' table and defines .NET Enums for t_enums
     */
    public void DefineEnums() {
        for (short i = 1; i <= data.Smoke->numTypes; i++) {
            DefineEnum(data.Smoke->types + i);
        }
    }

    /*
     * Generates an Enum member, creating the Enum if necessary.
     */
    public void DefineMember(Smoke.Method* meth) {
        if ((meth->flags & (uint) Smoke.MethodFlags.mf_enum) == 0)
            return;

        string typeName = ByteArrayManager.GetString(data.Smoke->types[meth->ret].name);
        if (typeName == "long") // unnamed enum
            return;

        CodeTypeDeclaration enumType;
        if (!data.EnumTypeMap.TryGetValue(typeName, out enumType)) {
            enumType = DefineEnum(typeName);
        }
        CodeMemberField member = new CodeMemberField();
        member.Name = ByteArrayManager.GetString(data.Smoke->methodNames[meth->name]);
        long value = GetEnumValue(data.Smoke, meth);

        if (value > int.MaxValue && enumType.BaseTypes.Count == 0) {
            // make the enum derive from 'long' if necessary
            enumType.BaseTypes.Add(new CodeTypeReference(typeof(long)));
        }

        member.InitExpression = new CodePrimitiveExpression(value);
        enumType.Members.Add(member);
    }
}
