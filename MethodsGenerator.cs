using System;
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
    
    public void Generate(short index) {
        Smoke.Method *method = smoke->methods + index;
        Generate(method);
    }
    
    public void Generate(Smoke.Method *method) {
        if ((method->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0 || (method->flags & (ushort) Smoke.MethodFlags.mf_property) > 0) {
            GenerateProperty(method);
        } else {
            GenerateMethod(method);
        }
    }

    public void GenerateMethod(Smoke.Method *method) {
        string cppSignature = smoke->GetMethodSignature(method);

        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        for (short* typeIndex = smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(Translator.CppToCSharp(smoke->types + *typeIndex, out isRef), "arg" + count);
                if (isRef) {
                    exp.Direction = FieldDirection.Ref;
                }
                args.Add(exp);
            } catch (NotSupportedException) {
//                 Console.WriteLine("  |--Won't wrap method {0}::{1}",
//                     ByteArrayManager.GetString(smoke->classes[method->classId].className), cppSignature);
                return;
            }
        }

        CodeTypeReference returnType = null;
        try {
            returnType = new CodeTypeReference(Translator.CppToCSharp(smoke->types + method->ret, out isRef));
        } catch (NotSupportedException) {
//             Console.WriteLine("  |--Won't wrap method {0}::{1}",
//                 ByteArrayManager.GetString(smoke->classes[method->classId].className), cppSignature);
            return;
        }

        StringBuilder builder = new StringBuilder(ByteArrayManager.GetString(smoke->methodNames[method->name]));
        builder[0] = char.ToUpper(builder[0]);

        string csName = builder.ToString();

        CodeMemberMethod cmm = new CodeMemberMethod();
        cmm.Name = csName;
        cmm.ReturnType = returnType;
        foreach (CodeParameterDeclarationExpression exp in args) {
            cmm.Parameters.Add(exp);
        }

        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeMethod",
            new CodeAttributeArgument(new CodePrimitiveExpression(cppSignature)));
        cmm.CustomAttributes.Add(attr);

        CodeMethodInvokeExpression invoke = new CodeMethodInvokeExpression(SmokeSupport.smokeInvocation_Invoke);
        
        cmm.Statements.Add(new CodeExpressionStatement(invoke));
        
        type.Members.Add(cmm);
    }

    public void GenerateProperty(Smoke.Method *method) {
    }
}
