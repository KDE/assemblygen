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
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

unsafe class ClassesGenerator {

    GeneratorData data;
    Translator translator;
    EnumGenerator eg;

    // needed to filter out superfluous methods from base classes
    SmokeMethodEqualityComparer smokeMethodComparer;

    static string qObjectDummyCtorCode =
"            try {\n" +
"                Type proxyInterface = Qyoto.GetSignalsInterface(GetType());\n" +
"                SignalInvocation realProxy = new SignalInvocation(proxyInterface, this);\n" +
"                Q_EMIT = realProxy.GetTransparentProxy();\n" +
"            }\n" +
"            catch (Exception e) {\n" +
"                Console.WriteLine(\"Could not retrieve signal interface: {0}\", e);\n" +
"            }";

    public ClassesGenerator(GeneratorData data, Translator translator) {
        this.data = data;
        this.translator = translator;
        smokeMethodComparer = new SmokeMethodEqualityComparer(data.Smoke);
        eg = new EnumGenerator(data);
    }

    /*
     * Create a .NET class from a smoke class.
     * A class Namespace::Foo is mapped to Namespace.Foo. Classes that are not in any namespace go into the default namespace.
     * For namespaces that contain functions, a Namespace.Global class is created which holds the functions as methods.
     */
    CodeTypeDeclaration DefineClass(Smoke.Class* smokeClass) {
        string smokeName = ByteArrayManager.GetString(smokeClass->className);
        string mapName = smokeName;
        string name;
        string prefix = string.Empty;
        if (smokeClass->size == 0 && !data.NamespacesAsClasses.Contains(smokeName)) {
            if (smokeName == "QGlobalSpace") {  // global space
                name = data.GlobalSpaceClassName;
            } else {
                // namespace
                prefix = smokeName;
                name = "Global";
                mapName = prefix + "::Global";
            }
        } else {
            int colon = smokeName.LastIndexOf("::");
            prefix = (colon != -1) ? smokeName.Substring(0, colon) : string.Empty;
            name = (colon != -1) ? smokeName.Substring(colon + 2) : smokeName;
        }

        // define the .NET class
        CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeClass",
            new CodeAttributeArgument(new CodePrimitiveExpression(smokeName)));
        CodeTypeDeclaration type = new CodeTypeDeclaration(name);
        type.CustomAttributes.Add(attr);
        type.IsPartial = true;

        if (smokeClass->parents == 0) {
            if (smokeName == "QObject") {
                type.BaseTypes.Add(new CodeTypeReference("Qt"));
            } else {
                type.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            }
        } else {
            short *parent = data.Smoke->inheritanceList + smokeClass->parents;
            if (*parent > 0) {
                type.BaseTypes.Add(new CodeTypeReference(ByteArrayManager.GetString((data.Smoke->classes + *parent)->className).Replace("::", ".")));
            }
        }

        DefineWrapperClassFieldsAndMethods(smokeClass, type);

        data.CSharpTypeMap[mapName] = type;
        data.SmokeTypeMap[(IntPtr) smokeClass] = type;
        data.GetTypeCollection(prefix).Add(type);
        return type;
    }

    void DefineWrapperClassFieldsAndMethods(Smoke.Class* smokeClass, CodeTypeDeclaration type) {
        string smokeName = ByteArrayManager.GetString(smokeClass->className);

        // define the dummy constructor
        if (smokeClass->size > 0) {
            CodeConstructor dummyCtor = new CodeConstructor();
            dummyCtor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(typeof(Type)), "dummy"));
            if (data.Smoke->inheritanceList[smokeClass->parents] > 0) {
                dummyCtor.BaseConstructorArgs.Add(new CodeSnippetExpression("(System.Type) null"));
            }
            dummyCtor.Attributes = MemberAttributes.Family;
            if (smokeName == "QObject") {
                dummyCtor.Statements.Add(new CodeSnippetStatement(qObjectDummyCtorCode));
                CodeMemberField Q_EMIT = new CodeMemberField(typeof(object), "Q_EMIT");
                Q_EMIT.Attributes = MemberAttributes.Family;
                Q_EMIT.InitExpression = new CodePrimitiveExpression(null);
                type.Members.Add(Q_EMIT);
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
        createProxy.Attributes = MemberAttributes.Family | MemberAttributes.Final | MemberAttributes.New;
        createProxy.Statements.Add(new CodeAssignStatement(
            new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "interceptor"), // left hand side
            new CodeObjectCreateExpression("SmokeInvocation", new CodeTypeOfExpression(type.Name), new CodeThisReferenceExpression()) // right hand side
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
    }

    /*
     * Loops through all wrapped methods. Any class that is found is converted to a .NET class (see DefineClass()).
     * A MethodGenerator is then created to generate the methods for that class.
     */
    public void Run() {
        for (short i = 1; i <= data.Smoke->numClasses; i++) {
            Smoke.Class* klass = data.Smoke->classes + i;
            if (klass->external)
                continue;

            DefineClass(klass);
        }

        eg.DefineEnums();

        // create interfaces if necessary
        ClassInterfacesGenerator cig = new ClassInterfacesGenerator(data, translator);
        cig.Run();

        for (short i = 1; i <= data.Smoke->numClasses; i++) {
            Smoke.Class* klass = data.Smoke->classes + i;
            if (klass->external)
                continue;

            string className = ByteArrayManager.GetString(klass->className);
            CodeTypeDeclaration type = data.SmokeTypeMap[(IntPtr) klass];
            CodeTypeDeclaration iface;
            if (data.InterfaceTypeMap.TryGetValue(className, out iface)) {
                type.BaseTypes.Add(new CodeTypeReference('I' + type.Name));
            }

            short *parent = data.Smoke->inheritanceList + klass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                if (firstParent) {
                    firstParent = false;
                    parent++;
                    continue;
                }
                // Translator.CppToCSharp() will take care of 'interfacifying' the class name
                type.BaseTypes.Add(translator.CppToCSharp(data.Smoke->classes + *parent));
                parent++;
            }
        }

        PropertyGenerator pg = new PropertyGenerator(data, translator);
        pg.Run();

        GenerateMethods();
    }

    /*
     * Adds the methods to the classes created by Run()
     */
    void GenerateMethods() {
        short currentClassId = 0;
        Smoke.Class *klass = (Smoke.Class*) IntPtr.Zero;
        MethodsGenerator methgen = null;
        AttributeGenerator attrgen = null;
        CodeTypeDeclaration type = null;

        // Contains inherited methods that have to be implemented by the current class.
        // We use our custom comparer, so we don't end up with the same method multiple times.
        IDictionary<short, string> implementMethods = new Dictionary<short, string>(smokeMethodComparer);

        for (short i = 1; i < data.Smoke->numMethodMaps; i++) {
            Smoke.MethodMap *map = data.Smoke->methodMaps + i;

            if (currentClassId != map->classId) {
                // we encountered a new class
                currentClassId = map->classId;
                klass = data.Smoke->classes + currentClassId;
                type = data.SmokeTypeMap[(IntPtr) klass];

                methgen = new MethodsGenerator(data, translator, type);

                if (attrgen != null) {
                    // generate all scheduled attributes
                    attrgen.Run();
                }
                attrgen = new AttributeGenerator(data, translator, type);

                implementMethods.Clear();

                bool firstParent = true;
                for (short *parent = data.Smoke->inheritanceList + klass->parents; *parent > 0; parent++) {
                    if (firstParent) {
                        // we're only interested in parents implemented as interfaces
                        firstParent = false;
                        continue;
                    }
                    // collect all methods (+ inherited ones) and add them to the implementMethods Dictionary
                    data.Smoke->FindAllMethods(*parent, implementMethods, true);
                }

                foreach (KeyValuePair<short, string> pair in implementMethods) {
                    Smoke.Method *meth = data.Smoke->methods + pair.Key;
                    if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_ctor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_copyctor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_dtor) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_static) > 0
                        || (meth->flags & (ushort) Smoke.MethodFlags.mf_internal) > 0)
                    {
                        // no need to check for properties here - QObjects don't support multiple inheritance anyway
                        continue;
                    } else if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0) {
                        attrgen.ScheduleAttributeAccessor(meth);
                    }

                    methgen.GenerateMethod(meth, pair.Value);
                }
            }

            string mungedName = ByteArrayManager.GetString(data.Smoke->methodNames[map->name]);
            if (map->method > 0) {
                Smoke.Method *meth = data.Smoke->methods + map->method;
                if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0) {
                    eg.DefineMember(meth);
                    continue;
                }

                if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0   // non-virtual properties are excluded
                    && (meth->flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0
                    && (meth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
                {
                    continue;
                } else if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0) {
                    attrgen.ScheduleAttributeAccessor(meth);
                    continue;
                }

                // already implemented?
                if (implementMethods.ContainsKey(map->method))
                    continue;

                methgen.GenerateMethod(map->method, mungedName);
            } else if (map->method < 0) {
                for (short *overload = data.Smoke->ambiguousMethodList + (-map->method); *overload > 0; overload++) {
                    Smoke.Method *meth = data.Smoke->methods + *overload;
                    if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0) {
                        eg.DefineMember(meth);
                        continue;
                    }

                    if (   (meth->flags & (ushort) Smoke.MethodFlags.mf_property) > 0   // non-virtual properties are excluded
                        && (meth->flags & (ushort) Smoke.MethodFlags.mf_virtual) == 0
                        && (meth->flags & (ushort) Smoke.MethodFlags.mf_purevirtual) == 0)
                    {
                        continue;
                    } else if ((meth->flags & (ushort) Smoke.MethodFlags.mf_attribute) > 0) {
                        attrgen.ScheduleAttributeAccessor(meth);
                        continue;
                    }

                    // if the methods differ only by constness, we will generate special code
                    bool nextDiffersByConst = false;
                    if (*(overload + 1) > 0) {
                        if (SmokeMethodEqualityComparer.EqualExceptConstness(meth, data.Smoke->methods + *(overload + 1)))
                            nextDiffersByConst = true;
                    }

                    // already implemented?
                    if (implementMethods.ContainsKey(*overload))
                        continue;

                    methgen.GenerateMethod(*overload, mungedName);
                    if (nextDiffersByConst)
                        overload++;
                }
            }
        }

        // Generate the last scheduled attributes
        attrgen.Run();
        AddMissingOperators();
    }

    delegate void AddComplementingOperatorsFn(IList<CodeMemberMethod> a, IList<CodeMemberMethod> b, string opName, string returnExpression);

    /*
     * Adds complement operators if necessary.
     */
    void AddMissingOperators() {
        for (short i = 1; i <= data.Smoke->numClasses; i++) {
            Smoke.Class* klass = data.Smoke->classes + i;
            // skip external classes and namespaces
            if (klass->external || klass->size == 0)
                continue;

            CodeTypeDeclaration typeDecl = data.SmokeTypeMap[(IntPtr) klass];

            var lessThanOperators = new List<CodeMemberMethod>();
            var greaterThanOperators = new List<CodeMemberMethod>();
            var lessThanOrEqualOperators = new List<CodeMemberMethod>();
            var greaterThanOrEqualOperators = new List<CodeMemberMethod>();
            var equalOperators = new List<CodeMemberMethod>();
            var inequalOperators = new List<CodeMemberMethod>();

            foreach (CodeTypeMember member in typeDecl.Members) {
                CodeMemberMethod method = member as CodeMemberMethod;
                if (method == null)
                    continue;
                if (method.Name == "operator<") {
                    lessThanOperators.Add(method);
                } else if (method.Name == "operator>") {
                    greaterThanOperators.Add(method);
                } else if (method.Name == "operator<=") {
                    lessThanOrEqualOperators.Add(method);
                } else if (method.Name == "operator>=") {
                    greaterThanOrEqualOperators.Add(method);
                } else if (method.Name == "operator==") {
                    equalOperators.Add(method);
                } else if (method.Name == "operator!=") {
                    inequalOperators.Add(method);
                }
            }

            AddComplementingOperatorsFn checkAndAdd = delegate(IList<CodeMemberMethod> ops, IList<CodeMemberMethod> otherOps, string opName, string expr) {
                foreach (CodeMemberMethod op in ops) {
                    bool haveComplement = false;
                    foreach (CodeMemberMethod otherOp in otherOps) {
                        if (op.ParametersEqual(otherOp)) {
                            haveComplement = true;
                            break;
                        }
                    }
                    if (haveComplement)
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
        }
    }
}
