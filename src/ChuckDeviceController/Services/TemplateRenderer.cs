namespace ChuckDeviceController.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using HandlebarsDotNet;

    using Chuck.Infrastructure.Extensions;

    public static class TemplateRenderer
    {
        public static string ViewsDirectory => Path.Combine
        (
            Directory.GetCurrentDirectory(),
            Strings.ViewsFolder
        );

        private static readonly Dictionary<string, string> _templates;

        static TemplateRenderer()
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
    }
}