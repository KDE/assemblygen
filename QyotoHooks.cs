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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.CodeDom;

public unsafe class QyotoHooks : IHookProvider {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    delegate void AddSignal(string signature, string name, string returnType, IntPtr metaMethod);

    [DllImport("smokeloader", CallingConvention=CallingConvention.Cdecl)]
    static extern void GetSignals(Smoke* smoke, void *klass, AddSignal addSignalFn);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    delegate void AddParameter(string type, string name);

    [DllImport("smokeloader", CallingConvention=CallingConvention.Cdecl)]
    static extern void GetMetaMethodParameters(IntPtr metaMethod, AddParameter addParamFn);

    static string qObjectDummyCtorCode =
"            try {\n" +
"                Type proxyInterface = Qyoto.GetSignalsInterface(GetType());\n" +
"                SignalInvocation realProxy = new SignalInvocation(proxyInterface, this);\n" +
"                Q_EMIT = realProxy.GetTransparentProxy();\n" +
"            }\n" +
"            catch (Exception e) {\n" +
"                Console.WriteLine(\"Could not retrieve signal interface: {0}\", e);\n" +
"            }";

    public void RegisterHooks() {
        ClassesGenerator.PreMembersHooks += PreMembersHook;
        ClassesGenerator.PostMembersHooks += PostMembersHook;
        ClassesGenerator.SupportingMethodsHooks += SupportingMethodsHook;
        Console.WriteLine("Registered Qyoto hooks.");
    }

    public Translator Translator { get; set; }
    public GeneratorData Data { get; set; }

    public void PreMembersHook(Smoke *smoke, Smoke.Class *klass, CodeTypeDeclaration type) {
        if (type.Name == "QObject") {
            // Add 'Qt' base class
            type.BaseTypes.Add(new CodeTypeReference("Qt"));

            // add the Q_EMIT field
            CodeMemberField Q_EMIT = new CodeMemberField(typeof(object), "Q_EMIT");
            Q_EMIT.Attributes = MemberAttributes.Family;
            Q_EMIT.InitExpression = new CodePrimitiveExpression(null);
            type.Members.Add(Q_EMIT);
        }
    }

    public void PostMembersHook(Smoke *smoke, Smoke.Class *klass, CodeTypeDeclaration type) {
        if (Util.IsQObject(klass)) {
            CodeMemberProperty emit = new CodeMemberProperty();
            emit.Name = "Emit";
            emit.Attributes = MemberAttributes.Family | MemberAttributes.New | MemberAttributes.Final;
            emit.HasGet = true;
            emit.HasSet = false;

            string signalsIfaceName = "I" + type.Name + "Signals";
            CodeTypeReference returnType = new CodeTypeReference(signalsIfaceName);
            emit.Type = returnType;

            emit.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(
                returnType,
                new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "Q_EMIT")
            )));

            type.Members.Add(emit);

            string className = ByteArrayManager.GetString(klass->className);
            int colon = className.LastIndexOf("::");
            string prefix = (colon != -1) ? className.Substring(0, colon) : string.Empty;

            IList typeCollection = Data.GetTypeCollection(prefix);
            CodeTypeDeclaration ifaceDecl = new CodeTypeDeclaration(signalsIfaceName);
            ifaceDecl.IsInterface = true;

            if (className != "QObject") {
                string parentClassName = ByteArrayManager.GetString(smoke->classes[smoke->inheritanceList[klass->parents]].className);
                colon = parentClassName.LastIndexOf("::");
                prefix = (colon != -1) ? parentClassName.Substring(0, colon) : string.Empty;
                if (colon != -1) {
                    parentClassName = parentClassName.Substring(colon + 2);
                }

                string parentInterface = (prefix != string.Empty) ? prefix.Replace("::", ".") + "." : string.Empty;
                parentInterface += "I" + parentClassName + "Signals";

                ifaceDecl.BaseTypes.Add(new CodeTypeReference(parentInterface));
            }

            GetSignals(smoke, klass, delegate(string signature, string name, string typeName, IntPtr metaMethod) {
                CodeMemberMethod signal = new CodeMemberMethod();

                // capitalize the first letter
                StringBuilder builder = new StringBuilder(name);
                builder[0] = char.ToUpper(builder[0]);
                string tmp = builder.ToString();

                signal.Name = tmp;
                bool isRef;
                try {
                    if (typeName == string.Empty)
                        signal.ReturnType = new CodeTypeReference(typeof(void));
                    else
                        signal.ReturnType = Translator.CppToCSharp(typeName, out isRef);
                } catch (NotSupportedException) {
                    Debug.Print("  |--Won't wrap signal {0}::{1}", className, signature);
                    return;
                }

                CodeAttributeDeclaration attr = new CodeAttributeDeclaration("Q_SIGNAL",
                    new CodeAttributeArgument(new CodePrimitiveExpression(signature)));
                signal.CustomAttributes.Add(attr);

                int argNum = 1;
                GetMetaMethodParameters(metaMethod, delegate(string paramType, string paramName) {
                    if (paramName == string.Empty) {
                        paramName = "arg" + argNum.ToString();
                    }
                    argNum++;

                    CodeParameterDeclarationExpression param;
                    try {
                        param = new CodeParameterDeclarationExpression(Translator.CppToCSharp(paramType, out isRef), paramName);
                    } catch (NotSupportedException) {
                        Debug.Print("  |--Won't wrap signal {0}::{1}", className, signature);
                        return;
                    }
                    if (isRef) {
                        param.Direction = FieldDirection.Ref;
                    }

                    signal.Parameters.Add(param);
                });

                ifaceDecl.Members.Add(signal);
            });

            typeCollection.Add(ifaceDecl);
        }
    }

    public void SupportingMethodsHook(Smoke *smoke, Smoke.Method *method, CodeMemberMethod cmm, CodeTypeDeclaration type) {
        if (type.Name == "QObject" && cmm is CodeConstructor) {
            cmm.Statements.Add(new CodeSnippetStatement(qObjectDummyCtorCode));
        }
    }

}
