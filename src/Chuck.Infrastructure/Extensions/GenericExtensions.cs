namespace Chuck.Infrastructure.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public static class GenericExtensions
    {
        public static T PopFirst<T>(this List<T> list, out List<T> newList)
        {
            var item = list.FirstOrDefault();
            list.Remove(item);
            newList = list;
            return item;
        }

        public static T PopLast<T>(this List<T> list, out List<T> newList)
        {
            var item = list.LastOrDefault();
            list.Remove(item);
            newList = list;
            return item;
        }

        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start index, exclusive for end index.
        /// </summary>
        public static List<T> Slice<T>(this List<T> source, int start, int end)
        {
            // Handles negative ends.
            if (end < 0)
                end = source.Count + end;

            var len = end - start;
            var list = new List<T>(len);
            for (var i = 0; i < len; i++)
            {
                list[i] = source[i + start];
            }
            return list;
        }

        public static T LoadFile<T>(this string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"{filePath} file not found.", filePath);
            }

            var data = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(data))
            {
                Console.Error.WriteLine($"{filePath} file is empty.");
                return default;
            }

            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
            return JsonSerializer.Deserialize<T>(data, options);
        }
    }
}