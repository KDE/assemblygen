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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

internal class MainClass
{
	// We could marshall this as a .NET object (i.e. define 'Smoke' as class instead of struct). But then the runtime takes ownership of
	// that pointer and tries to free it when it's garbage collected. That's fine on Linux, but on Windows we get an error because the
	// memory wasn't allocated with GlobalAlloc(). So just use unsafe code and Smoke* everywhere.
	[DllImport("assemblygen-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
	private static extern unsafe Smoke* InitSmoke(string module);

	[DllImport("assemblygen-native", CallingConvention = CallingConvention.Cdecl)]
	private static extern void DestroySmoke(IntPtr smoke);

	private const int NoError = 0;
	private const int SmokeLoadingFailure = 1;
	private const int CompilationError = 2;
	private const int MissingOptionError = 254;

	private static void PrintHelp()
	{
		Console.Write(
			@"Usage: {0} [options] <smoke library>

Possible options:
    -code-only               Produces only code. Requires the -code-file option to be set.
    -code-file:<file>        Writes the resulting code into <file>
    -out:<filename>          The name of the resulting assembly. Defaults to 'out.dll'.
    -warn:0-4                Sets the warning level, default is 4.
    -reference:<assembly>    References <assembly> (short: -r:).
    -global-class:<name>     Name of the class in which to put methods from namespaces. Defaults to 'Global'.
    -namespace:<name>        Name of the default namespace. Defaults to 'Qyoto'.
    -import:<name>[,n2,...]  Adds additional 'using <name>' statements to each namespace.
    -plugins:P1[,Pn]         Loads additional plugins. Absolute path or relative path to the 'plugins' directory.
    -dest:d1                 The destination directory.
    -verbose                 Be verbose (VERY verbose!).
    -help                    Shows this message.

Any options not listed here are directly passed to the compiler (leading dashes are replaces with slashes).
",
			Assembly.GetExecutingAssembly().Location);
	}

	public static unsafe int Main(string[] args)
	{
		string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
		string pluginDirectory;
		if (baseDirectory.EndsWith("bin"))
		{
			// Windows
			pluginDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "..\\lib\\plugins"));
		}
		else
		{
			// *nix
			pluginDirectory = Path.Combine(baseDirectory, "plugins");
		}

		List<CodeCompileUnit> codeSnippets = new List<CodeCompileUnit>();
		List<Assembly> references = new List<Assembly>();
		List<string> imports = new List<string>();
		StringBuilder compilerOptions = new StringBuilder();
		bool codeOnly = false;
		string codeFile = string.Empty;
		string assemblyFile = "out.dll";
		int warnLevel = 0;
		string smokeLib = null;
		string defaultNamespace = "Qyoto";
		string globalClass = "Global";
		string destination = string.Empty;
		string docs = string.Empty;

		List<Assembly> plugins = new List<Assembly>();

		foreach (string arg in args)
		{
			if (arg == "-help" || arg == "--help" || arg == "-h")
			{
				PrintHelp();
				return 0;
			}
			if (arg == "-verbose")
			{
				Debug.Listeners.Add(new ConsoleTraceListener(true));
				continue;
			}
			if (arg == "-code-only")
			{
				codeOnly = true;
				continue;
			}
			if (arg.StartsWith("-code-file:"))
			{
				codeFile = arg.Substring(11);
				continue;
			}
			if (arg.StartsWith("-out:"))
			{
				assemblyFile = arg.Substring(5);
				continue;
			}
			if (arg.StartsWith("-warn:"))
			{
				warnLevel = int.Parse(arg.Substring(6));
				continue;
			}
			if (arg.StartsWith("-r:"))
			{
				references.Add(Assembly.LoadFrom(arg.Substring(3)));
				continue;
			}
			if (arg.StartsWith("-reference:"))
			{
				references.Add(Assembly.LoadFrom(arg.Substring(11)));
				continue;
			}
			if (arg.StartsWith("-global-class:"))
			{
				globalClass = arg.Substring(14);
				continue;
			}
			if (arg.StartsWith("-namespace:"))
			{
				defaultNamespace = arg.Substring(11);
				continue;
			}
			if (arg.StartsWith("-dest:"))
			{
				destination = arg.Substring("-dest:".Length);
				continue;
			}
			if (arg.StartsWith("-import:"))
			{
				imports.AddRange(arg.Substring(8).Split(','));
				continue;
			}
			if (arg.StartsWith("-docs:"))
			{
				docs = arg.Substring("-docs:".Length);
				continue;
			}
			if (arg.StartsWith("-plugins:"))
			{
				foreach (string str in arg.Substring(9).Split(','))
				{
					Assembly a;
					try
					{
						a = Assembly.LoadFrom(str);
					}
					catch (FileNotFoundException)
					{
						a = Assembly.LoadFrom(Path.Combine(pluginDirectory, str));
					}
					plugins.Add(a);
				}
				continue;
			}
			if (arg.StartsWith("-"))
			{
				compilerOptions.Append(" /");
				compilerOptions.Append(arg.Substring(1));
				continue;
			}

			if (smokeLib == null)
			{
				smokeLib = arg;
				continue;
			}

			FileStream fs = new FileStream(arg, FileMode.Open);
			StreamReader sr = new StreamReader(fs);
			codeSnippets.Add(new CodeSnippetCompileUnit(sr.ReadToEnd()));
			sr.Close();
			fs.Close();
		}
		if (!string.IsNullOrEmpty(docs))
		{
			compilerOptions.Append(" /doc:" + Path.ChangeExtension(Path.GetFileName(assemblyFile), ".xml"));
		}

		if (smokeLib == null)
		{
			PrintHelp();
			return 1;
		}

		Smoke* smoke = InitSmoke(smokeLib);
		if (smoke == (Smoke*) 0)
		{
			return SmokeLoadingFailure;
		}

		List<ICustomTranslator> customTranslators = (from plugin in plugins
		                                             from type in plugin.GetTypes()
		                                             from iface in type.GetInterfaces()
		                                             where iface == typeof(ICustomTranslator)
		                                             select (ICustomTranslator) Activator.CreateInstance(type)).ToList();

		GeneratorData data = new GeneratorData(smoke, defaultNamespace, imports, references, destination, docs);
		data.GlobalSpaceClassName = globalClass;
		Translator translator = new Translator(data, customTranslators);

		foreach (IHookProvider provider in from type in plugins.SelectMany(plugin => plugin.GetTypes())
		                                   where type.GetInterfaces().Any(iface => iface == typeof(IHookProvider))
		                                   select (IHookProvider) Activator.CreateInstance(type))
		{
			provider.Translator = translator;
			provider.Data = data;
			provider.RegisterHooks();
		}

		ClassesGenerator classgen = new ClassesGenerator(data, translator);
		Console.Error.WriteLine("Generating CodeCompileUnit...");
		classgen.Run();
		DestroySmoke((IntPtr) smoke);

		Dictionary<string, string> providerOptions = new Dictionary<string, string>();
		providerOptions.Add("CompilerVersion", "v4.0");
		CodeDomProvider csharp = new CSharpCodeProvider(providerOptions);
		if (codeFile != string.Empty)
		{
			FileStream fs = new FileStream(codeFile, FileMode.Create);
			StreamWriter sw = new StreamWriter(fs);

			Console.Error.WriteLine("Generating code...");
			CodeGeneratorOptions cgo = new CodeGeneratorOptions();
			csharp.GenerateCodeFromCompileUnit(data.CompileUnit, sw, cgo);
			sw.Close();
			fs.Close();
		}

		if (codeOnly)
		{
			if (codeFile == string.Empty)
			{
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
		cp.ReferencedAssemblies.Add(typeof(Regex).Assembly.Location);
		cp.ReferencedAssemblies.Add(typeof(ExtensionAttribute).Assembly.Location);
		foreach (Assembly assembly in references)
		{
			cp.ReferencedAssemblies.Add(assembly.Location);
		}
		CompilerResults cr = csharp.CompileAssemblyFromDom(cp, codeSnippets.ToArray());

		bool errorsOccured = false;
		foreach (CompilerError error in cr.Errors)
		{
			if (!error.IsWarning)
				errorsOccured = true;
			Console.Error.WriteLine(error);
		}

		if (errorsOccured)
		{
			Console.Error.WriteLine("Errors occured. No assembly was generated.");
			return CompilationError;
		}
		Console.Error.WriteLine("Done.");
		return NoError;
	}
}
