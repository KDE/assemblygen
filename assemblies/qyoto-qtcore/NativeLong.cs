using System;

public struct NativeLong : IComparable, IComparable<long>, IEquatable<long>, IFormattable
{
    long value;

    public NativeLong(long value)
    {
        this.value = value;
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public override bool Equals(object other)
    {
        return value.Equals(other);
    }

    public bool Equals(long other)
    {
        return value.Equals(other);
    }

    public override string ToString()
    {
        return value.ToString();
    }

    public int CompareTo(long other)
    {
        return value.CompareTo(other);
    }

    public int CompareTo(object other)
    {
        return value.CompareTo(other);
    }

    public string ToString(string format, IFormatProvider provider)
    {
        return value.ToString(format, provider);
    }

    public long Value
    {
        get {
            return value;
        }
        set {
            this.value = value;
        }
    }

    public static implicit operator long(NativeLong nativeLong)
    {
        return nativeLong.Value;
    }

    public static implicit operator NativeLong(long value)
    {
        return new NativeLong(value);
    }
}

public struct NativeULong : IComparable, IComparable<ulong>, IEquatable<ulong>, IFormattable
{
    ulong value;

    public NativeULong(ulong value)
    {
        this.value = value;
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public override bool Equals(object other)
    {
        return value.Equals(other);
    }

    public bool Equals(ulong other)
    {
        return value.Equals(other);
    }

    public override string ToString()
    {
        return value.ToString();
    }

    public int CompareTo(ulong other)
    {
        return value.CompareTo(other);
    }

    public int CompareTo(object other)
    {
        return value.CompareTo(other);
    }

    public string ToString(string format, IFormatProvider provider)
    {
        return value.ToString(format, provider);
    }

    public ulong Value
    {
        get {
            return value;
        }
        set {
            this.value = value;
        }
    }

    public static implicit operator ulong(NativeULong nativeLong)
    {
        return nativeLong.Value;
    }

    public static implicit operator NativeULong(ulong value)
    {
        return new NativeULong(value);
    }
}
