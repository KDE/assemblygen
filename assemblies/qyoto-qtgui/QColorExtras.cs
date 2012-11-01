namespace QtGui {

	using System;
	using QtCore;

	public partial class QColor : Object, IDisposable {
		public static implicit operator QColor(Qt.GlobalColor arg) {
			return new QColor(arg);
		}
	}
}
