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

    private static string[] putNames = new string[] { "put", "replace", "create", "createOrReplace" };
    private static string[] patchNames = new string[] { "patch", "update", "create", "createOrUpdate" };
    private static string[] postNames = new string[] { "post", "post", "create", "post" };

    public string GetPath(CodeNamespace currentNamespace, CodeElement currentElement, bool shouldNormalizePath = true)
    {
        ArgumentNullException.ThrowIfNull(currentNamespace);
        if (currentElement is CodeMethod currentMethod
           && currentMethod.HttpMethod != null)
        {
            var has200 = currentMethod.ResponseCodes.Any(x => x == "200");
            var has201 = currentMethod.ResponseCodes.Any(x => x == "201");
            var nameIndex = (has200 ? 1 : 0) + (has201 ? 2 : 0);
            // This CodeMethod is a RequestBuilder. We need to find the corresponding RequestExecutor
            // because that is where the PagingInformation is stored.
            var requestExecutor = currentMethod.Parent?.GetChildElements().OfType<CodeMethod>()
                .Where(static e => e.IsOfKind(CodeMethodKind.RequestExecutor)).First();
            var pageable = requestExecutor?.PagingInformation != null;
            var method = currentMethod.HttpMethod switch
            {
                HttpMethod.Put => putNames[nameIndex],
                HttpMethod.Patch => patchNames[nameIndex],
                HttpMethod.Post => postNames[nameIndex],
                HttpMethod.Get => pageable ? "list" : "get",
                _ => currentMethod.HttpMethod.ToString()!.ToLowerInvariant(),
            };
            var segments = currentNamespace.Name.Split('.') ?? Array.Empty<string>();
            var filename = string.Join(string.Empty, segments.Skip(1).Select(static x => x.ToFirstCharacterUpperCase().Trim()));
            return Path.Combine(RootPath, method + filename + FileSuffix);
        }
        return "junk"; // Ignore this element
    }
}
