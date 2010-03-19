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
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

unsafe class Translator {

    GeneratorData data;

    public Translator(GeneratorData data) {
        this.data = data;
    }

#region private data

    class TypeInfo {
        public TypeInfo() {}
        public TypeInfo(string name, int pDepth, bool isRef, bool isConst, bool isUnsigned, string templateParams) {
            Name = name; PointerDepth = pDepth; IsCppRef = isRef; IsConst = isConst; IsUnsigned = isUnsigned; TemplateParameters = templateParams;
        }
        public string Name = string.Empty;
        public int PointerDepth = 0;
        public bool IsCppRef = false;
        public bool IsConst = false;
        public bool IsUnsigned = false;
        public bool IsRef = false;
        public string TemplateParameters = string.Empty;
    }

    delegate object TranslateFunc(TypeInfo type);

    // map a C++ type string to a .NET type
    Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
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
        { "ulong long", typeof(ulong) },
        { "float", typeof(float) },
        { "double", typeof(double) },
        { "bool", typeof(bool) },
        { "void", typeof(void) },

        // used in properties
        { "qreal", typeof(double) },

        // flag types that are not recognised as such
        { "ChangeFlags", typeof(uint) },
        { "ColorDialogOptions", typeof(uint) },
        { "FontDialogOptions", typeof(uint) },
        { "Options", typeof(uint) },
        { "PageSetupDialogOptions", typeof(uint) },
        { "PrintDialogOptions", typeof(uint) },
    };

    // map a C++ type string to a .NET type string
    Dictionary<string, string> typeStringMap = new Dictionary<string, string>()
    {
        { "QList", "System.Collections.Generic.List" },
        { "QStringList", "System.Collections.Generic.List<string>" },
        { "QVector", "System.Collections.Generic.List" },
        { "QHash", "System.Collections.Generic.Dictionary" },
        { "QMap", "System.Collections.Generic.Dictionary" },
        // a hash set would be better, but do we want to depend on System.Core.dll (i.e. .NET 3.5)?
        { "QSet", "System.Collections.Generic.List" },
        { "QQueue", "System.Collections.Generic.Queue" },
        { "QStack", "System.Collections.Generic.Stack" },
    };

    // custom translation code
    Dictionary<string, TranslateFunc> typeCodeMap = new Dictionary<string, TranslateFunc>()
    {
        { "size_t", delegate { throw new NotSupportedException(); } },
        { "_IO_FILE", delegate { throw new NotSupportedException(); } },

        { "_XEvent", delegate { throw new NotSupportedException(); } },
        { "_XDisplay", delegate { throw new NotSupportedException(); } },
        { "_XRegion", delegate { throw new NotSupportedException(); } },
        { "FT_FaceRec_", delegate { throw new NotSupportedException(); } },

        // this is only temporary so that qtgui compiles
        { "Qt::HitTestAccuracy", delegate { throw new NotSupportedException(); } },
        { "QThread", delegate { throw new NotSupportedException(); } },
        { "QMutex", delegate { throw new NotSupportedException(); } },
        { "QDebug", delegate { throw new NotSupportedException(); } },
//         { "QWidget", delegate { throw new NotSupportedException(); } },

        { "QFlags", delegate { throw new NotSupportedException(); } },
        { "QFlag", delegate { throw new NotSupportedException(); } },
        { "QIncompatibleFlag", delegate { throw new NotSupportedException(); } },

        { "QString::Null", delegate { throw new NotSupportedException(); } },
        { "QHashDummyValue", delegate { throw new NotSupportedException(); } },
        { "QPostEventList", delegate { throw new NotSupportedException(); } },
        { "QTextStreamManipulator", delegate { throw new NotSupportedException(); } },
        { "QVariant::Private", delegate { throw new NotSupportedException(); } },
        { "QVariant::Handler", delegate { throw new NotSupportedException(); } },

        { "QGraphicsEffectSource", delegate { throw new NotSupportedException(); } },
        { "QFileDialogArgs", delegate { throw new NotSupportedException(); } },
        { "QX11InfoData", delegate { throw new NotSupportedException(); } },
        { "QTextEngine", delegate { throw new NotSupportedException(); } },
        { "QWindowSurface", delegate { throw new NotSupportedException(); } },

        { "QImageData", delegate { throw new NotSupportedException(); } },
        { "QDrawPixmaps::Data", delegate { throw new NotSupportedException(); } },

        { "QEventPrivate", delegate { throw new NotSupportedException(); } },
        { "QGraphicsSceneEventPrivate", delegate { throw new NotSupportedException(); } },
        { "QIconPrivate", delegate { throw new NotSupportedException(); } },
        { "QKeySequencePrivate", delegate { throw new NotSupportedException(); } },
        { "QPenPrivate", delegate { throw new NotSupportedException(); } },
        { "QUrlPrivate", delegate { throw new NotSupportedException(); } },
        { "QTextDocumentPrivate", delegate { throw new NotSupportedException(); } },

        { "QGenericMatrix", delegate { throw new NotSupportedException(); } },
        { "QScopedPointer", delegate { throw new NotSupportedException(); } },
        { "QExplicitlySharedDataPointer", delegate { throw new NotSupportedException(); } },

        { "void", type => (type.PointerDepth == 0) ? new CodeTypeReference(typeof(void)) : new CodeTypeReference(typeof(IntPtr)) },
        { "char", delegate(TypeInfo type) {
                    if (type.PointerDepth == 1) {
                        if (type.IsUnsigned)
                            return new CodeTypeReference("Pointer<byte>");
                        if (type.IsConst)
                            return "String";
                        return new CodeTypeReference("Pointer<sbyte>");
                    }
                    return null;
                  }},
        { "QString", type => (type.PointerDepth > 0) ? "System.Text.StringBuilder" : "String" },
    };

#endregion

#region public functions

    public CodeTypeReference CppToCSharp(Smoke.Class* klass) {
        bool isRef;
        return CppToCSharp(ByteArrayManager.GetString(klass->className), out isRef);
    }

    public CodeTypeReference CppToCSharp(Smoke.Type* type, out bool isRef) {
        isRef = false;
        if ((IntPtr) type->name == IntPtr.Zero) {
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
        if (typeRef != null)
            return typeRef;

        string typeString = ByteArrayManager.GetString(type->name);
        return CppToCSharp(typeString, out isRef);
    }

    public CodeTypeReference CppToCSharp(string typeString, out bool isRef) {
        // yes, this won't match Foo<...>::Bar - but we can't wrap that anyway
        isRef = false;
        Match match = Regex.Match(typeString, @"^(const )?(unsigned |signed )?([\w\s:]+)(<.+>)?(\*)*(&)?$");
        if (!match.Success) {
            // Console.WriteLine("Can't handle type {0}", typeString);
            throw new NotSupportedException(typeString);
        }
        string ret;
        bool isConst = match.Groups[1].Value != string.Empty;
        bool isUnsigned = match.Groups[2].Value == "unsigned ";
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
            TypeInfo typeInfo = new TypeInfo(name, pointerDepth, isCppRef, isConst, isUnsigned, templateArgument);
            object obj = typeFunc(typeInfo);
            isRef = typeInfo.IsRef;
            if (obj is string) {
                name = (string) obj;
            } else if (obj is CodeTypeReference) {
                return (CodeTypeReference) obj;
            }
        } else {
            // if everything fails, just do some standard mapping
            CodeTypeDeclaration ifaceDecl;
            if (data.InterfaceTypeMap.TryGetValue(name, out ifaceDecl)) {
                // this class is used in multiple inheritance, we need the interface
                int colon = name.LastIndexOf("::");
                string prefix = (colon != -1) ? name.Substring(0, colon) : string.Empty;
                string className = (colon != -1) ? name.Substring(colon + 2) : name;
                name = prefix;
                if (name != string.Empty) {
                    name += '.';
                }
                name += 'I' + className;
            }
            name = name.Replace("::", ".");
        }

        // append 'u' for unsigned types - this is later resolved to a .NET type
        ret = isUnsigned ? 'u' + name : name;
        if (templateArgument != string.Empty) {
            // convert template parameters
            ret += '<';
            bool tmp;
            List<string> args = Util.SplitUnenclosed(templateArgument, ',', '<', '>');
            for (int i = 0; i < args.Count; i++) {
                if (i > 0) ret += ',';
                ret += CppToCSharp(args[i], out tmp).BaseType;
            }
            ret += '>';
        }
        if (pointerDepth == 2 || (Util.IsPrimitiveType(ret) && (pointerDepth == 1 || (isCppRef && !isConst)))) {
            isRef = true;
        }
        Type type;
        if (typeMap.TryGetValue(ret, out type)) {
            return new CodeTypeReference(type);
        }
        return new CodeTypeReference(ret);
    }

#endregion

}
