namespace ChuckDeviceController.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HandlebarsDotNet;

    using ChuckDeviceController.Extensions;

    public static class Renderer
    {
        public static string ViewsDirectory => Path.Combine
        (
            Directory.GetCurrentDirectory(),
            Strings.ViewsFolder
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
                ConsoleExt.WriteWarn($"Template is not registered {name}");
                return null;
            }
            return Parse(_templates[name], model);
        }

        public static string Parse(string text, dynamic model)
        {
            var template = Handlebars.Compile(text);
            return template(model);
        }

        private static void RegisterAllTemplates()
        {
            foreach (var file in Directory.GetFiles(ViewsDirectory, "*" + Strings.TemplateExt))
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
            var viewPath = Path.Combine(ViewsDirectory, name + Strings.TemplateExt);
            if (!File.Exists(viewPath))
            {
                ConsoleExt.WriteError($"View does not exist at {viewPath}");
                return null;
            }
            //return await File.ReadAllTextAsync(viewPath);
            return File.ReadAllText(viewPath);
        }
    }
}