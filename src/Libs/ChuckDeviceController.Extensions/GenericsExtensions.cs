namespace ChuckDeviceController.Extensions
{
    using System.Diagnostics;

    public static class GenericsExtensions
    {
        /// <summary>
        /// Converts a string to its object representation.
        /// </summary>
        /// <typeparam name="T">The type of object to convert the string to.</typeparam>
        /// <param name="value">The actual string value.</param>
        /// <returns>Returns an object relating to the converted string.</returns>
        public static T StringToObject<T>(this string value)
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

        public static T LoadFromFile<T>(this string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                Console.WriteLine($"{filePath} file is empty.");
                return default;
            }

            return data.FromJson<T>();
        }
    }
}