namespace KIO {

    using System;
    using System.Collections.Generic;
    using Qyoto;
    using Kimono;

    public class MetaData : Dictionary<string, string> {
        public MetaData(Dictionary<string, string> metaData) : base(metaData) {}

        public MetaData(Dictionary<string, QVariant> variantMap) {
            foreach (KeyValuePair<string, QVariant> pair in variantMap) {
                Add(pair.Key, pair.Value.ToString());
            }
        }

        public QVariant ToVariant() {
            Dictionary<string, QVariant> map = new Dictionary<string, QVariant>();

            foreach (KeyValuePair<string, string> pair in this) {
                map.Add(pair.Key, (QVariant) pair.Value);
            }

            return QVariant.FromValue(map);
        }
    }

}
