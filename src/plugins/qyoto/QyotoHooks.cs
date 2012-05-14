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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

public unsafe class QyotoHooks : IHookProvider {

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    delegate void AddSignal(string signature, string name, string returnType, IntPtr metaMethod);

    [DllImport("qyotogenerator-native", CallingConvention=CallingConvention.Cdecl)]
    static extern void GetSignals(Smoke* smoke, void *klass, AddSignal addSignalFn);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
    delegate void AddParameter(string type, string name);

    [DllImport("qyotogenerator-native", CallingConvention=CallingConvention.Cdecl)]
    static extern void GetMetaMethodParameters(IntPtr metaMethod, AddParameter addParamFn);

    static string qObjectDummyCtorCode =
"            try {\n" +
"                Type proxyInterface = Qyoto.GetSignalsInterface(GetType());\n" +
"                SignalInvocation realProxy = new SignalInvocation(proxyInterface, this);\n" +
"                Q_EMIT = realProxy.GetTransparentProxy();\n" +
"            }\n" +
"            catch (Exception e) {\n" +
"                Console.WriteLine(\"Could not retrieve signal interface: {0}\", e);\n" +
"            }";

    public void RegisterHooks() {
        ClassesGenerator.PreMembersHooks += PreMembersHook;
        ClassesGenerator.PostMembersHooks += PostMembersHook;
        ClassesGenerator.SupportingMethodsHooks += SupportingMethodsHook;
        ClassesGenerator.PreClassesHook += PreClassesHook;
		MethodsGenerator.PostMethodBodyHooks += this.PostMethodBodyHooks;
        Console.WriteLine("Registered Qyoto hooks.");
    }

    public Translator Translator { get; set; }
    public GeneratorData Data { get; set; }

    public void PreMembersHook(Smoke *smoke, Smoke.Class *klass, CodeTypeDeclaration type) {
        if (type.Name == "QObject") {
            // Add 'Qt' base class
            type.BaseTypes.Add(new CodeTypeReference("Qt"));

            // add the Q_EMIT field
            CodeMemberField Q_EMIT = new CodeMemberField(typeof(object), "Q_EMIT");
            Q_EMIT.Attributes = MemberAttributes.Family;
            Q_EMIT.InitExpression = new CodePrimitiveExpression(null);
            type.Members.Add(Q_EMIT);
        }
    }

    public void PostMembersHook(Smoke *smoke, Smoke.Class *klass, CodeTypeDeclaration type) {
        if (Util.IsQObject(klass)) {
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
            int colon = className.LastIndexOf("::");
            string prefix = (colon != -1) ? className.Substring(0, colon) : string.Empty;

            IList typeCollection = Data.GetTypeCollection(prefix);
            CodeTypeDeclaration ifaceDecl = new CodeTypeDeclaration(signalsIfaceName);
            ifaceDecl.IsInterface = true;

            if (className != "QObject") {
                string parentClassName = ByteArrayManager.GetString(smoke->classes[smoke->inheritanceList[klass->parents]].className);
                colon = parentClassName.LastIndexOf("::");
                prefix = (colon != -1) ? parentClassName.Substring(0, colon) : string.Empty;
                if (colon != -1) {
                    parentClassName = parentClassName.Substring(colon + 2);
                }

                string parentInterface = (prefix != string.Empty) ? prefix.Replace("::", ".") + "." : string.Empty;
                parentInterface += "I" + parentClassName + "Signals";

                ifaceDecl.BaseTypes.Add(new CodeTypeReference(parentInterface));
            }
            OrderedDictionary signalImplementations = new OrderedDictionary ();
            OrderedDictionary signalParamNames = new OrderedDictionary ();
            GetSignals(smoke, klass, delegate(string signature, string name, string typeName, IntPtr metaMethod) {
                CodeMemberEvent signal = new CodeMemberEvent();
				// HACK: both .NET and Mono have bugs with a generic CodeTypeReference so different implementations are needed
				if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
					signal.Type = new CodeTypeReference("Slot");
				}
                StringBuilder fullNameBuilder = new StringBuilder("Slot");
                signal.Attributes = MemberAttributes.Abstract;

                // capitalize the first letter
                StringBuilder builder = new StringBuilder(name);
                builder[0] = char.ToUpper(builder[0]);
                string tmp = builder.ToString();

                signal.Name = tmp;
                bool isRef;

                CodeAttributeDeclaration attr = new CodeAttributeDeclaration("Q_SIGNAL",
                    new CodeAttributeArgument(new CodePrimitiveExpression(signature)));
                signal.CustomAttributes.Add(attr);

                int argNum = 1;
                List<string> paramNames = new List<string>(argNum);
                GetMetaMethodParameters(metaMethod, delegate(string paramType, string paramName) {
                    if (paramName == string.Empty) {
                        paramName = "arg" + argNum.ToString();
                    }
                    argNum++;

                    CodeParameterDeclarationExpression param;
                    try {
                        short id = smoke->idType(paramType);
                        CodeTypeReference paramTypeRef;
                        if (id > 0) {
                            paramTypeRef = Translator.CppToCSharp(smoke->types + id, out isRef);
                        } else {
                            if (!paramType.Contains("::")) {
                                id = smoke->idType(className + "::" + paramType);
                                if (id > 0) {
                                    paramTypeRef = Translator.CppToCSharp(smoke->types + id, out isRef);
                                } else {
                                    paramTypeRef = Translator.CppToCSharp(paramType, out isRef);
                                }
                            } else {
                                paramTypeRef = Translator.CppToCSharp (paramType, out isRef);                                
                            }
                        }
                        param = new CodeParameterDeclarationExpression(paramTypeRef, paramName);
                    } catch (NotSupportedException) {
                        Debug.Print("  |--Won't wrap signal {0}::{1}", className, signature);
                        return;
                    }
                    if (isRef) {
                        param.Direction = FieldDirection.Ref;
                    }
                    paramNames.Add(paramName);
                    signal.Type.TypeArguments.Add(param.Type);
                    if (argNum == 2) {
                        fullNameBuilder.Append ('<');
                    }
                    fullNameBuilder.Append (param.Type.BaseType);
                    fullNameBuilder.Append (',');
                });
                if (fullNameBuilder[fullNameBuilder.Length - 1] == ',') {
                    fullNameBuilder[fullNameBuilder.Length - 1] = '>';
                }
				if (Environment.OSVersion.Platform != PlatformID.Win32NT) {
					signal.Type = new CodeTypeReference(fullNameBuilder.ToString());                                                                                                		
                }
                signalParamNames.Add(signal, paramNames);
                CodeMemberEvent existing = ifaceDecl.Members.Cast<CodeMemberEvent>().FirstOrDefault(m => m.Name == signal.Name);
                if (existing != null) {
                    CodeMemberEvent signalToUse = paramNames.Count == 0 ? existing : signal;
                    string suffix = ((IEnumerable<string>) signalParamNames[signalToUse]).Last();
                    if (suffix.StartsWith("arg") && suffix.Length > 3 && char.IsDigit(suffix[3])) {
						if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
							string lastType = signalToUse.Type.TypeArguments.Cast<CodeTypeReference>().Last().BaseType;
							suffix = lastType.Substring(lastType.LastIndexOf('.') + 1);
						} else {
							suffix = Regex.Match(signalToUse.Type.BaseType, @"<(\w+,)*(\w+\.)*(\w+)>").Groups[3].Value;
						}
                    } else {
                        StringBuilder lastParamBuilder = new StringBuilder(suffix);
                        lastParamBuilder[0] = char.ToUpper(lastParamBuilder[0]);
                        suffix = lastParamBuilder.ToString();
                    }
                    signalToUse.Name += suffix;
                    if (signalImplementations.Contains(signalToUse)) {
                        CodeSnippetTypeMember implementation = (CodeSnippetTypeMember) signalImplementations[signalToUse];
                        implementation.Text = implementation.Text.Replace(implementation.Name, implementation.Name += suffix);
                    }
                }
                ifaceDecl.Members.Add(signal);
                CodeSnippetTypeMember signalImplementation = new CodeSnippetTypeMember();
                signalImplementation.Name = signal.Name;
                signalImplementation.Text = string.Format(@"
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
		}}", fullNameBuilder, signal.Name, signature, signature.Substring(signature.IndexOf('(')));
                signalImplementations.Add(signal, signalImplementation);
            });

            typeCollection.Add(ifaceDecl);
            type.BaseTypes.Add(ifaceDecl.Name);
            foreach (DictionaryEntry dictionaryEntry in signalImplementations) {
                CodeTypeMember signalImplementation = (CodeTypeMember) dictionaryEntry.Value;
                foreach (CodeTypeMember current in from CodeTypeMember member in type.Members
                                                   where member.Name == signalImplementation.Name
                                                   select member) {
                    current.Name = "On" + current.Name;
                }
                type.Members.Add(signalImplementation);
            }
        }
    }

    public void SupportingMethodsHook(Smoke *smoke, Smoke.Method *method, CodeMemberMethod cmm, CodeTypeDeclaration type) {
        if (type.Name == "QObject" && cmm is CodeConstructor) {
            cmm.Statements.Add(new CodeSnippetStatement(qObjectDummyCtorCode));
        }
    }

    public void PreClassesHook() {
        PropertyGenerator pg = new PropertyGenerator(Data, Translator);
        pg.Run();
    }

	private void PostMethodBodyHooks(Smoke* smoke, Smoke.Method* smokeMethod, CodeMemberMethod cmm, CodeTypeDeclaration type)
	{
		if (!cmm.Name.EndsWith("Event") || (type.Name == "QCoreApplication" && (cmm.Name == "PostEvent" || cmm.Name == "SendEvent")))
		{
			return;
		}
		string paramType;
		// TODO: add support for IQGraphicsItem
		if (cmm.Parameters.Count == 1 && (paramType = cmm.Parameters[0].Type.BaseType).EndsWith("Event") &&
			(cmm.Attributes & MemberAttributes.Override) == 0 &&
			!new[] { "QGraphicsItem", "QGraphicsObject", "QGraphicsTextItem", 
					 "QGraphicsProxyWidget", "QGraphicsWidget", "QGraphicsLayout", 
					 "QGraphicsScene"}.Contains(type.Name))
		{
			if (!HasField(type, "eventFilters"))
			{
				CodeSnippetTypeMember eventFilters = new CodeSnippetTypeMember();
				eventFilters.Name = "eventFilters";
				eventFilters.Text = "protected readonly List<QEventHandler> eventFilters = new List<QEventHandler>();";
				type.Members.Add(eventFilters);
			}
			CodeSnippetTypeMember codeMemberEvent = new CodeSnippetTypeMember();
			codeMemberEvent.Name = cmm.Name;
			codeMemberEvent.Text = string.Format(@"
		public event EventHandler<QEventArgs<{0}>> {1}
		{{
			add
			{{
				QEventArgs<{0}> qEventArgs = new QEventArgs<{0}>({2});
				QEventHandler<{0}> qEventHandler = new QEventHandler<{0}>(this, qEventArgs, value);
				eventFilters.Add(qEventHandler);
				this.InstallEventFilter(qEventHandler);
			}}
			remove
			{{
				for (int i = eventFilters.Count - 1; i >= 0; i--)
				{{
					QEventHandler eventFilter = eventFilters[i];
					if (eventFilter.Handler == value)
					{{
						this.RemoveEventFilter(eventFilter);
						eventFilters.RemoveAt(i);
                        break;
					}}
				}}
			}}
		}}
				", paramType, cmm.Name, GetEventTypes(cmm.Name));
			codeMemberEvent.Attributes = (codeMemberEvent.Attributes & ~MemberAttributes.AccessMask) |
										 MemberAttributes.Public;
			type.Members.Add(codeMemberEvent);
		}
		if (!cmm.Name.StartsWith("~"))
		{
			cmm.Name = "On" + cmm.Name;
		}
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
