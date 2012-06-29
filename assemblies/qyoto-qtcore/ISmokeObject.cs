using System;

namespace Qyoto {
    public interface ISmokeObject {
        IntPtr SmokeObject { get; set; }
        void CreateProxy();
    }
}
