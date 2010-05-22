namespace Kimono {

    using System;
    using System.Collections.Generic;
    using Qyoto;

    public class KFileItemList : List<KFileItem> {
        public KFileItemList() {}

        public KFileItemList(List<KFileItem> list) : base(list) {}

        public KFileItem FindByName(string fileName) {
            foreach (KFileItem item in this) {
                if (item.Name() == fileName)
                    return item;
            }
            return null;
        }

        public KFileItem FindByUrl(KUrl url) {
            foreach (KFileItem item in this) {
                if (item.Url() == url)
                    return item;
            }
            return null;
        }

        public KUrl.List TargetUrlList() {
            KUrl.List list = new KUrl.List();

            foreach (KFileItem item in this) {
                list.Add(item.TargetUrl());
            }

            return list;
        }

        public KUrl.List UrlList() {
            KUrl.List list = new KUrl.List();

            foreach (KFileItem item in this) {
                list.Add(item.Url());
            }

            return list;
        }

    }
}
