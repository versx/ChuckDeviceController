namespace ChuckDeviceController.PluginManager.Mvc.Razor
{
    using System.Collections.Concurrent;

    using Microsoft.AspNetCore.Mvc.Razor.Compilation;
    using Microsoft.Extensions.Primitives;

    public class PluginViewCompiler : IViewCompiler
    {
        #region Variables

        private readonly Dictionary<string, Task<CompiledViewDescriptor>> _compiledViews;
        private readonly ConcurrentDictionary<string, string> _normalizedPathCache;

        #endregion

        public PluginViewCompiler(IList<CompiledViewDescriptor> compiledViews)
        {
            if (!(compiledViews?.Any() ?? false))
            {
                throw new ArgumentNullException(nameof(compiledViews));
            }

            _normalizedPathCache = new(StringComparer.Ordinal);
            _compiledViews = new(compiledViews.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var compiledView in compiledViews)
            {
                if (_compiledViews.ContainsKey(compiledView.RelativePath))
                    continue;

                _compiledViews.Add(compiledView.RelativePath, Task.FromResult(compiledView));
            }

            //if (_compiledViews.Count == 0)
            //{
            //    // TODO: Throw error/inform user
            //}
        }

        public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            if (_compiledViews.TryGetValue(relativePath, out var cachedResult))
            {
                return cachedResult;
            }

            var normalizedPath = GetNormalizedPath(relativePath);
            if (_compiledViews.TryGetValue(normalizedPath, out cachedResult))
            {

                return cachedResult;
            }

            return Task.FromResult(new CompiledViewDescriptor
            {
                RelativePath = normalizedPath,
                ExpirationTokens = Array.Empty<IChangeToken>(),
            });
        }

        public static string NormalizePath(string path)
        {
            var addLeadingSlash = path[0] != '\\' && path[0] != '/';
            var transformSlashes = path.IndexOf('\\') != -1;

            if (!addLeadingSlash && !transformSlashes)
            {
                return path;
            }

            var length = path.Length;
            if (addLeadingSlash)
            {
                length++;
            }

            return string.Create(length, (path, addLeadingSlash), (span, tuple) =>
            {
                var (pathValue, addLeadingSlashValue) = tuple;
                var spanIndex = 0;

                if (addLeadingSlashValue)
                {
                    span[spanIndex++] = '/';
                }

                foreach (var ch in pathValue)
                {
                    span[spanIndex++] = ch == '\\' ? '/' : ch;
                }
            });
        }

        private string GetNormalizedPath(string relativePath)
        {
            if (relativePath.Length == 0)
            {
                return relativePath;
            }

            if (!_normalizedPathCache.TryGetValue(relativePath, out var normalizedPath))
            {
                normalizedPath = NormalizePath(relativePath);
                _normalizedPathCache[relativePath] = normalizedPath;
            }

            return normalizedPath;
        }
    }
}