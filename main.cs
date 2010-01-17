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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

    const int NoError = 0;
    const int SmokeLoadingFailure = 1;
    const int CompilationError = 2;
    const int MissingOptionError = 254;

    public unsafe static int Main(string[] args) {
        List<CodeCompileUnit> codeSnippets = new List<CodeCompileUnit>();
        List<Assembly> references = new List<Assembly>();
        StringBuilder compilerOptions = new StringBuilder();
        bool codeOnly = false;
        string codeFile = string.Empty;
        string assemblyFile = "out.dll";
        int warnLevel = 0;
        string smokeLib = null;

        foreach (string arg in args) {
            if (arg == "-verbose") {
                Debug.Listeners.Add(new ConsoleTraceListener(true));
                continue;
            } else if (arg == "-code-only") {
                codeOnly = true;
                continue;
            } else if (arg.StartsWith("-code-file:")) {
                codeFile = arg.Substring(11);
                continue;
            } else if (arg.StartsWith("-out:")) {
                assemblyFile = arg.Substring(5);
                continue;
            } else if (arg.StartsWith("-warn:")) {
                warnLevel = int.Parse(arg.Substring(6));
                continue;
            } else if (arg.StartsWith("-r:")) {
                references.Add(Assembly.LoadFrom(arg.Substring(3)));
                continue;
            } else if (arg.StartsWith("-reference:")) {
                references.Add(Assembly.LoadFrom(arg.Substring(11)));
                continue;
            } else if (arg.StartsWith("-")) {
                compilerOptions.Append(" /");
                compilerOptions.Append(arg.Substring(1));
                continue;
            }

            if (smokeLib == null) {
                smokeLib = arg;
                continue;
            }

            FileStream fs = new FileStream(arg, FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            codeSnippets.Add(new CodeSnippetCompileUnit(sr.ReadToEnd()));
            sr.Close();
            fs.Close();
        }

        Smoke *smoke = InitSmoke(smokeLib);
        if (smoke == (Smoke*) 0) {
            return SmokeLoadingFailure;
        }

        GeneratorData data = new GeneratorData(smoke, "Qyoto", references);
        Translator translator = new Translator(data);

        ClassesGenerator classgen = new ClassesGenerator(data, translator);
        Console.Error.WriteLine("Generating CodeCompileUnit...");
        classgen.Run();
        DestroySmoke((IntPtr) smoke);

        CodeDomProvider csharp = CodeDomProvider.CreateProvider("CSharp");
        if (codeFile != string.Empty) {
            FileStream fs = new FileStream(codeFile, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            Console.Error.WriteLine("Generating code...");
            CodeGeneratorOptions cgo = new CodeGeneratorOptions();
            csharp.GenerateCodeFromCompileUnit(data.CompileUnit, sw, cgo);
            sw.Close();
            fs.Close();
        }

        if (codeOnly) {
            if (codeFile == string.Empty) {
                Console.Error.WriteLine("Missing output filename. Use the -code-file:<file> option.");
                return MissingOptionError;
            }
            return NoError;
        }

        codeSnippets.Add(data.CompileUnit);

        Console.Error.WriteLine("Compiling assembly...");
        CompilerParameters cp = new CompilerParameters();
        cp.GenerateExecutable = false;
        cp.TreatWarningsAsErrors = false;
        cp.OutputAssembly = assemblyFile;
        cp.GenerateInMemory = false;
        cp.WarningLevel = warnLevel;
        cp.CompilerOptions = compilerOptions.ToString();
        CompilerResults cr = csharp.CompileAssemblyFromDom(cp, codeSnippets.ToArray());

        bool errorsOccured = false;
        foreach (CompilerError error in cr.Errors) {
            if (!error.IsWarning)
                errorsOccured = true;
            Console.Error.WriteLine(error);
        }

        if (errorsOccured) {
            Console.Error.WriteLine("Errors occured. No assembly was generated.");
            return CompilationError;
        } else {
            Console.Error.WriteLine("Done.");
            return NoError;
        }
    }
}
