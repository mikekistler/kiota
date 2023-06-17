using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Curl;
class CurlWriter : LanguageWriter
{
    public CurlWriter(string rootPath, string clientNamespaceName, bool usesBackingStore = false)
    {
        PathSegmenter = new CurlPathSegmenter(rootPath, clientNamespaceName);
        var conventionService = new CurlConventionService();
        AddOrReplaceCodeElementWriter(new CodeMethodWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodePropertyWriter(conventionService));
        AddOrReplaceCodeElementWriter(new CodeEnumWriter(conventionService));
    }
}
