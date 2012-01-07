using System;

namespace Qyoto
{
	public class QEventArgs<T> : EventArgs where T : QEvent
	{
		public QEventArgs(QEvent.Type eventType)
		{
			this.EventType = eventType;
		}

		public QEvent.Type EventType { get; private set; }

		public bool Handled { get; set; }

		public T Event { get; set; }
	}
}
