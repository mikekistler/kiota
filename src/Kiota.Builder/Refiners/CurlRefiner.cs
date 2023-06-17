using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Refiners;
public class CurlRefiner : CommonLanguageRefiner, ILanguageRefiner
{
    public CurlRefiner(GenerationConfiguration configuration) : base(configuration) { }
    public override Task Refine(CodeNamespace generatedCode, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            RemoveModels(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceIndexersByMethodsWithParameter(generatedCode,
                false,
                static x => $"by_{x.ToSnakeCase()}",
                static x => x.ToSnakeCase());
            RemoveCancellationParameter(generatedCode);
            cancellationToken.ThrowIfCancellationRequested();
            ReplaceReservedNames(
                generatedCode,
                new CurlReservedNamesProvider(),
                static x => $"{x}_"
            );
            cancellationToken.ThrowIfCancellationRequested();
            ReplacePropertyNames(generatedCode,
                new() {
                    CodePropertyKind.Custom,
                },
                static s => s.ToCamelCase());
        }, cancellationToken);
    }

    // Remove the models namespace from the client
    private void RemoveModels(CodeNamespace generatedCode)
    {
        // The client is the first and only child of generatedCode
        if (generatedCode.GetChildElements(true).FirstOrDefault() is CodeNamespace client)
        {
            // The client has a child namespace for the models whose name ends with ".models"
            var modelsNamespace = client.GetChildElements(true).FirstOrDefault(x => x.Name.EndsWith(".models", StringComparison.OrdinalIgnoreCase));
            if (modelsNamespace != null)
            {
                client.RemoveChildElement(modelsNamespace);
            }
        }
    }
}
