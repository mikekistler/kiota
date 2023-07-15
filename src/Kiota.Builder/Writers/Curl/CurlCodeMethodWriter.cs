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

        // Create output file name based on generated file name
        var currentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
        var shFilename = Path.GetFileName(writer.PathSegmenter!.GetPath(currentNamespace, codeElement, false));
        var outFilename = shFilename.Replace(".sh", ".out", StringComparison.OrdinalIgnoreCase);

        // Get paging information from the RequestExecutor
        var requestExecutor = codeElement.Parent?.GetChildElements().OfType<CodeMethod>()
            .Where(static e => e.IsOfKind(CodeMethodKind.RequestExecutor)).First();
        var pagingInfo = requestExecutor?.PagingInformation;

        // Set SerializationName for each parameter
        foreach (var parameter in codeElement.PathQueryAndHeaderParameters)
        {
            if (string.IsNullOrEmpty(parameter.SerializationName))
                parameter.SerializationName = parameter.Name;
        }

        // Get the list of path parameters (they are always required)
        var pathParameters = codeElement.PathQueryAndHeaderParameters
                                .Where(static p => p.IsOfKind(CodeParameterKind.Path))
                                .ToList()
                                ?? new List<CodeParameter>();

        // Get the list of required query parameters
        var requiredQueryParameters = codeElement.PathQueryAndHeaderParameters
                .Where(static p => p.IsOfKind(CodeParameterKind.QueryParameter) && !p.Optional)
                .ToList()
                ?? new List<CodeParameter>();

        var bodyParameter = codeElement.Parameters.FirstOrDefault(p => p.IsOfKind(CodeParameterKind.RequestBody));

        writer.WriteLine("#!/bin/bash\n");

        // Generate a function to print a usage statement and exit
        writer.WriteLine("function usage() {");
        writer.IncreaseIndent();
        writer.WriteLine($"echo \"Usage:\"");
        writer.Write("echo \"    $0");
        if (pagingInfo != null)
        {
            writer.Write(" [--nextPage]", false);
        }
        if (pathParameters.Count > 0)
        {
            writer.Write($" {string.Join(" ", pathParameters.Select(p => $"<{p.Name}>"))}", false);
        }
        writer.WriteLine("\"", false);
        if (pathParameters.Any() || requiredQueryParameters.Any())
        {
            writer.WriteLine($"echo \"Environment variables:\"");
            var allParams = pathParameters.Concat(requiredQueryParameters).ToList();
            writer.WriteLine($"echo \"    {string.Join(",", allParams.Select(p => $"{p.Name}"))}\"");
        }
        writer.WriteLine("exit 1");
        writer.DecreaseIndent();
        writer.WriteLine($"}}\n");

        // Display usage if the first parameter is "-h" or "--help".
        writer.WriteLine("if [[ $1 == \"-h\" || $1 == \"--help\" ]]; then");
        writer.IncreaseIndent();
        writer.WriteLine("usage");
        writer.DecreaseIndent();
        writer.WriteLine("fi\n");

        writer.WriteLine("source ./init.sh\n");

        // Set the baseUrl
        var baseUrl = "${endpoint}";
        if (!string.IsNullOrEmpty(codeElement.BaseUrl))
        {
            baseUrl += $"/{codeElement.BaseUrl}";
        }
        writer.WriteLine($"baseUrl={baseUrl}\n");

        // Create shell variables for each required parameter
        // Path parameters are always required, so we'll handle them first

        for (int i = 0; i < pathParameters.Count; i++)
        {
            var parameterName = pathParameters[i].Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{parameterName}=${{{parameterName}:-${i + 1}}}");
        }
        foreach (var parameter in requiredQueryParameters)
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{parameterName}=${{{parameterName}:-{parameterName}}}");
        }
        if (pathParameters.Any() || requiredQueryParameters.Any())
            writer.WriteLine();

        if (bodyParameter != null)
        {
            writer.WriteLine("# Set the request body");
            var parameterName = bodyParameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"{parameterName}='{{\n}}'\n");
        }

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

        if (pagingInfo != null)
        {
            writer.WriteLine("if [[ $1 == \"--nextPage\" ]]; then");
            writer.IncreaseIndent();
            writer.WriteLine($"if [[ ! -e {outFilename} ]]; then");
            writer.IncreaseIndent();
            writer.WriteLine($"echo \"{outFilename} not found. Please run without --nextPage first.\"");
            writer.WriteLine("exit 1");
            writer.DecreaseIndent();
            writer.WriteLine("fi");
            writer.WriteLine($"url=$(awk '/^\\r?$/{{f=1}}f' {outFilename} | jq -r '.{pagingInfo.NextLinkName}')");
            writer.WriteLine("if [[ \"$url\" == \"null\" ]]; then");
            writer.IncreaseIndent();
            writer.WriteLine("echo \"No next page found.\"");
            writer.WriteLine("exit 1");
            writer.DecreaseIndent();
            writer.WriteLine("fi");
            writer.DecreaseIndent();
            writer.WriteLine("else");
            writer.IncreaseIndent();
            writeParamChecks(writer, pathParameters);
            writer.WriteLine($"url=\"${{baseUrl}}/{urlTemplate}{queryString}\"");
            writer.DecreaseIndent();
            writer.WriteLine("fi\n");
        }
        else
        {
            writeParamChecks(writer, pathParameters);
            if (pathParameters.Any() || requiredQueryParameters.Any())
                writer.WriteLine();
        }

        var httpMethod = codeElement.HttpMethod.ToString()!.ToUpperInvariant();
        // -s hides the progress bar; -D - sends the headers to stdout
        writer.WriteLine($"curl -s -D - -X {httpMethod} \\");
        writer.IncreaseIndent();
        writer.WriteLine("-H \"Authorization: Bearer ${token}\" \\");
        if (bodyParameter != null)
        {
            if (codeElement.RequestBodyContentType != null)
            {
                writer.WriteLine($"-H \"Content-Type: {codeElement.RequestBodyContentType}\" \\");
            }
            var parameterName = bodyParameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"-d \"${{{parameterName}}}\" \\");
        }
        if (pagingInfo != null)
        {
            writer.WriteLine($"${{url}} \\");
        }
        else
        {
            writer.WriteLine($"\"${{baseUrl}}/{urlTemplate}{queryString}\" \\");
        }
        writer.WriteLine($"> {outFilename}\n");
        writer.DecreaseIndent();
        writer.WriteLine($"head -n 1 {outFilename}");
        if (codeElement.AcceptedResponseTypes.All(t => t == "application/json"))
        {
            writer.WriteLine($"awk '/^\\r?$/{{f=1}}f' {outFilename} | jq '.'");
        }
        else
        {
            writer.WriteLine($"awk '/^\\r?$/{{f=1}}f' {outFilename}");
        }
    }

    private void writeParamChecks(LanguageWriter writer, List<CodeParameter> parameters)
    {
        foreach (var parameter in parameters)
        {
            var parameterName = parameter.Name.ToFirstCharacterLowerCase();
            writer.WriteLine($"if [[ ! \"{parameterName}\" ]]; then");
            writer.IncreaseIndent();
            writer.WriteLine($"echo \"A value must be provided for {parameterName}.\"");
            writer.WriteLine("usage");
            writer.DecreaseIndent();
            writer.WriteLine("fi");
        }
    }
}
