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
using System.CodeDom;
using System.Linq;

public delegate void AttributePropertyHook(CodeTypeMember cmm, CodeTypeDeclaration typeDecl);

public unsafe class AttributeGenerator
{
	private class Attribute
	{
		public Smoke* Smoke = (Smoke*) 0;
		public Smoke.Method* GetMethod = (Smoke.Method*) 0;
		public Smoke.Method* SetMethod = (Smoke.Method*) 0;
	}

	public static event AttributePropertyHook PostAttributeProperty;

	private readonly Dictionary<string, Attribute> attributes = new Dictionary<string, Attribute>();

	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly CodeTypeDeclaration type;

	public AttributeGenerator(GeneratorData data, Translator translator, CodeTypeDeclaration type)
	{
		this.data = data;
		this.translator = translator;
		this.type = type;
	}

	public void ScheduleAttributeAccessor(Smoke.Method* meth)
	{
		ScheduleAttributeAccessor(data.Smoke, meth);
	}

	public void ScheduleAttributeAccessor(Smoke* smoke, Smoke.Method* meth)
	{
		string name = ByteArrayManager.GetString(smoke->methodNames[meth->name]);
		bool isSetMethod = false;

		if (name.StartsWith("set"))
		{
			name = name.Remove(0, 3);
			isSetMethod = true;
		}
		else
		{
			// capitalize the first letter
			StringBuilder builder = new StringBuilder(name);
			builder[0] = char.ToUpper(builder[0]);
			name = builder.ToString();
		}

		// If the new name clashes with a name of a type declaration, keep the lower-case name.
		var typesWithSameName = from member in data.GetAccessibleMembers(smoke->classes + meth->classId)
		                        where (member.Type == MemberTypes.NestedType
		                               || member.Type == MemberTypes.Method)
		                              && member.Name == name
		                        select member;
		if (typesWithSameName.Any())
		{
			string className = ByteArrayManager.GetString(smoke->classes[meth->classId].className);
			Debug.Print("  |--Conflicting names: property/type: {0} in class {1} - keeping original property name", name,
			            className);
			name = char.ToLower(name[0]) + name.Substring(1);
		}

		Attribute attr;
		if (!attributes.TryGetValue(name, out attr))
		{
			attr = new Attribute();
			attr.Smoke = smoke;
			attributes.Add(name, attr);
		}

		if (isSetMethod)
		{
			attr.SetMethod = meth;
		}
		else
		{
			attr.GetMethod = meth;
		}
	}

	public List<CodeMemberProperty> GenerateBasicAttributeDefinitions()
	{
		List<CodeMemberProperty> ret = new List<CodeMemberProperty>();
		foreach (KeyValuePair<string, Attribute> pair in attributes)
		{
			Attribute attr = pair.Value;
			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Name = pair.Key;
			try
			{
				bool isRef;
				prop.Type = translator.CppToCSharp(attr.Smoke->types + attr.GetMethod->ret, out isRef);
			}
			catch (NotSupportedException)
			{
				string className = ByteArrayManager.GetString(attr.Smoke->classes[attr.GetMethod->classId].className);
				Debug.Print("  |--Won't wrap Attribute {0}::{1}", className, prop.Name);
				continue;
			}
			prop.HasGet = true;
			prop.HasSet = attr.SetMethod != (Smoke.Method*) 0;

			if ((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_protected) > 0)
			{
				prop.Attributes = MemberAttributes.Family | MemberAttributes.New | MemberAttributes.Final;
			}
			else
			{
				prop.Attributes = MemberAttributes.Public | MemberAttributes.New | MemberAttributes.Final;
			}

			if ((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_static) > 0)
				prop.Attributes |= MemberAttributes.Static;

			ret.Add(prop);
			if (PostAttributeProperty != null)
			{
				PostAttributeProperty(prop, type);
			}
		}
		return ret;
	}

	public void Run()
	{
		foreach (CodeMemberProperty cmp in GenerateBasicAttributeDefinitions())
		{
			Attribute attr = attributes[cmp.Name];
			CodeMethodReferenceExpression interceptorReference =
				((attr.GetMethod->flags & (uint) Smoke.MethodFlags.mf_static) == 0)
					? SmokeSupport.interceptor_Invoke
					: SmokeSupport.staticInterceptor_Invoke;
			cmp.GetStatements.Add(
				new CodeMethodReturnStatement(
					new CodeCastExpression(cmp.Type,
			            new CodeMethodInvokeExpression(
				            interceptorReference,
				            new CodePrimitiveExpression(ByteArrayManager.GetString(attr.Smoke->methodNames[attr.GetMethod->name])),
				            new CodePrimitiveExpression(attr.Smoke->GetMethodSignature(attr.GetMethod)),
				            new CodeTypeOfExpression(cmp.Type),
				            new CodePrimitiveExpression(false)))));

			if (cmp.HasSet)
			{
				cmp.SetStatements.Add(
					new CodeMethodInvokeExpression(interceptorReference,
				        new CodePrimitiveExpression(ByteArrayManager.GetString(attr.Smoke->methodNames[attr.Smoke->FindMungedName(attr.SetMethod)])),
				        new CodePrimitiveExpression(attr.Smoke->GetMethodSignature(attr.SetMethod)),
				        new CodeTypeOfExpression(typeof(void)),
				        new CodePrimitiveExpression(false),
				        new CodeTypeOfExpression(cmp.Type),
				        new CodeArgumentReferenceExpression("value")));
			}

			type.Members.Add(cmp);
		}
	}
}
