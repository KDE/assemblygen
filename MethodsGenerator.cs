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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;

unsafe class MethodsGenerator {
    GeneratorData data;
    Translator translator;
    CodeTypeDeclaration type;

    public MethodsGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type) {
        this.data = data;
        this.translator = translator;
        this.type = type;
    }

    bool MethodOverrides(Smoke.Method* method) {
        Dictionary<short, string> allMethods = data.Smoke->FindAllMethods(method->classId, true);
        // Do this with linq... there's probably room for optimization here.
        // Select virtual and pure virtual methods from superclasses.
        var inheritedVirtuals = from entry in allMethods
                                where ((data.Smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
                                    || (data.Smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
                                where data.Smoke->methods[entry.Key].classId != method->classId
                                select entry.Key;

        foreach (short index in inheritedVirtuals) {
            Smoke.Method* meth = data.Smoke->methods + index;
            if (meth->name == method->name && meth->args == method->args &&
                (meth->flags & (uint) Smoke.MethodFlags.mf_const) == (method->flags & (uint) Smoke.MethodFlags.mf_const))
            {
                return true;
            }
        }
        return false;
    }

    public void Generate(short index, string mungedName) {
        Smoke.Method *method = data.Smoke->methods + index;
        Generate(method, mungedName);
    }

    public void Generate(Smoke.Method *method, string mungedName) {
        if ((method->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0 || (method->flags & (ushort) Smoke.MethodFlags.mf_property) > 0) {
            GenerateProperty(method);
        } else {
            GenerateMethod(method, mungedName);
        }
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        return GenerateBasicMethodDefinition(method, cppSignature);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, string cppSignature) {
        // translate arguments
        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        for (short* typeIndex = data.Smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(translator.CppToCSharp(data.Smoke->types + *typeIndex, out isRef), "arg" + count++);
                if (isRef) {
                    exp.Direction = FieldDirection.Ref;
                }
                args.Add(exp);
            } catch (NotSupportedException) {
                Console.WriteLine("  |--Won't wrap method {0}::{1}",
                    ByteArrayManager.GetString(data.Smoke->classes[method->classId].className), cppSignature);
                return null;
            }
        }

        // translate return type
        CodeTypeReference returnType = null;
        try {
            returnType = translator.CppToCSharp(data.Smoke->types + method->ret, out isRef);
        } catch (NotSupportedException) {
            Console.WriteLine("  |--Won't wrap method {0}::{1}",
                ByteArrayManager.GetString(data.Smoke->classes[method->classId].className), cppSignature);
            return null;
        }

        CodeMemberMethod cmm;
        if ((method->flags & (uint) Smoke.MethodFlags.mf_ctor) > 0) {
            cmm = new CodeConstructor();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=
        } else if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) > 0) {
            // a destructor is actually 'protected override void Finalize() { try { ... } finally { base.Finalize(); } }'
            cmm = new CodeMemberMethod();
            cmm.Name = "Finalize";
            cmm.ReturnType = new CodeTypeReference(typeof(void));
            cmm.Attributes = MemberAttributes.Override | MemberAttributes.Family;
        } else {
            cmm = new CodeMemberMethod();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=

            string csName = ByteArrayManager.GetString(data.Smoke->methodNames[method->name]);
            if (!csName.StartsWith("operator")) {
                StringBuilder builder = new StringBuilder(csName);
                builder[0] = char.ToUpper(builder[0]);
                csName = builder.ToString();
            }
            cmm.Name = csName;
            cmm.ReturnType = returnType;
        }

        // for destructors we already have this stuff set
        if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) == 0) {
            // set access
            if ((method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                cmm.Attributes |= MemberAttributes.Family;
            } else {
                cmm.Attributes |= MemberAttributes.Public;
            }

            // virtual/final
            if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0) {
                cmm.Attributes |= MemberAttributes.Final | MemberAttributes.New;
            } else {
                if (MethodOverrides(method))
                    cmm.Attributes |= MemberAttributes.Override;
            }
        }

        // add the parameters
        foreach (CodeParameterDeclarationExpression exp in args) {
            cmm.Parameters.Add(exp);
        }
        return cmm;
    }

    public void GenerateMethod(Smoke.Method *method, string mungedName) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        CodeMemberMethod cmm = GenerateBasicMethodDefinition(method, cppSignature);
        if (cmm == null)
            return;

        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeMethod",
            new CodeAttributeArgument(new CodePrimitiveExpression(cppSignature)));
        cmm.CustomAttributes.Add(attr);

//         CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression(SmokeSupport.smokeInvocation_Invoke);
//         cmm.Statements.Add(new CodeExpressionStatement(invoke));

        type.Members.Add(cmm);
    }

    public void GenerateProperty(Smoke.Method *method) {
    }
}
