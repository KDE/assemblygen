namespace QtCore {
	using System;
	public partial class QByteArray : Object, IDisposable {
		public QByteArray(byte[] array) : this(array.Length, (sbyte) '\0') {
			Pointer<sbyte> p = Data();
			for (int i = 0; i < array.Length; i++) {
				p[i] = (sbyte) array[i];
			}
		}
		
		public byte[] ToArray() {
			Pointer<sbyte> p = Data();
			byte[] array = new byte[Size()];
			for (int i = 0; i < Size(); i++) {
				array[i] = (byte) p[i];
			}
			return array;
		}
		
		public static implicit operator QByteArray(string arg) {
			return new QByteArray(arg);
		}
	}
}
