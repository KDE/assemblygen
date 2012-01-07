using System;

namespace Qyoto
{
	public class QEventHandler<T> : QObject where T : QEvent
	{
		private readonly EventHandler<QEventArgs<T>> handler;
		private readonly QObject sender;
		private readonly QEventArgs<T> args;

		public QEventHandler(QObject sender, QEventArgs<T> args, EventHandler<QEventArgs<T>> handler)
		{
			this.sender = sender;
			this.args = args;
			this.handler = handler;
		}

		public override bool EventFilter(QObject arg1, QEvent arg2)
		{
			if (arg1 == sender && arg2.type() == args.EventType)
			{
				args.Event = (T) arg2;
				handler(sender, args);
				return args.Handled;
			}
			return base.EventFilter(arg1, arg2);
		}
	}
}
