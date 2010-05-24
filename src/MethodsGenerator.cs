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
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;

public unsafe delegate void MethodHook(Smoke *smoke, Smoke.Method *smokeMethod, CodeMemberMethod cmm, CodeTypeDeclaration typeDecl);

public unsafe class MethodsGenerator {
    GeneratorData data;
    Translator translator;
    CodeTypeDeclaration type;
    Smoke.Class *smokeClass;

    static Regex qMethodExp = new Regex("^[a-z][A-Z]");

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

    bool m_internalImplementation = false;

    public bool InternalImplementation {
        get {
            return m_internalImplementation;
        }
        set {
            m_internalImplementation = value;
        }
    }

    public static event MethodHook PreMethodBodyHooks;
    public static event MethodHook PostMethodBodyHooks;

    bool MethodOverrides(Smoke *smoke, Smoke.Method* method, out MemberAttributes access, out bool foundInInterface) {
        access = MemberAttributes.Public;
        foundInInterface = false;

        if (smoke->inheritanceList[smoke->classes[method->classId].parents] == 0) {
            return false;
        }

        long id = method - smoke->methods;
        Smoke.ModuleIndex methodModuleIndex = new Smoke.ModuleIndex(smoke, (short) id);

        Smoke.Method *firstMethod = (Smoke.Method*) IntPtr.Zero;
        short *firstParent = smoke->inheritanceList + smoke->classes[method->classId].parents;

        for (short *parent = firstParent; *parent > 0; parent++) {
            if (firstMethod != (Smoke.Method*) IntPtr.Zero && !foundInInterface) {
                // already found a method in the first parent class
                break;
            }

            // Do this with linq... there's probably room for optimization here.
            // Select virtual and pure virtual methods from superclasses.
            var inheritedVirtuals = from key in smoke->FindAllMethods(*parent, true).Keys
                                    where ((key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
                                        || (key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
                                    select key;

            foreach (Smoke.ModuleIndex mi in inheritedVirtuals) {
                Smoke.Method* meth = mi.smoke->methods + mi.index;

                if (SmokeMethodEqualityComparer.DefaultEqualityComparer.Equals(methodModuleIndex, mi))
                {
                    if ((meth->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                        access = MemberAttributes.Family;
                    } else {
                        access = MemberAttributes.Public;
                    }

                    // don't return here - we need the access of the method in the topmost superclass
                    firstMethod = meth;
                    if (parent != firstParent) {
                        foundInInterface = true;
                    }
                }
            }
        }

        // we need to have a method that's not in a interface to mark it as overriden
        bool ret = firstMethod != (Smoke.Method*) IntPtr.Zero && !foundInInterface;

        // we need to have a public method in one of the interfaces for this to be set
        foundInInterface = firstMethod != (Smoke.Method*) IntPtr.Zero && foundInInterface && access == MemberAttributes.Public;

        return ret;
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke *smoke, Smoke.Method *method) {
        return GenerateBasicMethodDefinition(smoke, method, (CodeTypeReference) null);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke *smoke, Smoke.Method *method, CodeTypeReference iface) {
        string cppSignature = smoke->GetMethodSignature(method);
        return GenerateBasicMethodDefinition(smoke, method, cppSignature, iface);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke *smoke, Smoke.Method *method, string cppSignature) {
        return GenerateBasicMethodDefinition(smoke, method, cppSignature, null);
    }

    public CodeMemberMethod GenerateBasicMethodDefinition(Smoke *smoke, Smoke.Method *method, string cppSignature, CodeTypeReference iface) {
        // do we actually want that method?
        string className = ByteArrayManager.GetString(smokeClass->className);
        string completeSignature = className + "::" + cppSignature;
        foreach (Regex regex in translator.ExcludedMethods) {
            if (regex.IsMatch(completeSignature))
                return null;
        }

        List<CodeParameterDeclarationExpression> args = new List<CodeParameterDeclarationExpression>();
        int count = 1;
        bool isRef;
        string csharpClassName = className;
        int indexOfColon = csharpClassName.LastIndexOf("::");
        if (indexOfColon != -1) {
            csharpClassName = csharpClassName.Substring(indexOfColon + 2);
        }

        // make instance operators static and bring the arguments in the correct order
        string methName = ByteArrayManager.GetString(smoke->methodNames[method->name]);
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
                if (smoke->classes[method->classId].size > 0) {
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
                    if (smoke->classes[method->classId].size > 0) {
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
        for (short* typeIndex = smoke->argumentList + method->args; *typeIndex > 0; typeIndex++) {
            try {
                CodeParameterDeclarationExpression exp =
                    new CodeParameterDeclarationExpression(translator.CppToCSharp(smoke->types + *typeIndex, out isRef), "arg" + count++);
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
            returnType = translator.CppToCSharp(smoke->types + method->ret, out isRef);
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
            if (!isOperator && methName != "finalize" && !qMethodExp.IsMatch(methName)) {
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

                if (iface != null && typesWithSameName.Count() == 1 && (method->flags & (uint) Smoke.MethodFlags.mf_protected) == 0) {
                    foreach (var member in typesWithSameName) {
                        if (member.Type == MemberTypes.Property) {
                            cmm.PrivateImplementationType = iface;
                            csName = tmp;
                        }
                    }
                } else {
                    if (typesWithSameName.Count() > 0) {
                        Debug.Print("  |--Conflicting names: method/(type or property): {0} in class {1} - keeping original method name", tmp, className);
                    } else if (tmp == csharpClassName) {
                        Debug.Print("  |--Conflicting names: method/classname: {0} in class {1} - keeping original method name", tmp, className);
                    } else {
                        csName = tmp;
                    }
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
            } else if (cmm.Name == "ToString" && args.Count == 0) {
                cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            } else {
                if ((method->flags & (uint) Smoke.MethodFlags.mf_static) > 0) {
                    cmm.Attributes |= MemberAttributes.Static;
                } else {
                    // virtual/final
                    MemberAttributes access;
                    bool isOverride;
                    bool foundInInterface = false;

                    // methods that have to be implemented from interfaces can't override anything
                    if (iface == null && (isOverride = MethodOverrides(smoke, method, out access, out foundInInterface))) {
                        cmm.Attributes = access | MemberAttributes.Override;
                    } else if (foundInInterface) {
                        cmm.Attributes = access;
                    }

                    if ((method->flags & (uint) Smoke.MethodFlags.mf_purevirtual) > 0) {
                        if (!m_internalImplementation) {
                            cmm.Attributes = (cmm.Attributes & ~MemberAttributes.ScopeMask) | MemberAttributes.Abstract;

                            // The code generator doesn't like MemberAttributes.Abstract | MemberAttributes.Override being set.
                            if (isOverride && !type.IsInterface) {
                                cmm.ReturnType.BaseType = "override " + cmm.ReturnType.BaseType;
                            }
                        } else {
                            cmm.Attributes |= MemberAttributes.Override;
                        }
                    }

                    if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0 &&
                        (method->flags & (uint) Smoke.MethodFlags.mf_purevirtual) == 0 &&
                        !isOverride)
                    {
                        cmm.Attributes |= MemberAttributes.Final | MemberAttributes.New;
                    }
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

    public CodeMemberMethod GenerateMethod(short idx, string mungedName, CodeTypeReference iface) {
        return GenerateMethod(data.Smoke, idx, mungedName, iface);
    }

    public CodeMemberMethod GenerateMethod(short idx, string mungedName) {
        return GenerateMethod(data.Smoke, idx, mungedName);
    }

    public CodeMemberMethod GenerateMethod(Smoke.Method *method, string mungedName) {
        return GenerateMethod(data.Smoke, method, mungedName);
    }

    public CodeMemberMethod GenerateMethod(Smoke.Method *method, string mungedName, CodeTypeReference iface) {
        return GenerateMethod(data.Smoke, method, mungedName, iface);
    }

    public CodeMemberMethod GenerateMethod(Smoke *smoke, short idx, string mungedName, CodeTypeReference iface) {
        return GenerateMethod(smoke, smoke->methods + idx, mungedName, iface);
    }

    public CodeMemberMethod GenerateMethod(Smoke *smoke, short idx, string mungedName) {
        return GenerateMethod(smoke, smoke->methods + idx, mungedName, null);
    }

    public CodeMemberMethod GenerateMethod(Smoke *smoke, Smoke.Method *method, string mungedName) {
        return GenerateMethod(smoke, method, mungedName, null);
    }

    public CodeMemberMethod GenerateMethod(Smoke *smoke, Smoke.Method *method, string mungedName, CodeTypeReference iface) {
        string cppSignature = smoke->GetMethodSignature(method);
        CodeMemberMethod cmm = GenerateBasicMethodDefinition(smoke, method, cppSignature, iface);
        if (cmm == null) {
            return null;
        }

        // put the method into the correct type
        CodeTypeDeclaration containingType = type;
        if (cmm.Name.StartsWith("operator") || cmm.Name.StartsWith("explicit ")) {
            if (!data.CSharpTypeMap.TryGetValue(cmm.Parameters[0].Type.GetStringRepresentation(), out containingType)) {
                if (cmm.Parameters.Count < 2 || !data.CSharpTypeMap.TryGetValue(cmm.Parameters[1].Type.GetStringRepresentation(), out containingType)) {
                    Debug.Print("  |--Can't find containing type for {0} - skipping", cppSignature);
                }
                return null;
            }
        }

        // already implemented?
        if (containingType.HasMethod(cmm)) {
            if (iface == null || (method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0) {
                // protected methods are not available in interfaces
                Debug.Print("  |--Skipping already implemented method {0}", cppSignature);
                return null;
            } else {
                cmm.PrivateImplementationType = iface;
            }
        }

        if (PreMethodBodyHooks != null) {
            PreMethodBodyHooks(smoke, method, cmm, containingType);
        }

        // do we have pass-by-ref parameters?
        bool generateInvokeForRefParams = false;
        foreach (CodeParameterDeclarationExpression expr in cmm.Parameters) {
            if (expr.Direction == FieldDirection.Ref) {
                generateInvokeForRefParams = true;
                break;
            }
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

        if (!generateInvokeForRefParams) {
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
        } else {
            // we have a method with by-ref parameters
            CodeVariableDeclarationStatement stackDecl = new CodeVariableDeclarationStatement(new CodeTypeReference("StackItem[]", 0), "stack",
                new CodeArrayCreateExpression(new CodeTypeReference("StackItem"), cmm.Parameters.Count + 1));
            cmm.Statements.Add(stackDecl);

            int i = 1;
            foreach (CodeParameterDeclarationExpression param in cmm.Parameters) {
                Type t = Type.GetType(param.Type.BaseType);
                string stackItemField = Util.StackItemFieldFromType(t);
                cmm.Statements.Add(new CodeAssignStatement(
                    new CodeFieldReferenceExpression(
                        new CodeArrayIndexerExpression(
                            new CodeSnippetExpression("stack"), new CodeExpression[] { new CodePrimitiveExpression(i) } ),
                        stackItemField
                    ),
                    (t != null && t.IsPrimitive) ?
                        (CodeExpression) new CodeArgumentReferenceExpression(string.Format("arg{0}", i)) :
                        (CodeExpression) new CodeCastExpression(new CodeTypeReference(typeof(IntPtr)),
                            new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("GCHandle"), "Alloc",
                                new CodeExpression[] { new CodeArgumentReferenceExpression(string.Format("arg{0}", i)) } ))
                ));
                i++;
            }

            invoke.Parameters.Add(new CodeVariableReferenceExpression("stack"));
        }

        // add the method call statement
        CodeStatement statement;

        // with by-ref arguments?
        if (generateInvokeForRefParams) {
            // add invoke call
            statement = new CodeExpressionStatement(invoke);
            cmm.Statements.Add(statement);

            Type t;
            string stackItemField;
            CodeExpression fieldReference;
            int i = 0;
            foreach (CodeParameterDeclarationExpression param in cmm.Parameters) {
                i++;
                if (param.Direction != FieldDirection.Ref)
                    continue;

                t = Type.GetType(param.Type.BaseType);
                stackItemField = Util.StackItemFieldFromType(t);
                fieldReference = new CodeFieldReferenceExpression(
                    new CodeArrayIndexerExpression(
                        new CodeVariableReferenceExpression("stack"),
                        new CodeExpression[] { new CodePrimitiveExpression(i) }
                    ), stackItemField);

                if (t != null && t.IsPrimitive) {
                    cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(string.Format("arg{0}", i)), fieldReference));
                } else {
                    cmm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(typeof(System.Runtime.InteropServices.GCHandle)), string.Format("arg{0}Handle", i),
                        new CodeCastExpression(new CodeTypeReference(typeof(System.Runtime.InteropServices.GCHandle)), fieldReference)));
                    CodeVariableReferenceExpression handleVar = new CodeVariableReferenceExpression(string.Format("arg{0}Handle", i));
                    cmm.Statements.Add(new CodeAssignStatement(new CodeArgumentReferenceExpression(string.Format("arg{0}", i)),
                        new CodeCastExpression(param.Type, new CodePropertyReferenceExpression(handleVar, "Target"))));
                    cmm.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(handleVar, "Free")));
                }
            }

            if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0) {
                t = Type.GetType(returnType.BaseType);
                stackItemField = Util.StackItemFieldFromType(t);
                fieldReference = new CodeFieldReferenceExpression(
                    new CodeArrayIndexerExpression(
                        new CodeVariableReferenceExpression("stack"),
                        new CodeExpression[] { new CodePrimitiveExpression(0) }
                    ), stackItemField);

                if (t != null && t.IsPrimitive) {
                    // primitive types can be returned directly
                    statement = new CodeMethodReturnStatement(fieldReference);
                    cmm.Statements.Add(statement);
                } else {
                    statement = new CodeVariableDeclarationStatement(new CodeTypeReference("System.Runtime.InteropServices.GCHandle"), "returnedHandle",
                        new CodeCastExpression(new CodeTypeReference("System.Runtime.InteropServices.GCHandle"), fieldReference));
                    cmm.Statements.Add(statement);
                    statement = new CodeVariableDeclarationStatement(returnType, "returnValue",
                        new CodeCastExpression(returnType, new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("returnedHandle"), "Target")));
                    cmm.Statements.Add(statement);
                    statement = new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("returnedHandle"), "Free"));
                    cmm.Statements.Add(statement);
                    statement = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("returnValue"));
                    cmm.Statements.Add(statement);
                }
            }
        } else {
            // no by-ref arguments
            if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0) {
                statement = new CodeMethodReturnStatement(new CodeCastExpression(returnType, invoke));
            } else {
                statement = new CodeExpressionStatement(invoke);
            }
            cmm.Statements.Add(statement);
        }

        if (PostMethodBodyHooks != null) {
            PostMethodBodyHooks(smoke, method, cmm, containingType);
        }

        containingType.Members.Add(cmm);

        if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) != 0) {
            containingType.BaseTypes.Add(new CodeTypeReference(typeof(IDisposable)));
            CodeMemberMethod dispose = new CodeMemberMethod();
            dispose.Name = "Dispose";
            dispose.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;
            dispose.Statements.AddRange(cmm.Statements);
            dispose.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression("GC"), "SuppressFinalize", new CodeThisReferenceExpression()
            )));
            containingType.Members.Add(dispose);
        }
        return cmm;
    }
}
