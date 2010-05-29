namespace Qyoto {

	using System;

	[SmokeClass("QDBusVariant")]
	public class QDBusVariant : QVariant {
		protected QDBusVariant(System.Type dummy) : base((System.Type) null) {}
		public QDBusVariant() : base() { }
		public QDBusVariant(QVariant variant) : base(variant) { }

		static public new QDBusVariant FromValue<T>(object value) {
			return new QDBusVariant(QVariant.FromValue(value, typeof(T)));
		}

		public QVariant Variant {
			get { return this; }
		}
	}
}
