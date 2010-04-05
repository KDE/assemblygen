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
using System.CodeDom;

public unsafe class QyotoHooks : IHookProvider {

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
        ClassesGenerator.SupportingMethodsHooks += SupportingMethodsHook;
        Console.WriteLine("Registered Qyoto hooks.");
    }

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

    public void SupportingMethodsHook(Smoke *smoke, Smoke.Method *method, CodeMemberMethod cmm, CodeTypeDeclaration type) {
        if (type.Name == "QObject" && cmm is CodeConstructor) {
            cmm.Statements.Add(new CodeSnippetStatement(qObjectDummyCtorCode));
        }
    }

}
