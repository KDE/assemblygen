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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.Text.RegularExpressions;

public unsafe class QyotoHooks : IHookProvider
{

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	delegate void AddSignal(string signature, string name, string returnType, IntPtr metaMethod);

	[DllImport("qyotogenerator-native", CallingConvention = CallingConvention.Cdecl)]
	static extern void GetSignals(Smoke* smoke, void* klass, AddSignal addSignalFn);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	delegate void AddParameter(string type, string name);

	[DllImport("qyotogenerator-native", CallingConvention = CallingConvention.Cdecl)]
	static extern void GetMetaMethodParameters(IntPtr metaMethod, AddParameter addParamFn);

	const string QObjectDummyCtorCode =
"            try {\n" +
"                Type proxyInterface = Qyoto.GetSignalsInterface(GetType());\n" +
"                SignalInvocation realProxy = new SignalInvocation(proxyInterface, this);\n" +
"                Q_EMIT = realProxy.GetTransparentProxy();\n" +
"            }\n" +
"            catch (Exception e) {\n" +
"                Console.WriteLine(\"Could not retrieve signal interface: {0}\", e);\n" +
"            }";

	public void RegisterHooks()
	{
		ClassesGenerator.PreMembersHooks += PreMembersHook;
		ClassesGenerator.PostMembersHooks += PostMembersHook;
		ClassesGenerator.SupportingMethodsHooks += SupportingMethodsHook;
		ClassesGenerator.PreClassesHook += PreClassesHook;
		ClassesGenerator.PostClassesHook += PostClassesHook;
		EnumGenerator.PostEnumMemberHook += this.PostEnumMemberHook;
		MethodsGenerator.PostMethodBodyHooks += this.PostMethodBodyHooks;
		Console.WriteLine("Registered Qyoto hooks.");
	}

	public Translator Translator { get; set; }
	public GeneratorData Data { get; set; }
	private readonly Dictionary<string, CodeMemberMethod> eventMethods = new Dictionary<string, CodeMemberMethod>();
	private readonly Dictionary<CodeTypeDeclaration, string> memberDocumentation = new Dictionary<CodeTypeDeclaration, string>();

	public void PreMembersHook(Smoke* smoke, Smoke.Class* klass, CodeTypeDeclaration type)
	{
		if (type.Name == "QObject")
		{
			// Add 'Qt' base class
			type.BaseTypes.Add(new CodeTypeReference("Qt"));

			// add the Q_EMIT field
			CodeMemberField Q_EMIT = new CodeMemberField(typeof(object), "Q_EMIT");
			Q_EMIT.Attributes = MemberAttributes.Family;
			Q_EMIT.InitExpression = new CodePrimitiveExpression(null);
			type.Members.Add(Q_EMIT);
		}
	}

	public void PostMembersHook(Smoke* smoke, Smoke.Class* klass, CodeTypeDeclaration type)
	{
		if (Util.IsQObject(klass))
		{
			CodeMemberProperty emit = new CodeMemberProperty();
			emit.Name = "Emit";
			emit.Attributes = MemberAttributes.Family | MemberAttributes.New | MemberAttributes.Final;
			emit.HasGet = true;
			emit.HasSet = false;

			string signalsIfaceName = "I" + type.Name + "Signals";
			CodeTypeReference returnType = new CodeTypeReference(signalsIfaceName);
			emit.Type = returnType;

			emit.GetStatements.Add(new CodeMethodReturnStatement(new CodeCastExpression(
				returnType,
				new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "Q_EMIT")
			)));

			type.Members.Add(emit);

			string className = ByteArrayManager.GetString(klass->className);
			int colon = className.LastIndexOf("::", StringComparison.Ordinal);
			string prefix = (colon != -1) ? className.Substring(0, colon) : string.Empty;

			IList typeCollection = Data.GetTypeCollection(prefix);
			CodeTypeDeclaration ifaceDecl = new CodeTypeDeclaration(signalsIfaceName);
			ifaceDecl.IsInterface = true;

			if (className != "QObject")
			{
				string parentClassName = ByteArrayManager.GetString(smoke->classes[smoke->inheritanceList[klass->parents]].className);
				colon = parentClassName.LastIndexOf("::", StringComparison.Ordinal);
				prefix = (colon != -1) ? parentClassName.Substring(0, colon) : string.Empty;
				if (colon != -1)
				{
					parentClassName = parentClassName.Substring(colon + 2);
				}

				string parentInterface = (prefix != string.Empty) ? prefix.Replace("::", ".") + "." : string.Empty;
				parentInterface += "I" + parentClassName + "Signals";

				ifaceDecl.BaseTypes.Add(new CodeTypeReference(parentInterface));
			}
			Dictionary<CodeSnippetTypeMember, CodeMemberMethod> signalEvents = new Dictionary<CodeSnippetTypeMember, CodeMemberMethod>();
			GetSignals(smoke, klass, delegate(string signature, string name, string typeName, IntPtr metaMethod)
			{
				CodeMemberMethod signal = new CodeMemberMethod();
				signal.Attributes = MemberAttributes.Abstract;

				// capitalize the first letter
				StringBuilder builder = new StringBuilder(name);
				builder[0] = char.ToUpper(builder[0]);
				string tmp = builder.ToString();

				signal.Name = tmp;
				bool isRef;
				try
				{
					if (typeName == string.Empty)
						signal.ReturnType = new CodeTypeReference(typeof(void));
					else
						signal.ReturnType = Translator.CppToCSharp(typeName, out isRef);
				}
				catch (NotSupportedException)
				{
					Debug.Print("  |--Won't wrap signal {0}::{1}", className, signature);
					return;
				}

				CodeAttributeDeclaration attr = new CodeAttributeDeclaration("Q_SIGNAL",
					new CodeAttributeArgument(new CodePrimitiveExpression(signature)));
				signal.CustomAttributes.Add(attr);

				int argNum = 1;
				StringBuilder fullNameBuilder = new StringBuilder("Slot");
				GetMetaMethodParameters(metaMethod, delegate(string paramType, string paramName)
				{
					if (paramName == string.Empty)
					{
						paramName = "arg" + argNum.ToString();
					}
					argNum++;

					CodeParameterDeclarationExpression param;
					try
					{
						short id = smoke->IDType(paramType);
						CodeTypeReference paramTypeRef;
						if (id > 0)
						{
							paramTypeRef = Translator.CppToCSharp(smoke->types + id, out isRef);
						}
						else
						{
							if (!paramType.Contains("::"))
							{
								id = smoke->IDType(className + "::" + paramType);
								if (id > 0)
								{
									paramTypeRef = Translator.CppToCSharp(smoke->types + id, out isRef);
								}
								else
								{
									paramTypeRef = Translator.CppToCSharp(paramType, out isRef);
								}
							}
							else
							{
								paramTypeRef = Translator.CppToCSharp(paramType, out isRef);
							}
						}
						param = new CodeParameterDeclarationExpression(paramTypeRef, paramName);
					}
					catch (NotSupportedException)
					{
						Debug.Print("  |--Won't wrap signal {0}::{1}", className, signature);
						return;
					}
					if (isRef)
					{
						param.Direction = FieldDirection.Ref;
					}
					signal.Parameters.Add(param);
					if (argNum == 2)
					{
						fullNameBuilder.Append('<');
					}
					fullNameBuilder.Append(param.Type.BaseType);
					fullNameBuilder.Append(',');
				});
				if (fullNameBuilder[fullNameBuilder.Length - 1] == ',')
				{
					fullNameBuilder[fullNameBuilder.Length - 1] = '>';
				}
				ifaceDecl.Members.Add(signal);
				CodeSnippetTypeMember signalEvent = new CodeSnippetTypeMember();
				signalEvent.Name = signal.Name;
				CodeSnippetTypeMember existing = signalEvents.Keys.FirstOrDefault(m => m.Name == signal.Name);
				if (existing != null)
				{
					CodeSnippetTypeMember signalEventToUse;
					CodeMemberMethod signalToUse;
					if (signal.Parameters.Count == 0)
					{
						signalEventToUse = existing;
						signalToUse = signalEvents[existing];
					}
					else
					{
						signalEventToUse = signalEvent;
						signalToUse = signal;
					}
					string suffix = signalToUse.Parameters.Cast<CodeParameterDeclarationExpression>().Last().Name;
					if (suffix.StartsWith("arg") && suffix.Length > 3 && char.IsDigit(suffix[3]))
					{
						string lastType = signalToUse.Parameters.Cast<CodeParameterDeclarationExpression>().Last().Type.BaseType;
						suffix = lastType.Substring(lastType.LastIndexOf('.') + 1);
					}
					else
					{
						StringBuilder lastParamBuilder = new StringBuilder(suffix);
						lastParamBuilder[0] = char.ToUpper(lastParamBuilder[0]);
						suffix = lastParamBuilder.ToString();
					}
					signalEventToUse.Text = signalEventToUse.Text.Replace(signalEventToUse.Name, signalEventToUse.Name += suffix);
				}
				signalEvent.Text = string.Format(@"
        public event {0} {1}
		{{
			add
			{{
                QObject.Connect(this, Qt.SIGNAL(""{2}""), (QObject) value.Target, Qt.SLOT(value.Method.Name + ""{3}""));
			}}
			remove
			{{
                QObject.Disconnect(this, Qt.SIGNAL(""{2}""), (QObject) value.Target, Qt.SLOT(value.Method.Name + ""{3}""));
			}}
		}}", fullNameBuilder, signalEvent.Name, signature, signature.Substring(signature.IndexOf('(')));
				signalEvents.Add(signalEvent, signal);
			});

			typeCollection.Add(ifaceDecl);
			foreach (KeyValuePair<CodeSnippetTypeMember, CodeMemberMethod> signalEvent in signalEvents)
			{
				CodeSnippetTypeMember implementation = signalEvent.Key;
				CodeCommentStatementCollection comments = new CodeCommentStatementCollection();
				foreach (CodeTypeMember current in from CodeTypeMember member in type.Members
												   where member.Name == implementation.Name
												   select member)
				{
					if (comments.Count == 0)
					{
						comments.AddRange(current.Comments);
					}
					current.Name = "On" + current.Name;
				}
				signalEvent.Value.Comments.AddRange(comments);
				signalEvent.Key.Comments.AddRange(comments);
				type.Members.Add(signalEvent.Key);
			}
		}
	}

	public void SupportingMethodsHook(Smoke* smoke, Smoke.Method* method, CodeMemberMethod cmm, CodeTypeDeclaration type)
	{
		if (type.Name == "QObject" && cmm is CodeConstructor)
		{
			cmm.Statements.Add(new CodeSnippetStatement(QObjectDummyCtorCode));
		}
	}

	public void PreClassesHook()
	{
		string htmlDocs = Path.Combine(Data.Docs, "html");
		if (Directory.Exists(htmlDocs))
		{
			foreach (KeyValuePair<IntPtr, CodeTypeDeclaration> codeTypeDeclaration in Data.SmokeTypeMap)
			{
				CodeTypeDeclaration type = codeTypeDeclaration.Value;
				string classDocs = Path.Combine(htmlDocs, type.Name.ToLowerInvariant() + ".html");
				if (File.Exists(classDocs))
				{
					string docs = StripTags(File.ReadAllText(classDocs));
					Match match = Regex.Match(docs, "(?<class>The " + type.Name + @".+?)More\.\.\..*?\r?\n" +
						@"Detailed Description\s+(?<detailed>.*?)(\r?\n){3,}((\w+ )*\w+ Documentation\r?\n(?<members>.+))", RegexOptions.Singleline);
					if (match.Success)
					{
						type.Comments.Add(new CodeCommentStatement("<summary>", true));
						type.Comments.Add(new CodeCommentStatement(match.Groups["class"].Value, true));
						type.Comments.Add(new CodeCommentStatement("</summary>", true));
						type.Comments.Add(new CodeCommentStatement("<remarks>", true));
						type.Comments.Add(new CodeCommentStatement(match.Groups["detailed"].Value, true));
						type.Comments.Add(new CodeCommentStatement("</remarks>", true));
						this.memberDocumentation[type] = match.Groups["members"].Value;
					}
				}
			}
		}
		PropertyGenerator pg = new PropertyGenerator(Data, Translator, this.memberDocumentation);
		pg.Run();
	}

	void PostClassesHook()
	{
		if (!Data.CSharpTypeMap.ContainsKey("QAbstractScrollArea"))
		{
			return;
		}
		CodeTypeDeclaration typeQAbstractScrollArea = Data.CSharpTypeMap["QAbstractScrollArea"];
		foreach (KeyValuePair<string, CodeMemberMethod> eventMethod in eventMethods.Where(e => !typeQAbstractScrollArea.Members.Contains(e.Value)))
		{
			GenerateEvent(eventMethod.Value, eventMethod.Key, typeQAbstractScrollArea, false);
		}
	}

	private void PostEnumMemberHook(Smoke* smoke, Smoke.Method* smokeMethod, CodeMemberField cmm, CodeTypeDeclaration type)
	{
		CodeTypeDeclaration parentType = this.memberDocumentation.Keys.FirstOrDefault(t => t.Name == (string) type.UserData["parent"]);
		if (parentType != null)
		{
			string docs = this.memberDocumentation[parentType];
			string typeName = Regex.Escape(parentType.Name) + "::" + Regex.Escape(type.Name);
			if (type.Comments.Count == 0)
			{
				const string enumDoc = @"enum {0}(\s*flags {1}::\w+\s+)?(?<docsStart>.*?)" + 
					@"(ConstantValue(Description)?.*?)(\r?\n){{2}}" +
					@"(([^\n]+\n)+ConstantValue(Description)?.*?(\r?\n){{2}})*(?<docsEnd1>.*?)(\r?\n){{2,}}";
				Match matchEnum = Regex.Match(docs, string.Format(enumDoc, typeName, parentType.Name), RegexOptions.Singleline);
				if (matchEnum.Success)
				{
					string doc = (matchEnum.Groups["docsStart"].Value + matchEnum.Groups["docsEnd1"].Value).Trim();
					doc = Regex.Replace(doc,
					                    @"The \S+ type is a typedef for QFlags&lt;\S+&gt;\. It stores an OR combination of \S+ values\.",
					                    string.Empty).Trim();
					if (!string.IsNullOrEmpty(doc))
					{
						Translator.FormatComment(doc, type);
					}
				}
			}
			string memberName = Regex.Escape(parentType.Name) + "::" +
			                    Regex.Escape(ByteArrayManager.GetString(smoke->methodNames[smokeMethod->name]));
			const string memberDoc = @"enum {0}.*?{1}(0x)?[0-9]+(?<docs>.*?)(&\w+;)?(\r?\n)";
			Match match = Regex.Match(docs, string.Format(memberDoc, typeName, memberName), RegexOptions.Singleline);
			if (match.Success)
			{
				string doc = match.Groups["docs"].Value.Trim();
				if (!string.IsNullOrEmpty(doc))
				{
					Translator.FormatComment(char.ToUpper(doc[0]) + doc.Substring(1), cmm);
				}
			}
		}
	}

	private void PostMethodBodyHooks(Smoke* smoke, Smoke.Method* smokeMethod, CodeMemberMethod cmm, CodeTypeDeclaration type)
	{
		this.CommentMember(smoke, smokeMethod, cmm, type);
		GenerateEvent(cmm, cmm.Name, type, true);
	}

	private void CommentMember(Smoke* smoke, Smoke.Method* smokeMethod, CodeTypeMember cmm, CodeTypeDeclaration type)
	{
		if (this.memberDocumentation.ContainsKey(type))
		{
			string docs = this.memberDocumentation[type];
			string typeName = Regex.Escape(type.Name);
			string signature = smoke->GetMethodSignature(smokeMethod);
			StringBuilder signatureRegex = new StringBuilder();
			Match matchSignature = Regex.Match(signature, @"(?<name>[^\(]+)\((?<args>.*)\)");
			string methodName = Regex.Escape(matchSignature.Groups["name"].Value);
			signatureRegex.Append(methodName);
			signatureRegex.Append(@"\s*\(\s*");
			string[] parts = matchSignature.Groups["args"].Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			const string separator = @",\s*";
			foreach (string part in parts)
			{
				string p = Regex.Escape(part).Replace(@"\*", @"\s*\*");
				signatureRegex.Append(Translator.MatchFunctionPointer(p).Success ? @"[^,]+" : (p + @"\s+\w+(\s*=\s*\w+)?"));
				signatureRegex.Append(separator);
			}
			if (parts.Length > 0)
			{
				signatureRegex.Remove(signatureRegex.Length - separator.Length, separator.Length);
			}
			signatureRegex.Append(@"\s*\)\s*");
			string memberDoc = @"{0}( |(::)){1}( const)?( \[(\w+\s*)+\])?\r?\n\W*(?<docs>.*?)(\r?\n){{2}}(&?\S* --)?(\r?\n)";
			Match match = Regex.Match(docs, string.Format(memberDoc, typeName, signatureRegex), RegexOptions.Singleline);
			if (match.Success)
			{
				Translator.FormatComment(match.Groups["docs"].Value, cmm);
			}
			else
			{
				memberDoc = @"{0}( |(::)){1}\s*\([^\n]*\)( const)?( \[(\w+\s*)+\])?\r?\n\W*(?<docs>.*?)(\r?\n){{2}}(&?\S* --)?(\r?\n)";
				match = Regex.Match(docs, string.Format(memberDoc, typeName, methodName), RegexOptions.Singleline);
				if (match.Success)
				{
					Translator.FormatComment(match.Groups["docs"].Value, cmm);
				}
			}
		}
	}

	private static string StripTags(string source)
	{
		char[] array = new char[source.Length];
		int arrayIndex = 0;
		bool inside = false;

		foreach (char @let in source)
		{
			if (@let == '<')
			{
				inside = true;
				continue;
			}
			if (@let == '>')
			{
				inside = false;
				continue;
			}
			if (!inside)
			{
				array[arrayIndex] = @let;
				arrayIndex++;
			}
		}
		return new string(array, 0, arrayIndex);
	}

	private void GenerateEvent(CodeMemberMethod cmm, string name, CodeTypeDeclaration type, bool isVirtual)
	{
		if (!name.EndsWith("Event") ||
			(type.Name == "QCoreApplication" && (name == "PostEvent" || name == "SendEvent")))
		{
			return;
		}
		string paramType;
		// TODO: add support for IQGraphicsItem
		if (cmm.Parameters.Count == 1 && (paramType = cmm.Parameters[0].Type.BaseType).EndsWith("Event") &&
			(cmm.Attributes & MemberAttributes.Override) == 0 &&
			!new[]
                 {
                     "QGraphicsItem", "QGraphicsObject", "QGraphicsTextItem",
                     "QGraphicsProxyWidget", "QGraphicsWidget", "QGraphicsLayout",
                     "QGraphicsScene"
                 }.Contains(type.Name))
		{
			if (!this.HasField(type, "eventFilters"))
			{
				CodeSnippetTypeMember eventFilters = new CodeSnippetTypeMember();
				eventFilters.Name = "eventFilters";
				eventFilters.Text = "protected readonly List<QEventHandler> eventFilters = new List<QEventHandler>();";
				type.Members.Add(eventFilters);
			}
			CodeSnippetTypeMember codeMemberEvent = new CodeSnippetTypeMember();
			codeMemberEvent.Name = name;
			codeMemberEvent.Text =
				string.Format(
					@"
		public {0} event EventHandler<QEventArgs<{1}>> {2}
		{{
			add
			{{
				QEventArgs<{1}> qEventArgs = new QEventArgs<{1}>({3});
				QEventHandler<{1}> qEventHandler = new QEventHandler<{1}>(this{4}, qEventArgs, value);
                foreach (QEventHandler eventFilter in eventFilters)
                {{
                    this{4}.RemoveEventFilter(eventFilter);
                }}
				eventFilters.Add(qEventHandler);
                for (int i = eventFilters.Count - 1; i >= 0; i--)
                {{
				    this{4}.InstallEventFilter(eventFilters[i]);                    
                }}
			}}
			remove
			{{
				for (int i = eventFilters.Count - 1; i >= 0; i--)
				{{
					QEventHandler eventFilter = eventFilters[i];
					if (eventFilter.Handler == value)
					{{
						this{4}.RemoveEventFilter(eventFilter);
						eventFilters.RemoveAt(i);
                        break;
					}}
				}}
			}}
		}}
				",
				 isVirtual ? "virtual" : "override", paramType, codeMemberEvent.Name, GetEventTypes(name), isVirtual ? string.Empty : ".Viewport");
			codeMemberEvent.Attributes = (codeMemberEvent.Attributes & ~MemberAttributes.AccessMask) |
										 MemberAttributes.Public;
			codeMemberEvent.Comments.AddRange(cmm.Comments);
			type.Members.Add(codeMemberEvent);
			if (isVirtual && InheritsQWidget(type))
			{
				eventMethods[cmm.Name] = cmm;
			}
		}
		if (isVirtual && !cmm.Name.StartsWith("~"))
		{
			cmm.Name = "On" + cmm.Name;
		}
	}

	private bool InheritsQWidget(CodeTypeDeclaration type)
	{
		if (type.Name == "QWidget")
		{
			return true;
		}
		foreach (CodeTypeReference baseType in type.BaseTypes)
		{
			if (baseType.BaseType == "QWidget")
			{
				return true;
			}
			if (Data.CSharpTypeMap.ContainsKey(baseType.BaseType) && this.InheritsQWidget(Data.CSharpTypeMap[baseType.BaseType]))
			{
				return true;
			}
		}
		return false;
	}

	private bool HasField(CodeTypeDeclaration containingType, string field)
	{
		return containingType.Members.Cast<CodeTypeMember>().Any(m => m.Name == field) ||
			   containingType.BaseTypes.Cast<CodeTypeReference>().Any(
				baseType => (Data.CSharpTypeMap.ContainsKey(baseType.BaseType) &&
							 this.HasField(Data.CSharpTypeMap[baseType.BaseType], field)) ||
							(Data.ReferencedTypeMap.ContainsKey(baseType.BaseType) &&
							 Data.ReferencedTypeMap[baseType.BaseType].GetField(field, BindingFlags.NonPublic | BindingFlags.Instance) != null));
	}

	private static string GetEventTypes(string methodName)
	{
		string eventName = methodName.Substring(0, methodName.IndexOf("Event", StringComparison.Ordinal));
		switch (eventName)
		{
			case "Change":
				return string.Format(@"new List<QEvent.Type> {{ {0}{1}, {0}{2}, {0}{3}, {0}{4}, 
																{0}{5}, {0}{6}, {0}{7}, {0}{8}, 
																{0}{9}, {0}{10}, {0}{11}, {0}{12},
																{0}{13}, {0}{14}, {0}{15}}}",
									 "QEvent.Type.",
									 "ToolBarChange", "ActivationChange", "EnabledChange", "FontChange", "StyleChange",
									 "PaletteChange", "WindowTitleChange", "IconTextChange", "ModifiedChange", "MouseTrackingChange",
									 "ParentChange", "WindowStateChange", "LanguageChange", "LocaleChange", "LayoutDirectionChange");
			case "Action":
				return string.Format("new List<QEvent.Type> {{ {0}{1}, {0}{2}, {0}{3} }}", "QEvent.Type.",
									 eventName + "Added", eventName + "Removed", eventName + "Changed");
			case "Child":
				return string.Format("new List<QEvent.Type> {{ {0}{1}, {0}{2}, {0}{3} }}", "QEvent.Type.",
									 eventName + "Added", eventName + "Polished", eventName + "Removed");
			case "Tablet":
				return string.Format("new List<QEvent.Type> {{ {0}{1}, {0}{2}, {0}{3}, {0}{4}, {0}{5} }}", "QEvent.Type.",
									 eventName + "Move", eventName + "Press", eventName + "Release", eventName + "EnterProximity", eventName + "LeaveProximity");
			case "Filter":// QInputContext
				return string.Format("new List<QEvent.Type> {{ {0}{1}, {0}{2}, {0}{3}, {0}{4}, {0}{5}, {0}{6}, {0}{7}, {0}{8} }}", "QEvent.Type.",
									 "CloseSoftwareInputPanel", "KeyPress", "KeyRelease", "MouseButtonDblClick",
									 "MouseButtonPress", "MouseButtonRelease", "MouseMove", "RequestSoftwareInputPanel");
			case "MouseDoubleClick":
				return string.Format("new List<QEvent.Type> {{ QEvent.Type.MouseButtonDblClick }}");
			case "MousePress":
				return string.Format("new List<QEvent.Type> {{ QEvent.Type.MouseButtonPress }}");
			case "MouseRelease":
				return string.Format("new List<QEvent.Type> {{ QEvent.Type.MouseButtonRelease }}");
			case "Send":// QInputContext
				return string.Format("new List<QEvent.Type> {{ QEvent.Type.InputMethod }}");
			case "Viewport":
			case "Post":
			case "Widget":
			case "":
				return "new List<QEvent.Type> ()";
			case "Custom":
				return "new List<QEvent.Type> {{ QEvent.Type.User }}";
			case "SwallowContextMenu":
				return "new List<QEvent.Type> {{ QEvent.Type.ContextMenu }}";
			default:
				return string.Format("new List<QEvent.Type> {{ QEvent.Type.{0} }}", eventName);
		}
	}
}
