namespace ChuckDeviceController.Extensions
{
    using System.Diagnostics;
    using System.Security.Cryptography;

    public static class GenericsExtensions
    {
        /// <summary>
        /// Converts a string to its object representation.
        /// </summary>
        /// <typeparam name="T">The type of object to convert the string to.</typeparam>
        /// <param name="value">The actual string value.</param>
        /// <returns>Returns an object relating to the converted string.</returns>
        public static T? StringToObject<T>(this string value)
        {
            //TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
            //return (T)tc.ConvertFromString(value);
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"StringToObject: {ex}");
                return default;
            }
        }

        /// <summary>
        /// Converts a string to its object representation.
        /// </summary>
        /// <param name="value">The actual string value.</param>
        /// <param name="obj">The object type to convert the string to.</param>
        /// <returns>Returns an object relating to the converted string.</returns>
        public static object StringToObject(this string value, object obj)
        {
            return Enum.Parse(obj.GetType(), value, true);
        }

        /// <summary>
        /// Converts an object to its string representation.
        /// </summary>
        /// <typeparam name="T">The type of object to convert.</typeparam>
        /// <param name="value">The actual object value.</param>
        /// <returns>Returns the string representation of the converted object.</returns>
        public static string ObjectToString<T>(this T value)
        {
            //TypeConverter tc = TypeDescriptor.GetConverter(typeof(T));
            //return tc.ConvertToString(value);
            try
            {
                return Enum.GetName(typeof(T), value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ObjectToString: {ex}");
                return value.ToString();
            }
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            var generator = RandomNumberGenerator.Create();
            var count = list.Count;
            while (count > 1)
            {
                var random = generator.Next(0, count);
                var remainder = random % count;
                count--;

                (list[count], list[remainder]) = (list[remainder], list[count]);
            }
        }

        /// <summary>
        /// Compare two list objects for equality, ignoring order of lists if specified
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list1"></param>
        /// <param name="list2"></param>
        /// <param name="ignoreOrder"></param>
        /// <returns></returns>
        public static bool IsEqual<T>(this List<T> list1, List<T> list2, bool ignoreOrder = true)
        {
            var listA = ignoreOrder
                ? list1
                : new List<T>(list1).OrderBy(x => x)
                                    .ToList();
            var listB = ignoreOrder
                ? list2
                :  new List<T>(list2).OrderBy(x => x)
                                     .ToList();
            var isEqual = Enumerable.SequenceEqual(listA, listB);
            return isEqual;
        }

        /// <summary>
        /// Gets a public property value of the specified object.
        /// </summary>
        /// <typeparam name="T">Reference type to search property for.</typeparam>
        /// <param name="obj">Source object</param>
        /// <param name="propertyName">Name of property</param>
        /// <returns>Returns the value of the specified property of the object.</returns>
        public static object? GetPropertyValue<T>(this T obj, string propertyName)
        {
            if (obj == null)
            {
                return null;
            }
            if (string.IsNullOrEmpty(propertyName))
            {
                return obj;
            }
            var value = obj.GetType()!.GetProperty(propertyName)!.GetValue(obj);
            return value;
        }
    }
}