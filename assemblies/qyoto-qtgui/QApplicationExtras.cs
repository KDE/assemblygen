namespace QtGui {

	using System;
	using System.Reflection;
	using System.Collections;
	using System.Text;
	using QtCore;

	public partial class QApplication : QCoreApplication, IDisposable {
	
		string[] GenerateArgs(string[] argv)
		{
			string[] args = new string[argv.Length + 1];
			Assembly a = System.Reflection.Assembly.GetEntryAssembly();
			
			if(a == null)
				a = System.Reflection.Assembly.GetExecutingAssembly();
			
			object[] attrs = a.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (attrs.Length > 0) {
				args[0] = ((AssemblyTitleAttribute) attrs[0]).Title;
			} else {
				QFileInfo info = new QFileInfo(a.Location);
				args[0] = info.BaseName;
			}
			argv.CopyTo(args, 1);
			
			return args;
		}

		public QApplication(string[] argv) : this((System.Type) null) {
			CreateProxy();
			Qt.qApp = this;
			
			string[] args = GenerateArgs(argv);

			interceptor.Invoke(	"QApplication$?", "QApplication(int&, char**)", true,
								typeof(void), false, typeof(int), args.Length, typeof(string[]), args );
			SetupEventReceiver();
		}

		public QApplication(string[] argv, bool GUIenabled) : this((System.Type) null) {
			CreateProxy();
			Qt.qApp = this;
			
			string[] args = GenerateArgs(argv);

			interceptor.Invoke(	"QApplication$?$", "QApplication(int&, char**, bool)", true,
								typeof(void), false, typeof(int), args.Length, typeof(string[]), args, typeof(bool), GUIenabled );
			SetupEventReceiver();
		}
	
		public QApplication(string[] argv, QApplication.Type arg3) : this((System.Type) null) {
			CreateProxy();
			Qt.qApp = this;
			
			string[] args = GenerateArgs(argv);

			interceptor.Invoke(	"QApplication$?$", "QApplication(int&, char**, QApplication::Type)", true,
								typeof(void), false, typeof(int), args.Length, typeof(string[]), args, typeof(QApplication.Type), arg3 );
			SetupEventReceiver();
		}

		public static new int Exec() {
			int result = (int) staticInterceptor.Invoke("exec", "exec()", typeof(int), false);
			Qyoto.Cleanup();
			return result;
		}
	}
}
