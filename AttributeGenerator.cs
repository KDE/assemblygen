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
            attributes.Add(name, attr);
        }

        if (isSetMethod) {
            attr.SetMethod = meth;
        } else {
            attr.GetMethod = meth;
        }
    }

    public List<CodeMemberProperty> GenerateBasicAttributeDefinitions() {
        List<CodeMemberProperty> ret = new List<CodeMemberProperty>();
        foreach (KeyValuePair<string, Attribute> pair in attributes) {
            Attribute attr = pair.Value;
            CodeMemberProperty prop = new CodeMemberProperty();
            prop.Name = pair.Key;
            try {
                bool isRef;
                prop.Type = translator.CppToCSharp(data.Smoke->types + attr.GetMethod->ret, out isRef);
            } catch (NotSupportedException) {
                string className = ByteArrayManager.GetString(data.Smoke->classes[attr.GetMethod->classId].className);
                Debug.Print("  |--Won't wrap Attribute {0}::{1}", className, prop.Name);
                continue;
            }
            prop.HasGet = true;
            prop.HasSet = attr.SetMethod != (Smoke.Method*) 0;

            if ((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                prop.Attributes = MemberAttributes.Family | MemberAttributes.New | MemberAttributes.Final;
            } else {
                prop.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;
            }

            if ((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_static) > 0)
                prop.Attributes |= MemberAttributes.Static;

            ret.Add(prop);
        }
        return ret;
    }

    public void Run() {
        foreach (CodeMemberProperty cmp in GenerateBasicAttributeDefinitions()) {
            Attribute attr = attributes[cmp.Name];
            CodeMethodReferenceExpression interceptorReference =
                ((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_static) == 0) ? SmokeSupport.interceptor_Invoke : SmokeSupport.staticInterceptor_Invoke;
            cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type,
                new CodeMethodInvokeExpression(interceptorReference, new CodePrimitiveExpression(ByteArrayManager.GetString(data.Smoke->methodNames[attr.GetMethod->name])),
                    new CodePrimitiveExpression(data.Smoke->GetMethodSignature(attr.GetMethod)), new CodeTypeOfExpression(cmp.Type)
                )
            )));

            if (cmp.HasSet) {
                cmp.SetStatements.Add(new CodeMethodInvokeExpression(interceptorReference,
                        new CodePrimitiveExpression(ByteArrayManager.GetString(data.Smoke->methodNames[data.Smoke->FindMungedName(attr.SetMethod)])),
                        new CodePrimitiveExpression(data.Smoke->GetMethodSignature(attr.SetMethod)), new CodeTypeOfExpression(typeof(void)),
                        new CodeTypeOfExpression(cmp.Type), new CodeArgumentReferenceExpression("value")
                ));
            }

            type.Members.Add(cmp);
        }
    }
}
