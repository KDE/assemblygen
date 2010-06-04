namespace Kimono {
    using System;
    using Qyoto;
    public partial class KUniqueApplication : KApplication, IDisposable {
        public KUniqueApplication(bool GUIenabled, bool configUnique) : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KUniqueApplication$$", "KUniqueApplication(bool, bool)", typeof(void), false, typeof(bool), GUIenabled, typeof(bool), configUnique);
            qApp = this;
        }
        public KUniqueApplication(bool GUIenabled) : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KUniqueApplication$", "KUniqueApplication(bool)", typeof(void), false, typeof(bool), GUIenabled);
            qApp = this;
        }
        public KUniqueApplication() : this((System.Type) null) {
            CreateProxy();
            interceptor.Invoke("KUniqueApplication", "KUniqueApplication()", typeof(void), false);
            qApp = this;
        }
    }
}
