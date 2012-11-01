namespace QtCore {

	using System;
	using System.Runtime.InteropServices;
	using System.Collections.Generic;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void Slot();
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    public delegate void Slot<T> (T arg);
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    public delegate void Slot<T1, T2> (T1 arg1, T2 arg2);
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    public delegate void Slot<T1, T2, T3> (T1 arg1, T2 arg2, T3 arg3);
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    public delegate void Slot<T1, T2, T3, T4> (T1 arg1, T2 arg2, T3 arg3, T4 arg4);
    [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
    public delegate void Slot<T1, T2, T3, T4, T5> (T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);

	public partial class QObject : Qt, IDisposable {
        [UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        private delegate void AddToListFn (IntPtr obj);
		
		[DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr FindQObjectChild(IntPtr parent, string childTypeName, IntPtr childMetaObject, string childName);
		
		[DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		private static extern void FindQObjectChildren(IntPtr parent, string childTypeName, IntPtr childMetaObject, IntPtr regexp,
									string childName, AddToListFn addFn);
		
		[DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool ConnectDelegate(IntPtr obj, string signal, Delegate d, IntPtr handle);
		
		[DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		private static extern bool DisconnectDelegate(IntPtr obj, string signal, Delegate d);

		[SmokeMethod("metaObject()")]
		public virtual QMetaObject MetaObject() {
			if (SmokeMarshallers.IsSmokeClass(GetType())) {
				return (QMetaObject) interceptor.Invoke("metaObject", "metaObject()", typeof(QMetaObject), false);
			} else {
				return Qyoto.GetMetaObject(this);
			}
		}
		
		public static bool Connect(QObject obj, string signal, Slot d) {
			// allocate a gchandle so the delegate won't be collected
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}
		
		public static bool Connect<T>(QObject obj, string signal, Slot<T> d) {
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}
		
		public static bool Connect<T1, T2>(QObject obj, string signal, Slot<T1, T2> d) {
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}

		public static bool Connect<T1, T2, T3>(QObject obj, string signal, Slot<T1, T2, T3> d) {
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}

		public static bool Connect<T1, T2, T3, T4>(QObject obj, string signal, Slot<T1, T2, T3, T4> d) {
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}

		public static bool Connect<T1, T2, T3, T4, T5>(QObject obj, string signal, Slot<T1, T2, T3, T4, T5> d) {
			IntPtr handle = (IntPtr) GCHandle.Alloc(d);
			return ConnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d, handle);
		}

		public static bool Disconnect(QObject obj, string signal, Slot d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public static bool Disconnect<T>(QObject obj, string signal, Slot<T> d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public static bool Disconnect<T1, T2>(QObject obj, string signal, Slot<T1, T2> d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public static bool Disconnect<T1, T2, T3>(QObject obj, string signal, Slot<T1, T2, T3> d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public static bool Disconnect<T1, T2, T3, T4>(QObject obj, string signal, Slot<T1, T2, T3, T4> d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public static bool Disconnect<T1, T2, T3, T4, T5>(QObject obj, string signal, Slot<T1, T2, T3, T4, T4> d) {
			return DisconnectDelegate((IntPtr) GCHandle.Alloc(obj), signal, d);
		}

		public T FindChild<T>(string name) {
			string childClassName = null;
			IntPtr metaObject = IntPtr.Zero;
			if (SmokeMarshallers.IsSmokeClass(typeof(T))) {
				childClassName = SmokeMarshallers.SmokeClassName(typeof(T));
			} else {
				metaObject = (IntPtr) GCHandle.Alloc(Qyoto.GetMetaObject(typeof(T)));
			}

			IntPtr child = FindQObjectChild((IntPtr) GCHandle.Alloc(this), childClassName, metaObject, name);
			if (child != IntPtr.Zero) {
				try {
					return (T) ((GCHandle) child).Target;
				} catch (Exception e) {
					Console.WriteLine("Found child, but an error has occurred: {0}", e.Message);
					return default(T);
				}
			} else {
				return default(T);
			}
		}

		public T FindChild<T>() {
			return FindChild<T>(string.Empty);
		}

		public List<T> FindChildren<T>(string name) {
			List<T> list = new List<T>();
			AddToListFn addFn = delegate(IntPtr obj) {
				T o = (T) ((System.Runtime.InteropServices.GCHandle) obj).Target;
				list.Add(o);
			};

			string childClassName = null;
			IntPtr metaObject = IntPtr.Zero;
			if (SmokeMarshallers.IsSmokeClass(typeof(T))) {
				childClassName = SmokeMarshallers.SmokeClassName(typeof(T));
			} else {
				metaObject = (IntPtr) GCHandle.Alloc(Qyoto.GetMetaObject(typeof(T)));
			}
			FindQObjectChildren((IntPtr) GCHandle.Alloc(this), childClassName, metaObject, IntPtr.Zero, name, addFn);
			return list;
		}

		public List<T> FindChildren<T>() {
			return FindChildren<T>(string.Empty);
		}

		public List<T> FindChildren<T>(QRegExp regExp) {
			List<T> list = new List<T>();
			AddToListFn addFn = delegate(IntPtr obj) {
				T o = (T) ((System.Runtime.InteropServices.GCHandle) obj).Target;
				list.Add(o);
			};

			string childClassName = null;
			IntPtr metaObject = IntPtr.Zero;
			if (SmokeMarshallers.IsSmokeClass(typeof(T))) {
				childClassName = SmokeMarshallers.SmokeClassName(typeof(T));
			} else {
				metaObject = (IntPtr) GCHandle.Alloc(Qyoto.GetMetaObject(typeof(T)));
			}
			FindQObjectChildren((IntPtr) GCHandle.Alloc(this), childClassName, metaObject, (IntPtr) GCHandle.Alloc(regExp), string.Empty, addFn);
			return list;
		}
	}
}