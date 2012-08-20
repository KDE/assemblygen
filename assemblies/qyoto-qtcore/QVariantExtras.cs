namespace Qyoto {

    using System;
    using System.Collections;
    using System.Collections.Generic; 
    using System.Text;
    using System.Runtime.InteropServices;

    public partial class QVariant : Object, IDisposable {

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr QVariantValue(string typeName, IntPtr variant);

        [DllImport("qyoto-qtcore-native", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr QVariantFromValue(int type, IntPtr value);

        static QVariant() {
            QMetaType.RegisterType<object>();
        }

        public T Value<T>() {
            return (T) Value();
        }

        public object Value() {
            Type valueType = type();
            if (valueType == Type.Invalid) {
                return null;
            } else if (valueType == Type.Bool) {
                return ToBool();
            } else if (valueType == Type.Double) {
                return ToDouble();
            } else if (valueType == Type.BitArray) {
                return ToBitArray();
            } else if (valueType == Type.ByteArray) {
                return ToByteArray();
            } else if (valueType == Type.Char) {
                return ToChar();
            } else if (valueType == Type.Date) {
                return ToDate();
            } else if (valueType == Type.DateTime) {
                return ToDateTime();
            } else if (valueType == Type.Hash) {
                return ToHash();
            } else if (valueType == Type.Int) {
                return ToInt();
            } else if (valueType == Type.Line) {
                return ToLine();
            } else if (valueType == Type.LineF) {
                return ToLineF();
            } else if (valueType == Type.Locale) {
                return ToLocale();
            } else if (valueType == Type.LongLong) {
                return ToLongLong();
            } else if (valueType == Type.Point) {
                return ToPoint();
            } else if (valueType == Type.PointF) {
                return ToPointF();
            } else if (valueType == Type.Rect) {
                return ToRect();
            } else if (valueType == Type.RectF) {
                return ToRectF();
            } else if (valueType == Type.RegExp) {
                return ToRegExp();
            } else if (valueType == Type.Size) {
                return ToSize();
            } else if (valueType == Type.SizeF) {
                return ToSizeF();
            } else if (valueType == Type.String) {
                return ToString();
            } else if (valueType == Type.StringList) {
                return ToStringList();
            } else if (valueType == Type.List) {
                return ToList();
            } else if (valueType == Type.Map) {
                return ToMap();
            } else if (valueType == Type.Time) {
                return ToTime();
            } else if (valueType == Type.UInt) {
                return ToUInt();
            } else if (valueType == Type.ULongLong) {
                return ToULongLong();
            } else if (valueType == Type.Url) {
                return ToUrl();
            } else {
                string typeName = TypeName();
                IntPtr instancePtr = QVariantValue(typeName, (IntPtr) GCHandle.Alloc(this));
                return ((GCHandle) instancePtr).Target;
            }
        }

        public object Value(System.Type valueType) {
            if (valueType == typeof(bool)) {
                return ToBool();
            } else if (valueType == typeof(double)) {
                return ToDouble();
            } else if (valueType == typeof(QBitArray)) {
                return ToBitArray();
            } else if (valueType == typeof(QByteArray)) {
                return ToByteArray();
            } else if (valueType == typeof(char)) {
                return ToChar();
            } else if (valueType == typeof(QDate)) {
                return ToDate();
            } else if (valueType == typeof(QDateTime)) {
                return ToDateTime();
            } else if (valueType == typeof(int)) {
                return ToInt();
            } else if (valueType == typeof(QLine)) {
                return ToLine();
            } else if (valueType == typeof(QLineF)) {
                return ToLineF();
            } else if (valueType == typeof(QLocale)) {
                return ToLocale();
            } else if (valueType == typeof(QPoint)) {
                return ToPoint();
            } else if (valueType == typeof(QPointF)) {
                return ToPointF();
            } else if (valueType == typeof(QRect)) {
                return ToRect();
            } else if (valueType == typeof(QRectF)) {
                return ToRectF();
            } else if (valueType == typeof(QRegExp)) {
                return ToRegExp();
            } else if (valueType == typeof(QSize)) {
                return ToSize();
            } else if (valueType == typeof(QSizeF)) {
                return ToSizeF();
            } else if (valueType == typeof(string)) {
                return ToString();
            } else if (valueType == typeof(List<string>)) {
                return ToStringList();
            } else if (valueType == typeof(List<QVariant>)) {
                return ToList();
            } else if (valueType == typeof(Dictionary<string, QVariant>)) {
                object o = ToMap();
                if (o == null)
                    o = ToHash();
                return o;
            } else if (valueType == typeof(QTime)) {
                return ToTime();
            } else if (valueType == typeof(uint)) {
                return ToUInt();
            } else if (valueType == typeof(QUrl)) {
                return ToUrl();
            } else if (valueType == typeof(QVariant)) {
                return this;
            } else if (valueType.IsEnum) {
                return Enum.ToObject(valueType, ToLongLong());
            } else {
                string typeName;
                if (SmokeMarshallers.IsSmokeClass(valueType))
                    typeName = SmokeMarshallers.SmokeClassName(valueType);
                else
                    typeName = valueType.ToString();
                Type type = NameToType(typeName);
                if (type > Type.LastCoreType) {
                    IntPtr instancePtr = QVariantValue(typeName, (IntPtr) GCHandle.Alloc(this));
                    return ((GCHandle) instancePtr).Target;
                } else if (type == Type.Invalid) {
                    Console.WriteLine("QVariant.Value(): invalid type: {0}", valueType);
                }

                return null;
            }
        }

        static public QVariant FromValue<T>(T value) {
            return FromValue(value, typeof(T));
        }

        static public QVariant FromValue(object value, System.Type valueType) {
            if (valueType == typeof(bool)) {
                return new QVariant((bool) value);
            } else if (valueType == typeof(double)) {
                return new QVariant((double) value);
            } else if (valueType == typeof(QBitArray)) {
                return new QVariant((QBitArray) value);
            } else if (valueType == typeof(QByteArray)) {
                return new QVariant((QByteArray) value);
            } else if (valueType == typeof(char)) {
                return new QVariant(new QChar((char) value));
            } else if (valueType == typeof(QDate)) {
                return new QVariant((QDate) value);
            } else if (valueType == typeof(QDateTime)) {
                return new QVariant((QDateTime) value);
            } else if (valueType == typeof(int)) {
                return new QVariant((int) value);
            } else if (valueType == typeof(QLine)) {
                return new QVariant((QLine) value);
            } else if (valueType == typeof(QLineF)) {
                return new QVariant((QLineF) value);
            } else if (valueType == typeof(QLocale)) {
                return new QVariant((QLocale) value);
            } else if (valueType == typeof(QPoint)) {
                return new QVariant((QPoint) value);
            } else if (valueType == typeof(QPointF)) {
                return new QVariant((QPointF) value);
            } else if (valueType == typeof(QRect)) {
                return new QVariant((QRect) value);
            } else if (valueType == typeof(QRectF)) {
                return new QVariant((QRectF) value);
            } else if (valueType == typeof(QRegExp)) {
                return new QVariant((QRegExp) value);
            } else if (valueType == typeof(QSize)) {
                return new QVariant((QSize) value);
            } else if (valueType == typeof(QSizeF)) {
                return new QVariant((QSizeF) value);
            } else if (valueType == typeof(string)) {
                return new QVariant((string) value);
            } else if (valueType == typeof(List<string>)) {
                return new QVariant((List<string>) value);
            } else if (valueType == typeof(List<QVariant>)) {
                return new QVariant((List<QVariant>) value);
            } else if (valueType == typeof(Dictionary<string, QVariant>)) {
                return new QVariant((Dictionary<string, QVariant>) value);
            } else if (valueType == typeof(QTime)) {
                return new QVariant((QTime) value);
            } else if (valueType == typeof(uint)) {
                return new QVariant((uint) value);
            } else if (valueType == typeof(QUrl)) {
                return new QVariant((QUrl) value);
            } else if (valueType == typeof(QVariant)) {
                return new QVariant((QVariant) value);
            } else if (valueType.IsEnum) {
                return new QVariant((int) value);
            } else {
                string typeName;
                if (SmokeMarshallers.IsSmokeClass(valueType))
                    typeName = SmokeMarshallers.SmokeClassName(valueType);
                else
                    typeName = valueType.ToString();
                Type type = NameToType(typeName);
                if (type == Type.Invalid) {
                    return FromValue<object>(value);
                } else if (type > Type.LastCoreType) {
                    IntPtr valueHandle = IntPtr.Zero;
                    if (value != null) {
                        valueHandle = (IntPtr) GCHandle.Alloc(value);
                    }
                    GCHandle handle = (GCHandle) QVariantFromValue(QMetaType.type(typeName), valueHandle);
                    QVariant v = (QVariant) handle.Target;
                    handle.Free();
                    return v;
                }

                return new QVariant();
            }
        }

        public static implicit operator QVariant(int arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(uint arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(long arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(ulong arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(bool arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(double arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(string arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(QBitArray arg) {
            return new QVariant(arg);
        }
        public static implicit operator QVariant(QByteArray arg) {
            return new QVariant(arg);
        }
        public static implicit operator int(QVariant arg) {
            return arg.ToInt();
        }
        public static implicit operator uint(QVariant arg) {
            return arg.ToUInt();
        }
        public static implicit operator long(QVariant arg) {
            return arg.ToLongLong();
        }
        public static implicit operator ulong(QVariant arg) {
            return arg.ToULongLong();
        }
        public static implicit operator bool(QVariant arg) {
            return arg.ToBool();
        }
        public static implicit operator double(QVariant arg) {
            return arg.ToDouble();
        }
        public static implicit operator string(QVariant arg) {
            return arg.ToString();
        }
        public static implicit operator QBitArray(QVariant arg) {
            return arg.ToBitArray();
        }
        public static implicit operator QByteArray(QVariant arg) {
            return arg.ToByteArray();
        }
    }
}

// kate: space-indent on; indent-width 4; replace-tabs on; mixed-indent off;
