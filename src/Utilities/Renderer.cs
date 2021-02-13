namespace ChuckDeviceController.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HandlebarsDotNet;

    public static class Renderer
    {
        public static string ViewsDirectory => Path.Combine
        (
            Directory.GetCurrentDirectory(),
            "Views"
        );

        private static readonly Dictionary<string, string> _templates;

        static Renderer()
        {
            _templates = new Dictionary<string, string>();

            RegisterAllTemplates();
        }

        public static string ParseTemplate(string name, dynamic model)
        {
            if (!_templates.ContainsKey(name))
            {
                Console.WriteLine($"Template is not registered {name}");
                return null;
            }
            return Parse(_templates[name], model);
        }

        public static string Parse(string text, dynamic model)
        {
            var template = Handlebars.Compile(text);
            return template(model);
        }

        public static string ParseFile(string path, dynamic model)
        {
            var templateData = ReadData(path); // REVIEW: Replace with File.ReadAllText?
            return Parse(templateData, model);
        }

        private static string ReadData(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Template does not exist at path: {path}", path);
            }
            using (var sr = new StreamReader(path))
            {
                return sr.ReadToEnd();
            }
        }

        private static void RegisterAllTemplates()
        {
            foreach (var file in Directory.GetFiles(ViewsDirectory, "*.mustache"))
            {
                var viewName = Path.GetFileNameWithoutExtension(file);
                var viewData = File.ReadAllText(file);
                Handlebars.RegisterTemplate(viewName, viewData);
                if (!_templates.ContainsKey(viewName))
                {
                    _templates.Add(viewName, viewData);
                }
            }
        }

        public static string GetView(string name)
        {
            var viewPath = Path.Combine(ViewsDirectory, name + ".mustache");
            if (!File.Exists(viewPath))
            {
                Console.WriteLine($"View does not exist at {viewPath}");
                return null;
            }
            //return await File.ReadAllTextAsync(viewPath);
            return File.ReadAllText(viewPath);
        }
    }
}