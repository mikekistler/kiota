using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Curl;
public class CodeMethodWriter : BaseElementWriter<CodeMethod, CurlConventionService>
{
    public CodeMethodWriter(CurlConventionService conventionService) : base(conventionService) { }

    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (!codeElement.IsOfKind(CodeMethodKind.RequestGenerator)) return;
        if (codeElement.HttpMethod == null) throw new InvalidOperationException($"{nameof(codeElement.HttpMethod)} should not be null");

        writer.WriteLine("#!/bin/bash\n");
        writer.WriteLine("source ./init.sh\n");

        // Create output file name based on generated file name
        var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        var shFilename = Path.GetFileName(writer.PathSegmenter!.GetPath(currentNamespace, codeElement, false));
        var outFilename = shFilename.Replace(".sh", ".out", StringComparison.OrdinalIgnoreCase);

        // Set the baseUrl
        var baseUrl = "${endpoint}";
        if (!string.IsNullOrEmpty(codeElement.BaseUrl))
        {
            baseUrl += $"/{codeElement.BaseUrl}";
        }
        writer.WriteLine($"baseUrl={baseUrl}\n");

        // Set SerializationName for each parameter
        foreach (var parameter in codeElement.PathQueryAndHeaderParameters)
        {
            if (string.IsNullOrEmpty(parameter.SerializationName))
                parameter.SerializationName = parameter.Name;
        }

        // Create shell variables for each required parameter
        // Path parameters are always required, so we'll handle them first
        var pathParameters = codeElement.PathQueryAndHeaderParameters
                .Where(static p => p.IsOfKind(CodeParameterKind.Path))
                .ToList()
                ?? new List<CodeParameter>();
        for (int i = 0; i < pathParameters.Count; i++)
        {
            var parameterName = pathParameters[i].Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{parameterName}=${i + 1}");
        }
        var requiredQueryParameters = codeElement.PathQueryAndHeaderParameters
                .Where(static p => p.IsOfKind(CodeParameterKind.QueryParameter) && !p.Optional)
                ?? new List<CodeParameter>();
        foreach (var parameter in requiredQueryParameters)
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{parameterName}=\"{parameterName}\"");
        }
        if (pathParameters.Any() || requiredQueryParameters.Any())
            writer.WriteLine();

        var urlTemplate = "";
        if (codeElement.Parent is CodeClass parentClass)
        {
            if (parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate) is CodeProperty urlTemplateProperty)
            {
                var segments = urlTemplateProperty.DefaultValue.Split("{?")[0].Split("/").Skip(1).ToList();
                // Replace segments that are path parameters with the shell variable
                var pathParameterNames = pathParameters.Select(p => p.Name.ToFirstCharacterLowerCase());
                foreach (var name in pathParameterNames)
                {
                    // Find the first segment that starts with "{"
                    var index = segments.FindIndex(s => s.StartsWith("{", StringComparison.OrdinalIgnoreCase));
                    if (index >= 0)
                    {
                        segments[index] = $"${{{name}}}";
                    }
                }
                urlTemplate = string.Join("/", segments);
            }
        }
        else throw new InvalidOperationException($"{nameof(codeElement.Parent)} should be a {nameof(CodeClass)}");

        // Create the query string from the required query parameters
        var queryString = requiredQueryParameters.Any() ?
            "?" + string.Join("&", requiredQueryParameters.Select(p => $"{p.SerializationName}=${{{p.Name}}}"))
            : "";

        // -s hides the progress bar; -D - sends the headers to stdout

        // Convert 
        var httpMethod = codeElement.HttpMethod.ToString()!.ToUpperInvariant();
        writer.WriteLine($"curl -s -D - -X {httpMethod} \\");
        writer.IncreaseIndent();
        writer.WriteLine($"${{baseUrl}}/{urlTemplate}{queryString} \\");
        writer.WriteLine($"> {outFilename}\n");
        writer.DecreaseIndent();
        writer.WriteLine($"head -n 1 {outFilename}");
        writer.WriteLine($"awk '/^\\r?$/{{f=1}}f' {outFilename} | jq '.'\n");
    }
}
