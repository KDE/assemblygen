namespace Attica {

    using System;
    using System.Collections.Generic;
    using Qyoto;

    public abstract class ItemJob<T> : GetJob {

        protected ItemJob(Type dummy) : base((Type) null) {}

        /// TODO: implement me!
        public T Result() {
            return default(T);
        }

        protected override abstract QNetworkReply ExecuteRequest();
        protected override abstract void Parse(string xml);
    }

    internal class ItemJobInternal<T> : ItemJob<T> {

        protected ItemJobInternal(Type dummy) : base((Type) null) {}

        /// TODO: implement me!
        protected override QNetworkReply ExecuteRequest() {
            return null;
        }

        /// TODO: implement me!
        protected override void Parse(string xml) {
        }
    }

    public abstract class ItemPostJob<T> : PostJob {

        protected ItemPostJob(Type dummy) : base((Type) null) {}

        /// TODO: implement me!
        public T Result() {
            return default(T);
        }

        protected override abstract QNetworkReply ExecuteRequest();
        protected override abstract void Parse(string xml);
    }

    internal class ItemPostJobInternal<T> : ItemPostJob<T> {

        protected ItemPostJobInternal(Type dummy) : base((Type) null) {}

        /// TODO: implement me!
        protected override QNetworkReply ExecuteRequest() {
            return null;
        }

        /// TODO: implement me!
        protected override void Parse(string xml) {
        }
    }
}
