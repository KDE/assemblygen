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
using System.IO;
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

    public unsafe static void Main(string[] args) {
        Smoke *smoke = InitSmoke("qtcore");
        if (smoke == (Smoke*) 0) {
            return;
        }

        GeneratorData data = new GeneratorData(smoke, "Qyoto");
        Translator translator = new Translator(data);

        ClassesGenerator classgen = new ClassesGenerator(data, translator);
        Console.Error.WriteLine("Generating CodeCompileUnit...");
        classgen.Run();
        DestroySmoke((IntPtr) smoke);

        FileStream fs = new FileStream("out.cs", FileMode.Create);
        StreamWriter sw = new StreamWriter(fs);

        Console.Error.WriteLine("Generating code...");
        CodeDomProvider csharp = CodeDomProvider.CreateProvider("CSharp");
        CodeGeneratorOptions cgo = new CodeGeneratorOptions();
        csharp.GenerateCodeFromCompileUnit(data.CompileUnit, sw, cgo);
        sw.Close();
        fs.Close();

        Console.Error.WriteLine("Done.");
    }
}
