namespace ChuckDeviceController.Extensions;

using System.Reflection;

public static class EnumExtensions
{
    /// <summary>
    ///     A generic extension method that aids in reflecting 
    ///     and retrieving any attribute that is applied to an `Enum`.
    /// </summary>
    public static TAttribute? GetAttribute<TAttribute>(this Enum enumValue)
        where TAttribute : Attribute
    {
        return enumValue
            .GetType()
            .GetMember(enumValue.ToString())
            .First()
            .GetCustomAttribute<TAttribute>();
    }

    /// <summary>
    /// Includes an enumerated type and returns the new value
    /// </summary>
    public static TEnum SetFlag<TEnum>(this TEnum flags, TEnum flag)
        where TEnum : unmanaged
    {
        object value = flags;
        var parsed = new Value<TEnum>(flag);
        if (parsed.Signed != null)
        {
            value = Convert.ToInt64(flags) | (long)parsed.Signed;
        }

        var result = Enum.Parse<TEnum>(value.ToString() ?? "");
        return result;
    }

    /// <summary>
    /// Removes an enumerated type and returns the new value
    /// </summary>
    public static TEnum UnsetFlag<TEnum>(this TEnum flags, TEnum flag)
        where TEnum : unmanaged
    {
        object value = flags;
        var parsed = new Value<TEnum>(flag);
        if (parsed.Signed != null)
        {
            value = Convert.ToInt64(flags) & ~(long)parsed.Signed;
        }

        var result = Enum.Parse<TEnum>(value.ToString() ?? "");
        return result;
    }

    /// <summary>
    /// Checks if an enumerated type contains a value
    /// </summary>
    public static bool HasFlag<TEnum>(this TEnum flags, TEnum flag)
        where TEnum : unmanaged
    {
        var parsed = new Value<TEnum>(flag);
        var result = parsed.Signed != null &&
            (Convert.ToInt64(flags) & (long)parsed.Signed) == (long)parsed.Signed;
        return result;
    }

    /// <summary>
    /// Toggles an enumerated type and returns the new value
    /// </summary>
    public static TEnum ToggleFlag<TEnum>(this TEnum flags, TEnum flag)
        where TEnum : unmanaged
    {
        object value = flags;
        var parsed = new Value<TEnum>(flag);
        if (parsed.Signed != null)
        {
            value = Convert.ToInt64(flags) ^ (long)parsed.Signed;
        }

        var result = Enum.Parse<TEnum>(value.ToString() ?? "");
        return result;
    }

    /// <summary>
    /// Internal class to simplfy narrowing values between a 
    /// ulong and long since either value should cover any 
    /// lesser value.
    /// </summary>
    private class Value<T>
    {
        public readonly long? Signed;

        // Cached comparisons for tye to use
        private static readonly Type _uInt64 = typeof(ulong);
        private static readonly Type _uInt32 = typeof(long);

        public Value(object value)
            : this(value, typeof(T))
        {
        }

        public Value(object value, Type type)
        {
            // Make sure it is even an enum to work with
            if (!type.IsEnum)
            {
                throw new ArgumentException("Value provided is not an enumerated type!");
            }

            // Then check for the enumerated value
            var compare = Enum.GetUnderlyingType(type);

            // If this is an unsigned long then the only
            // value that can hold it would be a ulong
            if (compare == _uInt32 || compare == _uInt64)
            {
                Unsigned = Convert.ToUInt64(value);
            }
            else
            {
                // Otherwise, a long should cover anything else
                Signed = Convert.ToInt64(value);
            }
        }

        public ulong? Unsigned { get; set; }
    }
}