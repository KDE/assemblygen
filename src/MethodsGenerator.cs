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
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom;
using Microsoft.CSharp;

public unsafe delegate void MethodHook(Smoke *smoke, Smoke.Method *smokeMethod, CodeMemberMethod cmm, CodeTypeDeclaration typeDecl);

public unsafe class MethodsGenerator
{
	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly CodeTypeDeclaration type;
	private readonly Smoke.Class* smokeClass;
	private readonly List<CodeMemberMethod> setters = new List<CodeMemberMethod>();
	private readonly List<CodeMemberMethod> setMethods = new List<CodeMemberMethod>();
	private readonly List<CodeMemberMethod> nonSetters = new List<CodeMemberMethod>();

	private static readonly Regex qMethodExp = new Regex("^[a-z][A-Z]");

	private static readonly List<string> binaryOperators = new List<string>
		{
	                                                       		"!=",
	                                                       		"==",
	                                                       		"%",
	                                                       		"&",
	                                                       		"*",
	                                                       		"+",
	                                                       		"-",
	                                                       		"/",
	                                                       		"<",
	                                                       		"<=",
	                                                       		">",
	                                                       		">=",
	                                                       		"^",
	                                                       		"|"
	                                                       	};

	private static readonly List<string> unaryOperators = new List<string>
		{
	                                                      		"!",
	                                                      		"~",
	                                                      		"+",
	                                                      		"++",
	                                                      		"-",
	                                                      		"--"
	                                                      	};

	private static readonly List<string> unsupportedOperators = new List<string>()
	                                                            	{
	                                                            		"=",
	                                                            		"->",
	                                                            		"+=",
	                                                            		"-=",
	                                                            		"/=",
	                                                            		"*=",
	                                                            		"%=",
	                                                            		"^=",
	                                                            		"&=",
	                                                            		"|=",
	                                                            		"[]",
	                                                            		"()"
	                                                            	};

	public MethodsGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type, Smoke.Class* klass)
	{
		this.data = data;
		this.translator = translator;
		this.type = type;
		this.smokeClass = klass;
	}

	private bool m_internalImplementation;
	private static readonly CodeDomProvider provider = new CSharpCodeProvider();

	public bool InternalImplementation
	{
		get { return m_internalImplementation; }
		set { m_internalImplementation = value; }
	}

	public static CodeDomProvider Provider
	{
		get { return provider; }
	}

	public static event MethodHook PreMethodBodyHooks;
	public static event MethodHook PostMethodBodyHooks;

	private static bool MethodOverrides(Smoke* smoke, Smoke.Method* method, out MemberAttributes access, out bool foundInInterface)
	{
		access = MemberAttributes.Public;
		foundInInterface = false;

		if (smoke->inheritanceList[smoke->classes[method->classId].parents] == 0)
		{
			return false;
		}

		long id = method - smoke->methods;
		Smoke.ModuleIndex methodModuleIndex = new Smoke.ModuleIndex(smoke, (short) id);

		Smoke.Method* firstMethod = (Smoke.Method*) IntPtr.Zero;
		short* firstParent = smoke->inheritanceList + smoke->classes[method->classId].parents;

		for (short* parent = firstParent; *parent > 0; parent++)
		{
			if (firstMethod != (Smoke.Method*) IntPtr.Zero && !foundInInterface)
			{
				// already found a method in the first parent class
				break;
			}

			// Do this with linq... there's probably room for optimization here.
			// Select virtual and pure virtual methods from superclasses.
			var inheritedVirtuals = from key in smoke->FindAllMethods(*parent, true).Keys
			                        where ((key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_virtual) > 0
			                               ||
			                               (key.smoke->methods[key.index].flags & (ushort) Smoke.MethodFlags.mf_purevirtual) > 0)
			                        select key;

			foreach (Smoke.ModuleIndex mi in inheritedVirtuals)
			{
				Smoke.Method* meth = mi.smoke->methods + mi.index;

				if (SmokeMethodEqualityComparer.DefaultEqualityComparer.Equals(methodModuleIndex, mi))
				{
					if ((meth->flags & (uint) Smoke.MethodFlags.mf_protected) > 0)
					{
						access = MemberAttributes.Family;
					}
					else
					{
						access = MemberAttributes.Public;
					}

					// don't return here - we need the access of the method in the topmost superclass
					firstMethod = meth;
					if (parent != firstParent)
					{
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

	public CodeMemberMethod GenerateBasicMethodDefinition(Smoke* smoke, Smoke.Method* method)
	{
		string cppSignature = smoke->GetMethodSignature(method);
		return this.GenerateBasicMethodDefinition(smoke, method, cppSignature, null);
	}

	public CodeMemberMethod GenerateBasicMethodDefinition(Smoke* smoke, Smoke.Method* method, string cppSignature,
	                                                      CodeTypeReference iface)
	{
		// do we actually want that method?
		string className = ByteArrayManager.GetString(smokeClass->className);
		string completeSignature = className + "::" + cppSignature;
		if (translator.ExcludedMethods.Any(regex => regex.IsMatch(completeSignature)))
		{
			return null;
		}

		CodeParameterDeclarationExpressionCollection args = new CodeParameterDeclarationExpressionCollection();
		int count = 1;
		bool isRef;

		// make instance operators static and bring the arguments in the correct order
		string methName = ByteArrayManager.GetString(smoke->methodNames[method->name]);
		bool isOperator = false;
		string explicitConversionType = null;
		if (methName.StartsWith("operator"))
		{
			string op = methName.Substring(8);
			if (unsupportedOperators.Contains(op))
			{
				// not supported
				Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
				return null;
			}

			if (op == "<<")
			{
				methName = "Write";
			}
			else if (op == ">>")
			{
				methName = "Read";
			}

			// binary/unary operator
			if (binaryOperators.Contains(op) || unaryOperators.Contains(op))
			{
				// instance operator
				if (smoke->classes[method->classId].size > 0)
				{
					if (op == "*" && method->numArgs == 0 || (op == "++" || op == "--") && method->numArgs == 1)
					{
						// dereference operator and postfix in-/decrement operator are not supported
						Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
						return null;
					}

					try
					{
						CodeParameterDeclarationExpression exp =
							new CodeParameterDeclarationExpression(translator.CppToCSharp(className, out isRef), "one");
						args.Add(exp);
					}
					catch (NotSupportedException)
					{
						Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
						return null;
					}
				}
				else
				{
					// global operator
					if (op == "*" && method->numArgs == 0 || (op == "++" || op == "--") && method->numArgs == 2)
					{
						// dereference operator and postfix in-/decrement operator are not supported
						Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
						return null;
					}
				}
				isOperator = true;
			}
			else if (op[0] == ' ')
			{
				// conversion operator
				explicitConversionType = op.Substring(1);
				if (explicitConversionType.Contains("QVariant"))
				{
					return null;
				}
				try
				{
					explicitConversionType = translator.CppToCSharp(explicitConversionType, out isRef).GetStringRepresentation();
					if (smoke->classes[method->classId].size > 0)
					{
						CodeParameterDeclarationExpression exp =
							new CodeParameterDeclarationExpression(translator.CppToCSharp(className, out isRef), "value");
						args.Add(exp);
					}
				}
				catch (NotSupportedException)
				{
					Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
					return null;
				}
				isOperator = true;
			}
		}

		// translate arguments
		string[] methodArgs = this.GetMethodArgs(smoke, method);
		for (short* typeIndex = smoke->argumentList + method->args; *typeIndex > 0; typeIndex++)
		{
			try
			{
				args.Add(this.GetArgument(smoke, typeIndex, methodArgs, args, ref count));
			}
			catch (NotSupportedException)
			{
				Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
				return null;
			}
		}
		this.RemovePreviousOverload(args, char.ToUpper(methName[0]) + methName.Substring(1));

		// translate return type
		CodeTypeReference returnType;
		try
		{
			returnType = translator.CppToCSharp(smoke->types + method->ret, out isRef);
		}
		catch (NotSupportedException)
		{
			Debug.Print("  |--Won't wrap method {0}::{1}", className, cppSignature);
			return null;
		}

		CodeMemberMethod cmm;
		if ((method->flags & (uint) Smoke.MethodFlags.mf_ctor) > 0)
		{
			cmm = new CodeConstructor();
			cmm.Attributes = 0; // initialize to 0 so we can do |=
			((CodeConstructor) cmm).ChainedConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
		}
		else
		{
			cmm = new CodeMemberMethod();
			cmm.Attributes = 0; // initialize to 0 so we can do |=

			string csName = methName;
			if (!isOperator && methName != "finalize" && !qMethodExp.IsMatch(methName))
			{
				// capitalize the first letter
				StringBuilder builder = new StringBuilder(csName);
				builder[0] = char.ToUpper(builder[0]);
				string tmp = builder.ToString();

				// If the new name clashes with a name of a type declaration, keep the lower-case name.
				var typesWithSameName = from member in data.GetAccessibleMembers(smokeClass)
				                        where member.Type == MemberTypes.NestedType && member.Name == tmp
				                        select member;

				var propertiesWithSameName = (from member in data.GetAccessibleMembers(smokeClass)
				                              where member.Type == MemberTypes.Property && member.Name == tmp
				                              select member).ToList();

				if (iface != null && propertiesWithSameName.Count() == 1 &&
				    (method->flags & (uint) Smoke.MethodFlags.mf_protected) == 0)
				{
					cmm.PrivateImplementationType = iface;
					csName = tmp;
				}
				else
				{
					if (propertiesWithSameName.Any())
					{
						if ((method->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0)
						{
							Debug.Print(
								"  |--Conflicting names: method/(type or property): {0} in class {1} - keeping original method name", tmp,
								className);
						}
						else
						{
							csName = tmp;
						}
					}
					else if (typesWithSameName.Any())
					{
						Debug.Print("  |--Conflicting names: method/classname: {0} in class {1} - keeping original method name", tmp,
						            className);
					}
					else
					{
						csName = tmp;
					}
				}
			}

			if (explicitConversionType != null)
			{
				cmm.Name = "explicit operator " + explicitConversionType;
				cmm.ReturnType = new CodeTypeReference(" ");
			}
			else
			{
				cmm.Name = csName;
				cmm.ReturnType = returnType;
			}
		}

		// for destructors we already have this stuff set
		if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) == 0)
		{
			// set access
			if ((method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0)
			{
				cmm.Attributes |= MemberAttributes.Family;
			}
			else
			{
				cmm.Attributes |= MemberAttributes.Public;
			}

			if (isOperator)
			{
				cmm.Attributes |= MemberAttributes.Final | MemberAttributes.Static;
			}
			else if (cmm.Name == "ToString" && args.Count == 0 && cmm.ReturnType.BaseType == "System.String")
			{
				cmm.Attributes = MemberAttributes.Public | MemberAttributes.Override;
			}
			else
			{
				if ((method->flags & (uint) Smoke.MethodFlags.mf_static) > 0)
				{
					cmm.Attributes |= MemberAttributes.Static;
				}
				else
				{
					// virtual/final
					MemberAttributes access;
					bool foundInInterface;
					bool isOverride = MethodOverrides(smoke, method, out access, out foundInInterface);

					// methods that have to be implemented from interfaces can't override anything
					if (iface == null && (isOverride = MethodOverrides(smoke, method, out access, out foundInInterface)))
					{
						cmm.Attributes = access | MemberAttributes.Override;
					}
					else if (foundInInterface)
					{
						cmm.Attributes = access;
					}

					if ((method->flags & (uint) Smoke.MethodFlags.mf_purevirtual) > 0)
					{
						if (!m_internalImplementation)
						{
							cmm.Attributes = (cmm.Attributes & ~MemberAttributes.ScopeMask) | MemberAttributes.Abstract;

							// The code generator doesn't like MemberAttributes.Abstract | MemberAttributes.Override being set.
							if (isOverride && !type.IsInterface)
							{
								cmm.ReturnType.BaseType = "override " + cmm.ReturnType.BaseType == "System.Void"
								                          	? "void"
								                          	: cmm.ReturnType.BaseType;
							}
						}
						else
						{
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
		}
		else
		{
			// hack, so we don't have to use CodeSnippetTypeMember to generator the destructor
			cmm.ReturnType = new CodeTypeReference(" ");
		}

		// add the parameters
		foreach (CodeParameterDeclarationExpression exp in args)
		{
			cmm.Parameters.Add(exp);
		}
		this.DistributeMethod(cmm);
		return cmm;
	}

	private void CorrectParameterNames(CodeMemberMethod cmm)
	{
		CodeMemberMethod baseVirtualMethod = this.GetBaseVirtualMethod(type, cmm);
		if (baseVirtualMethod != null)
		{
			RenameParameters(cmm, baseVirtualMethod.Parameters.Cast<CodeParameterDeclarationExpression>().Select(p => p.Name).ToList());
		}
	}

	private class ParameterTypeComparer : IEqualityComparer<CodeParameterDeclarationExpression>
	{
		public bool Equals(CodeParameterDeclarationExpression x, CodeParameterDeclarationExpression y)
		{
			return x.Type.BaseType == y.Type.BaseType;
		}

		public int GetHashCode(CodeParameterDeclarationExpression obj)
		{
			return obj.Type.GetHashCode();
		}
	}

	private CodeParameterDeclarationExpression GetArgument(Smoke* smoke, short* typeIndex, IList<string> methodArgs, IEnumerable args, ref int count)
	{
		bool isRef;
		CodeTypeReference argType = translator.CppToCSharp(smoke->types + *typeIndex, out isRef);
		string argName = this.GetArgName(smoke, typeIndex, methodArgs, ref count, isRef, argType);
		if (!argName.Contains(" = "))
		{
			RemoveDefaultValuesFromPreviousArgs(args);
		}
		CodeParameterDeclarationExpression arg = new CodeParameterDeclarationExpression(argType, argName);
		if (isRef)
		{
			arg.Direction = FieldDirection.Ref;
		}
		return arg;
	}

	private string GetArgName(Smoke* smoke, short* typeIndex, IList<string> methodArgs, ref int count, bool isRef, CodeTypeReference argType)
	{
		if (methodArgs == null)
		{
			return "arg" + count++;
		}
		string arg = methodArgs[count++ - 1];
		int nameEnd = arg.IndexOf(' ');
		if (nameEnd > 0)
		{
			string defaultValue = arg.Substring(nameEnd + 3);
			arg = arg.Substring(0, nameEnd);
			if (isRef)
			{
				return arg;
			}
			// HACK
			if (argType.BaseType.EndsWith("NativeLong") || argType.BaseType.EndsWith("NativeULong"))
			{
				return arg;
			}
			arg = Provider.CreateEscapedIdentifier(arg);
			Smoke.TypeId typeId = (Smoke.TypeId) ((smoke->types + *typeIndex)->flags & (ushort) Smoke.TypeFlags.tf_elem);
			switch (typeId)
			{
				case Smoke.TypeId.t_voidp:
				case Smoke.TypeId.t_class:
					switch (argType.BaseType)
					{
						case "System.Int64":
							return arg + " = 0";
						case "System.IntPtr":
							return arg + " = new IntPtr()";
					}
					if (argType.BaseType.Contains("QObjectWrapOption"))
					{
						return arg + " = 0";
					}
					switch (defaultValue)
					{
						case "0":
							return arg + " = null";
						case "QString()":
							return arg + " = \"\"";
					}
					break;
				case Smoke.TypeId.t_int:
					if (char.IsLetter(defaultValue[0]))
					{
						int indexOfColon = defaultValue.IndexOf("::", StringComparison.Ordinal);
						string containingType;
						string enumMember;
						string additional;
						if (indexOfColon > 0)
						{
							containingType = defaultValue.Substring(0, indexOfColon);
							Match match = Regex.Match(defaultValue, @"::(\w+)(.*)");
							enumMember = match.Groups[1].Value;
							additional = match.Groups[2].Value;
						}
						else
						{
							containingType = this.type.Name;
							enumMember = defaultValue;
							additional = string.Empty;
						}
						string enumType = (from keyValue in this.data.EnumTypeMap
						                   where keyValue.Value.IsEnum && keyValue.Key.StartsWith(containingType + "::")
						                   from CodeTypeMember member in keyValue.Value.Members
						                   where member.Name == enumMember
						                   select keyValue.Value.Name).FirstOrDefault() ??
						                  (from referencedType in this.data.ReferencedTypeMap.Values
						                   where referencedType.IsEnum && referencedType.FullName.Contains("." + containingType + "+")
						                   from FieldInfo member in referencedType.GetFields(BindingFlags.Public | BindingFlags.Static)
						                   where member.Name == enumMember
						                   select referencedType.Name).FirstOrDefault();
						if (string.IsNullOrEmpty(enumType))
						{
							var result = (from CodeTypeReference baseType in this.type.BaseTypes
							              where this.data.CSharpTypeMap.ContainsKey(baseType.BaseType)
							              let baseTypeDeclaration =
								              this.data.CSharpTypeMap[baseType.BaseType]
							              from CodeTypeDeclaration member in
								              baseTypeDeclaration.Members.OfType<CodeTypeDeclaration>()
							              where member.IsEnum
							              from CodeTypeMember enumValue in member.Members
							              where enumValue.Name == enumMember
							              select new KeyValuePair<string, string>(baseType.BaseType, member.Name)).FirstOrDefault();
							containingType = result.Key;
							enumType = result.Value;
						}
						return arg + " = (int) " + GetEnumMembers(string.Format("{0}.{1}", containingType, enumType), enumMember) + additional;
					}
					goto default;
				case Smoke.TypeId.t_uint:
				case Smoke.TypeId.t_enum:
					if (defaultValue == "0" || defaultValue.EndsWith("()"))
					{
						return arg + " = 0";
					}
					if (char.IsLetter(defaultValue[0]))
					{
						return arg + " = " + GetEnumMembers(argType.BaseType, defaultValue);
					}
					goto default;
				case Smoke.TypeId.t_char:
					break;
				default:
					return arg + " = " + defaultValue;
			}
		}
		return arg;
	}

	private void RemovePreviousOverload(CodeParameterDeclarationExpressionCollection args, string methodName)
	{
		if (methodName == type.Name)
		{
			methodName = ".ctor";
		}
		if (args.Count > 0 && args[args.Count - 1].Name.Contains(" = "))
		{
			IEnumerable<CodeParameterDeclarationExpression> parameters = args.Cast<CodeParameterDeclarationExpression>().Take(args.Count - 1);
			foreach (CodeMemberMethod method in (from method in this.type.Members.OfType<CodeMemberMethod>()
			                                     where method.Name == methodName && 
			                                           (method.Attributes & MemberAttributes.Override) == 0 &&
			                                           method.Parameters.Cast<CodeParameterDeclarationExpression>()
			                                                 .SequenceEqual(parameters, new ParameterTypeComparer())
			                                     select method).ToList())
			{
				this.type.Members.Remove(method);
			}
		}
	}

	private static void RemoveDefaultValuesFromPreviousArgs(IEnumerable args)
	{
		foreach (CodeParameterDeclarationExpression parameterDeclarationExpression in args)
		{
			int indexOfSpace = parameterDeclarationExpression.Name.IndexOf(' ');
			if (indexOfSpace > 0)
			{
				parameterDeclarationExpression.Name = parameterDeclarationExpression.Name.Substring(0, indexOfSpace);
			}
		}
	}

	private static string GetEnumMembers(string type, string defaultValue)
	{
		StringBuilder enumValueBuilder = new StringBuilder();
		foreach (string enumValue in defaultValue.Split('|'))
		{
			enumValueBuilder.Append(type);
			enumValueBuilder.Append('.');
			MatchCollection matches = Regex.Matches(enumValue, @"(\w+)");
			enumValueBuilder.Append(matches[matches.Count - 1].Groups[1].Value);
			enumValueBuilder.Append('|');
		}
		if (enumValueBuilder[enumValueBuilder.Length - 1] == '|')
		{
			enumValueBuilder.Remove(enumValueBuilder.Length - 1, 1);
		}
		return enumValueBuilder.ToString();
	}

	public CodeMemberMethod GenerateMethod(short idx, string mungedName)
	{
		return GenerateMethod(data.Smoke, idx, mungedName);
	}

	public CodeMemberMethod GenerateMethod(Smoke* smoke, short idx, string mungedName)
	{
		return GenerateMethod(smoke, smoke->methods + idx, mungedName, null);
	}

	public CodeMemberMethod GenerateMethod(Smoke* smoke, Smoke.Method* method, string mungedName, CodeTypeReference iface)
	{
		string cppSignature = smoke->GetMethodSignature(method);
		CodeMemberMethod cmm = GenerateBasicMethodDefinition(smoke, method, cppSignature, iface);
		if (cmm == null)
		{
			return null;
		}

		// put the method into the correct type
		CodeTypeDeclaration containingType = type;
		if (cmm.Name.StartsWith("operator") || cmm.Name.StartsWith("explicit "))
		{
			if (!data.CSharpTypeMap.TryGetValue(cmm.Parameters[0].Type.GetStringRepresentation(), out containingType))
			{
				if (cmm.Parameters.Count < 2 ||
				    !data.CSharpTypeMap.TryGetValue(cmm.Parameters[1].Type.GetStringRepresentation(), out containingType))
				{
					Debug.Print("  |--Can't find containing type for {0} - skipping", cppSignature);
				}
				return null;
			}
		}

		// already implemented?
		if (containingType.HasMethod(cmm))
		{
			if (iface == null || (method->flags & (uint) Smoke.MethodFlags.mf_protected) > 0)
			{
				// protected methods are not available in interfaces
				Debug.Print("  |--Skipping already implemented method {0}", cppSignature);
				return null;
			}
			else
			{
				cmm.PrivateImplementationType = iface;
			}
		}

		if (PreMethodBodyHooks != null)
		{
			PreMethodBodyHooks(smoke, method, cmm, containingType);
		}

		// do we have pass-by-ref parameters?
		bool generateInvokeForRefParams = cmm.Parameters.Cast<CodeParameterDeclarationExpression>().Any(expr => expr.Direction == FieldDirection.Ref);

		// generate the SmokeMethod attribute
		CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeMethod",
		                                                             new CodeAttributeArgument(
		                                                             	new CodePrimitiveExpression(cppSignature)));
		cmm.CustomAttributes.Add(attr);

		// choose the correct 'interceptor'
		CodeMethodInvokeExpression invoke;
		if ((cmm.Attributes & MemberAttributes.Static) == MemberAttributes.Static)
		{
			invoke = new CodeMethodInvokeExpression(SmokeSupport.staticInterceptor_Invoke);
		}
		else
		{
			invoke = new CodeMethodInvokeExpression(SmokeSupport.interceptor_Invoke);
		}

		// first pass the munged name, then the C++ signature
		invoke.Parameters.Add(new CodePrimitiveExpression(mungedName));
		invoke.Parameters.Add(new CodePrimitiveExpression(cppSignature));

		// retrieve the return type
		CodeTypeReference returnType;
		if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) > 0)
		{
			// destructor
			returnType = new CodeTypeReference(typeof(void));
		}
		else if (cmm.Name.StartsWith("explicit operator "))
		{
			// strip 'explicit operator' from the name to get the return type
			returnType = new CodeTypeReference(cmm.Name.Substring(18));
		}
		else
		{
			returnType = cmm.ReturnType;
		}

		// add the return type
		invoke.Parameters.Add(new CodeTypeOfExpression(returnType));
		invoke.Parameters.Add(new CodePrimitiveExpression(generateInvokeForRefParams));
		invoke.Parameters.Add(new CodeVariableReferenceExpression("smokeArgs"));

		CodeArrayCreateExpression argsInitializer = new CodeArrayCreateExpression(typeof(object[]));

		// add the parameters
		foreach (CodeParameterDeclarationExpression param in cmm.Parameters)
		{
			argsInitializer.Initializers.Add(new CodeTypeOfExpression(param.Type));
			string argReference = param.Name;
			int indexOfSpace = argReference.IndexOf(' ');
			if (indexOfSpace > 0)
			{
				argReference = argReference.Substring(0, indexOfSpace);
			}
			argsInitializer.Initializers.Add(new CodeArgumentReferenceExpression(argReference));
		}

		CodeStatement argsStatement = new CodeVariableDeclarationStatement(typeof(object[]), "smokeArgs", argsInitializer);
		cmm.Statements.Add(argsStatement);

		// we have to call "CreateProxy()" in constructors
		if (cmm is CodeConstructor)
		{
			cmm.Statements.Add(
				new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "CreateProxy")));
		}

		// add the method call statement
		CodeStatement statement;

		if (!generateInvokeForRefParams)
		{
			if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0)
			{
				statement = new CodeMethodReturnStatement(new CodeCastExpression(returnType, invoke));
			}
			else
			{
				statement = new CodeExpressionStatement(invoke);
			}
			cmm.Statements.Add(statement);
		}
		else
		{
			if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0)
			{
				statement = new CodeVariableDeclarationStatement(returnType, "smokeRetval",
				                                                 new CodeCastExpression(returnType, invoke));
				cmm.Statements.Add(statement);
			}
			else
			{
				statement = new CodeExpressionStatement(invoke);
				cmm.Statements.Add(statement);
			}

			int i = 0;
			foreach (CodeParameterDeclarationExpression param in cmm.Parameters)
			{
				++i;
				if (param.Direction != FieldDirection.Ref)
				{
					continue;
				}
				cmm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(param.Name),
				                                           new CodeCastExpression(param.Type.BaseType,
				                                                                  new CodeArrayIndexerExpression(
				                                                                  	new CodeVariableReferenceExpression("smokeArgs"),
				                                                                  	new CodePrimitiveExpression(i*2 - 1)
				                                                                  	)
				                                           	)
				                   	));
			}

			if (method->ret > 0 && (method->flags & (uint) Smoke.MethodFlags.mf_ctor) == 0)
			{
				cmm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("smokeRetval")));
			}
		}

		if (PostMethodBodyHooks != null)
		{
			PostMethodBodyHooks(smoke, method, cmm, containingType);
		}

		containingType.Members.Add(cmm);

		if ((method->flags & (uint) Smoke.MethodFlags.mf_dtor) != 0)
		{
			containingType.BaseTypes.Add(new CodeTypeReference(typeof(IDisposable)));
			CodeMemberMethod dispose = new CodeMemberMethod();
			dispose.Name = "Dispose";
			dispose.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;
			dispose.Statements.AddRange(cmm.Statements);
			dispose.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(
			                                                   	new CodeTypeReferenceExpression("GC"), "SuppressFinalize",
			                                                   	new CodeThisReferenceExpression()
			                                                   	)));
			containingType.Members.Add(dispose);
		}
		this.CorrectParameterNames(cmm);
		this.DistributeMethod(cmm);
		return cmm;
	}

	private string[] GetMethodArgs(Smoke* smoke, Smoke.Method* method)
	{
		if (method->numArgs == 0)
		{
			return new string[0];
		}
		string className = ByteArrayManager.GetString(smoke->classes[method->classId].className);
		className = className.Substring(className.LastIndexOf(":") + 1);
		StringBuilder keyBuilder =
			new StringBuilder(className + "," + ByteArrayManager.GetString(smoke->methodNames[method->name]));
		for (short* typeIndex = smoke->argumentList + method->args; *typeIndex > 0; typeIndex++)
		{
			keyBuilder.Append(',');
			keyBuilder.Append(*typeIndex);
		}
		string key = keyBuilder.ToString();
		if (data.ArgumentNames.ContainsKey(key))
		{
			return data.ArgumentNames[key];
		}
		string argNamesFile = data.GetArgNamesFile(smoke);
		if (File.Exists(argNamesFile))
		{
			foreach (string[] strings in File.ReadAllLines(argNamesFile).Select(line => line.Split(';')))
			{
				data.ArgumentNames[strings[0]] = strings[1].Split(',');
			}
			return data.ArgumentNames[key];
		}
		return null;
	}

	public void GenerateProperties()
	{
		List<CodeMemberMethod> methods = type.Members.OfType<CodeMemberMethod>().ToList();
		this.GenerateProperties(methods, this.setters, false);
		this.GenerateProperties(methods, this.setMethods, true);
	}

	private void GenerateProperties(ICollection<CodeMemberMethod> methods, IEnumerable<CodeMemberMethod> settersToUse, bool readOnly)
	{
		foreach (CodeMemberMethod setter in settersToUse)
		{
			if (!this.type.Members.Contains(setter))
			{
				continue;
			}
			string afterSet = setter.Name.Substring(3);
			for (int i = this.nonSetters.Count - 1; i >= 0; i--)
			{
				CodeMemberMethod getter = this.nonSetters[i];
				if (!this.type.Members.Contains(getter))
				{
					continue;
				}
				if (string.Compare(getter.Name, afterSet, StringComparison.OrdinalIgnoreCase) == 0 &&
				    getter.Parameters.Count == 0 && getter.ReturnType.BaseType == setter.Parameters[0].Type.BaseType &&
				    (getter.Attributes & MemberAttributes.Public) == (setter.Attributes & MemberAttributes.Public) &&
				    !methods.Any(m => m != getter && string.Compare(getter.Name, m.Name, StringComparison.OrdinalIgnoreCase) == 0))
				{
					methods.Remove(getter);
					if (this.type.IsInterface)
					{
						CodeSnippetTypeMember property = new CodeSnippetTypeMember();
						property.Name = getter.Name;
						string template = readOnly ? "        {0} {1} {{ get; }}" : "        {0} {1} {{ get; set; }}";
						property.Text = string.Format(template, getter.ReturnType.BaseType, getter.Name);
						this.type.Members.Add(property);
						this.type.Members.Remove(getter);
						if (!readOnly)
						{
							this.type.Members.Remove(setter);							
						}
					}
					else
					{
						this.GenerateProperty(getter, readOnly ? null : setter);
					}
					goto next;
				}
			}
			CodeTypeMember baseVirtualProperty = this.GetBaseVirtualProperty(this.type, afterSet);
			if (!this.type.IsInterface && baseVirtualProperty != null)
			{
				CodeMemberMethod getter = new CodeMemberMethod { Name = baseVirtualProperty.Name };
				getter.ReturnType = setter.Parameters[0].Type;
				getter.Statements.Add(new CodeSnippetStatement(string.Format("            return base.{0};", afterSet)));
				this.GenerateProperty(getter, readOnly ? null : setter);
			}
			next:
			;
		}
		foreach (CodeMemberMethod nonSetter in this.nonSetters)
		{
			CodeTypeMember baseVirtualProperty = this.GetBaseVirtualProperty(this.type, nonSetter.Name);
			if (!this.type.IsInterface && baseVirtualProperty != null)
			{
				bool isReadOnly = (baseVirtualProperty is CodeMemberProperty && !((CodeMemberProperty) baseVirtualProperty).HasSet) ||
								  !Regex.IsMatch(((CodeSnippetTypeMember) baseVirtualProperty).Text, @"\s+set\s*{");
				if (readOnly == isReadOnly)
				{
					CodeMemberMethod setter = new CodeMemberMethod { Name = baseVirtualProperty.Name };
					setter.Statements.Add(new CodeSnippetStatement(string.Format("            base.{0} = value;", nonSetter.Name)));
					this.GenerateProperty(nonSetter, readOnly ? null : setter);
				}
			}
		}
	}

	private void GenerateProperty(CodeMemberMethod getter, CodeMemberMethod setter = null)
	{
		CodeCommentStatementCollection comments = new CodeCommentStatementCollection();
		comments.AddRange(getter.Comments);
		getter.Comments.Clear();
		if (setter != null)
		{
			comments.AddRange(setter.Comments);
			setter.Comments.Clear();
		}
		if (type.Members.OfType<CodeSnippetTypeMember>().All(
				p => string.Compare(getter.Name, p.Name, StringComparison.OrdinalIgnoreCase) != 0) &&
			type.Members.OfType<CodeMemberProperty>().All(
				p => string.Compare(getter.Name, p.Name, StringComparison.OrdinalIgnoreCase) != 0))
		{
			CodeMemberProperty property = new CodeMemberProperty();
			property.Name = getter.Name;
			property.Type = getter.ReturnType;
			property.Attributes = setter == null ? getter.Attributes : setter.Attributes;
			property.GetStatements.AddRange(getter.Statements);
			if (setter != null)
			{
				CodeVariableDeclarationStatement variableStatement =
					setter.Statements.OfType<CodeVariableDeclarationStatement>().First();
				CodeArrayCreateExpression arrayExpression = (CodeArrayCreateExpression) variableStatement.InitExpression;
				CodeArgumentReferenceExpression argExpression =
					arrayExpression.Initializers.OfType<CodeArgumentReferenceExpression>().First();
				argExpression.ParameterName = "value";
				property.SetStatements.AddRange(setter.Statements);
			}
			CodeSnippetTypeMember completeProperty = AddAttributes(getter, setter, property);
			AddComments(completeProperty, comments, setter == null);
			type.Members.Add(completeProperty);
			if (type.Members.Contains(getter))
			{
				type.Members.Remove(getter);
			}
			if (setter != null && this.type.Members.Contains(setter))
			{
				type.Members.Remove(setter);
			}
		}
	}

	private static CodeSnippetTypeMember AddAttributes(CodeTypeMember getter, CodeTypeMember setter, CodeTypeMember property)
	{
		CodeSnippetTypeMember propertySnippet = new CodeSnippetTypeMember();
		AddAttributes(getter, property, propertySnippet, @"{(\s*)get", @"{{$1{0}$1get");
		if (setter != null)
		{
			AddAttributes(setter, property, propertySnippet, @"}(\s*)set", @"}}$1{0}$1set");
		}
		return propertySnippet;
	}

	private static void AddAttributes(CodeTypeMember method, CodeTypeMember property, CodeSnippetTypeMember propertySnippet,
	                                  string findRegex, string replaceRegex)
	{
		if (method.CustomAttributes.Count > 0)
		{
			using (StringWriter writer = new StringWriter())
			{
				string propertyCode = string.IsNullOrEmpty(propertySnippet.Name)
					                      ? GetMemberCode(property, writer)
					                      : propertySnippet.Text;
				writer.GetStringBuilder().Length = 0;
				string getterCode = GetMemberCode(method, writer);
				string attribute = string.Format(replaceRegex, Regex.Match(getterCode, @"(\[.+\])").Groups[1].Value);
				string propertyWithAttribute = Regex.Replace(propertyCode, findRegex, attribute);
				propertySnippet.Name = property.Name;
				propertySnippet.Attributes = property.Attributes;
				propertySnippet.Text = propertyWithAttribute;
			}
		}
	}

	private static string GetMemberCode(CodeTypeMember member, TextWriter writer)
	{
		CodeTypeDeclaration dummyType = new CodeTypeDeclaration();
		dummyType.Members.Add(member);
		Provider.GenerateCodeFromType(dummyType, writer, null);
		string propertyCode = writer.ToString();
		StringBuilder propertyCodeBuilder = new StringBuilder(propertyCode);
		propertyCodeBuilder.Remove(0, propertyCode.IndexOf('{') + 1);
		propertyCodeBuilder.Length -= propertyCode.Length - propertyCode.LastIndexOf('}');
		propertyCode = propertyCodeBuilder.ToString();
		return propertyCode;
	}

	private CodeTypeMember GetBaseVirtualProperty(CodeTypeDeclaration containingType, string propertyName)
	{
		return (from CodeTypeReference baseType in containingType.BaseTypes
		        where data.CSharpTypeMap.ContainsKey(baseType.BaseType)
		        select GetBaseVirtualProperty(data.CSharpTypeMap[baseType.BaseType], propertyName)).FirstOrDefault() ??
		       (from CodeTypeReference baseType in containingType.BaseTypes
		        let @interface = data.InterfaceTypeMap.Values.FirstOrDefault(t => t.Name == baseType.BaseType)
		        where @interface != null
		        select GetBaseVirtualProperty(@interface, propertyName)).FirstOrDefault() ??
		       (containingType != type
		        	? (containingType.IsInterface
		        	   	? (from CodeTypeMember prop in containingType.Members
		        	   	   where (prop is CodeSnippetTypeMember || prop is CodeMemberProperty) &&
		        	   	         string.Compare(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase) == 0
		        	   	   select prop).FirstOrDefault()
		        	   	: (from CodeTypeMember prop in containingType.Members
		        	   	   where (prop is CodeSnippetTypeMember || prop is CodeMemberProperty) &&
		        	   	         string.Compare(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase) == 0 &&
		        	   	         (prop.Attributes & MemberAttributes.Final) == 0
		        	   	   select prop).FirstOrDefault())
		        	: null);
	}

	private CodeMemberMethod GetBaseVirtualMethod(CodeTypeDeclaration containingType, CodeMemberMethod method)
	{
		return (from CodeTypeReference baseType in containingType.BaseTypes
				where data.CSharpTypeMap.ContainsKey(baseType.BaseType)
				select this.GetBaseVirtualMethod(data.CSharpTypeMap[baseType.BaseType], method)).FirstOrDefault() ??
			   (from CodeTypeReference baseType in containingType.BaseTypes
				let @interface = data.InterfaceTypeMap.Values.FirstOrDefault(t => t.Name == baseType.BaseType)
				where @interface != null
				select this.GetBaseVirtualMethod(@interface, method)).FirstOrDefault() ??
			   (containingType != type
					? (containingType.IsInterface
						   ? (from CodeMemberMethod memberMethod in containingType.Members.OfType<CodeMemberMethod>()
							  where memberMethod.Name == method.Name && memberMethod.Parameters.Count == method.Parameters.Count &&
									memberMethod.Parameters.Cast<CodeParameterDeclarationExpression>().SequenceEqual(
										method.Parameters.Cast<CodeParameterDeclarationExpression>(), new ParameterTypeComparer())
							  select memberMethod).FirstOrDefault()
						   : (from CodeMemberMethod memberMethod in containingType.Members.OfType<CodeMemberMethod>()
							  where memberMethod.Name == method.Name && memberMethod.Parameters.Count == method.Parameters.Count &&
									(memberMethod.Attributes & MemberAttributes.Final) == 0 &&
									memberMethod.Parameters.Cast<CodeParameterDeclarationExpression>().SequenceEqual(
										method.Parameters.Cast<CodeParameterDeclarationExpression>(), new ParameterTypeComparer())
							  select memberMethod).FirstOrDefault())
					: null);
	}

	private static void AddComments(CodeTypeMember completeProperty, CodeCommentStatementCollection comments, bool readOnly)
	{
		if (!readOnly)
		{
			for (int i = comments.Count - 2; i >= 1; i--)
			{
				string comment = comments[i].Comment.Text;
				if (comment == "<summary>" || comment == "</summary>" || comment.StartsWith("<para>See also "))
				{
					comments.RemoveAt(i);
				}
			}	
		}
		completeProperty.Comments.AddRange(comments);
	}

	public static void RenameParameters(CodeMemberMethod method, IList<string> args)
	{
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			CodeParameterDeclarationExpression parameter = method.Parameters[i];
			string oldArgName = parameter.Name;
			int index = oldArgName.IndexOf(" = ", StringComparison.Ordinal);
			string nameOnly = index > 0 ? oldArgName.Substring(0, index) : oldArgName;
			if (nameOnly.Length == 4 && nameOnly.StartsWith("arg") && char.IsDigit(nameOnly[3]))
			{
				string name = Regex.Match(args[i], @"(?<name>\w+)(\s*=\s*\w+)?$").Groups["name"].Value;
				if (!string.IsNullOrEmpty(name))
				{
					parameter.Name = name + (index > 0 ? oldArgName.Substring(index) : string.Empty);
					IEnumerable<CodeStatement> statements = method.Statements.Cast<CodeStatement>().ToList();
					var variableDeclarationStatement = statements.OfType<CodeVariableDeclarationStatement>().First();
					var arrayCreateExpression = (CodeArrayCreateExpression) variableDeclarationStatement.InitExpression;
					var argumentReferenceExpression = (CodeArgumentReferenceExpression) arrayCreateExpression.Initializers[2 * i + 1];
					argumentReferenceExpression.ParameterName = parameter.Name;
					foreach (var variable in from assignStatement in statements.OfType<CodeAssignStatement>()
											 let var = (CodeVariableReferenceExpression) assignStatement.Left
											 where var.VariableName == oldArgName
											 select var)
					{
						variable.VariableName = parameter.Name;
					}
				}
			}
		}
	}

	private void DistributeMethod(CodeMemberMethod method)
	{
		if (method != null)
		{
			if (method.Name.StartsWith("Set") && method.ReturnType.BaseType == "System.Void")
			{
				if (method.Parameters.Count == 1)
				{
					setters.Add(method);
				}
				else
				{
					setMethods.Add(method);
				}
			}
			else
			{
				nonSetters.Add(method);
			}
		}
	}
}
