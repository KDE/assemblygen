using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

static class Translator {

/*
    static bool IsPrimitiveType(string input) {
        if (   input == "char" || input == "short" || input == "int" || input == "long" || input == "float"
            || input == "double" || input == "long long")
        {
            return true;
        }
        return false;
    }
*/

    static List<string> SplitUnenclosed(string input, char delimeter, char open, char close) {
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

    public unsafe static string CppToCSharp(Smoke.Type* type, out bool isRef) {
        if ((IntPtr) type->name == IntPtr.Zero) {
            isRef = false;
            return "void";
        }

        Smoke.TypeId typeId = (Smoke.TypeId) (type->flags & (ushort) Smoke.TypeFlags.tf_elem);
        string primitiveType = null;
        if (typeId == Smoke.TypeId.t_bool) {
            primitiveType = "bool";
        } else if (typeId == Smoke.TypeId.t_char) {
            primitiveType = "sbyte";
        } else if (typeId == Smoke.TypeId.t_uchar) {
            primitiveType = "byte";
        } else if (typeId == Smoke.TypeId.t_short) {
            primitiveType = "short";
        } else if (typeId == Smoke.TypeId.t_ushort) {
            primitiveType = "ushort";
        } else if (typeId == Smoke.TypeId.t_int) {
            primitiveType = "int";
        } else if (typeId == Smoke.TypeId.t_uint) {
            primitiveType = "uint";
        } else if (typeId == Smoke.TypeId.t_long) {
            primitiveType = "long";
        } else if (typeId == Smoke.TypeId.t_ulong) {
            primitiveType = "ulong";
        } else if (typeId == Smoke.TypeId.t_float) {
            primitiveType = "float";
        } else if (typeId == Smoke.TypeId.t_double) {
            primitiveType = "double";
        }
        if (primitiveType != null) {
            if ((type->flags & (ushort) Smoke.TypeFlags.tf_ptr) > 0) {
                if (primitiveType == "sbyte") {
                    if ((type->flags & (ushort) Smoke.TypeFlags.tf_const) > 0) {
                        // const char*
                        isRef = false;
                        return "string";
                    } else {
                        isRef = false;
                        return "Pointer<sbyte>";
                    }
                }
                isRef = true;
                return primitiveType;
            }
            if ((type->flags & (ushort) Smoke.TypeFlags.tf_ref) > 0) {
                isRef = true;
                return primitiveType;
            }
        }

        string typeString = ByteArrayManager.GetString(type->name);
        return CppToCSharp(typeString, out isRef);
    }

    public static string CppToCSharp(string typeString, out bool isRef) {
        // yes, this won't match Foo<...>::Bar - but we can't wrap that anyway
        isRef = false;
        Match match = Regex.Match(typeString, @"^(const )?(unsigned )?([\w\s:]+)(<.+>)?(\*)*(&)?$");
        if (!match.Success) {
//             Console.WriteLine("Can't handle type {0}", typeString);
            throw new NotSupportedException(typeString);
        }
        string ret;
//         bool isConst = match.Groups[1].Value != string.Empty;
        bool isUnsigned = match.Groups[2].Value != string.Empty;
        string name = match.Groups[3].Value;
        string templateArgument = match.Groups[4].Value;
        if (templateArgument != string.Empty) {
            // strip surrounding < and >
            templateArgument = templateArgument.Substring(1, templateArgument.Length - 2);
        }
        int pointerDepth = match.Groups[5].Value.Length;
//         bool isCppRef = match.Groups[6].Value != string.Empty;

        if (name == "QString") {
            if (pointerDepth > 0) {
                name = "System.Text.StringBuilder";
            } else {
                name = "string";
            }
        } else if (name == "QList") {
            name = "System.Collections.Generic.List";
        } else if (name == "QVector") {
            name = "System.Collections.Generic.List";
        } else if (name == "QHash") {
            name = "System.Collections.Generic.Dictionary";
        } else if (name == "QMap") {
            name = "System.Collections.Generic.Dictionary";
        } else {
            name = name.Replace("::", ".");
        }

        ret = isUnsigned ? 'u' + name : name;
        if (templateArgument != string.Empty) {
            ret += '<';
            bool tmp;
            List<string> args = SplitUnenclosed(templateArgument, ',', '<', '>');
            for (int i = 0; i < args.Count; i++) {
                if (i > 0) ret += ',';
                ret += CppToCSharp(args[i], out tmp);
            }
            ret += '>';
        }
        if (pointerDepth == 2) {
            isRef = true;
        }
        return ret;
    }
}
