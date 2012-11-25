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
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.Linq;
using System.Text.RegularExpressions;

public unsafe class PropertyGenerator
{
	private class Property
	{
		public string Name;
		public string OriginalType;
		public string Type;
		public bool IsWritable;
		public bool IsEnum;

		public Property(string name, string originalType, string type, bool writable, bool isEnum)
		{
			Name = name;
			OriginalType = originalType;
			Type = type;
			IsWritable = writable;
			IsEnum = isEnum;
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	private delegate void AddProperty(
		string name, string originalType, string type, [MarshalAs(UnmanagedType.U1)] bool writable,
		[MarshalAs(UnmanagedType.U1)] bool isEnum);

	[DllImport("qyotogenerator-native", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	private static extern bool GetProperties(Smoke* smoke, short classId, AddProperty addProp);

	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly Dictionary<CodeTypeDeclaration, List<string>> documentation;

	public PropertyGenerator(GeneratorData data, Translator translator, Dictionary<CodeTypeDeclaration, List<string>> documentation)
	{
		this.data = data;
		this.translator = translator;
		this.documentation = documentation;
	}

	public void Run()
	{
		for (short classId = 1; classId <= data.Smoke->numClasses; classId++)
		{
			Smoke.Class* klass = data.Smoke->classes + classId;
			if (klass->external)
				continue;

			List<Property> props = new List<Property>();
			if (!GetProperties(data.Smoke, classId,
				               (name, originalType, typeName, writable, isEnum) =>
				               props.Add(new Property(name, originalType, typeName, writable, isEnum))))
			{
				continue;
			}

			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];
			string className = ByteArrayManager.GetString(klass->className);

			foreach (Property prop in props)
			{
				CodeMemberProperty cmp = new CodeMemberProperty();

				try
				{
					bool isRef;
					short id = data.Smoke->IDType(prop.Type);
					if (id > 0)
					{
						cmp.Type = translator.CppToCSharp(data.Smoke->types + id, out isRef);
					}
					else
					{
						if (!prop.Type.Contains("::"))
						{
							id = data.Smoke->IDType(className + "::" + prop.Type);
							if (id > 0)
							{
								cmp.Type = translator.CppToCSharp(data.Smoke->types + id, out isRef);
							}
							else
							{
								cmp.Type = translator.CppToCSharp(prop.Type, out isRef);
							}
						}
						cmp.Type = translator.CppToCSharp(prop.Type, out isRef);
					}
				}
				catch (NotSupportedException)
				{
					Debug.Print("  |--Won't wrap Property {0}::{1}", className, prop.Name);
					continue;
				}

				if (documentation.ContainsKey(type))
				{
					IList<string> docs = documentation[type];
					for (int i = 0; i < docs.Count; i++)
					{
						Match match = Regex.Match(docs[i],
												  prop.Name + " : (const )?" + prop.OriginalType +
												  @"\n(?<docs>This.*?)\nAccess functions:", RegexOptions.Singleline);
						if (match.Success)
						{
							Util.FormatComment(match.Groups["docs"].Value, cmp, i > 0);
							break;
						}
					}
				}
				cmp.Name = prop.Name;
				// capitalize the first letter
				StringBuilder builder = new StringBuilder(cmp.Name);
				builder[0] = char.ToUpper(builder[0]);
				string capitalized = builder.ToString();

				// If the new name clashes with a name of a type declaration, keep the lower-case name (or even make the name lower-case).
				var typesWithSameName = from member in data.GetAccessibleMembers(data.Smoke->classes + classId)
				                        where (member.Type == MemberTypes.NestedType
				                               || member.Type == MemberTypes.Method)
				                              && member.Name == capitalized
				                        select member;
				if (typesWithSameName.Any())
				{
					Debug.Print(
						"  |--Conflicting names: property/(type or method): {0} in class {1} - keeping original property name",
						capitalized, className);

					if (capitalized == cmp.Name)
					{
						builder[0] = char.ToLower(builder[0]);
						cmp.Name = builder.ToString(); // lower case the property if necessary
					}
				}
				else
				{
					cmp.Name = capitalized;
				}

				cmp.HasGet = true;
				cmp.HasSet = prop.IsWritable;
				cmp.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;

				cmp.CustomAttributes.Add(new CodeAttributeDeclaration("Q_PROPERTY",
				                                                      new CodeAttributeArgument(
				                                                      	new CodePrimitiveExpression(prop.OriginalType)),
				                                                      new CodeAttributeArgument(
				                                                      	new CodePrimitiveExpression(prop.Name))));

				// ===== get-method =====
				short getterMapId = FindQPropertyGetAccessorMethodMapId(classId, prop, capitalized);
				if (getterMapId == 0)
				{
					Debug.Print("  |--Missing 'get' method for property {0}::{1} - using QObject.Property()", className, prop.Name);
					cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type,
					                                                                           new CodeMethodInvokeExpression(
					                                                                           	new CodeThisReferenceExpression(),
					                                                                           	"Property",
					                                                                           	new CodePrimitiveExpression(prop.Name)))));
				}
				else
				{
					Smoke.MethodMap* map = data.Smoke->methodMaps + getterMapId;
					short getterId = map->method;
					if (getterId < 0)
					{
						// simply choose the first (i.e. non-const) version if there are alternatives
						getterId = data.Smoke->ambiguousMethodList[-getterId];
					}

					Smoke.Method* getter = data.Smoke->methods + getterId;
					if (getter->classId != classId)
					{
						// The actual get method is defined in a parent class - don't create a property for it.
						continue;
					}
					if ((getter->flags & (uint) Smoke.MethodFlags.mf_virtual) == 0
					    && (getter->flags & (uint) Smoke.MethodFlags.mf_purevirtual) == 0)
					{
						cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(cmp.Type,
						                                                                           new CodeMethodInvokeExpression(
						                                                                           	SmokeSupport.interceptor_Invoke,
						                                                                           	new CodePrimitiveExpression(
						                                                                           		ByteArrayManager.GetString(
						                                                                           			data.Smoke->methodNames[getter->name
						                                                                           				])),
						                                                                           	new CodePrimitiveExpression(
						                                                                           		data.Smoke->GetMethodSignature(getter)),
						                                                                           	new CodeTypeOfExpression(cmp.Type),
						                                                                           	new CodePrimitiveExpression(false)))));
					}
					else
					{
						cmp.HasGet = false;
						if (!cmp.HasSet)
						{
							// the get accessor is virtual and there's no set accessor => continue
							continue;
						}
					}
				}

				// ===== set-method =====
				if (!prop.IsWritable)
				{
					// not writable? => continue
					type.Members.Add(cmp);
					continue;
				}

				char mungedSuffix;
				short setterMethId = FindQPropertySetAccessorMethodId(classId, prop, capitalized, out mungedSuffix);
				if (setterMethId == 0)
				{
					Debug.Print("  |--Missing 'set' method for property {0}::{1} - using QObject.SetProperty()", className, prop.Name);
					cmp.SetStatements.Add(new CodeExpressionStatement(
					                      	new CodeMethodInvokeExpression(new CodeThisReferenceExpression(), "SetProperty",
					                      	                               new CodePrimitiveExpression(prop.Name),
					                      	                               new CodeArgumentReferenceExpression("value"))));
				}
				else
				{
					Smoke.Method* setter = data.Smoke->methods + setterMethId;
					if (setter->classId != classId)
					{
						// defined in parent class, continue
						type.Members.Add(cmp);
						continue;
					}
					string setterName = ByteArrayManager.GetString(data.Smoke->methodNames[setter->name]);
					if (!cmp.HasGet)
					{
						// so the 'get' method is virtual - generating a property for only the 'set' method is a bad idea
						MethodsGenerator mg = new MethodsGenerator(data, translator, type, klass);
						mg.GenerateMethod(setterMethId, setterName + mungedSuffix);
						continue;
					}
					cmp.SetStatements.Add(new CodeExpressionStatement(
					                      	new CodeMethodInvokeExpression(SmokeSupport.interceptor_Invoke,
					                      	                               new CodePrimitiveExpression(setterName + mungedSuffix),
					                      	                               new CodePrimitiveExpression(
					                      	                               	this.data.Smoke->GetMethodSignature(setterMethId)),
					                      	                               new CodeTypeOfExpression(typeof(void)),
					                      	                               new CodePrimitiveExpression(false),
					                      	                               new CodeTypeOfExpression(cmp.Type),
					                      	                               new CodeArgumentReferenceExpression("value"))));
				}

				type.Members.Add(cmp);
			}
		}
	}

	private short FindQPropertyGetAccessorMethodMapId(short classId, Property prop, string capitalized)
	{
		short getterMapId = 0;
		string firstPrefixName = "is" + capitalized;
		string secondPrefixName = "has" + capitalized;
		short* parents = data.Smoke->inheritanceList + data.Smoke->classes[classId].parents;

		while (getterMapId == 0 && classId > 0)
		{
			short methNameId = data.Smoke->IDMethodName(prop.Name);
			getterMapId = data.Smoke->IDMethod(classId, methNameId);
			if (getterMapId == 0 && prop.Type == "bool")
			{
				// bool methods often begin with isFoo()
				methNameId = data.Smoke->IDMethodName(firstPrefixName);
				getterMapId = data.Smoke->IDMethod(classId, methNameId);

				if (getterMapId == 0)
				{
					// or hasFoo()
					methNameId = data.Smoke->IDMethodName(secondPrefixName);
					getterMapId = data.Smoke->IDMethod(classId, methNameId);
				}
			}
			classId = *(parents++);
		}
		return getterMapId;
	}

	private short FindQPropertySetAccessorMethodId(short classId, Property prop, string capitalized, out char mungedSuffix)
	{
		// guess munged suffix
		if (Util.IsPrimitiveType(prop.Type) || prop.IsEnum)
		{
			// scalar
			mungedSuffix = '$';
		}
		else if (prop.Type.Contains("<"))
		{
			// generic type
			mungedSuffix = '?';
		}
		else
		{
			mungedSuffix = '#';
		}
		char firstMungedSuffix = mungedSuffix;

		short setterMethId = 0;
		string name = "set" + capitalized;
		string otherPrefixName = "re" + prop.Name;
		short* parents = data.Smoke->inheritanceList + data.Smoke->classes[classId].parents;

		while (setterMethId == 0 && classId > 0)
		{
			mungedSuffix = firstMungedSuffix;
			setterMethId = TryMungedNames(classId, name, ref mungedSuffix);
			if (setterMethId == 0)
			{
				// try with 're' prefix
				mungedSuffix = firstMungedSuffix;
				setterMethId = TryMungedNames(classId, otherPrefixName, ref mungedSuffix);
			}
			classId = *(parents++);
		}
		return setterMethId;
	}

	private static char[] mungedSuffixes = {'#', '$', '?'};

	private short TryMungedNames(short classId, string name, ref char mungedSuffix)
	{
		int idx = Array.IndexOf(mungedSuffixes, mungedSuffix);
		int i = idx;
		do
		{
			// loop through the other elements, try various munged names
			mungedSuffix = mungedSuffixes[i];
			short methNameId = data.Smoke->IDMethodName(name + mungedSuffix);
			short methMapId = data.Smoke->IDMethod(classId, methNameId);
			if (methMapId == 0)
				continue;
			short methId = data.Smoke->methodMaps[methMapId].method;
			if (methId < 0)
			{
				for (short* id = data.Smoke->ambiguousMethodList + (-methId); *id > 0; id++)
				{
					///TODO: check parameters
					return *id;
				}
			}
			else
			{
				///TODO: check parameters
				return methId;
			}
		} while ((i = (i + 1)%mungedSuffixes.Length) != idx); // automatically moves from the end to the beginning
		return 0;
	}
}
