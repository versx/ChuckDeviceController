namespace ChuckDeviceController.Common
{
    using System.Reflection;

    public class GitHub
    {
        /// <summary>
        /// Gets the git hash value from the assembly or '--' if it cannot be found.
        /// </summary>
        /// <param name="assembly">Assembly to retrieve the SHA hash.</param>
        /// <param name="defaultValue">Default value if SHA hash not found.</param>
        /// <returns>Returns the SHA hash of the current git commit.</returns>
        /// <credits>https://stackoverflow.com/a/45248069</credits>
        public static string GetGitHash(Assembly assembly, string defaultValue = "--")
        {
            //var assembly = Assembly.GetExecutingAssembly();
            var attr = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var split = attr?.InformationalVersion?.Split('+');
            if (split?.Length == 2)
            {
                return split.Last();
            }
            return defaultValue;
        }
    }
}