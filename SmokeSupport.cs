using System;
using System.CodeDom;

static class SmokeSupport {
    public static CodeMethodReferenceExpression smokeInvocation_Invoke =
        new CodeMethodReferenceExpression(new CodeSnippetExpression("SmokeInvocation"), "Invoke");
}
