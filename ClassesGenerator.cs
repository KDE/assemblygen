using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

unsafe class ClassesGenerator {
    Smoke* smoke;
    CodeCompileUnit unit;
    CodeNamespace cnDefault;
    Dictionary<string, CodeNamespace> namespaceMap = new Dictionary<string, CodeNamespace>();
    Dictionary<string, CodeTypeDeclaration> typeMap = new Dictionary<string, CodeTypeDeclaration>();

    public ClassesGenerator(Smoke* smoke, CodeCompileUnit unit, string defaultNamespace) {
        this.smoke = smoke;
        this.unit = unit;
        this.cnDefault = new CodeNamespace(defaultNamespace);
        unit.Namespaces.Add(cnDefault);
        namespaceMap[defaultNamespace] = cnDefault;
    }

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

    public void Run() {
        MethodsGenerator methgen = null;
        short klass = 0;
        CodeTypeDeclaration type = null;

        for (short i = 0; i < smoke->numMethods; i++) {
            Smoke.Method *meth = smoke->methods + i;

            if ((meth->flags & (ushort) Smoke.MethodFlags.mf_enum) > 0)
                continue;   // don't process enums here

            if (klass != meth->classId) {
                klass = meth->classId;
                Smoke.Class *smokeClass = smoke->classes + klass;
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

                CodeAttributeDeclaration attr = new CodeAttributeDeclaration("SmokeClass",
                    new CodeAttributeArgument(new CodePrimitiveExpression(smokeName)));
                type = new CodeTypeDeclaration(name);
                type.CustomAttributes.Add(attr);
                typeMap[mapName] = type;
                GetTypeCollection(prefix).Add(type);
                methgen = new MethodsGenerator(smoke, type);
            }
            methgen.Generate(i);
        }
    }
}
