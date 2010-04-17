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
using System.Collections.Generic;
using System.CodeDom;
using System.Text.RegularExpressions;

public class QyotoTranslator : ICustomTranslator {

    Dictionary<string, Type> typeMap = new Dictionary<string, Type>()
    {
        // used in properties, signals and slots
        { "qreal", typeof(double) },
        { "qint64", typeof(long) },

        // flag types that are not recognised as such
        { "ChangeFlags", typeof(uint) },
        { "ColorDialogOptions", typeof(uint) },
        { "FontDialogOptions", typeof(uint) },
        { "Options", typeof(uint) },
        { "PageSetupDialogOptions", typeof(uint) },
        { "PrintDialogOptions", typeof(uint) },
        { "QScriptEngine.QObjectWrapOptions", typeof(uint) },
        { "QScriptValue.PropertyFlags", typeof(uint) },
        { "QScriptValue.ResolveFlags", typeof(uint) },
        { "WatchMode", typeof(uint) },
    };

    Dictionary<string, string> typeStringMap = new Dictionary<string, string>()
    {
        { "QList", "System.Collections.Generic.List" },
        { "QStringList", "System.Collections.Generic.List<string>" },
        { "QVector", "System.Collections.Generic.List" },
        { "QHash", "System.Collections.Generic.Dictionary" },
        { "QMap", "System.Collections.Generic.Dictionary" },
        { "QMultiMap", "System.Collections.Generic.Dictionary" },
        // a hash set would be better, but do we want to depend on System.Core.dll (i.e. .NET 3.5)?
        { "QSet", "System.Collections.Generic.List" },
        { "QQueue", "System.Collections.Generic.Queue" },
        { "QStack", "System.Collections.Generic.Stack" },
    };

    Dictionary<string, Translator.TranslateFunc> typeCodeMap = new Dictionary<string, Translator.TranslateFunc>()
    {
        { "QThread", delegate { throw new NotSupportedException(); } },
        { "QMutex", delegate { throw new NotSupportedException(); } },
        { "QDebug", delegate { throw new NotSupportedException(); } },

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
        { "DBusError", delegate { throw new NotSupportedException(); } },

        { "QImageData", delegate { throw new NotSupportedException(); } },
        { "QDrawPixmaps::Data", delegate { throw new NotSupportedException(); } },

        { "QEventPrivate", delegate { throw new NotSupportedException(); } },
        { "QGraphicsSceneEventPrivate", delegate { throw new NotSupportedException(); } },
        { "QIconPrivate", delegate { throw new NotSupportedException(); } },
        { "QKeySequencePrivate", delegate { throw new NotSupportedException(); } },
        { "QPenPrivate", delegate { throw new NotSupportedException(); } },
        { "QUrlPrivate", delegate { throw new NotSupportedException(); } },

        { "QPatternist::Item", delegate { throw new NotSupportedException(); } },

        { "QScriptClassPrivate", delegate { throw new NotSupportedException(); } },
        { "QScriptClassPropertyIteratorPrivate", delegate { throw new NotSupportedException(); } },
        { "QScriptEnginePrivate", delegate { throw new NotSupportedException(); } },
        { "QScriptEngineAgentPrivate", delegate { throw new NotSupportedException(); } },
        { "QScriptProgram", delegate { throw new NotSupportedException(); } },

        { "QWebPagePrivate", delegate { throw new NotSupportedException(); } },
        { "QWebSettingsPrivate", delegate { throw new NotSupportedException(); } },

        { "QTextDocumentPrivate", delegate { throw new NotSupportedException(); } },
        { "QDomNodePrivate", delegate { throw new NotSupportedException(); } },

        { "QGenericMatrix", delegate { throw new NotSupportedException(); } },
        { "QScopedPointer", delegate { throw new NotSupportedException(); } },
        { "QExplicitlySharedDataPointer", delegate { throw new NotSupportedException(); } },

        { "void", type => (type.PointerDepth == 0) ? new CodeTypeReference(typeof(void)) : new CodeTypeReference(typeof(IntPtr)) },
        { "char", delegate(Translator.TypeInfo type) {
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

        { "QWidget", delegate(Translator.TypeInfo type) {
                        unsafe {
                            if (type.GeneratorData.Smoke->ToString() == "qtcore") {
                                throw new NotSupportedException();
                            }
                            return null;
                        }
                     }},
    };

    List<Regex> excludedMethods = new List<Regex>()
    {
        new Regex(@".*::qt_.*\("),
        new Regex(@"QCoreApplication::QCoreApplication.*"),
        new Regex(@"QApplication::QApplication.*"),
    };

    List<string> namespacesAsClasses = new List<string>()
    {
        "Qt",
    };

    public IDictionary<string, Type> TypeMap { get { return typeMap; } }
    public IDictionary<string, string> TypeStringMap { get { return typeStringMap; } }
    public IDictionary<string, Translator.TranslateFunc> TypeCodeMap { get { return typeCodeMap; } }

    public IList<Regex> ExcludedMethods { get { return excludedMethods; } }
    public IList<string> NamespacesAsClasses { get { return namespacesAsClasses; } }
}
