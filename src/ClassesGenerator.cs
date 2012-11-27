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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

public unsafe delegate void ClassHook(Smoke *smoke, Smoke.Class *klass, CodeTypeDeclaration typeDecl);

public unsafe class ClassesGenerator
{
	private readonly GeneratorData data;
	private readonly Translator translator;
	private readonly EnumGenerator eg;

	private static readonly CodeSnippetTypeMember getHashCode = new CodeSnippetTypeMember(
		"public override int GetHashCode() { return interceptor.GetHashCode(); }"
		);

	private const string EqualsCode =
		"public override bool Equals(object o) {{\n" +
		"            if (!(o is {0})) {{ return false; }}\n" +
		"            return this == ({0}) o;\n" +
		"        }}";


	public ClassesGenerator(GeneratorData data, Translator translator)
	{
		this.data = data;
		this.translator = translator;
		eg = new EnumGenerator(data);
	}

	public static event ClassHook PreMembersHooks;
	public static event ClassHook PostMembersHooks;
	public static event MethodHook SupportingMethodsHooks;
	public static event Action PreClassesHook;
	public static event Action PostClassesHook;

	/*
	 * Create a .NET class from a smoke class.
	 * A class Namespace::Foo is mapped to Namespace.Foo. Classes that are not in any namespace go into the default namespace.
	 * For namespaces that contain functions, a Namespace.Global class is created which holds the functions as methods.
	 */

	private void DefineClass(short classId)
	{
		Smoke.Class* smokeClass = data.Smoke->classes + classId;
		string smokeName = ByteArrayManager.GetString(smokeClass->className);
		string mapName = smokeName;
		string name;
		string prefix = string.Empty;
		if (smokeClass->size == 0 && !translator.NamespacesAsClasses.Contains(smokeName))
		{
			if (smokeName == "QGlobalSpace")
			{
				// global space
				name = data.GlobalSpaceClassName;
				mapName = name;
			}
			else
			{
				// namespace
				prefix = smokeName;
				name = "Global";
				mapName = prefix + "::Global";
			}
		}
		else
		{
			int colon = smokeName.LastIndexOf("::", StringComparison.Ordinal);
			prefix = (colon != -1) ? smokeName.Substring(0, colon) : string.Empty;
			name = (colon != -1) ? smokeName.Substring(colon + 2) : smokeName;
		}

		// define the .NET class
		CodeTypeDeclaration type;
		bool alreadyDefined;
		if (!(alreadyDefined = data.CSharpTypeMap.TryGetValue(mapName, out type)))
		{
			type = new CodeTypeDeclaration(name);
			CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeClass",
			                                                             new CodeAttributeArgument(
				                                                             new CodePrimitiveExpression(smokeName)));
			type.CustomAttributes.Add(attr);
			type.IsPartial = true;
		}
		else
		{
			int toBeRemoved = -1;

			for (int i = 0; i < type.CustomAttributes.Count; i++)
			{
				CodeAttributeDeclaration attr = type.CustomAttributes[i];
				if (attr.Name == "SmokeClass" && attr.Arguments.Count == 1 &&
				    ((string) ((CodePrimitiveExpression) attr.Arguments[0].Value).Value) == "QGlobalSpace")
				{
					toBeRemoved = i;
					break;
				}
			}

			if (toBeRemoved > -1)
			{
				type.CustomAttributes.RemoveAt(toBeRemoved);
				CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeClass",
				                                                             new CodeAttributeArgument(
					                                                             new CodePrimitiveExpression(smokeName)));
				type.CustomAttributes.Add(attr);
			}
		}

		if (smokeClass->parents != 0)
		{
			short* parent = data.Smoke->inheritanceList + smokeClass->parents;
			if (*parent > 0)
			{
				type.BaseTypes.Add(
					new CodeTypeReference(ByteArrayManager.GetString((data.Smoke->classes + *parent)->className).Replace("::", ".")));
			}
		}

		if (Util.IsClassAbstract(data.Smoke, classId))
		{
			type.TypeAttributes |= TypeAttributes.Abstract;
		}

		if (PreMembersHooks != null)
		{
			PreMembersHooks(data.Smoke, smokeClass, type);
		}

		if (!alreadyDefined)
		{
			DefineWrapperClassFieldsAndMethods(smokeClass, type);
			data.CSharpTypeMap[mapName] = type;
			IList collection = data.GetTypeCollection(prefix);
			collection.Add(type);
			type.UserData.Add("parent", prefix);

			// add the internal implementation type for abstract classes
			if ((type.TypeAttributes & TypeAttributes.Abstract) == TypeAttributes.Abstract)
			{
				CodeTypeDeclaration implType = new CodeTypeDeclaration();
				implType.Name = type.Name + "Internal";
				implType.BaseTypes.Add(new CodeTypeReference(type.Name));
				implType.IsPartial = true;
				implType.TypeAttributes = TypeAttributes.NotPublic;

				CodeConstructor dummyCtor = new CodeConstructor();
				dummyCtor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Type)), "dummy"));
				dummyCtor.BaseConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
				dummyCtor.Attributes = MemberAttributes.Family;
				implType.Members.Add(dummyCtor);

				data.InternalTypeMap[type] = implType;

				collection.Add(implType);
			}
		}

		data.SmokeTypeMap[(IntPtr) smokeClass] = type;
	}

	private void DefineWrapperClassFieldsAndMethods(Smoke.Class* smokeClass, CodeTypeDeclaration type)
	{
		// define the dummy constructor
		if (smokeClass->size > 0)
		{
			CodeConstructor dummyCtor = new CodeConstructor();
			dummyCtor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Type)), "dummy"));
			if (data.Smoke->inheritanceList[smokeClass->parents] > 0)
			{
				dummyCtor.BaseConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
			}
			dummyCtor.Attributes = MemberAttributes.Family;
			if (SupportingMethodsHooks != null)
			{
				SupportingMethodsHooks(data.Smoke, (Smoke.Method*) 0, dummyCtor, type);
			}
			type.Members.Add(dummyCtor);
		}

		CodeMemberField staticInterceptor = new CodeMemberField("SmokeInvocation", "staticInterceptor");
		staticInterceptor.Attributes = MemberAttributes.Static;
		CodeObjectCreateExpression initExpression = new CodeObjectCreateExpression("SmokeInvocation");
		initExpression.Parameters.Add(new CodeTypeOfExpression(type.Name));
		initExpression.Parameters.Add(new CodePrimitiveExpression(null));
		staticInterceptor.InitExpression = initExpression;
		type.Members.Add(staticInterceptor);

		if (smokeClass->size == 0)
			return;

		// we only need this for real classes
		CodeMemberMethod createProxy = new CodeMemberMethod();
		createProxy.Name = "CreateProxy";
		createProxy.Attributes = MemberAttributes.Public;
		if (data.Smoke->inheritanceList[smokeClass->parents] != 0)
		{
			createProxy.Attributes |= MemberAttributes.Override;
		}
		createProxy.Statements.Add(new CodeAssignStatement(
			                           new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "interceptor"),
			                           // left hand side
			                           new CodeObjectCreateExpression("SmokeInvocation", new CodeTypeOfExpression(type.Name),
			                                                          new CodeThisReferenceExpression()) // right hand side
			                           ));
		type.Members.Add(createProxy);

		if (data.Smoke->inheritanceList[smokeClass->parents] != 0)
			return;
		// The following fields are only necessary for classes without superclasses.

		CodeMemberField interceptor = new CodeMemberField("SmokeInvocation", "interceptor");
		interceptor.Attributes = MemberAttributes.Family;
		type.Members.Add(interceptor);
		CodeMemberField smokeObject = new CodeMemberField(typeof(IntPtr), "smokeObject");
		type.Members.Add(smokeObject);
		type.BaseTypes.Add(new CodeTypeReference("ISmokeObject"));
		CodeMemberProperty propertySmokeObject = new CodeMemberProperty();
		propertySmokeObject.Name = "SmokeObject";
		propertySmokeObject.Type = new CodeTypeReference(typeof(IntPtr));
		propertySmokeObject.Attributes = MemberAttributes.Public;
		CodeFieldReferenceExpression smokeObjectReference = new CodeFieldReferenceExpression(
			new CodeThisReferenceExpression(), smokeObject.Name);
		propertySmokeObject.GetStatements.Add(new CodeMethodReturnStatement(smokeObjectReference));
		propertySmokeObject.SetStatements.Add(new CodeAssignStatement(smokeObjectReference,
		                                                              new CodePropertySetValueReferenceExpression()));
		type.Members.Add(propertySmokeObject);
	}

	/*
	 * Loops through all wrapped methods. Any class that is found is converted to a .NET class (see DefineClass()).
	 * A MethodGenerator is then created to generate the methods for that class.
	 */

	public void Run()
	{
		for (short i = 1; i <= data.Smoke->numClasses; i++)
		{
			Smoke.Class* klass = data.Smoke->classes + i;
			if (klass->external)
				continue;

			DefineClass(i);
		}

		eg.DefineEnums();

		// create interfaces if necessary
		ClassInterfacesGenerator cig = new ClassInterfacesGenerator(data, translator);
		cig.Run();

		for (short i = 1; i <= data.Smoke->numClasses; i++)
		{
			Smoke.Class* klass = data.Smoke->classes + i;
			if (klass->external)
				continue;

			string className = ByteArrayManager.GetString(klass->className);
			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];
			CodeTypeDeclaration iface;
			if (data.InterfaceTypeMap.TryGetValue(className, out iface))
			{
				type.BaseTypes.Add(new CodeTypeReference('I' + type.Name));
			}

			short* parent = data.Smoke->inheritanceList + klass->parents;
			bool firstParent = true;
			while (*parent > 0)
			{
				if (firstParent)
				{
					firstParent = false;
					parent++;
					continue;
				}
				// Translator.CppToCSharp() will take care of 'interfacifying' the class name
				type.BaseTypes.Add(translator.CppToCSharp(data.Smoke->classes + *parent));
				parent++;
			}
		}

		if (PreClassesHook != null)
		{
			PreClassesHook();
		}

		GenerateMethods();
		GenerateInternalImplementationMethods();

		if (PostClassesHook != null)
		{
			PostClassesHook();
		}
		MethodsGenerator.Provider.Dispose();
	}

	private void GenerateInheritedMethods(Smoke.Class* klass, MethodsGenerator methgen, AttributeGenerator attrgen,
	                                      List<Smoke.ModuleIndex> alreadyImplemented)
	{
		// Contains inherited methods that have to be implemented by the current class.
		// We use our custom comparer, so we don't end up with the same method multiple times.
		IDictionary<Smoke.ModuleIndex, string> implementMethods =
			new Dictionary<Smoke.ModuleIndex, string>(SmokeMethodEqualityComparer.DefaultEqualityComparer);

		bool firstParent = true;
		for (short* parent = data.Smoke->inheritanceList + klass->parents; *parent > 0; parent++)
		{
			if (firstParent)
			{
				// we're only interested in parents implemented as interfaces
				firstParent = false;
				continue;
			}
			// collect all methods (+ inherited ones) and add them to the implementMethods Dictionary
			data.Smoke->FindAllMethods(*parent, implementMethods, true);
		}

		foreach (KeyValuePair<Smoke.ModuleIndex, string> pair in implementMethods)
		{
			Smoke.Method* meth = pair.Key.smoke->methods + pair.Key.index;
			Smoke.Class* ifaceKlass = pair.Key.smoke->classes + meth->classId;

			if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0
			    || (meth->flags & (ushort) Smoke.MethodFlags.mf_ctor) > 0
			    || (meth->flags & (ushort) Smoke.MethodFlags.mf_copyctor) > 0
			    || (meth->flags & (ushort) Smoke.MethodFlags.mf_dtor) > 0
			    || (meth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0
			    || (meth->flags & (ushort) Smoke.MethodFlags.mf_internal) > 0)
			{
				// no need to check for properties here - QObjects don't support multiple inheritance anyway
				continue;
			}
			if (alreadyImplemented.Contains(pair.Key, SmokeMethodEqualityComparer.DefaultEqualityComparer))
			{
				continue;
			}
			if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0)
			{
				attrgen.ScheduleAttributeAccessor(pair.Key.smoke, meth);
				continue;
			}

			CodeTypeReference type = translator.CppToCSharp(ByteArrayManager.GetString(ifaceKlass->className));
			methgen.GenerateMethod(pair.Key.smoke, meth, pair.Value, type);
		}
	}

	private void GenerateInternalImplementationMethods()
	{
		for (short i = 1; i <= data.Smoke->numClasses; i++)
		{
			Smoke.Class* klass = data.Smoke->classes + i;
			if (klass->external)
			{
				continue;
			}
			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];

			CodeTypeDeclaration implType;
			if (!data.InternalTypeMap.TryGetValue(type, out implType))
			{
				continue;
			}

			MethodsGenerator methgen = new MethodsGenerator(data, translator, implType, klass);
			methgen.InternalImplementation = true;

			foreach (KeyValuePair<Smoke.ModuleIndex, string> pair in Util.GetAbstractMethods(data.Smoke, i))
			{
				methgen.GenerateMethod(pair.Key.smoke, pair.Key.index, pair.Value);
			}
			methgen.GenerateProperties();
		}
	}

	/*
	 * Adds the methods to the classes created by Run()
	 */

	private void GenerateMethods()
	{
		List<Smoke.ModuleIndex> alreadyImplemented = new List<Smoke.ModuleIndex>();

		this.FillEnums();

		Dictionary<short, List<Smoke.MethodMap>> dictionary = new Dictionary<short, List<Smoke.MethodMap>>();
		List<short> classes = new List<short>();
		for (short i = 1; i < data.Smoke->numMethodMaps; i++)
		{
			Smoke.MethodMap* map = data.Smoke->methodMaps + i;
			if (!dictionary.ContainsKey(map->classId))
			{
				dictionary.Add(map->classId, new List<Smoke.MethodMap>());
			}
			dictionary[map->classId].Add(*map);
			if (!classes.Contains(map->classId))
			{
				classes.Add(map->classId);
			}
		}
		foreach (KeyValuePair<short, List<Smoke.MethodMap>> pair in dictionary.OrderBy(k =>
			{
				int chainLength = 0;
				Smoke.Class* klass = data.Smoke->classes + k.Key;
				short* parent = data.Smoke->inheritanceList + klass->parents;
				while (*parent > 0)
				{
					++chainLength;
					klass = data.Smoke->classes + *parent;
					parent = data.Smoke->inheritanceList + klass->parents;
				}
				return chainLength;
			}))
		{
			Smoke.Class* klass = data.Smoke->classes + pair.Key;
			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];

			alreadyImplemented.Clear();
			AttributeGenerator attrgen = new AttributeGenerator(data, translator, type);
			MethodsGenerator methgen = new MethodsGenerator(data, translator, type, klass);

			foreach (Smoke.MethodMap map in pair.Value)
			{
				string mungedName = ByteArrayManager.GetString(data.Smoke->methodNames[map.name]);
				if (map.method > 0)
				{
					Smoke.Method* meth = data.Smoke->methods + map.method;
					if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
					{
						continue;
					}

					if ((meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0 // non-virtual properties are excluded
					    && (meth->flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0
					    && (meth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
					{
						continue;
					}
					if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0)
					{
						attrgen.ScheduleAttributeAccessor(meth);
						continue;
					}

					methgen.GenerateMethod(map.method, mungedName);
					alreadyImplemented.Add(new Smoke.ModuleIndex(data.Smoke, map.method));
				}
				else if (map.method < 0)
				{
					for (short* overload = data.Smoke->ambiguousMethodList + (-map.method); *overload > 0; overload++)
					{
						Smoke.Method* meth = data.Smoke->methods + *overload;
						if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
						{
							continue;
						}

						if ((meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0 // non-virtual properties are excluded
						    && (meth->flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0
						    && (meth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
						{
							continue;
						}
						if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0)
						{
							attrgen.ScheduleAttributeAccessor(meth);
							continue;
						}

						// if the methods differ only by constness, we will generate special code
						bool nextDiffersByConst = false;
						if (*(overload + 1) > 0)
						{
							if (SmokeMethodEqualityComparer.EqualExceptConstness(meth, data.Smoke->methods + *(overload + 1)))
								nextDiffersByConst = true;
						}

						methgen.GenerateMethod(*overload, mungedName);
						alreadyImplemented.Add(new Smoke.ModuleIndex(data.Smoke, *overload));
						if (nextDiffersByConst)
							overload++;
					}
				}
			}
			// generate inherited methods
			this.GenerateInheritedMethods(klass, methgen, attrgen, alreadyImplemented);

			// generate all scheduled attributes
			attrgen.Run();
			methgen.GenerateProperties();
		}
		foreach (short @class in classes)
		{
			Smoke.Class* klass = data.Smoke->classes + @class;
			CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];
			if (PostMembersHooks != null)
			{
				PostMembersHooks(this.data.Smoke, klass, type);
			}
		}
		AddMissingOperators();
	}

	private void FillEnums()
	{
		for (short i = 1; i < this.data.Smoke->numMethodMaps; i++)
		{
			Smoke.MethodMap* map = this.data.Smoke->methodMaps + i;
			if (map->method > 0)
			{
				Smoke.Method* meth = this.data.Smoke->methods + map->method;
				if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
				{
					this.eg.DefineMember(meth);
				}
			}
			else if (map->method < 0)
			{
				for (short* overload = this.data.Smoke->ambiguousMethodList + (-map->method); *overload > 0; overload++)
				{
					Smoke.Method* meth = this.data.Smoke->methods + *overload;
					if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
					{
						this.eg.DefineMember(meth);
						continue;
					}

					// if the methods differ only by constness, we will generate special code
					bool nextDiffersByConst = false;
					if (*(overload + 1) > 0)
					{
						if (SmokeMethodEqualityComparer.EqualExceptConstness(meth, this.data.Smoke->methods + *(overload + 1)))
							nextDiffersByConst = true;
					}
					if (nextDiffersByConst)
						overload++;
				}
			}
		}
	}

	private delegate void AddComplementingOperatorsFn(
		IList<CodeMemberMethod> a, IList<CodeMemberMethod> b, string opName, string returnExpression);

	/*
	 * Adds complementing operators if necessary.
	 */

	private void AddMissingOperators()
	{
		for (short i = 1; i <= this.data.Smoke->numClasses; i++)
		{
			Smoke.Class* klass = this.data.Smoke->classes + i;
			// skip external classes and namespaces
			if (klass->external || klass->size == 0)
				continue;

			CodeTypeDeclaration typeDecl = this.data.SmokeTypeMap[(IntPtr) klass];

			var lessThanOperators = new List<CodeMemberMethod>();
			var greaterThanOperators = new List<CodeMemberMethod>();
			var lessThanOrEqualOperators = new List<CodeMemberMethod>();
			var greaterThanOrEqualOperators = new List<CodeMemberMethod>();
			var equalOperators = new List<CodeMemberMethod>();
			var inequalOperators = new List<CodeMemberMethod>();

			foreach (CodeMemberMethod method in typeDecl.Members.OfType<CodeMemberMethod>())
			{
				switch (method.Name)
				{
					case "operator<":
						lessThanOperators.Add(method);
						break;
					case "operator>":
						greaterThanOperators.Add(method);
						break;
					case "operator<=":
						lessThanOrEqualOperators.Add(method);
						break;
					case "operator>=":
						greaterThanOrEqualOperators.Add(method);
						break;
					case "operator==":
						equalOperators.Add(method);
						break;
					case "operator!=":
						inequalOperators.Add(method);
						break;
				}
			}

			AddComplementingOperatorsFn checkAndAdd =
				delegate(IList<CodeMemberMethod> ops, IList<CodeMemberMethod> otherOps, string opName, string expr)
					{
						foreach (CodeMemberMethod op in ops)
						{
							if (otherOps.Any(otherOp => op.ParametersEqual(otherOp)))
								continue;

							CodeMemberMethod complement = new CodeMemberMethod();
							complement.Name = opName;
							complement.Attributes = op.Attributes;
							complement.ReturnType = op.ReturnType;
							complement.Parameters.AddRange(op.Parameters);
							complement.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression(expr)));
							typeDecl.Members.Add(complement);
						}
					};

			checkAndAdd(lessThanOperators, greaterThanOperators, "operator>", "!(arg1 < arg2) && arg1 != arg2");
			checkAndAdd(greaterThanOperators, lessThanOperators, "operator<", "!(arg1 > arg2) && arg1 != arg2");

			checkAndAdd(lessThanOrEqualOperators, greaterThanOrEqualOperators, "operator>=", "!(arg1 < arg2)");
			checkAndAdd(greaterThanOrEqualOperators, lessThanOrEqualOperators, "operator<=", "!(arg1 > arg2)");

			checkAndAdd(equalOperators, inequalOperators, "operator!=", "!(arg1 == arg2)");
			checkAndAdd(inequalOperators, equalOperators, "operator==", "!(arg1 != arg2)");

			if (equalOperators.Count == 0 && inequalOperators.Count == 0)
				continue; // then we're done

			// add Equals(object) and GetHashCode() overrides
			CodeSnippetTypeMember equals = new CodeSnippetTypeMember(string.Format(EqualsCode, typeDecl.Name));

			typeDecl.Members.Add(equals);
			typeDecl.Members.Add(getHashCode);
		}
	}
}
