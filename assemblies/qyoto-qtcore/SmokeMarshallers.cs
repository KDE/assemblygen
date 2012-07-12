/***************************************************************************
                          SmokeMarshallers.cs  -  description
                             -------------------
    begin                : Wed Jun 16 2004
    copyright            : (C) 2004-2007 by Richard Dale, Arno Rehn
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
	using System.Reflection;
	using System.Runtime.Remoting.Proxies;
	using System.Runtime.InteropServices;
	using System.Text;

	public static class GCHandleExtensions {
		static object HandleLock = new object();

		public static void SynchronizedFree(this GCHandle handle) {
			lock (HandleLock) {
				handle.Free();
			}
		}
	}

	public class SmokeMarshallers : object {
		
#region C++ functions
		/** Marshalling functions begin **/
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr StringArrayToCharStarStar(int length, string[] strArray);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr StringToQString(string str);

		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Unicode, CallingConvention=CallingConvention.Cdecl)]
		public static extern string StringFromQString(IntPtr ptr);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr StringArrayToQStringList(int length, string[] strArray);
		/** Marshalling functions end **/
		
		/** Other functions **/
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern int SizeOfLong();

		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ConstructPointerList();
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void AddObjectToPointerList(IntPtr ptr, IntPtr obj);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ConstructQListInt();
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void AddIntToQList(IntPtr ptr, int i);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr ConstructQListQRgb();
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void AddUIntToQListQRgb(IntPtr ptr, uint i);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ConstructQHash(int type);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddIntQVariantToQHash(IntPtr ptr, int i, IntPtr qv);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddQStringQStringToQHash(IntPtr ptr, string str1, string str2);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddQStringQVariantToQHash(IntPtr ptr, string str, IntPtr qv);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr ConstructQMap(int type);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddIntQVariantToQMap(IntPtr ptr, int i, IntPtr qv);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddQStringQStringToQMap(IntPtr ptr, string str1, string str2);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void AddQStringQVariantToQMap(IntPtr ptr, string str, IntPtr qv);
		/** Other functions end **/
		
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallFreeGCHandle(FromIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallGetSmokeObject(GetIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallSetSmokeObject(SetIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallAddGlobalRef(SetIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallRemoveGlobalRef(SetIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallMapPointer(MapPointerFn callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallUnmapPointer(FromIntPtr callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallGetInstance(GetInstanceFn callback);

		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallCreateInstance(CreateInstanceFn callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallInvokeCustomSlot(InvokeCustomSlotFn callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallInvokeDelegate(InvokeDelegateFn callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern bool InstallGetProperty(OverridenMethodFn callback);
		
		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern bool InstallSetProperty(SetPropertyFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallIntPtrToCharStarStar(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallIntPtrToCharStar(GetStringFromIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallIntPtrFromCharStar(GetIntPtrFromString callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallIntPtrFromQString(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallStringBuilderToQString(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallStringBuilderFromQString(SetIntPtrFromCharStar callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallOverridenMethod(OverridenMethodFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallInvokeMethod(InvokeMethodFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallConstructList(CreateListFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallStringListToQStringList(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallListToPointerList(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallListIntToQListInt(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallListUIntToQListQRgb(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallAddIntPtrToList(SetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallAddIntToListInt(AddInt callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr InstallConstructDictionary(ConstructDict callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallAddObjectObjectToDictionary(AddToDictionaryFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallAddIntObjectToDictionary(AddIntObject callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallDictionaryToQHash(DictToHash callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallDictionaryToQMap(DictToMap callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallAddUIntToListUInt(AddUInt callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallQPairGetFirst(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallQPairGetSecond(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallCreateQPair(CreateQPairFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallUnboxToStackItem(SetIntPtrType callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallBoxFromStackItem(CreateInstanceFn callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallGenericPointerGetIntPtr(GetIntPtr callback);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		public static extern void InstallCreateGenericPointer(CreateInstanceFn callback);

		[DllImport("qyoto-qtcore-native", CharSet=CharSet.Ansi, CallingConvention=CallingConvention.Cdecl)]
		public static extern void InstallTryDispose(FromIntPtr callback);
#endregion
		
#region delegates
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr NoArgs();
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr GetIntPtr(IntPtr instance);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetIntPtr(IntPtr instance, IntPtr ptr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate TypeId SetIntPtrType(IntPtr instance, IntPtr ptr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void FromIntPtr(IntPtr ptr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MapPointerFn(IntPtr instance, IntPtr ptr, bool createGlobalReference);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr CreateListFn(string className);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr CreateInstanceFn(string className, IntPtr smokeObjectPtr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr GetInstanceFn(IntPtr ptr, bool allInstances);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void InvokeCustomSlotFn(IntPtr obj, string slot, IntPtr stack, IntPtr ret);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr GetIntPtrFromString(string str);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate string GetStringFromIntPtr(IntPtr ptr);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetIntPtrFromCharStar(IntPtr ptr, string str);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr OverridenMethodFn(IntPtr instance, string method);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void InvokeMethodFn(IntPtr instance, IntPtr method, IntPtr args, IntPtr	typeIDs);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AddToDictionaryFn(IntPtr instance, IntPtr method, IntPtr args);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AddInt(IntPtr obj, int i);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AddUInt(IntPtr obj, uint i);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AddIntObject(IntPtr hash, int key, IntPtr val);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr DictToHash(IntPtr ptr, int type);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr DictToMap(IntPtr ptr, int type);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr ConstructDict(string type1, string type2);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetPropertyFn(IntPtr obj, string name, IntPtr variant);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void InvokeDelegateFn(Delegate d, IntPtr stack);
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate IntPtr CreateQPairFn(IntPtr first, IntPtr second);
#endregion
		
#region delegate fields
        private static FromIntPtr dFreeGCHandle = FreeGCHandle;
        private static GetIntPtr dGetSmokeObject = GetSmokeObject;
        private static SetIntPtr dSetSmokeObject = SetSmokeObject;
        private static SetIntPtr dAddGlobalRef = AddGlobalRef;
        private static SetIntPtr dRemoveGlobalRef = RemoveGlobalRef;
        private static MapPointerFn dMapPointer = MapPointer;
        private static FromIntPtr dUnmapPointer = UnmapPointer;
        private static GetInstanceFn dGetInstance = GetInstance;
        private static GetIntPtr dIntPtrToCharStarStar = IntPtrToCharStarStar;
        private static GetStringFromIntPtr dIntPtrToString = IntPtrToString;
        private static GetIntPtrFromString dIntPtrFromString = IntPtrFromString;
        private static GetIntPtr dIntPtrFromQString = IntPtrFromQString;
        private static GetIntPtr dStringBuilderToQString = StringBuilderToQString;
        private static SetIntPtrFromCharStar dStringBuilderFromQString = StringBuilderFromQString;
        private static CreateListFn dConstructList = ConstructList;
        private static GetIntPtr dStringListToQStringList = StringListToQStringList;
        private static GetIntPtr dListToPointerList = ListToPointerList;
        private static GetIntPtr dListIntToQListInt = ListIntToQListInt;
        private static GetIntPtr dListUIntToQListQRgb = ListUIntToQListQRgb;
        private static SetIntPtr dAddIntPtrToList = AddIntPtrToList;
        private static AddInt dAddIntToListInt = AddIntToListInt;
        private static AddUInt dAddUIntToListUInt = AddUIntToListUInt;
        private static ConstructDict dConstructDictionary = ConstructDictionary;
		private static AddToDictionaryFn dAddObjectObjectToDictionary = AddObjectObjectToDictionary;
        private static AddIntObject dAddIntObjectToDictionary = AddIntObjectToDictionary;
        private static DictToHash dDictionaryToQHash = DictionaryToQHash;
        private static DictToMap dDictionaryToQMap = DictionaryToQMap;
        private static OverridenMethodFn dOverridenMethod = SmokeInvocation.OverridenMethod;
        private static InvokeMethodFn dInvokeMethod = SmokeInvocation.InvokeMethod;
        private static CreateInstanceFn dCreateInstance = CreateInstance;
        private static InvokeCustomSlotFn dInvokeCustomSlot = SmokeInvocation.InvokeCustomSlot;
        private static InvokeDelegateFn dInvokeDelegate = SmokeInvocation.InvokeDelegate;
        private static OverridenMethodFn dGetProperty = GetProperty;
        private static SetPropertyFn dSetProperty = SetProperty;
        private static GetIntPtr dQPairGetFirst = QPairGetFirst;
        private static GetIntPtr dQPairGetSecond = QPairGetSecond;
        private static CreateQPairFn dCreateQPair = CreateQPair;
		private static SetIntPtrType dUnboxToStackItem = UnboxToStackItem;
        private static CreateInstanceFn dBoxFromStackItem = BoxFromStackItem;
        private static GetIntPtr dGenericPointerGetIntPtr = GenericPointerGetIntPtr;
        private static CreateInstanceFn dCreateGenericPointer = CreateGenericPointer;
        private static FromIntPtr dTryDispose = TryDispose;
#endregion

#region marshalling functions

		public static int SizeOfNativeLong = SizeOfLong();

		public static void FreeGCHandle(IntPtr handle) {
			if (handle == IntPtr.Zero) {
				Console.WriteLine("In FreeGCHandle(IntPtr): handle == 0 - This should not happen!");
				return;
			}
			if (handle is GCHandle) {
#if DEBUG
				DebugGCHandle.Free((GCHandle) handle);
#else
				((GCHandle) handle).SynchronizedFree();
#endif
			}
		}
		
		public static IntPtr GetSmokeObject(IntPtr instancePtr) {
			if (instancePtr == IntPtr.Zero) {
				return IntPtr.Zero;
			}

			Object instance = ((GCHandle) instancePtr).Target;
            ISmokeObject smokeObject = instance as ISmokeObject;
            if (smokeObject != null) {
                return smokeObject.SmokeObject;
            }
            return IntPtr.Zero;
		}
		
		public static void SetSmokeObject(IntPtr instancePtr, IntPtr smokeObjectPtr) {
			Object instance = ((GCHandle) instancePtr).Target;
// 			Debug.Assert(instance != null);
            ((ISmokeObject) instance).SmokeObject = smokeObjectPtr;
			return;
		}
		
		public static IntPtr GetProperty(IntPtr obj, string propertyName) {
			object o = ((GCHandle) obj).Target;
			Type t = o.GetType();
			PropertyInfo pi = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic |
									BindingFlags.Instance | BindingFlags.Static);
			
			MethodInfo fromValue = typeof(QVariant).GetMethod("FromValue", BindingFlags.Public | BindingFlags.Static);
			if (fromValue == null) throw new Exception("Couldn't find QVariant.FromValue<T> method");
			fromValue = fromValue.MakeGenericMethod( new Type[]{ pi.PropertyType } );

			if (pi != null) {
				object value = pi.GetValue(o, null);
				object[] args = { value };
				QVariant variant = (QVariant) fromValue.Invoke(null, args);
#if DEBUG
				return (IntPtr) DebugGCHandle.Alloc(variant);
#else
				return (IntPtr) GCHandle.Alloc(variant);
#endif
			}
			
			return (IntPtr) 0;
		}
		
		public static void SetProperty(IntPtr obj, string propertyName, IntPtr variant) {
			object o = ((GCHandle) obj).Target;
			Type t = o.GetType();
			object v = ((GCHandle) variant).Target;
			PropertyInfo pi = t.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic |
									BindingFlags.Instance | BindingFlags.Static);
									
			MethodInfo value = typeof(QVariant).GetMethod("Value", BindingFlags.Public | BindingFlags.Instance);
			if (value == null) throw new Exception("Couldn't find QVariant.Value<T> method");
			value = value.MakeGenericMethod( new Type[]{ pi.PropertyType } );

			if (pi != null) {
				object ret = value.Invoke(v, null);
				pi.SetValue(o, ret, null);
			}
		}

		// The key is an IntPtr corresponding to the address of the C++ instance,
		// and the value is the C# instance. This is used to prevent garbage
		// collection for instances which are contained inside, and owned by
		// other instances. For instance, a QObject can have a parent which will
		// delete the child when it is deleted. This Dictionary will prevent the
		// child from being GCd even if there are no references to it in the Qyoto
		// application code.
		static private Dictionary<IntPtr, object> globalReferenceMap = new Dictionary<IntPtr, object>();

		// The key is an IntPtr corresponding to the address of the C++ instance,
		// and the value is a WeakReference to the C# instance.
		static private Dictionary<IntPtr, WeakReference> pointerMap = new Dictionary<IntPtr, WeakReference>();

		public static void AddGlobalRef(IntPtr instancePtr, IntPtr ptr) {
			Object instance = ((GCHandle) instancePtr).Target;
			lock (globalReferenceMap) {
				globalReferenceMap[ptr] = instance;
			}
#if DEBUG
			if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
				Console.WriteLine("AddGlobalRef() Creating global reference 0x{0:x8} -> {1}", (int) ptr, instance);
			}
#endif
		}

		public static void RemoveGlobalRef(IntPtr instancePtr, IntPtr ptr) {
			Object instance = ((GCHandle) instancePtr).Target;
			lock (globalReferenceMap) {
				if (globalReferenceMap.ContainsKey(ptr)) {
#if DEBUG
					if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
						object reference;
						if (globalReferenceMap.TryGetValue(ptr, out reference)) {
							Console.WriteLine("RemoveGlobalRef() Removing global reference 0x{0:x8} -> {1}", (int) ptr, reference);
						}
					}
#endif
					globalReferenceMap.Remove(ptr);
				}
			}
		}

		public static void MapPointer(IntPtr ptr, IntPtr instancePtr, bool createGlobalReference) {
			lock (pointerMap) {
				Object instance = ((GCHandle) instancePtr).Target;
				WeakReference weakRef = new WeakReference(instance);
				pointerMap[ptr] = weakRef;
#if DEBUG
				if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
					Console.WriteLine("MapPointer() Creating weak reference 0x{0:x8} -> {1}", (int) ptr, instance);
				}
#endif
				if (createGlobalReference) {
					lock (globalReferenceMap) {
						globalReferenceMap[ptr] = instance;
#if DEBUG
						if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
							Console.WriteLine("MapPointer() Creating global reference 0x{0:x8} -> {1}", (int) ptr, instance);
						}
#endif
					}
				}
			}
		}
		
		public static void UnmapPointer(IntPtr ptr) {
			lock (pointerMap) {
#if DEBUG
				if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
					Console.WriteLine("UnmapPointer() Removing weak reference 0x{0:x8}", (int) ptr);
				}
#endif
				pointerMap.Remove(ptr);
				lock (globalReferenceMap) {
					if (globalReferenceMap.ContainsKey(ptr)) {
#if DEBUG
						if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
							object reference;
							if (globalReferenceMap.TryGetValue(ptr, out reference)) {
								Console.WriteLine("UnmapPointer() Removing global reference 0x{0:x8} -> {1}", (int) ptr, reference);
							}
						}
#endif
						globalReferenceMap.Remove(ptr);
					}
				}
			}
		}
		
		// If 'allInstances' is false then only return a result if the instance a custom subclass
		// of a Qyoto class and therefore could have custom slots or overriden methods
		public static IntPtr GetInstance(IntPtr ptr, bool allInstances) {
			WeakReference weakRef;
			object strongRef;
			lock (pointerMap) {
				if (!pointerMap.TryGetValue(ptr, out weakRef)) {
#if DEBUG
					if (	(QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0
							&& QDebug.debugLevel >= DebugLevel.Extensive ) 
					{
						Console.WriteLine("GetInstance() pointerMap[0x{0:x8}] == null", (int) ptr);
					}
#endif
					return IntPtr.Zero;
				}

				if (weakRef != null && weakRef.IsAlive) {
#if DEBUG
					if (	(QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0
							&& QDebug.debugLevel >= DebugLevel.Extensive ) 
					{
						Console.WriteLine("GetInstance() weakRef.IsAlive 0x{0:x8} -> {1}", (int) ptr, weakRef.Target is MethodInfo ? ((MethodInfo) weakRef.Target).Name : weakRef.Target.ToString());
					}
#endif
					if (!allInstances && IsSmokeClass(weakRef.Target.GetType())) {
						return IntPtr.Zero;
					} 

#if DEBUG
					GCHandle instanceHandle = DebugGCHandle.Alloc(weakRef.Target);
#else
					GCHandle instanceHandle = GCHandle.Alloc(weakRef.Target);
#endif
					return (IntPtr) instanceHandle;
				} else if (Environment.HasShutdownStarted && globalReferenceMap.TryGetValue(ptr, out strongRef)) {
#if DEBUG
					if (	(QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0
							&& QDebug.debugLevel >= DebugLevel.Extensive ) 
					{
						Console.WriteLine("GetInstance() strongRef 0x{0:x8} -> {1}", (int) ptr, strongRef);
					}
#endif
					if (!allInstances && IsSmokeClass(strongRef.GetType())) {
						return IntPtr.Zero;
					} 

#if DEBUG
					GCHandle instanceHandle = DebugGCHandle.Alloc(strongRef);
#else
					GCHandle instanceHandle = GCHandle.Alloc(strongRef);
#endif
					return (IntPtr) instanceHandle;
				} else {
#if DEBUG
					if (	(QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0
							&& QDebug.debugLevel >= DebugLevel.Extensive ) 
					{
						Console.WriteLine("GetInstance() weakRef dead ptr: 0x{0:x8}", (int) ptr);
					}
#endif
					pointerMap.Remove(ptr);
					return IntPtr.Zero;
				}
			}
		}

		// converts weak references to strong references so they are still available
		// when the application is shutting down.
		public static void ConvertRefs() {
			lock (pointerMap) {
				foreach (KeyValuePair<IntPtr, WeakReference> pair in pointerMap) {
					if (!pair.Value.IsAlive)
						continue;
					lock (globalReferenceMap)
						globalReferenceMap[pair.Key] = pair.Value.Target;
				}
			}
		}

		private class SmokeClassData {
			public string className;
			public ConstructorInfo constructorInfo;
			public object[] constructorParamTypes;
		}

		private static Dictionary<Type, SmokeClassData> smokeClassCache = new Dictionary<Type, SmokeClassData> ();
		
		private static SmokeClassData GetSmokeClassData(Type t) {
			SmokeClassData result = null;

			if (smokeClassCache.TryGetValue(t, out result)) {
				return result;
			}

			string className = null;

			object[] attr = t.GetCustomAttributes(typeof(SmokeClass), false);
			if (attr.Length > 0) {
				className = ((SmokeClass) attr[0]).signature;
			}

			result = new SmokeClassData();
			result.className = className;
			smokeClassCache[t] = result;

			if (t.IsInterface) {
				return result;
			}

			Type[] paramTypes = new Type[1];
			paramTypes[0] = typeof(Type);
			result.constructorParamTypes = new object[] { paramTypes[0] };

			result.constructorInfo = t.GetConstructor(BindingFlags.NonPublic 
				| BindingFlags.Instance, null, new Type[ ] { typeof( Type ) } , null);

			return result;
		}

		// The class is not a custom subclass of a Qyoto class, and also is not
		// a superclass of a Qyoto class, such as a MarshalByRefObject.
		public static bool IsSmokeClass(Type klass) {
			SmokeClassData data = GetSmokeClassData(klass);
			return data != null && data.className != null;
		}

		// The C++ class name signature of a Smoke class or interface
		public static string SmokeClassName(Type klass) {
			SmokeClassData data = GetSmokeClassData(klass);
			while (data.className == null) {
				klass = klass.BaseType;
				data = GetSmokeClassData(klass);
			}
			
			return data.className;
		}

		// CreateInstance() creates a wrapper instance around a C++ instance which
		// has been created in C++ code, and not via a Qyoto C# constructor call.
		// It takes the class name string and obtains its Type. Then it finds the
		// dummy constructor which takes a Type as an arg, like this for example:
		//
 		//		protected QWidget(Type dummy) : base((Type) null) {}
		//
		// The constructor is run to create the wrapper instance. Then the method 
		// 'CreateProxy()' to create the transparent proxy to forward the method
		// calls to SmokeInvocation.Invoke() is called.
		public static IntPtr CreateInstance(string className, IntPtr smokeObjectPtr) {
			SmokeClassData data = null;
			foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
				Type t = a.GetType(className);
				if (t != null) {
					if (t.IsAbstract) {
						return CreateInstance(className + "Internal", smokeObjectPtr);
					}
					data = GetSmokeClassData(t);
				}
			}

			if (data == null) {
				if (className.Contains(".")) {
					StringBuilder sb = new StringBuilder(className);
					sb[className.LastIndexOf(".")] = '+';
					return CreateInstance(sb.ToString(), smokeObjectPtr);
				}
				Console.Error.WriteLine("CreateInstance() ** Missing class ** {0}", className);
			}

			object result = data.constructorInfo.Invoke(data.constructorParamTypes);
            ISmokeObject smokeObject = (ISmokeObject) result;
            smokeObject.CreateProxy();
            smokeObject.SmokeObject = smokeObjectPtr;
#if DEBUG
			if ((QDebug.DebugChannel() & QtDebugChannel.QTDB_GC) != 0) {
				Console.WriteLine("CreateInstance(\"{0}\") constructed {1}", className, result);
			}
#endif
#if DEBUG
			return (IntPtr) DebugGCHandle.Alloc(result);
#else
			return (IntPtr) GCHandle.Alloc(result);
#endif
		}

		public static IntPtr IntPtrToCharStarStar(IntPtr ptr) {
			string[] temp = (string[]) ((GCHandle) ptr).Target;
			return StringArrayToCharStarStar(temp.Length, temp);
		}

		public static string IntPtrToString(IntPtr ptr) {
			string value = Marshal.PtrToStringAuto(ptr);
			Marshal.FreeHGlobal(ptr);
			return value;
		}

		public static IntPtr IntPtrFromString(string str) {
			return Marshal.StringToHGlobalAuto(str);
		}

		public static IntPtr IntPtrFromQString(IntPtr ptr) {
			return Marshal.StringToHGlobalAuto(StringFromQString(ptr));
		}

		public static IntPtr StringBuilderToQString(IntPtr ptr) {
			object obj = (object) ((GCHandle) ptr).Target;
			if (obj.GetType() == typeof(StringBuilder)) {
				return StringToQString(((StringBuilder) obj).ToString());
			} else {
				return StringToQString((string) obj);
			}
		}

		public static void StringBuilderFromQString(IntPtr ptr, string str) {
			object obj = (object) ((GCHandle) ptr).Target;
			if (obj.GetType() == typeof(StringBuilder)) {
				StringBuilder temp = (StringBuilder) obj;
				temp.Remove(0, temp.Length);
				temp.Append(str);
			}
		}

		public static IntPtr StringListToQStringList(IntPtr ptr) {
			List<string> sl = (List<string>) ((GCHandle) ptr).Target;
			string[] s = (string[]) sl.ToArray();
			return StringArrayToQStringList(s.Length, s);
		}

		public static IntPtr ListIntToQListInt(IntPtr ptr) {
			List<int> il = (List<int>) ((GCHandle) ptr).Target;
			IntPtr QList = ConstructQListInt();
			foreach (int i in il) {
				AddIntToQList(QList, i);
			}
			return QList;
		}
		
		public static IntPtr ListUIntToQListQRgb(IntPtr ptr) {
			List<uint> il = (List<uint>) ((GCHandle) ptr).Target;
			IntPtr QList = ConstructQListQRgb();
			foreach (uint i in il) {
				AddUIntToQListQRgb(QList, i);
			}
			return QList;
		}

		public static void AddUIntToListUInt(IntPtr ptr, uint i) {
			List<uint> il = (List<uint>) ((GCHandle) ptr).Target;
			il.Add(i);
		}

		public static IntPtr ListToPointerList(IntPtr ptr) {
			if (ptr.ToInt64() < 0) {
				Console.WriteLine("The IntPtr is invalid!");
				return IntPtr.Zero;
			}
			object l;
			try {
				l = ((GCHandle) ptr).Target;
			} catch (Exception e) {
				Console.WriteLine("An error occured when retrieving the target: {0}", e);
				return ConstructPointerList();
			}
			// convert the list to an array via reflection. this is probably the easiest way
			object[] oa = (object[]) l.GetType().GetMethod("ToArray").Invoke(l, null);
			
			IntPtr list = ConstructPointerList();
			foreach (object o in oa) {
#if DEBUG
				AddObjectToPointerList(list, (IntPtr) DebugGCHandle.Alloc(o));
#else
				AddObjectToPointerList(list, (IntPtr) GCHandle.Alloc(o));
#endif
			}
			return list;
		}

		public static IntPtr ConstructList(string type) {
			Type basetype = typeof(List<>);
			Type t = null;
			foreach(Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
				t = a.GetType(type);
				if (t != null) {
					break;
				}
			}

			// check for multiple inheritance - use the interface if necessary
			string ifacename = "I" + t.Name;
			foreach(Type iface in t.GetInterfaces()) {
				if (iface.Name == ifacename) {
					t = iface;
					break;
				}
			}

			Type[] generic = { t };
			Type merged = basetype.MakeGenericType(generic);
			
			object o = Activator.CreateInstance(merged);
#if DEBUG		
			return (IntPtr) DebugGCHandle.Alloc(o);
#else
			return (IntPtr) GCHandle.Alloc(o);
#endif
		}

		public static void AddIntPtrToList(IntPtr obj, IntPtr ptr) {
			object list = ((GCHandle) obj).Target;
			object o = ((GCHandle) ptr).Target;
        	((IList) list).Add(o);
		}

		public static void AddIntToListInt(IntPtr obj, int i) {
			List<int> il = (List<int>) ((GCHandle) obj).Target;
			il.Add(i);
		}

		public static IntPtr ConstructDictionary(string type1, string type2) {
			Type basetype = typeof(Dictionary<,>);
			Type[] generic = { Type.GetType(type1), Type.GetType(type2) };
			Type merged = basetype.MakeGenericType(generic);
			
			object o = Activator.CreateInstance(merged);

#if DEBUG
			return (IntPtr) DebugGCHandle.Alloc(o);
#else
			return (IntPtr) GCHandle.Alloc(o);
#endif
		}

		public static void AddObjectObjectToDictionary(IntPtr dic, IntPtr key, IntPtr val) {
			object d = ((GCHandle) dic).Target;
			object k = ((GCHandle) key).Target;
			object v = ((GCHandle) val).Target;
        	((IDictionary) d).Add(k, v);
		}

		public static void AddIntObjectToDictionary(IntPtr dict, int key, IntPtr val) {
			object d = ((GCHandle) dict).Target;
			object v = ((GCHandle) val).Target;
        	((IDictionary) d).Add(key, v);
		}

		public static IntPtr DictionaryToQHash(IntPtr dict, int type) {
			object d = ((GCHandle) dict).Target;
			IntPtr hash = ConstructQHash(type);
			
			if (type == 0) {
				Dictionary<int, QVariant> d1 = (Dictionary<int, QVariant>) d;
				foreach (KeyValuePair<int, QVariant> kvp in d1) {
#if DEBUG
					AddIntQVariantToQHash(hash, kvp.Key, (IntPtr) DebugGCHandle.Alloc(kvp.Value));
#else
					AddIntQVariantToQHash(hash, kvp.Key, (IntPtr) GCHandle.Alloc(kvp.Value));
#endif
				}
			} else if (type == 1) {
				Dictionary<string, string> d1 = (Dictionary<string, string>) d;
				foreach (KeyValuePair<string, string> kvp in d1) {
					AddQStringQStringToQHash(hash, kvp.Key, kvp.Value);
				}
			} else if (type == 2) {
				Dictionary<string, QVariant> d1 = (Dictionary<string, QVariant>) d;
				foreach (KeyValuePair<string, QVariant> kvp in d1) {
#if DEBUG
					AddQStringQVariantToQHash(hash, kvp.Key, (IntPtr) DebugGCHandle.Alloc(kvp.Value));
#else
					AddQStringQVariantToQHash(hash, kvp.Key, (IntPtr) GCHandle.Alloc(kvp.Value));
#endif
				}
			}
			return hash;
		}		

		public static IntPtr DictionaryToQMap(IntPtr dict, int type) {
			object d = ((GCHandle) dict).Target;
			IntPtr map = ConstructQMap(type);
			
			if (type == 0) {
				Dictionary<int, QVariant> d1 = (Dictionary<int, QVariant>) d;
				foreach (KeyValuePair<int, QVariant> kvp in d1) {
#if DEBUG
					AddIntQVariantToQMap(map, kvp.Key, (IntPtr) DebugGCHandle.Alloc(kvp.Value));
#else
					AddIntQVariantToQMap(map, kvp.Key, (IntPtr) GCHandle.Alloc(kvp.Value));
#endif
				}
			} else if (type == 1) {
				Dictionary<string, string> d1 = (Dictionary<string, string>) d;
				foreach (KeyValuePair<string, string> kvp in d1) {
					AddQStringQStringToQMap(map, kvp.Key, kvp.Value);
				}
			} else if (type == 2) {
				Dictionary<string, QVariant> d1 = (Dictionary<string, QVariant>) d;
				foreach (KeyValuePair<string, QVariant> kvp in d1) {
#if DEBUG
					AddQStringQVariantToQMap(map, kvp.Key, (IntPtr) DebugGCHandle.Alloc(kvp.Value));
#else
					AddQStringQVariantToQMap(map, kvp.Key, (IntPtr) GCHandle.Alloc(kvp.Value));
#endif
				}
			}
			return map;
		}
		
		public static IntPtr QPairGetFirst(IntPtr pair) {
			object o = ((GCHandle) pair).Target;
			object field = o.GetType().GetField("first").GetValue(o);
			if (field == null) return IntPtr.Zero;
			return (IntPtr) GCHandle.Alloc(field);
		}
		
		public static IntPtr QPairGetSecond(IntPtr pair) {
			object o = ((GCHandle) pair).Target;
			object field = o.GetType().GetField("second").GetValue(o);
			if (field == null) return IntPtr.Zero;
			return (IntPtr) GCHandle.Alloc(field);
		}
		
		public static IntPtr CreateQPair(IntPtr first, IntPtr second) {
			Type t = typeof(QPair<,>);
			object firstObject = ((GCHandle) first).Target;
			object secondObject = ((GCHandle) second).Target;
			t = t.MakeGenericType(new Type[] { firstObject.GetType(), secondObject.GetType() });
			object pair = Activator.CreateInstance(t, new object[] { firstObject, secondObject });
			return (IntPtr) GCHandle.Alloc(pair);
		}

		public static unsafe TypeId UnboxToStackItem(object o, StackItem *item) {
			if (o == null) {
				item->s_class = IntPtr.Zero;
				return TypeId.t_class;
			}
			Type t = o.GetType();

			if (t.IsEnum) {
				if (SizeOfNativeLong < sizeof(long)) {
					int value = Enum.GetUnderlyingType(t) == typeof(long) ? (int) (long) o : (int) o;
					item->s_int = value;
					return TypeId.t_int;
				} else {
					long value = Enum.GetUnderlyingType(t) == typeof(long) ? (long) o : (int) o;
					item->s_long = value;
					return TypeId.t_long;
				}
			} else if (t == typeof(int)) {
				item->s_int = (int) o;
				return TypeId.t_int;
			} else if (t == typeof(bool)) {
				item->s_bool = (bool) o;
				return TypeId.t_bool;
			} else if (t == typeof(short)) {
				item->s_short = (short) o;
				return TypeId.t_short;
			} else if (t == typeof(float)) {
				item->s_float = (float) o;
				return TypeId.t_float;
			} else if (t == typeof(double)) {
				item->s_double = (double) o;
				return TypeId.t_double;
			} else if (t == typeof(NativeLong)) {
				if (SizeOfNativeLong < sizeof(long)) {
					item->s_int = (int) (NativeLong) o;
					return TypeId.t_int;
				} else {
					item->s_long = (NativeLong) o;
					return TypeId.t_long;
				}
			} else if (t == typeof(ushort)) {
				item->s_ushort = (ushort) o;
				return TypeId.t_ushort;
			} else if (t == typeof(uint)) {
				item->s_uint = (uint) o;
				return TypeId.t_uint;
			} else if (t == typeof(NativeULong)) {
				if (SizeOfNativeLong < sizeof(long)) {
					item->s_uint = (uint) (NativeULong) o;
					return TypeId.t_uint;
				} else {
					item->s_ulong = (NativeULong) o;
					return TypeId.t_ulong;
				}
			} else if (t == typeof(long)) {
				item->s_long = (long) o;
				return TypeId.t_long;
			} else if (t == typeof(ulong)) {
				item->s_ulong = (ulong) o;
				return TypeId.t_ulong;
			} else if (t == typeof(sbyte)) {
				item->s_char = (sbyte) o;
				return TypeId.t_uchar;
			} else if (t == typeof(byte)) {
				item->s_uchar = (byte) o;
				return TypeId.t_uchar;
			} else if (t == typeof(char)) {
				item->s_char = (sbyte) (char) o;
				return TypeId.t_char;
			} else {
#if DEBUG
				if (o is Delegate) {
					item->s_class = Marshal.GetFunctionPointerForDelegate((Delegate) o);
				} else {
					item->s_class = (IntPtr) DebugGCHandle.Alloc(o);
				}
#else
				string text = o as string;
				if (text != null) {
					item->s_class = Marshal.StringToHGlobalAuto(text);
					return TypeId.t_string;
				}
				if (o is Delegate) {
					item->s_class = Marshal.GetFunctionPointerForDelegate((Delegate) o);
				} else {
					item->s_class = (IntPtr) GCHandle.Alloc(o);
				}
#endif
				return t == typeof(string) ? TypeId.t_string : TypeId.t_class;
			}
			return TypeId.t_class;
		}

		public static unsafe TypeId UnboxToStackItem(IntPtr obj, IntPtr st) {
			StackItem *item = (StackItem*) st;
			object o = ((GCHandle) obj).Target;

			return UnboxToStackItem(o, item);
		}

		public static TypeId GetTypeId(Type t) {
			if (t.IsEnum) {
				if (SizeOfNativeLong < sizeof(long)) {
					return TypeId.t_int;
				} else {
					return TypeId.t_long;
				}
			} else if (t == typeof(int)) {
				return TypeId.t_int;
			} else if (t == typeof(bool)) {
				return TypeId.t_bool;
			} else if (t == typeof(short)) {
				return TypeId.t_short;
			} else if (t == typeof(float)) {
				return TypeId.t_float;
			} else if (t == typeof(double)) {
				return TypeId.t_double;
			} else if (t == typeof(NativeLong)) {
				if (SizeOfNativeLong < sizeof(long)) {
					return TypeId.t_int;
				} else {
					return TypeId.t_long;
				}
			} else if (t == typeof(ushort)) {
				return TypeId.t_ushort;
			} else if (t == typeof(uint)) {
				return TypeId.t_uint;
			} else if (t == typeof(NativeULong)) {
				if (SizeOfNativeLong < sizeof(long)) {
					return TypeId.t_uint;
				} else {
					return TypeId.t_ulong;
				}
			} else if (t == typeof(long)) {
				return TypeId.t_long;
			} else if (t == typeof(ulong)) {
				return TypeId.t_ulong;
			} else if (t == typeof(sbyte)) {
				return TypeId.t_uchar;
			} else if (t == typeof(byte)) {
				return TypeId.t_uchar;
			} else if (t == typeof(char)) {
				return TypeId.t_char;
			} else {
				return t == typeof(string) ? TypeId.t_string : TypeId.t_class;
			}
			return TypeId.t_class;
		}

		public static unsafe object BoxFromStackItem(Type t, int typeID, StackItem *item) {
			if (t.IsByRef) {
				t = t.GetElementType();
			}

			if (typeID == 0) {
				if (t.IsEnum) {
					if (SizeOfNativeLong < sizeof(long)) {
						return Enum.ToObject(t, item->s_int);
					} else {
						return Enum.ToObject(t, item->s_long);
					}
				} else if (t == typeof(int)) {
					return item->s_int;
				} else if (t == typeof(bool)) {
					return item->s_bool;
				} else if (t == typeof(short)) {
					return item->s_short;
				} else if (t == typeof(float)) {
					return item->s_float;
				} else if (t == typeof(double)) {
					return item->s_double;
				} else if (t == typeof(NativeLong)) {
					return new NativeLong((SizeOfNativeLong < sizeof(long))? item->s_int : item->s_long);
				} else if (t == typeof(ushort)) {
					return item->s_ushort;
				} else if (t == typeof(uint)) {
					return item->s_uint;
				} else if (t == typeof(NativeULong)) {
					return new NativeULong((SizeOfNativeLong < sizeof(long))? item->s_uint : item->s_ulong);
				} else if (t == typeof(long)) {
					return item->s_long;
				} else if (t == typeof(ulong)) {
					return item->s_ulong;
				} else if (t == typeof(sbyte)) {
					return item->s_char;
				} else if (t == typeof(byte)) {
					return item->s_uchar;
				} else if (t == typeof(char)) {
					return (char) item->s_char;
				} else if (item->s_class != IntPtr.Zero) {
					if (typeof(Delegate).IsAssignableFrom(t)) {
						return Marshal.GetDelegateForFunctionPointer(item->s_class, t);
					}
					if (t == typeof(string)) {
						string value = Marshal.PtrToStringAuto(item->s_class);
						Marshal.FreeHGlobal(item->s_class);
						return value;
					}
					// the StackItem contains a GCHandle to an object
					GCHandle handle = (GCHandle) item->s_class;
					object ret = handle.Target;
#if DEBUG
					DebugGCHandle.Free(handle);
#else
					handle.SynchronizedFree();
#endif
					return ret;
				}
				return null;
			}

			return GetByTypeID(typeID, item);
		}

		private static unsafe object GetByTypeID(int typeID, StackItem *item) {
			switch ((TypeId) typeID) {
				case TypeId.t_bool:
					return item->s_bool;
				case TypeId.t_char:
					return (char) item->s_char;
				case TypeId.t_uchar:
					return item->s_uchar;
				case TypeId.t_short:
					return item->s_short;
				case TypeId.t_ushort:
					return item->s_ushort;
				case TypeId.t_enum:
				case TypeId.t_int:
					return item->s_int;
				case TypeId.t_uint:
					return item->s_uint;
				case TypeId.t_long:
					return item->s_long;
				case TypeId.t_ulong:
					return item->s_ulong;
				case TypeId.t_float:
					return item->s_float;
				case TypeId.t_double:
					return item->s_double;
				case TypeId.t_string:
					string value = Marshal.PtrToStringAuto(item->s_class);
					Marshal.FreeHGlobal(item->s_class);
					return value;
				default:
					GCHandle handle = (GCHandle) item->s_class;
					object ret = handle.Target;
#if DEBUG
					DebugGCHandle.Free(handle);
#else
					handle.SynchronizedFree();
#endif
					return ret;
			}
		}

		public static unsafe IntPtr BoxFromStackItem(string type, IntPtr st) {
			Type t = Type.GetType(type);
			if (t == null) return IntPtr.Zero;
			StackItem *item = (StackItem*) st;

			object box = BoxFromStackItem(t, 0, item);

			if (box == null) return IntPtr.Zero;
			return (IntPtr) GCHandle.Alloc(box);
		}
		
		public static IntPtr GenericPointerGetIntPtr(IntPtr obj) {
			object o = ((GCHandle) obj).Target;
			return (IntPtr) o.GetType().GetMethod("ToIntPtr").Invoke(o, null);
		}
		
		public static IntPtr CreateGenericPointer(string type, IntPtr ptr) {
			Type t = typeof(Pointer<>);
			t = t.MakeGenericType( new Type[] { Type.GetType(type) } );
			object o = Activator.CreateInstance(t, new object[] { ptr });
			return (IntPtr) GCHandle.Alloc(o);
		}
		
		public static void TryDispose(IntPtr obj) {
			object o = ((GCHandle) obj).Target;
			if (o == null) {
				return;
			}

			Type t = o.GetType();
			MethodInfo mi = t.GetMethod("Dispose");

			if (mi == null || IsSmokeClass(mi.DeclaringType)) {
				return;
			}

			((IDisposable) o).Dispose();
		}
#endregion
		
#region Setup
		public static void SetUp() {
            
			InstallFreeGCHandle(dFreeGCHandle);

			InstallGetSmokeObject(dGetSmokeObject);
			InstallSetSmokeObject(dSetSmokeObject);
			
			InstallAddGlobalRef(dAddGlobalRef);
			InstallRemoveGlobalRef(dRemoveGlobalRef);
			InstallMapPointer(dMapPointer);
			InstallUnmapPointer(dUnmapPointer);
			InstallGetInstance(dGetInstance);

			InstallIntPtrToCharStarStar(dIntPtrToCharStarStar);
			InstallIntPtrToCharStar(dIntPtrToString);
			InstallIntPtrFromCharStar(dIntPtrFromString);
			InstallIntPtrFromQString(dIntPtrFromQString);
			InstallStringBuilderToQString(dStringBuilderToQString);
			InstallStringBuilderFromQString(dStringBuilderFromQString);
			InstallConstructList(dConstructList);
			InstallStringListToQStringList(dStringListToQStringList);
			InstallListToPointerList(dListToPointerList);
			InstallListIntToQListInt(dListIntToQListInt);
			InstallListUIntToQListQRgb(dListUIntToQListQRgb);
			InstallAddIntPtrToList(dAddIntPtrToList);
			InstallAddIntToListInt(dAddIntToListInt);
			InstallAddUIntToListUInt(dAddUIntToListUInt);

			InstallConstructDictionary(dConstructDictionary);
			InstallAddObjectObjectToDictionary(dAddObjectObjectToDictionary);
			InstallAddIntObjectToDictionary(dAddIntObjectToDictionary);
			
			InstallDictionaryToQHash(dDictionaryToQHash);
			InstallDictionaryToQMap(dDictionaryToQMap);

			InstallOverridenMethod(dOverridenMethod);
			InstallInvokeMethod(dInvokeMethod);

			InstallCreateInstance(dCreateInstance);
			InstallInvokeCustomSlot(dInvokeCustomSlot);
			InstallInvokeDelegate(dInvokeDelegate);
			
			InstallGetProperty(dGetProperty);
			InstallSetProperty(dSetProperty);
			
			InstallQPairGetFirst(dQPairGetFirst);
			InstallQPairGetSecond(dQPairGetSecond);
			InstallCreateQPair(dCreateQPair);
			
			InstallUnboxToStackItem(dUnboxToStackItem);
			InstallBoxFromStackItem(dBoxFromStackItem);
			
			InstallGenericPointerGetIntPtr(dGenericPointerGetIntPtr);
			InstallCreateGenericPointer(dCreateGenericPointer);
			
			InstallTryDispose(dTryDispose);
		}
#endregion

	}
}