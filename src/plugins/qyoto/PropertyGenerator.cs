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

public unsafe class PropertyGenerator
{
	private class Property
	{
		public string Name;
		public string Type;
		public bool IsWritable;
		public bool IsEnum;

		public Property(string name, string type, bool writable, bool isEnum)
		{
			Name = name;
			Type = type;
			IsWritable = writable;
			IsEnum = isEnum;
		}
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	private delegate void AddProperty(
		string name, string type, [MarshalAs(UnmanagedType.U1)] bool writable,
		[MarshalAs(UnmanagedType.U1)] bool isEnum);

	[DllImport("qyotogenerator-native", CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.U1)]
	private static extern bool GetProperties(Smoke* smoke, short classId, AddProperty addProp);

	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly Documentation documentation;

	public PropertyGenerator(GeneratorData data, Translator translator, Documentation documentation)
	{
		this.data = data;
		this.translator = translator;
		this.documentation = documentation;
		this.PropertyMethods = new List<IntPtr>();
	}

	public ICollection<IntPtr> PropertyMethods { get; private set; }

	public void Run()
	{
		for (short classId = 1; classId <= data.Smoke->numClasses; classId++)
		{
			Smoke.Class* klass = data.Smoke->classes + classId;
			if (klass->external)
				continue;

			string className = ByteArrayManager.GetString(klass->className);
			IEnumerable<Property> props = this.GetProperties(classId, className);
			List<GeneratorData.InternalMemberInfo> members = data.GetAccessibleMembers(data.Smoke->classes + classId);

			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];

			foreach (Property prop in props)
			{
				CodeMemberProperty cmp = new CodeMemberProperty();

				try
				{
					bool isRef;
					short id = data.Smoke->IDType(prop.Type);
					if (id > 0)
					{
						cmp.Type = translator.CppToCSharp(data.Smoke->types + id, type, out isRef);
					}
					else
					{
						if (!prop.Type.Contains("::"))
						{
							id = data.Smoke->IDType(className + "::" + prop.Type);
							if (id > 0)
							{
								cmp.Type = translator.CppToCSharp(data.Smoke->types + id, type, out isRef);
							}
							else
							{
								cmp.Type = translator.CppToCSharp(prop.Type, type, out isRef);
							}
						}
						cmp.Type = translator.CppToCSharp(prop.Type, type, out isRef);
					}
				}
				catch (NotSupportedException)
				{
					Debug.Print("  |--Won't wrap Property {0}::{1}", className, prop.Name);
					continue;
				}
				this.DocumentProperty(type, prop, cmp);
				string capitalized = NameProperty(cmp, prop, type, members, className);

				cmp.HasGet = true;
				cmp.HasSet = prop.IsWritable;
				cmp.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;

				cmp.CustomAttributes.Add(new CodeAttributeDeclaration("Q_PROPERTY",
				                                                      new CodeAttributeArgument(
				                                                      	new CodePrimitiveExpression(prop.Type)),
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
						this.PropertyMethods.Add((IntPtr) getter);
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
						if ((getter->flags & (uint) Smoke.MethodFlags.mf_virtual) == (int) Smoke.MethodFlags.mf_virtual)
						{
							cmp.Attributes &= ~MemberAttributes.Final;
						}
						else
						{
							if ((getter->flags & (uint) Smoke.MethodFlags.mf_purevirtual) == (int) Smoke.MethodFlags.mf_purevirtual)
							{
								cmp.Attributes &= ~MemberAttributes.Final;
								cmp.Attributes |= MemberAttributes.Abstract;
							}
						}
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
					if ((setter->flags & (uint) Smoke.MethodFlags.mf_virtual) == (int) Smoke.MethodFlags.mf_virtual)
					{
						cmp.Attributes &= ~MemberAttributes.Final;
					}
					else
					{
						if ((setter->flags & (uint) Smoke.MethodFlags.mf_purevirtual) == (int) Smoke.MethodFlags.mf_purevirtual)
						{
							cmp.Attributes &= ~MemberAttributes.Final;
							cmp.Attributes |= MemberAttributes.Abstract;
						}
					}
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
					this.PropertyMethods.Add((IntPtr) setter);
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

	private void DocumentProperty(CodeTypeDeclaration type, Property prop, CodeMemberProperty cmp)
	{
		if (type.Name == "QSvgGenerator")
		{
			switch (prop.Name)
			{
				case "viewBox":
					this.documentation.DocumentProperty(type, prop.Name, prop.Type + "F", cmp);
					break;
				case "viewBoxF":
					this.documentation.DocumentProperty(type, "viewBox", prop.Type, cmp);
					break;
				default:
					this.documentation.DocumentProperty(type, prop.Name, prop.Type, cmp);
					break;
			}
		}
		else
		{
			this.documentation.DocumentProperty(type, prop.Name, prop.Type, cmp);
		}
	}

	private static string NameProperty(CodeTypeMember cmp, Property prop, CodeTypeDeclaration type,
	                                   IEnumerable<GeneratorData.InternalMemberInfo> members, string className)
	{
		cmp.Name = prop.Name;
		// capitalize the first letter
		StringBuilder builder = new StringBuilder(cmp.Name);
		builder[0] = char.ToUpperInvariant(builder[0]);
		string capitalized = builder.ToString();

		// If the new name clashes with a name of a type declaration, keep the lower-case name (or even make the name lower-case).
		CodeMemberProperty existing = type.Members.OfType<CodeMemberProperty>().FirstOrDefault(p => p.Name == capitalized);
		if (members.Any(m => m.Name == capitalized && (m.Type == MemberTypes.NestedType || m.Type == MemberTypes.Method)) ||
		    existing != null)
		{
			Debug.Print(
				"  |--Conflicting names: property/(type or method): {0} in class {1} - keeping original property name",
				capitalized, className);

			if (capitalized == cmp.Name)
			{
				builder[0] = char.ToLowerInvariant(builder[0]);
				cmp.Name = builder.ToString(); // lower case the property if necessary
			}
		}
		else
		{
			cmp.Name = capitalized;
		}
		if (type.Name == "QSvgGenerator" && capitalized == "ViewBoxF")
		{
			return "ViewBox";
		}
		return capitalized;
	}

	private IEnumerable<Property> GetProperties(short classId, string className)
	{
		List<Property> props = new List<Property>();
		if (!GetProperties(this.data.Smoke, classId,
		                   (name, typeName, writable, isEnum) =>
		                   props.Add(new Property(name, typeName, writable, isEnum))))
		{
			for (short i = 1; i < this.data.Smoke->numMethodMaps; i++)
			{
				Smoke.MethodMap* map = this.data.Smoke->methodMaps + i;
				if (map->classId == classId && map->method != 0)
				{
					List<Smoke.Method> methods = new List<Smoke.Method>();
					if (map->method > 0)
					{
						methods.Add(*(this.data.Smoke->methods + map->method));
					}
					else
					{
						for (short* overload = this.data.Smoke->ambiguousMethodList + (-map->method); *overload > 0; overload++)
						{
							methods.Add(*(this.data.Smoke->methods + *overload));
						}
					}
					string originalName = ByteArrayManager.GetString(this.data.Smoke->methodNames[methods[0].name]);
					foreach (Smoke.Method meth in from meth in methods
					                              where ((meth.flags & (ushort) Smoke.MethodFlags.mf_property) > 0 &&
					                                    (meth.flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0 &&
					                                    (meth.flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0) ||
														// HACK: working around a SMOKE bug: a setter isn't marked as a property (this property is special, one getter and 2 setters)
														(className == "QSvgGenerator" && originalName == "setViewBox")
					                              select meth)
					{
						short propTypeId;
						bool writable = false;
						string name = originalName;
						string prefix = new string(name.TakeWhile(char.IsLower).ToArray());
						switch (prefix)
						{
							case "set":
								name = name.Substring(prefix.Length);
								propTypeId = *(this.data.Smoke->argumentList + meth.args);
								writable = true;
								break;
							case "is":
								name = name.Substring(prefix.Length);
								goto default;
							default:
								propTypeId = meth.ret;
								break;
						}
						name = char.ToLower(name[0]) + name.Substring(1);
						Property existing = props.FirstOrDefault(p => p.Name == name);
						if (existing == null || writable)
						{
							if (existing != null && !existing.IsWritable)
							{
								props.Remove(existing);
							}
							Smoke.Type propType = this.data.Smoke->types[propTypeId];
							string type = GetPropertyType(propTypeId);
							if (className == "QSvgGenerator" && originalName == "setViewBox" && type == "QRectF")
							{
								name += "F";
							}
							props.Add(new Property(name, type, writable,
							                       propType.flags == ((uint) Smoke.TypeId.t_enum | (uint) Smoke.TypeFlags.tf_stack)));
						}
					}
				}
			}
		}
		return props;
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
			setterMethId = TryMungedNames(classId, name, prop.Type, ref mungedSuffix);
			if (setterMethId == 0)
			{
				// try with 're' prefix
				mungedSuffix = firstMungedSuffix;
				setterMethId = TryMungedNames(classId, otherPrefixName, prop.Type, ref mungedSuffix);
			}
			classId = *(parents++);
		}
		return setterMethId;
	}

	private string GetPropertyType(short propTypeId)
	{
		Smoke.Type type = this.data.Smoke->types[propTypeId];
		StringBuilder typeBuilder = new StringBuilder(ByteArrayManager.GetString(type.name));
		typeBuilder.Replace("const ", string.Empty);
		typeBuilder.Replace("&", string.Empty);
		return typeBuilder.ToString();
	}

	private static readonly char[] mungedSuffixes = {'#', '$', '?'};

	private short TryMungedNames(short classId, string name, string type, ref char mungedSuffix)
	{
		int idx = Array.IndexOf(mungedSuffixes, mungedSuffix);
		int i = idx;
		do
		{
			// loop through the other elements, try various munged names
			mungedSuffix = mungedSuffixes[i];
			short methNameId = this.data.Smoke->IDMethodName(name + mungedSuffix);
			short methMapId = this.data.Smoke->IDMethod(classId, methNameId);
			if (methMapId == 0)
				continue;
			short methId = this.data.Smoke->methodMaps[methMapId].method;
			if (methId < 0)
			{
				for (short* id = this.data.Smoke->ambiguousMethodList + (-methId); *id > 0; id++)
				{
					if (this.CompareTypes(type, *id))
					{
						return *id;
					}
				}
			}
			else
			{
				if (this.CompareTypes(type, methId))
				{
					return methId;
				}
			}
		} while ((i = (i + 1) % mungedSuffixes.Length) != idx); // automatically moves from the end to the beginning
		return 0;
	}

	private bool CompareTypes(string type, short methId)
	{
		string propertyType = this.GetPropertyType(*(this.data.Smoke->argumentList + (this.data.Smoke->methods + methId)->args));
		if (type == propertyType || (type.Contains("::") && propertyType == "int"))
		{
			return true;
		}
		if (propertyType.Contains("::") && type == propertyType.Substring(propertyType.IndexOf("::", StringComparison.Ordinal) + 2))
		{
			return true;
		}
		if (!this.data.TypeDefsPerType.ContainsKey(propertyType))
		{
			return false;
		}
		return this.data.TypeDefsPerType[propertyType].Any(t => type == t || type == t.Substring(t.IndexOf("::", StringComparison.Ordinal) + 2));
	}
}
