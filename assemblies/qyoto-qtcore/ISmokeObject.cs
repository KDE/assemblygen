using System;

namespace QtCore {
    public interface ISmokeObject {
        IntPtr SmokeObject { get; set; }
        void CreateProxy();
    }
}
