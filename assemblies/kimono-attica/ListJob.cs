namespace Attica {

    using System;
    using System.Collections.Generic;
    using Qyoto;

    public abstract class ListJob<T> : GetJob {

        protected ListJob(Type dummy) : base((Type) null) {}

        /// TODO: implement me!
        public List<T> ItemList() {
            return null;
        }

        /// TODO: implement me!
        protected override void Parse(string xml) {
        }

        protected override abstract QNetworkReply ExecuteRequest();
    }
}
