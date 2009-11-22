using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

unsafe class ClassesGenerator {
    public readonly Smoke* smoke;
    public readonly CodeCompileUnit unit;
    public readonly CodeNamespace cnDefault;

    // maps a C++ namespace to a .NET namespace
    Dictionary<string, CodeNamespace> namespaceMap = new Dictionary<string, CodeNamespace>();
    // maps a C++ class to a .NET class
    Dictionary<string, CodeTypeDeclaration> typeMap = new Dictionary<string, CodeTypeDeclaration>();
    // maps a C++ class to a .NET interface (needed for multiple inheritance)
    Dictionary<string, CodeTypeDeclaration> interfaceTypeMap = new Dictionary<string, CodeTypeDeclaration>();

    public ClassesGenerator(Smoke* smoke, CodeCompileUnit unit, string defaultNamespace) {
        this.smoke = smoke;
        this.unit = unit;
        this.cnDefault = new CodeNamespace(defaultNamespace);
        unit.Namespaces.Add(cnDefault);
        namespaceMap[defaultNamespace] = cnDefault;
    }

    /*
     * Returns the collection of sub-types for a given prefix (which may be a namespace or a class).
     * If 'prefix' is empty, returns the collection of the default namespace.
     */
    public IList GetTypeCollection(string prefix) {
        if (prefix == null || prefix == string.Empty)
            return cnDefault.Types;
        CodeNamespace nspace;
        CodeTypeDeclaration typeDecl;
        if (namespaceMap.TryGetValue(prefix, out nspace)) {
            return nspace.Types;
        }
        if (typeMap.TryGetValue(prefix, out typeDecl)) {
            return typeDecl.Members;
        }
        
        short id = smoke->idClass(prefix);
        Smoke.Class *klass = smoke->classes + id;
        if (id != 0 && klass->size > 0) {
            throw new Exception("Found class instead of namespace - this should not happen!");
        }
        
        IList parentCollection = unit.Namespaces;
        string name = prefix;
        int colon = name.LastIndexOf("::");
        if (colon != -1) {
            parentCollection = GetTypeCollection(name.Substring(0, colon));
            name = prefix.Substring(colon + 2);
        }

        nspace = new CodeNamespace(name);
        parentCollection.Add(nspace);
        namespaceMap[prefix] = nspace;
        return nspace.Types;
    }

    CodeTypeDeclaration DefineClass(Smoke.Class* smokeClass) {
        string smokeName = ByteArrayManager.GetString(smokeClass->className), mapName = smokeName;
        string name;
        string prefix = string.Empty;
        if (smokeClass->size == 0) {
            // namespace
            prefix = smokeName;
            name = "Global";
            mapName = prefix + "::" + "Global";
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
        if (smokeClass->parents == 0) {
            if (smokeName == "QObject") {
                type.BaseTypes.Add(new CodeTypeReference("Qt"));
            } else {
                type.BaseTypes.Add(new CodeTypeReference(typeof(object)));
            }
        } else {
            short *parent = smoke->inheritanceList + smokeClass->parents;
            bool firstParent = true;
            while (*parent > 0) {
                if (firstParent) {

                    firstParent = false;
                    parent++;
                }
                parent++;
            }
        }

        typeMap[mapName] = type;
        GetTypeCollection(prefix).Add(type);
        return type;
    }

    /*
     * Loops through all wrapped methods. Any class that is found is converted to a .NET class.
     * A class Namespace::Foo is mapped to Namespace.Foo. Classes that are not in any namespace go into the default namespace.
     * For namespaces that contain functions, a Namespace.Global class is created which holds the functions as methods.
     * A MethodGenerator is then created to generate the methods for that class.
     */
    public void Run() {
        ClassInterfacesGenerator cig = new ClassInterfacesGenerator(this);
        cig.Run();
        MethodsGenerator methgen = null;
        short klass = 0;
        CodeTypeDeclaration type = null;

        for (short i = 0; i < smoke->numMethods; i++) {
            Smoke.Method *meth = smoke->methods + i;

            if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
                continue;   // don't process enums here

            if (klass != meth->classId) {
                // we encountered a new class
                type = DefineClass(smoke->classes + klass);
                methgen = new MethodsGenerator(smoke, type);
            }

            methgen.Generate(i);
        }
    }
}
