using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

static class Translator {

    // map a C++ type string to a .NET type
    static Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
    {
        { "char", typeof(sbyte) },
        { "uchar", typeof(byte) },
        { "short", typeof(short) },
        { "ushort", typeof(ushort) },
        { "int", typeof(int) },
        { "uint", typeof(uint) },
        { "long", typeof(long) },
        { "long long", typeof(long) },
        { "ulong", typeof(ulong) },
        { "float", typeof(float) },
        { "double", typeof(double) },
        { "bool", typeof(bool) },
        { "void", typeof(void) },
    };

    // map a C++ type string to a .NET type string
    static Dictionary<string, string> typeStringMap = new Dictionary<string, string>()
    {
        { "QList", "System.Collections.Generic.List" },
        { "QVector", "System.Collections.Generic.List" },
        { "QHash", "System.Collections.Generic.Dictionary" },
        { "QMap", "System.Collections.Generic.Dictionary" },
    };

    // custom translation code
    static Dictionary<string, TranslateFunc> typeCodeMap = new Dictionary<string, TranslateFunc>()
    {
        { "void", type => (type.PointerDepth == 0) ? new CodeTypeReference(typeof(void)) : new CodeTypeReference(typeof(IntPtr)) },
        { "QString", type => (type.PointerDepth > 0) ? "System.Text.StringBuilder" : "String" }
    };

    // C++ namespaces that should be mapped to .NET classes
    public static List<string> namespacesAsClasses = new List<string>()
    {
        "Qt",
        "KDE"
    };

    class TypeInfo {
        public TypeInfo() {}
        public TypeInfo(string name, int pDepth, bool isRef, bool isConst, string templateParams) {
            Name = name; PointerDepth = pDepth; IsCppRef = isRef; IsConst = isConst; TemplateParameters = templateParams;
        }
        public string Name = string.Empty;
        public int PointerDepth = 0;
        public bool IsCppRef = false;
        public bool IsConst = false;
        public bool IsRef = false;
        public string TemplateParameters = string.Empty;
    }

    delegate object TranslateFunc(TypeInfo type);

    public static bool IsPrimitiveType(string input) {
        if (   input == "char" || input == "short" || input == "int" || input == "long" || input == "float"
            || input == "double" || input == "long long" || input == "bool")
        {
            return true;
        }
        return false;
    }

    public static List<string> SplitUnenclosed(string input, char delimeter, char open, char close) {
        int enclosed = 0;
        int lastDelimeter = -1;
        List<string> ret = new List<string>();
        for (int i = 0; i < input.Length; i++) {
            char c = input[i];
            if (c == open) {
                enclosed++;
            } else if (c == close) {
                enclosed--;
            } else if (c == delimeter && enclosed == 0) {
                ret.Add(input.Substring(lastDelimeter + 1, i - lastDelimeter - 1));
                lastDelimeter = i;
            }
        }
        ret.Add(input.Substring(lastDelimeter + 1));
        return ret;
    }

    public unsafe static CodeTypeReference CppToCSharp(Smoke.Type* type, out bool isRef) {
        if ((IntPtr) type->name == IntPtr.Zero) {
            isRef = false;
            return new CodeTypeReference(typeof(void));
        }

        Smoke.TypeId typeId = (Smoke.TypeId) (type->flags & (ushort) Smoke.TypeFlags.tf_elem);
        CodeTypeReference typeRef = null;
        if (typeId == Smoke.TypeId.t_bool) {
            typeRef = new CodeTypeReference(typeof(bool));
        } else if (typeId == Smoke.TypeId.t_char) {
            typeRef = new CodeTypeReference(typeof(sbyte));
        } else if (typeId == Smoke.TypeId.t_uchar) {
            typeRef = new CodeTypeReference(typeof(byte));
        } else if (typeId == Smoke.TypeId.t_short) {
            typeRef = new CodeTypeReference(typeof(short));
        } else if (typeId == Smoke.TypeId.t_ushort) {
            typeRef = new CodeTypeReference(typeof(ushort));
        } else if (typeId == Smoke.TypeId.t_int) {
            typeRef = new CodeTypeReference(typeof(int));
        } else if (typeId == Smoke.TypeId.t_uint) {
            typeRef = new CodeTypeReference(typeof(uint));
        } else if (typeId == Smoke.TypeId.t_long) {
            typeRef = new CodeTypeReference(typeof(long));
        } else if (typeId == Smoke.TypeId.t_ulong) {
            typeRef = new CodeTypeReference(typeof(ulong));
        } else if (typeId == Smoke.TypeId.t_float) {
            typeRef = new CodeTypeReference(typeof(float));
        } else if (typeId == Smoke.TypeId.t_double) {
            typeRef = new CodeTypeReference(typeof(double));
        }
        if (typeRef != null) {
            if ((type->flags & (ushort) Smoke.TypeFlags.tf_ptr) > 0) {
                if (typeId == Smoke.TypeId.t_char) {
                    if ((type->flags & (ushort) Smoke.TypeFlags.tf_const) > 0) {
                        // const char*
                        isRef = false;
                        return new CodeTypeReference(typeof(string));
                    } else {
                        isRef = false;
                        return new CodeTypeReference("Pointer<sbyte>");
                    }
                }
                isRef = true;
            }
            if ((type->flags & (ushort) Smoke.TypeFlags.tf_ref) > 0) {
                isRef = true;
            }
            isRef = false;
            return typeRef;
        }

        string typeString = ByteArrayManager.GetString(type->name);
        return CppToCSharp(typeString, out isRef);
    }

    public static CodeTypeReference CppToCSharp(string typeString, out bool isRef) {
        // yes, this won't match Foo<...>::Bar - but we can't wrap that anyway
        isRef = false;
        Match match = Regex.Match(typeString, @"^(const )?(unsigned )?([\w\s:]+)(<.+>)?(\*)*(&)?$");
        if (!match.Success) {
            // Console.WriteLine("Can't handle type {0}", typeString);
            throw new NotSupportedException(typeString);
        }
        string ret;
        bool isConst = match.Groups[1].Value != string.Empty;
        bool isUnsigned = match.Groups[2].Value != string.Empty;
        string name = match.Groups[3].Value;
        string templateArgument = match.Groups[4].Value;
        if (templateArgument != string.Empty) {
            // strip surrounding < and >
            templateArgument = templateArgument.Substring(1, templateArgument.Length - 2);
        }
        int pointerDepth = match.Groups[5].Value.Length;
        bool isCppRef = match.Groups[6].Value != string.Empty;

        // look up the translations in the translation tables above
        string partialTypeStr;
        TranslateFunc typeFunc;
        if (typeStringMap.TryGetValue(name, out partialTypeStr)) {
            // C++ type string => .NET type string
            name = partialTypeStr;
        } else if (typeCodeMap.TryGetValue(name, out typeFunc)) {
            // try to look up custom translation code
            TypeInfo typeInfo = new TypeInfo(name, pointerDepth, isCppRef, isConst, templateArgument);
            object obj = typeFunc(typeInfo);
            if (obj is string) {
                name = (string) obj;
            } else if (obj is CodeTypeReference) {
                isRef = typeInfo.IsRef;
                return (CodeTypeReference) obj;
            }
        } else {
            // if everything fails, just do some standard mapping
            name = name.Replace("::", ".");
        }

        // append 'u' for unsigned types - this is later resolved to a .NET type
        ret = isUnsigned ? 'u' + name : name;
        if (templateArgument != string.Empty) {
            // convert template parameters
            ret += '<';
            bool tmp;
            List<string> args = SplitUnenclosed(templateArgument, ',', '<', '>');
            for (int i = 0; i < args.Count; i++) {
                if (i > 0) ret += ',';
                ret += CppToCSharp(args[i], out tmp);
            }
            ret += '>';
        }
        if (pointerDepth == 2 || (isCppRef && !isConst)) {
            isRef = true;
        }
        Type type;
        if (typeMap.TryGetValue(ret, out type)) {
            return new CodeTypeReference(type);
        }
        return new CodeTypeReference(ret);
    }
}
