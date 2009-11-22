using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;
using System.CodeDom;
using System.CodeDom.Compiler;

class MainClass {
    // We could marshall this as a .NET object (i.e. define 'Smoke' as class instead of struct). But then the runtime takes ownership of
    // that pointer and tries to free it when it's garbage collected. That's fine on Linux, but on Windows we get an error because the
    // memory wasn't allocated with GlobalAlloc(). So just use unsafe code and Smoke* everywhere.
    [DllImport("smokeloader", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
    static extern unsafe Smoke* InitSmoke(string module);

    [DllImport("smokeloader", CallingConvention=CallingConvention.Cdecl)]
    static extern unsafe void DestroySmoke(IntPtr smoke);

    public unsafe static void Main(string[] args) {
        CodeCompileUnit unit = new CodeCompileUnit();
        Smoke *smoke = InitSmoke("qt");
        if (smoke == (Smoke*) 0) {
            return;
        }
        ClassesGenerator classgen = new ClassesGenerator(smoke, unit, "Qyoto");
        Console.Error.WriteLine("Generating CodeCompileUnit...");
        classgen.Run();
        DestroySmoke((IntPtr) smoke);

        Console.Error.WriteLine("Generating code...");
        CodeDomProvider csharp = CodeDomProvider.CreateProvider("CSharp");
        CodeGeneratorOptions cgo = new CodeGeneratorOptions();
        csharp.GenerateCodeFromCompileUnit(unit, Console.Out, cgo);
    }
}
