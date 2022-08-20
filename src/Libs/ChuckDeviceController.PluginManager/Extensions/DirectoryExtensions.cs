namespace ChuckDeviceController.PluginManager.Extensions
{
    public static class DirectoryExtensions
    {
        public static IEnumerable<string> GetFiles(this string path, IEnumerable<string> searchPattern, EnumerationOptions? enumerationOptions = null)
        {
            var options = new EnumerationOptions
            {
                MaxRecursionDepth = 2,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false,
            };
            var assemblies = Directory.GetFiles(path, "*.*", enumerationOptions ?? options)
                                      .Where(file => searchPattern.Contains(Path.GetExtension(file)));
            return assemblies;
        }

        public static string? GetDirectoryName(this string filePath)
        {
            return Path.GetDirectoryName(filePath);
        }
    }
}