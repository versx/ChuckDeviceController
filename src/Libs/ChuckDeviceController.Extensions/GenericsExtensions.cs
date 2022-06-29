namespace ChuckDeviceController.Extensions
{
    public static class GenericsExtensions
    {
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