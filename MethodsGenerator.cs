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
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;

static class CodeDomExtensions {

    public static bool TypeEquals(this CodeTypeReference self, CodeTypeReference other) {
        if (self.BaseType != other.BaseType || self.TypeArguments.Count != other.TypeArguments.Count)
            return false;

        for (int i = 0; i < self.TypeArguments.Count; i++) {
            if (!self.TypeArguments[i].TypeEquals(other.TypeArguments[i]))
                return false;
        }
        return true;
    }

    public static string GetStringRepresentation(this CodeTypeReference self) {
        StringBuilder ret = new StringBuilder(self.BaseType);
        if (self.TypeArguments.Count > 0) {
            ret.Append('<');
            for (int i = 0; i < self.TypeArguments.Count; i++) {
                if (i > 0) ret.Append(", ");
                ret.Append(self.TypeArguments[i].GetStringRepresentation());
            }
            ret.Append('>');
        }
        return ret.ToString();
    }

    public static bool HasMethod(this CodeTypeDeclaration self, CodeMemberMethod method) {
        foreach (CodeTypeMember member in self.Members) {
            if (!(member is CodeMemberMethod) || member.Name != method.Name)
                continue;

            // now check the parameters
            CodeMemberMethod currentMeth = (CodeMemberMethod) member;
            if (currentMeth.Parameters.Count != method.Parameters.Count)
                continue;
            bool continueOuter = false;
            for (int i = 0; i < method.Parameters.Count; i++) {
                if (!method.Parameters[i].Type.TypeEquals(currentMeth.Parameters[i].Type) || method.Parameters[i].Direction != currentMeth.Parameters[i].Direction) {
                    continueOuter = true;
                    break;
                }
            }
            if (continueOuter)
                continue;
            return true;
        }
        return false;
    }

    public static bool ParametersEqual(this CodeMemberMethod self, CodeMemberMethod method) {
        if (method.Parameters.Count != self.Parameters.Count)
            return false;
        for (int i = 0; i < self.Parameters.Count; i++) {
            if (!self.Parameters[i].Type.TypeEquals(method.Parameters[i].Type) || self.Parameters[i].Direction != method.Parameters[i].Direction) {
                return false;
            }
        }
        return true;
    }
}

unsafe class MethodsGenerator {
    GeneratorData data;
    Translator translator;
    CodeTypeDeclaration type;
    Smoke.Class *smokeClass;

    static List<string> binaryOperators = new List<string>() {
        "!=", "==", "%", "&", "*", "+", "-", "/", "<", "<=", ">", ">=", "^", "|"
    };
    static List<string> unaryOperators = new List<string>() {
        "!", "~", "+", "++", "-", "--"
    };
    static List<string> unsupportedOperators = new List<string>() {
        "=", "->", "+=", "-=", "/=", "*=", "%=", "^=", "&=", "|=", "[]", "()"
    };

    public MethodsGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type, Smoke.Class *klass) {
        this.data = data;
        this.translator = translator;
        this.type = type;
        this.smokeClass = klass;
    }

    bool MethodOverrides(Smoke.Method* method, out MemberAttributes access) {
        Dictionary<Smoke.ModuleIndex, string> allMethods = data.Smoke->FindAllMethods(method->classId, true);
        // Do this with linq... there's probably room for optimization here.
        // Select virtual and pure virtual methods from superclasses.
        var inheritedVirtuals = from key in allMethods.Keys
                                where ((key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
                                    || (key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
                                where key.smoke->methods[key.index].classId != method->classId
                                select key;

        access = MemberAttributes.Public;
        bool ret = false;

        foreach (Smoke.ModuleIndex mi in inheritedVirtuals) {
            Smoke.Method* meth = mi.smoke->methods + mi.index;
            if (meth->name == method->name && meth->args == method->args &&
                (meth->flags & (uint) Smoke.MethodFlags.mf_const) == (method->flags & (uint) Smoke.MethodFlags.mf_const))
            {
                if ((meth->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                    access = MemberAttributes.Family;
                } else {
                    access = MemberAttributes.Public;
                }
                // don't return here - we need the access of the method in the topmost superclass
                ret = true;
            }
        }
        return ret;
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method) {
        return GenerateBasicMethodDefinition(method, (CodeTypeReference) null);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, CodeTypeReference iface) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        return GenerateBasicMethodDefinition(method, cppSignature, iface);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, string cppSignature) {
        return GenerateBasicMethodDefinition(method, cppSignature, null);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke.Method *method, string cppSignature, CodeTypeReference iface) {
        // do we actually want that method?
        foreach (Regex regex in data.ExcludedMethods) {
            if (regex.IsMatch(cppSignature))
                return null;
        }

        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        string className = ByteArrayManager.GetString(smokeClass->className);
        string csharpClassName = className;
        int indexOfColon = csharpClassName.LastIndexOf("::");
        if (indexOfColon != -1) {
            csharpClassName = csharpClassName.Substring(indexOfColon + 2);
        }

        // make instance operators static and bring the arguments in the correct order
        string methName = ByteArrayManager.GetString(data.Smoke->methodNames[method->name]);
        bool isOperator = false;
        string explicitConversionType = null;
        if (methName.StartsWith("operator")) {
            string op = methName.Substring(8);
            if (unsupportedOperators.Contains(op)) {
                // not supported
                Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                return null;
            }

            if (op == "<<") {
                methName = "Write";
            } else if (op == ">>") {
                methName = "Read";
            }

            // binary/unary operator
            if (binaryOperators.Contains(op) || unaryOperators.Contains(op)) {
                // instance operator
                if (data.Smoke->classes[method->classId].size > 0) {
                    if (op == "*" && method->numArgs == 0 || (op == "++" || op == "--") && method->numArgs == 1) {
                        // dereference operator and postfix in-/decrement operator are not supported
                        Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                        return null;
                    }

                    try {
                        CodeParameterDeclarationExpression exp =
                            new CodeParameterDeclarationExpression(translator.CppToCSharp(className, out isRef), "arg" + count++);
                        args.Add(exp);
                    } catch (NotSupportedException) {
                        Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                        return null;
                    }
                } else {    // global operator
                    if (op == "*" && method->numArgs == 0 || (op == "++" || op == "--") && method->numArgs == 2) {
                        // dereference operator and postfix in-/decrement operator are not supported
                        Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                        return null;
                    }
                }
                isOperator = true;
            } else if (op[0] == ' ') {
                // conversion operator
                explicitConversionType = op.Substring(1);
                try {
                    explicitConversionType = translator.CppToCSharp(explicitConversionType, out isRef).GetStringRepresentation();
                    if (data.Smoke->classes[method->classId].size > 0) {
                        CodeParameterDeclarationExpression exp =
                            new CodeParameterDeclarationExpression(translator.CppToCSharp(className, out isRef), "arg" + count++);
                        args.Add(exp);
                    }
                } catch (NotSupportedException) {
                    Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                    return null;
                }
                isOperator = true;
            }
        }

        // translate arguments
        for (short* typeIndex = data.Smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(translator.CppToCSharp(data.Smoke->types + *typeIndex, out isRef), "arg" + count++);
                if (isRef) {
                    exp.Direction = FieldDirection.Ref;
                }
                args.Add(exp);
            } catch (NotSupportedException) {
                Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
                return null;
            }
        }

        // translate return type
        CodeTypeReference returnType = null;
        try {
            returnType = translator.CppToCSharp(data.Smoke->types + method->ret, out isRef);
        } catch (NotSupportedException) {
            Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
            return null;
        }

        CodeMemberMethod cmm;
        if ((method->flags & (uint) Smoke.MethodFlags.mf_ctor) > 0) {
            cmm = new CodeConstructor();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=
            ((CodeConstructor) cmm).ChainedConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
        } else {
            cmm = new CodeMemberMethod();
            cmm.Attributes = (MemberAttributes) 0; // initialize to 0 so we can do |=

            string csName = methName;
            if (!isOperator) {
                // capitalize the first letter
                StringBuilder builder = new StringBuilder(csName);
                builder[0] = char.ToUpper(builder[0]);
                string tmp = builder.ToString();

                // If the new name clashes with a name of a type declaration, keep the lower-case name.
                var typesWithSameName = from member in data.GetAccessibleMembers(smokeClass)
                                        where (   member.Type == MemberTypes.NestedType
                                               || member.Type == MemberTypes.Property)
                                               && member.Name == tmp
                                        select member;
                data.Debug = false;
                if (typesWithSameName.Count() > 0) {
                    Debug.Print("  |--Conflicting names: method/(type or property): {0} in class {1} - keeping original method name", tmp, className);
/*                    if (iface != null) {
                        cmm.PrivateImplementationType = iface;
                    }*/
                } else if (tmp == csharpClassName) {
                    Debug.Print("  |--Conflicting names: method/classname: {0} in class {1} - keeping original method name", tmp, className);
                } else {
                    csName = tmp;
                }
            }

            if (explicitConversionType != null) {
                cmm.Name = "explicit operator " + explicitConversionType;
                cmm.ReturnType = new CodeTypeReference(" ");
            } else {
                cmm.Name = csName;
                cmm.ReturnType = returnType;
            }
        }

        // for destructors we already have this stuff set
        if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) == 0) {
            // set access
            if ((method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                cmm.Attributes |= MemberAttributes.Family;
            } else {
                cmm.Attributes |= MemberAttributes.Public;
            }

            if (isOperator) {
                cmm.Attributes |= MemberAttributes.Final | MemberAttributes.Static;
            } else {
                // virtual/final
                if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0) {
                    cmm.Attributes |= MemberAttributes.Final | MemberAttributes.New;
                } else {
                    MemberAttributes access;
                    if (MethodOverrides(method, out access)) {
                        cmm.Attributes = access | MemberAttributes.Override;
                    }
                }

                if ((method->flags & (uint) Smoke.MethodFlags.mf_static) > 0) {
                    cmm.Attributes |= MemberAttributes.Static;
                }
            }
        } else {
            // hack, so we don't have to use CodeSnippetTypeMember to generator the destructor
            cmm.ReturnType = new CodeTypeReference(" ");
        }

        // add the parameters
        foreach (CodeParameterDeclarationExpression exp in args) {
            cmm.Parameters.Add(exp);
        }
        return cmm;
    }

    public void GenerateMethod(short idx, string mungedName, CodeTypeReference iface) {
        GenerateMethod(data.Smoke->methods + idx, mungedName, iface);
    }

    public void GenerateMethod(short idx, string mungedName) {
        GenerateMethod(data.Smoke->methods + idx, mungedName, null);
    }

    public void GenerateMethod(Smoke.Method *method, string mungedName) {
        GenerateMethod(method, mungedName, null);
    }

    public void GenerateMethod(Smoke.Method *method, string mungedName, CodeTypeReference iface) {
        string cppSignature = data.Smoke->GetMethodSignature(method);
        CodeMemberMethod cmm = GenerateBasicMethodDefinition(method, cppSignature, iface);
        if (cmm == null)
            return;

        // put the method into the correct type
        CodeTypeDeclaration containingType = type;
        if (cmm.Name.StartsWith("operator") || cmm.Name.StartsWith("explicit ")) {
            if (!data.CSharpTypeMap.TryGetValue(cmm.Parameters[0].Type.GetStringRepresentation(), out containingType)) {
                if (cmm.Parameters.Count < 2 || !data.CSharpTypeMap.TryGetValue(cmm.Parameters[1].Type.GetStringRepresentation(), out containingType)) {
                    Debug.Print("  |--Can't find containing type for {0} - skipping", cppSignature);
                }
                return;
            }
        }

        // already implemented?
        if (containingType.HasMethod(cmm)) {
            Debug.Print("  |--Skipping already implemented method {0}", cppSignature);
            return;
        }

        // generate the SmokeMethod attribute
        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeMethod",
            new CodeAttributeArgument(new CodePrimitiveExpression(cppSignature)));
        cmm.CustomAttributes.Add(attr);

        // choose the correct 'interceptor'
        CodeMethodInvokeExpression invoke;
        if ((cmm.Attributes & MemberAttributes.Static) == MemberAttributes.Static) {
            invoke = new CodeMethodInvokeExpression(SmokeSupport.staticInterceptor_Invoke);
        } else {
            invoke = new CodeMethodInvokeExpression(SmokeSupport.interceptor_Invoke);
        }

        // first pass the munged name, then the C++ signature
        invoke.Parameters.Add(new CodePrimitiveExpression(mungedName));
        invoke.Parameters.Add(new CodePrimitiveExpression(cppSignature));

        // retrieve the return type
        CodeTypeReference returnType;
        if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) > 0) {
            // destructor
            returnType = new CodeTypeReference(typeof(void));
        } else if (cmm.Name.StartsWith("explicit operator ")) {
            // strip 'explicit operator' from the name to get the return type
            returnType = new CodeTypeReference(cmm.Name.Substring(18));
        } else {
            returnType = cmm.ReturnType;
        }
        // add the return type
        invoke.Parameters.Add(new CodeTypeOfExpression(returnType));

        // add the parameters
        foreach (CodeParameterDeclarationExpression param in cmm.Parameters) {
            invoke.Parameters.Add(new CodeTypeOfExpression(param.Type));
            invoke.Parameters.Add(new CodeArgumentReferenceExpression(param.Name));
        }

        // we have to call "CreateProxy()" in constructors
        if (cmm is CodeConstructor) {
            cmm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "CreateProxy")));
        }

        // add the method call statement
        CodeStatement statement;
        if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0) {
            statement = new CodeMethodReturnStatement(new CodeCastExpression(returnType, invoke));
        } else {
            statement = new CodeExpressionStatement(invoke);
        }
        cmm.Statements.Add(statement);

        containingType.Members.Add(cmm);
    }
}
