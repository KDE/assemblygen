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

// Generates C# enums from enums found in the smoke lib.
unsafe class EnumGenerator {
    readonly GeneratorData data;
    readonly Translator translator;

    public EnumGenerator(GeneratorData data, Translator translator) {
        this.data = data;
        this.translator = translator;
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
        return typeDecl;
    }

    /*
     * convenience overload
     */
    CodeTypeDeclaration DefineEnum(Smoke.Type* type) {
        Smoke.TypeId typeId = (Smoke.TypeId) (type->flags & (uint) Smoke.TypeFlags.tf_elem);
        if (typeId != Smoke.TypeId.t_enum) {
            // not an enum type
            return null;
        }

        string enumName = ByteArrayManager.GetString(type->name);
        return DefineEnum(enumName);
    }

    public void DefineEnums() {
        for (short i = 1; i <= data.Smoke->numTypes; i++) {
            DefineEnum(data.Smoke->types + i);
        }
    }

    /*
     * Generate an Enum member, creating the Enum if necessary.
     */
    public void Generate() {
    }
}
