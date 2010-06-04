namespace Kimono {
    using System;
    using Qyoto;

    public partial class KApplication : QApplication, IDisposable {
        public KApplication(bool GUIenabled) : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KApplication$", "KApplication(bool)", typeof(void), false, typeof(bool), GUIenabled);
            qApp = this;
        }
        public KApplication() : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KApplication", "KApplication()", typeof(void), false);
            qApp = this;
        }
        public KApplication(bool GUIenabled, KComponentData cData) : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KApplication$#", "KApplication(bool, const KComponentData&)", typeof(void), false, typeof(bool), GUIenabled, typeof(KComponentData), cData);
            qApp = this;
        }
    }
}
