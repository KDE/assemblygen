namespace QtGui {

	using System;
	using QtCore;

	public partial class QKeySequence : Object, IDisposable {
		public static implicit operator QKeySequence(int arg) {
			return new QKeySequence(arg);
		}
		public static implicit operator QKeySequence(Qt.Key arg) {
			return new QKeySequence((int) arg);
		}
		public static implicit operator QKeySequence(QKeySequence.StandardKey arg) {
			return new QKeySequence((int) arg);
		}
		public static implicit operator QKeySequence(string arg) {
			return new QKeySequence(arg);
		}
	}
}
