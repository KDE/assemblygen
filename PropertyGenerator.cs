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
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.Linq;

unsafe class PropertyGenerator {

    class Property {
        public string Name;
        public string Type;
        public bool IsWritable;
        public bool IsEnum;

        public Property(string name, string type, bool writable, bool isEnum) {
            Name = name;
            Type = type;
            IsWritable = writable;
            IsEnum = isEnum;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    delegate void AddProperty(string name, string type, [MarshalAs(UnmanagedType.U1)] bool writable, [MarshalAs(UnmanagedType.U1)] bool isEnum);

    [DllImport("smokeloader", CallingConvention=CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    static extern bool GetProperties(Smoke* smoke, short classId, AddProperty addProp);

    GeneratorData data;
    Translator translator;

    public PropertyGenerator(GeneratorData data, Translator translator) {
        this.data = data;
        this.translator = translator;
    }

    public void Run() {
        for (short classId = 1; classId <= data.Smoke->numClasses; classId++) {
            Smoke.Class* klass = data.Smoke->classes + classId;
            if (klass->external)
                continue;

            List<Property> props = new List<Property>();
            if (!GetProperties(data.Smoke, classId, (name, typeString, writable, isEnum) => props.Add(new Property(name, typeString, writable, isEnum)))) {
                continue;
            }

            CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];
            string className = ByteArrayManager.GetString(klass->className);

            foreach (Property prop in props) {
                CodeMemberProperty cmp = new CodeMemberProperty();

                try {
                    bool isRef;
                    cmp.Type = translator.CppToCSharp(prop.Type, out isRef);
                } catch (NotSupportedException) {
                    Debug.Print("  |--Won't wrap Property {0}::{1}", className, prop.Name);
                    continue;
                }

                cmp.Name = prop.Name;
                // capitalize the first letter
                StringBuilder builder = new StringBuilder(cmp.Name);
                builder[0] = char.ToUpper(builder[0]);
                string tmp = builder.ToString();

                // If the new name clashes with a name of a type declaration, keep the lower-case name.
                var typesWithSameName = from typeDecl in data.GetAccessibleNestedMembers(data.Smoke->classes + classId)
                                        where typeDecl is CodeTypeDeclaration
                                        where typeDecl.Name == tmp
                                        select typeDecl;
                if (typesWithSameName.Count() > 0) {
                    Debug.Print("  |--Conflicting names: property/type: {0} in class {1} - keeping original property name", tmp, className);
                } else {
                    cmp.Name = tmp;
                }

                cmp.HasGet = true;
                cmp.HasSet = prop.IsWritable;

                type.Members.Add(cmp);
            }
        }
    }
}
