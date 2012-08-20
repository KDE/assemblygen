/***************************************************************************
                          SmokeInvocation.cs  -  description
                             -------------------
    begin                : Wed Jun 16 2004
    copyright            : (C) 2004 by Richard Dale
    email                : richard.j.dale@gmail.com
 ***************************************************************************/

/***************************************************************************
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU Lesser General Public License as        *
 *   published by the Free Software Foundation; either version 2 of the    *
 *   License, or (at your option) any later version.                       *
 *                                                                         *
 ***************************************************************************/

namespace Qyoto {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using System.Reflection;
	using System.Runtime.Remoting.Proxies;
	using System.Runtime.Remoting.Messaging;
	using System.Runtime.Remoting;
	using System.Runtime.InteropServices;

	public struct ModuleIndex {
		public IntPtr smoke;
		public short index;
	}

    public enum TypeId {
        t_voidp,
        t_bool,
        t_char,
        t_uchar,
        t_short,
        t_ushort,
        t_int,
        t_uint,
        t_long,
        t_ulong,
        t_float,
        t_double,
        t_enum,
        t_class,
        t_last,      // number of pre-defined types
        // adding specific types that must be differentiated from System.Object
        t_string
    }

	[StructLayout(LayoutKind.Explicit)]
	unsafe public struct StackItem {
		[FieldOffset(0)] public void * s_voidp;
		[FieldOffset(0)] public bool s_bool;
		[FieldOffset(0)] public sbyte s_char;
		[FieldOffset(0)] public byte s_uchar;
		[FieldOffset(0)] public short s_short;
		[FieldOffset(0)] public ushort s_ushort;
		[FieldOffset(0)] public int s_int;
		[FieldOffset(0)] public uint s_uint;
		[FieldOffset(0)] public long s_long;
		[FieldOffset(0)] public ulong s_ulong;
		[FieldOffset(0)] public float s_float;
		[FieldOffset(0)] public double s_double;
        [FieldOffset(0)] public long s_enum;
		[FieldOffset(0)] public IntPtr s_class;
	}
	
	public class SmokeInvocation {
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		static extern ModuleIndex FindMethodId(string className, string mungedName, string signature);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, EntryPoint="CallSmokeMethod", CallingConvention=CallingConvention.Cdecl)]
		static extern void CallSmokeMethod(IntPtr smoke, int methodId, IntPtr target, IntPtr sp, int items, IntPtr typeIDs);

		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		static extern int QyotoHash(IntPtr obj);

		// The key is a type name of a class which has overriden one or more
		// virtual methods, and the value is a Hashtable of the smoke type
		// signatures as keys retrieving a suitable MethodInfo to invoke via 
		// reflection.
		static private Dictionary<Type, Dictionary<string, MemberInfo>> overridenMethods = 
			new Dictionary<Type, Dictionary<string, MemberInfo>>();

		static private MethodInfo metaObjectMethod = typeof(QObject).GetMethod("MetaObject", BindingFlags.Instance | BindingFlags.Public);
		
		static void AddOverridenMethods(Type klass) {
			if (SmokeMarshallers.IsSmokeClass(klass)) {
				return;
			}

			if (overridenMethods.ContainsKey(klass))
				return;

			Dictionary<string, MemberInfo> methodsHash = new Dictionary<string, MemberInfo>();
			overridenMethods.Add(klass, methodsHash);
			
			do {
				MemberInfo[] methods = klass.FindMembers(	MemberTypes.Method,
															BindingFlags.Public 
															| BindingFlags.NonPublic 
															| BindingFlags.Instance
															| BindingFlags.DeclaredOnly,
															Type.FilterName,
															"*" );
				foreach (MemberInfo method in methods) {
					Type parent = klass.BaseType;
					string signature = null;
					while (signature == null && parent != null && parent != typeof(Qt)) {
						MemberInfo[] parentMethods = parent.FindMembers(	MemberTypes.Method,
																			BindingFlags.Public 
																			| BindingFlags.NonPublic 
																			| BindingFlags.Instance
																			| BindingFlags.DeclaredOnly,
																			Type.FilterName,
																			method.Name );
						foreach (MemberInfo parentMethod in parentMethods) {
							if (method.ToString() == parentMethod.ToString()) {
								object[] smokeMethod = parentMethod.GetCustomAttributes(typeof(SmokeMethod), false);
								if (smokeMethod.Length > 0) {
									signature = ((SmokeMethod) smokeMethod[0]).Signature;
								}
								
							}
						}
	
						parent = parent.BaseType;
					}
	
					if (signature != null && !methodsHash.ContainsKey(signature)) {
						methodsHash.Add(signature, method);
					}
				}

				klass = klass.BaseType;
			} while (!SmokeMarshallers.IsSmokeClass(klass));
		}

		public static IntPtr OverridenMethod(IntPtr instance, string method) {
			Type klass = ((GCHandle) instance).Target.GetType();

			if (method == "metaObject() const") {
#if DEBUG
				return (IntPtr) DebugGCHandle.Alloc(metaObjectMethod);
#else
				return (IntPtr) GCHandle.Alloc(metaObjectMethod);
#endif
			}

			Dictionary<string, MemberInfo> methods;
			if (!overridenMethods.TryGetValue(klass, out methods)) {
				return (IntPtr) 0;
			}

			MemberInfo methodInfo;
			if (!methods.TryGetValue(method, out methodInfo)) {
				return (IntPtr) 0;
			}

#if DEBUG
			return (IntPtr) DebugGCHandle.Alloc(methodInfo);
#else
			return (IntPtr) GCHandle.Alloc(methodInfo);
#endif
		}

		public static void InvokeMethod(IntPtr instanceHandle, IntPtr methodHandle, IntPtr stack, IntPtr typeIDs) {
			object instance = ((GCHandle) instanceHandle).Target;
			MethodInfo method = (MethodInfo) ((GCHandle) methodHandle).Target;
#if DEBUG
			if (	(QDebug.DebugChannel() & QtDebugChannel.QTDB_TRANSPARENT_PROXY) != 0
					&& (QDebug.DebugChannel() & QtDebugChannel.QTDB_VIRTUAL) != 0 )
			{
				Console.WriteLine(	"ENTER InvokeMethod() {0}.{1}", 
									instance,
									method.Name );
			}
#endif
			unsafe {
				StackItem * stackPtr = (StackItem*) stack;
				ParameterInfo[] parameters = method.GetParameters();
				object[] args = new object[parameters.Length];

				for (int i = 0; i < args.Length; i++) {
					args[i] = SmokeMarshallers.BoxFromStackItem(parameters[i].ParameterType, 0, stackPtr + i + 1);
				}
				object returnValue = method.Invoke(instance, args);
				TypeId* typeIDsPtr = (TypeId*) typeIDs;
				*typeIDsPtr = SmokeMarshallers.GetTypeId(returnValue == null ? typeof(object) : returnValue.GetType());

				if (method.ReturnType != typeof(void)) {
					SmokeMarshallers.UnboxToStackItem(returnValue, stackPtr);
				}
			}

			return;
		}

		static public void InvokeCustomSlot(IntPtr obj, string slotname, IntPtr stack, IntPtr ret) {
			QObject qobj = (QObject) ((GCHandle)obj).Target;
#if DEBUG
			if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_TRANSPARENT_PROXY) != 0) {
				Console.WriteLine(	"ENTER InvokeCustomSlot() {0}.{1}", 
									qobj,
									slotname );
			}
#endif
		
			MethodInfo slot = Qyoto.GetSlotMethodInfo(qobj.GetType(), slotname);
			ParameterInfo[] parameters = slot.GetParameters();
			object[] args = new object[parameters.Length];

			unsafe {
				StackItem* stackPtr = (StackItem*) stack;
				for (int i = 0; i < args.Length; i++) {
					args[i] = SmokeMarshallers.BoxFromStackItem(parameters[i].ParameterType, 0, stackPtr + i);
				}

				object returnValue = slot.Invoke(qobj, args);

				StackItem* retval = (StackItem*) ret;

				if (slot.ReturnType != typeof(void)) {
					SmokeMarshallers.UnboxToStackItem(returnValue, retval);
				}
			}
		}

		public static unsafe void InvokeDelegate(Delegate d, IntPtr stack) {
			MethodInfo mi = d.Method;
			ParameterInfo[] parameters = mi.GetParameters();
			object[] args = new object[parameters.Length];
			StackItem* stackPtr = (StackItem*) stack;
			for (int i = 0; i < args.Length; i++) {
				args[i] = SmokeMarshallers.BoxFromStackItem(parameters[i].ParameterType, 0, stackPtr + i);
			}
			d.DynamicInvoke(args);
		}

		// list of assemblies on which CallInitSmoke() has already been called.
		public static List<Assembly> InitializedAssemblies = new List<Assembly>();
		// whether the qyoto (core) runtime has been initialized
		static bool runtimeInitialized = false;

		public static void InitRuntime() {
			if (runtimeInitialized)
				return;
			Qyoto.Init_qyoto_qtcore();
			SmokeMarshallers.SetUp();
			QMetaType.RegisterType<object>();
			// not set when mono is embedded
			if (AppDomain.CurrentDomain.SetupInformation.ConfigurationFile == null) {
				PropertyInfo pi = typeof(AppDomain).GetProperty("SetupInformationNoCopy", BindingFlags.NonPublic | BindingFlags.Instance);
				AppDomainSetup setup = (AppDomainSetup) pi.GetValue(AppDomain.CurrentDomain, null);
				setup.ConfigurationFile = Assembly.GetExecutingAssembly().Location + ".config";
			}

			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
				TryInitialize(a);
			}

			runtimeInitialized = true;
		}

		public static void TryInitialize(Assembly assembly) {
			if (InitializedAssemblies.Contains(assembly))
				return;
			AssemblySmokeInitializer attr = 
				(AssemblySmokeInitializer) Attribute.GetCustomAttribute(assembly, typeof(AssemblySmokeInitializer));
			if (attr != null) attr.CallInitSmoke();
			InitializedAssemblies.Add(assembly);
		}

		static SmokeInvocation() {
			InitRuntime();
		}
		
		private Type	classToProxy;
		private Object	instance;
		private string	className = "";
		private Dictionary<string, ModuleIndex> methodIdCache;

		private static Dictionary<Type, Dictionary<string, ModuleIndex>> globalMethodIdCache = new Dictionary<Type, Dictionary<string, ModuleIndex>>();
		
		public SmokeInvocation(Type klass, Object obj) 
		{
			classToProxy = klass;
			instance = obj;
			className = SmokeMarshallers.SmokeClassName(klass);

			TryInitialize(klass.Assembly);

			if (!globalMethodIdCache.TryGetValue(classToProxy, out methodIdCache)) {
				methodIdCache = new Dictionary<string, ModuleIndex>();
				globalMethodIdCache[classToProxy] = methodIdCache;
			}

			if (instance != null) {
				AddOverridenMethods(instance.GetType());
			}
		}

		public object Invoke(string mungedName, string signature, Type returnType, bool refArgs, params object[] args) {
#if DEBUG
			if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_TRANSPARENT_PROXY) != 0) {
				Console.WriteLine(	"ENTER SmokeInvocation.Invoke() MethodName: {0}.{1} Type: {2} ArgCount: {3}", 
									className,
									signature, 
									returnType, 
									args.Length / 2 );
			}
#endif

			if (signature.StartsWith("operator==")) {
				if (args[1] == null && args[3] == null)
					return true;
				else if (args[1] == null || args[3] == null)
					return false;
			}
			ModuleIndex methodId;
			methodId.smoke = IntPtr.Zero;
			methodId.index = -1;
			if (!methodIdCache.TryGetValue(signature, out methodId)) {
				methodId = FindMethodId(className, mungedName, signature);

				if (methodId.index == -1) {
					Console.Error.WriteLine(	"LEAVE Invoke() ** Missing method ** {0}.{1}", 
												className,
												signature );
					return null;
				}

				methodIdCache[signature] = methodId;
			}

			StackItem[] stack = new StackItem[(args.Length / 2) + 1];
			TypeId[] typeIDs = new TypeId[(args.Length / 2) + 1];

			unsafe {
				fixed(StackItem * stackPtr = stack) {
					fixed (TypeId * typeIDsPtr = typeIDs) {
						typeIDs[0] = 0;
						for (int i = 1, k = 1; i < args.Length; i += 2, k++) {
							typeIDs[k] = SmokeMarshallers.UnboxToStackItem(args[i], stackPtr + k);
						}

						object returnValue = null;

						if (instance == null) {
							CallSmokeMethod(methodId.smoke, (int) methodId.index, (IntPtr) 0, (IntPtr) stackPtr, args.Length / 2, (IntPtr) typeIDsPtr);
						} else {
#if DEBUG
							GCHandle instanceHandle = DebugGCHandle.Alloc(instance);
#else
							GCHandle instanceHandle = GCHandle.Alloc(instance);
#endif
							CallSmokeMethod(methodId.smoke, methodId.index, (IntPtr) instanceHandle, (IntPtr) stackPtr, args.Length / 2, (IntPtr) typeIDsPtr);
#if DEBUG
							DebugGCHandle.Free(instanceHandle);
#else
							instanceHandle.Free();
#endif
						}
					
						if (returnType != typeof(void)) {
							returnValue = SmokeMarshallers.BoxFromStackItem(returnType, (int) typeIDs[0], stackPtr);
						}

						if (refArgs) {
							for (int i = 1, k = 1; i < args.Length; i += 2, k++) {
								Type t = args[i].GetType();
								if (t.IsPrimitive || t == typeof(NativeLong) || t == typeof(NativeULong)) {
									args[i] = SmokeMarshallers.BoxFromStackItem(args[i].GetType(), (int) typeIDs[k], stackPtr + k);
								}
							}
						}

						return returnValue;
					}
				}
			}
		}
		
		public override int GetHashCode() {
#if DEBUG
			return QyotoHash((IntPtr) DebugGCHandle.Alloc(instance));
#else
			return QyotoHash((IntPtr) GCHandle.Alloc(instance));
#endif
		}
	}

	public class SignalInvocation : RealProxy {
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		static extern bool SignalEmit(string signature, string type, IntPtr target, IntPtr sp, int items);

		private Type	signalsInterface;
		private Object	instance;

		public SignalInvocation(Type iface, Object obj) : base(iface) 
		{
			signalsInterface = iface;
			instance = obj;
		}

		public override IMessage Invoke(IMessage message) {
			IMethodCallMessage callMessage = (IMethodCallMessage) message;
			StackItem[] stack = new StackItem[callMessage.ArgCount+1];

#if DEBUG
			if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_TRANSPARENT_PROXY) != 0) {
				Console.WriteLine(	"ENTER SignalInvocation.Invoke() MethodName: {0}.{1} Type: {2} ArgCount: {3}", 
									instance,
									callMessage.MethodName, 
									callMessage.TypeName, 
									callMessage.ArgCount.ToString() );
			}
#endif

			unsafe {
				fixed(StackItem * stackPtr = stack) {
					for (int i = 0; i < callMessage.ArgCount; i++) {
						SmokeMarshallers.UnboxToStackItem(callMessage.Args[i], stackPtr + i + 1);
					}

					IMethodReturnMessage returnMessage = new ReturnMessage(null, callMessage); /*(IMethodReturnMessage) message;*/
					MethodReturnMessageWrapper returnValue = new MethodReturnMessageWrapper((IMethodReturnMessage) returnMessage);

#if DEBUG
					GCHandle instanceHandle = DebugGCHandle.Alloc(instance);
#else
					GCHandle instanceHandle = GCHandle.Alloc(instance);
#endif

					Qyoto.CPPMethod signalEntry = Qyoto.GetSignalSignature(signalsInterface, (MethodInfo) callMessage.MethodBase);

					Type returnType = ((MethodInfo) returnMessage.MethodBase).ReturnType;
					SignalEmit(signalEntry.signature, signalEntry.type, (IntPtr) instanceHandle, (IntPtr) stackPtr, callMessage.ArgCount);

					if (returnType != typeof(void)) {
						returnValue.ReturnValue = SmokeMarshallers.BoxFromStackItem(returnType, 0, stackPtr);
					}

					returnMessage = returnValue;
					return returnMessage;
				}
			}
		}

		public override int GetHashCode() {
			return instance.GetHashCode();
		}
	}
}

// kate: space-indent off;
