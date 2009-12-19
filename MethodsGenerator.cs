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
    Smoke* smoke;
    CodeTypeDeclaration type;

    public MethodsGenerator(Smoke* smoke, CodeTypeDeclaration type) {
        this.smoke = smoke;
        this.type = type;
    }

    bool MethodOverrides(Smoke.Method* method) {
        Dictionary<short, string> allMethods = smoke->FindAllMethods(method->classId, true);
        // Do this with linq... there's probably room for optimization here.
        // Select virtual and pure virtual and pure virtual methods from superclasses.
        var inheritedVirtuals = from entry in allMethods
                                where ((smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
                                    || (smoke->methods[entry.Key].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
                                where smoke->methods[entry.Key].classId != method->classId
                                select entry.Key;

        foreach (short index in inheritedVirtuals) {
            Smoke.Method* meth = smoke->methods + index;
            if (meth->name == method->name && meth->args == method->args &&
                (meth->flags & (uint) Smoke.MethodFlags.mf_const) == (method->flags & (uint) Smoke.MethodFlags.mf_const))
            {
                return true;
            }
        }
        return false;
    }

    public void Generate(short index, string mungedName) {
        Smoke.Method *method = smoke->methods + index;
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
        string cppSignature = smoke->GetMethodSignature(method);
        return GenerateBasicMethodDefinition(method, cppSignature);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, string cppSignature) {
        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        for (short* typeIndex = smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(Translator.CppToCSharp(smoke->types + *typeIndex, out isRef), "arg" + count++);
                if (isRef) {
                    exp.Direction = FieldDirection.Ref;
                }
                args.Add(exp);
            } catch (NotSupportedException) {
                Console.WriteLine("  |--Won't wrap method {0}::{1}",
                    ByteArrayManager.GetString(smoke->classes[method->classId].className), cppSignature);
                return null;
            }
        }

        CodeTypeReference returnType = null;
        try {
            returnType = Translator.CppToCSharp(smoke->types + method->ret, out isRef);
        } catch (NotSupportedException) {
            Console.WriteLine("  |--Won't wrap method {0}::{1}",
                ByteArrayManager.GetString(smoke->classes[method->classId].className), cppSignature);
            return null;
        }

        CodeMemberMethod cmm;
        if ((method->flags & (uint) Smoke.MethodFlags.mf_ctor) > 0) {
            cmm = new CodeConstructor();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=
        } else if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) > 0) {
            cmm = new CodeMemberMethod();
            cmm.Name = "Finalize";
            cmm.ReturnType = new CodeTypeReference(typeof(void));
            cmm.Attributes = MemberAttributes.Override | MemberAttributes.Family;
        } else {
            cmm = new CodeMemberMethod();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=
            
            StringBuilder builder = new StringBuilder(ByteArrayManager.GetString(smoke->methodNames[method->name]));
            builder[0] = char.ToUpper(builder[0]);
            string csName = builder.ToString();
            cmm.Name = csName;
            cmm.ReturnType = returnType;
        }

        if ((method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
            cmm.Attributes |= MemberAttributes.Family;
        } else {
            cmm.Attributes |= MemberAttributes.Public;
        }

        if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0) {
            cmm.Attributes |= MemberAttributes.Final | MemberAttributes.New;
        } else {
            if (MethodOverrides(method))
                cmm.Attributes |= MemberAttributes.Override;
        }

        foreach (CodeParameterDeclarationExpression exp in args) {
            cmm.Parameters.Add(exp);
        }
        return cmm;
    }

    public void GenerateMethod(Smoke.Method *method, string mungedName) {
        string cppSignature = smoke->GetMethodSignature(method);
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
