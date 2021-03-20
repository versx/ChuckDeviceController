namespace Chuck.Net.Extensions
{
    using System;
    using System.Text;
    using System.Text.Json;

    using Microsoft.AspNetCore.Http;

    public static class HttpContextSessionExtensions
    {
        public static string TryGetValue(this ISession session, string key)
        {
            if (!session.TryGetValue(key, out var value))
                return null;

            if (value == null)
                return null;

            return Encoding.UTF8.GetString(value);
        }

        public static T GetValue<T>(this ISession session, string key)
        {
            var json = TryGetValue(session, key);
            if (string.IsNullOrEmpty(json))
                return default;
            var obj = JsonSerializer.Deserialize<T>(json);
            return obj;
        }

        public static void SetValue<T>(this ISession session, string key, T value)
        {
            var json = JsonSerializer.Serialize(value);
            session.SetString(key, json);
        }
    }
}