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

public class QyotoTranslator : ICustomTranslator
{
	private readonly Dictionary<string, Type> typeMap = new Dictionary<string, Type>
		{
			// used in properties, signals and slots
			{ "qreal", typeof(double) },
			{ "qint64", typeof(long) },
			{ "quint64", typeof(ulong) },
			{ "qlonglong", typeof(long) },
			{ "qulonglong", typeof(ulong) },

			// flag types that are not recognised as such
			{ "HDC__", typeof(IntPtr) },
			{ "HPALETTE__", typeof(IntPtr) },
			{ "HBITMAP__", typeof(IntPtr) },
			{ "HICON__", typeof(IntPtr) },
			{ "HFONT__", typeof(IntPtr) },
			{ "HINSTANCE__", typeof(IntPtr) },
			{ "HRGN__", typeof(IntPtr) },
			{ "__HIShape", typeof(IntPtr) },
			{ "OpaqueRgnHandle", typeof(IntPtr) },
			{ "OpaqueEventHandlerCallRef", typeof(IntPtr) },
			{ "OpaqueEventRef", typeof(IntPtr) },
			{ "CGImage", typeof(IntPtr) },

			// OpenGL types
			{ "GLint", typeof(int) },
			{ "GLsizei", typeof(int) },
			{ "GLuint", typeof(uint) },
			{ "GLfloat", typeof(float) },
			{ "GLclampf", typeof(float) },
			{ "GLenum", typeof(int) },
			{ "GLbitfield", typeof(int) },
			{ "GLboolean", typeof(bool) }
		};

	private readonly Dictionary<string, string> typeStringMap = new Dictionary<string, string>
		{
			{ "HWND__", "NativeULong" },
			{ "QList", "System.Collections.Generic.List" },
			{ "QStringList", "System.Collections.Generic.List<string>" },
			{ "QVector", "System.Collections.Generic.List" },
			{ "QHash", "System.Collections.Generic.Dictionary" },
			{ "QMap", "System.Collections.Generic.Dictionary" },
			{ "QMultiMap", "System.Collections.Generic.Dictionary" },
			{ "QSet", "System.Collections.Generic.HashSet" },
			{ "QQueue", "System.Collections.Generic.Queue" },
			{ "QStack", "System.Collections.Generic.Stack" },

			{ "QModelIndexList", "System.Collections.Generic.List<QModelIndex>" }
		};

	private readonly Dictionary<string, Translator.TranslateFunc> typeCodeMap = new Dictionary
		<string, Translator.TranslateFunc>
		{
			// HACK: Work around a missing qyoto-qtdeclarative assembly.
			{ "QDeclarativeListProperty", delegate { throw new NotSupportedException(); } },
			{ "NavigationMenu", delegate { throw new NotSupportedException(); } },

			{ "QThread", delegate { throw new NotSupportedException(); } },
			{ "QMutex", delegate { throw new NotSupportedException(); } },
			{ "QDebug", delegate { throw new NotSupportedException(); } },

			{ "QFlags", delegate { throw new NotSupportedException(); } },
			{ "QFlag", delegate { throw new NotSupportedException(); } },
			{ "QIncompatibleFlag", delegate { throw new NotSupportedException(); } },
			{ "QPointer", delegate { throw new NotSupportedException(); } },

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

			// phonon stuff
			{ "AudioOutputDevice", delegate { throw new NotSupportedException(); } },
			{ "Phonon::AudioOutputDevice", delegate { throw new NotSupportedException(); } },
			{ "Phonon::AbstractAudioOutputPrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::AbstractMediaStreamPrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::AbstractVideoOutputPrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::EffectPrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::MediaNodePrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::MediaSourcePrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::ObjectDescription", delegate { throw new NotSupportedException(); } },
			{ "Phonon::ObjectDescriptionPrivate", delegate { throw new NotSupportedException(); } },
			{ "Phonon::VideoWidgetPrivate", delegate { throw new NotSupportedException(); } },

			{ "__darwin_va_list", delegate { throw new NotSupportedException(); } },
			{ "__gnuc_va_list", delegate { throw new NotSupportedException(); } },

			{
				"void", delegate(Translator.TypeInfo type, GeneratorData data, Translator translator)
					{
						if (type.PointerDepth == 0)
						{
							return new CodeTypeReference(typeof(void));
						}
						throw new NotSupportedException();
					}
			},
			{
				"char", delegate(Translator.TypeInfo type, GeneratorData data, Translator translator)
					{
						if (type.PointerDepth == 1)
						{
							if (type.IsUnsigned)
								return new CodeTypeReference("Pointer<byte>");
							if (type.IsConst)
								return "System.String";
							return new CodeTypeReference("Pointer<sbyte>");
						}
						return null;
					}
			},
			{ "QString", (type, data, translator) => (type.PointerDepth > 0) ? "System.Text.StringBuilder" : "System.String" },
			{ "QVariant", (type, data, translator) => new CodeTypeReference(typeof(object)) },

			{
				"QWidget", delegate(Translator.TypeInfo type, GeneratorData data, Translator translator)
					{
						unsafe
						{
							if (data.Smoke->ToString() == "qtcore")
							{
								throw new NotSupportedException();
							}
							return null;
						}
					}
			},
		};

	private readonly List<string> interfaceClasses = new List<string>
		{
			"QSharedData",
		};

	private readonly List<Regex> excludedMethods = new List<Regex>
		{
			new Regex(@"QSysInfo::windowsVersion.*", RegexOptions.Compiled),
			new Regex(@".*::qt_.*\(", RegexOptions.Compiled),
			new Regex(@"QCoreApplication::QCoreApplication.*", RegexOptions.Compiled),
			new Regex(@"QCoreApplication::exec.*", RegexOptions.Compiled),
			new Regex(@"QApplication::QApplication.*", RegexOptions.Compiled),
			new Regex(@"QApplication::exec.*", RegexOptions.Compiled),
			new Regex(@".*::metaObject\(\).*", RegexOptions.Compiled),
		};

	private readonly List<string> namespacesAsClasses = new List<string>
		{
			"Qt",
		};

	public IDictionary<string, Type> TypeMap
	{
		get { return typeMap; }
	}

	public IDictionary<string, string> TypeStringMap
	{
		get { return typeStringMap; }
	}

	public IDictionary<string, Translator.TranslateFunc> TypeCodeMap
	{
		get { return typeCodeMap; }
	}

	public IList<string> InterfaceClasses
	{
		get { return interfaceClasses; }
	}

	public IList<Regex> ExcludedMethods
	{
		get { return excludedMethods; }
	}

	public IList<string> NamespacesAsClasses
	{
		get { return namespacesAsClasses; }
	}
}
