using System;
using System.CodeDom;

public interface IHookProvider {
    void RegisterHooks();
}

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
