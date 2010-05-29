namespace Qyoto {

    using System;

    [SmokeClass("QDBusObjectPath")]
    public class QDBusObjectPath {
        public QDBusObjectPath() {}
        public QDBusObjectPath(string path) {
            Path = path;
        }

        public string Path {
            get;
            set;
        }

        public static implicit operator QDBusObjectPath(string path) {
            return new QDBusObjectPath(path);
        }

        public static implicit operator string(QDBusObjectPath path) {
            return path.Path;
        }

        public override string ToString() {
            return Path;
        }
    }

}

// kate: space-indent on;
