namespace Qyoto {

    using System;
    
    public class QDBusSignature {
        
        public QDBusSignature() {}
        public QDBusSignature(string signature) {
            Signature = signature;
        }
        
        public string Signature {
            get;
            set;
        }
        
        public static implicit operator QDBusSignature(string signature) {
            return new QDBusSignature(signature);
        }
        
        public static implicit operator string(QDBusSignature signature) {
            return signature.Signature;
        }
    }

}
