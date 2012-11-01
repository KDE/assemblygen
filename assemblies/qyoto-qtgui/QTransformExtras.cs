namespace QtGui {

	using System;
	using QtCore;

	public partial class QTransform : Object, IDisposable {
		public static implicit operator QTransform(QMatrix arg) {
			return new QTransform(arg);
		}
	}
}
