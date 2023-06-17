
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;

namespace Kiota.Builder.CodeRenderers;
public class CurlCodeRenderer : CodeRenderer
{
    public CurlCodeRenderer(GenerationConfiguration configuration) : base(configuration, new CodeElementOrderComparerPython()) { }

    public override async Task RenderCodeNamespaceToFilePerClassAsync(LanguageWriter writer, CodeNamespace currentNamespace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(currentNamespace);
        if (cancellationToken.IsCancellationRequested) return;
        foreach (var codeElement in currentNamespace.GetChildElements(true))
        {
            switch (codeElement)
            {
                case CodeClass codeClass:
                    await RenderCodeClassToFilesPerMethodAsync(writer, codeClass, cancellationToken).ConfigureAwait(false);
                    break;
                case CodeNamespace codeNamespace:
                    //await RenderBarrel(writer, currentNamespace, codeNamespace, cancellationToken).ConfigureAwait(false);
                    await RenderCodeNamespaceToFilePerClassAsync(writer, codeNamespace, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }
    }

    private async Task RenderCodeClassToFilesPerMethodAsync(LanguageWriter writer, CodeClass codeClass, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeClass);
        if (cancellationToken.IsCancellationRequested) return;
        var currentNamespace = codeClass.GetImmediateParentOfType<CodeNamespace>();
        foreach (var codeElement in codeClass.GetChildElements(true))
        {
            // Only attempt to render RequestGenerator methods -- others would result in an empty file
            if (codeElement is CodeMethod currentMethod
                && currentMethod.IsOfKind(CodeMethodKind.RequestGenerator)
                && writer.PathSegmenter?.GetPath(currentNamespace, currentMethod) is string path)
            {
                await RenderCodeMethodToSingleFileAsync(writer, currentMethod, path, cancellationToken).ConfigureAwait(false);
            }
        }
    }
    private async Task RenderCodeMethodToSingleFileAsync(LanguageWriter writer, CodeElement codeElement, string outputFile, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentException.ThrowIfNullOrEmpty(outputFile);
#pragma warning disable CA2007
        await using var stream = new FileStream(outputFile, FileMode.Create);
#pragma warning restore CA2007

        var sw = new StreamWriter(stream);
        writer.SetTextWriter(sw);
        writer.Write(codeElement);
        if (cancellationToken.IsCancellationRequested) return;
        await sw.FlushAsync().ConfigureAwait(true); // stream writer doesn't not have a cancellation token overload https://github.com/dotnet/runtime/issues/64340
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}