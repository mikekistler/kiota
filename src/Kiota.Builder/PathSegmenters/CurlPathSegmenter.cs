using System;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;
public class CurlPathSegmenter : IPathSegmenter
{
    public CurlPathSegmenter(string rootPath, string clientNamespaceName)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);
        RootPath = rootPath.Contains(Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? rootPath : rootPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private string RootPath;

    public string FileSuffix => ".sh";

    public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true)
    {
        ArgumentNullException.ThrowIfNull(currentNamespace);
        if (currentElement is CodeMethod currentMethod
           && currentMethod.HttpMethod != null)
        {
            var method = currentMethod.HttpMethod.ToString()!.ToLowerInvariant();
            var segments = currentNamespace.Name.Split('.') ?? Array.Empty<string>();
            var filename = string.Join(string.Empty, segments.Skip(1).Select(static x => x.ToFirstCharacterUpperCase().Trim()));
            return Path.Combine(RootPath, method + filename + FileSuffix);
        }
        return "junk"; // Ignore this element
    }
}
