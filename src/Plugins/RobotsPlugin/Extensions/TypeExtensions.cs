namespace RobotsPlugin.Extensions
{
    public static class TypeExtensions
    {
        public static IEnumerable<T> GetRobotAttributes<T>(this Type type) where T : Attribute
        {
            var attributes = (IEnumerable<T>)type.GetCustomAttributes(true);
            return attributes;
        }

        public static T? GetRobotAttribute<T>(this Type type) where T : Attribute
        {
            var attributes = type.GetRobotAttributes<T>();
            var attribute = attributes.FirstOrDefault();
            return attribute;
        }
    }
}