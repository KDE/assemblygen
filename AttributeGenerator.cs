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
using System.CodeDom;

unsafe class AttributeGenerator {

    class Attribute {
        public Smoke.Method *GetMethod = (Smoke.Method*) 0;
        public Smoke.Method *SetMethod = (Smoke.Method*) 0;
    }

    Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>();

    GeneratorData data;
    Translator translator;
    CodeTypeDeclaration type;

    public AttributeGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type) {
        this.data = data;
        this.translator = translator;
        this.type = type;
    }

    public void ScheduleAttributeAccessor(Smoke.Method* meth) {
        string name = ByteArrayManager.GetString(data.Smoke->methodNames[meth->name]);
        bool isSetMethod = false;

        if (name.StartsWith("set")) {
            name = name.Remove(0, 3);
            isSetMethod = true;
        } else {
            // capitalize the first letter
            StringBuilder builder = new StringBuilder(name);
            builder[0] = char.ToUpper(builder[0]);
            name = builder.ToString();
        }

        Attribute attr;
        if (!attributes.TryGetValue(name, out attr)) {
            attr = new Attribute();
        }

        if (isSetMethod) {
            attr.SetMethod = meth;
        } else {
            attr.GetMethod = meth;
        }
    }

    public void Run() {
    }
}
