namespace ChuckDeviceController.Plugin.Helpers.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<T> GetAttributes<T>(this Type type) where T : Attribute
        {
            var attributes = (IEnumerable<T>)type.GetCustomAttributes(true);
            return attributes;
        }

        public static T? GetAttribute<T>(this Type type) where T : Attribute
        {
            var attributes = type.GetAttributes<T>();
            var attribute = attributes.FirstOrDefault();
            return attribute;
        }
    }
}