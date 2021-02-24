using System.Linq;
using System.Collections.Generic;
using System;

namespace kiota.core {
    public class TypeScriptRefiner : CommonLanguageRefiner, ILanguageRefiner
    {
        private readonly HashSet<string> defaultTypes = new HashSet<string> {"string", "integer", "boolean", "array", "object", "(input: ReadableStream) => object"};
        public override void Refine(CodeNamespace generatedCode)
        {
            PatchResponseHandlerType(generatedCode);
            AddDefaultImports(generatedCode);
            ReplaceIndexersByMethodsWithParameter(generatedCode, "ById");
            CorrectCodeType(generatedCode);
            AddPropertiesAndMethodTypesImports(generatedCode, true, true, true);
        }
        private static readonly Tuple<string, string>[] defaultNamespacesForRequestBuilders = new Tuple<string, string>[] { 
            new Tuple<string, string>("HttpCore", "@microsoft/kiota-abstractions"),
            new Tuple<string, string>("HttpMethod", "@microsoft/kiota-abstractions"),
            new Tuple<string, string>("RequestInfo", "@microsoft/kiota-abstractions")
        };
        private void AddDefaultImports(CodeElement current) {
            if(current is CodeClass currentClass && currentClass.ClassKind == CodeClassKind.RequestBuilder) {
                currentClass.AddUsing(defaultNamespacesForRequestBuilders
                                        .Select(x => new CodeUsing(currentClass) { 
                                                            Name = x.Item1,
                                                            GenerationProperties = new Dictionary<string, object>{ {"externalImportSymbol", x.Item2 } }
                                                        }).ToArray());
            }
            CrawlTree(current, AddDefaultImports);
        }
        private void CorrectCodeType(CodeElement currentElement) {
            if (currentElement is CodeProperty currentProperty && currentProperty.Type.Name.Equals("IHttpCore", StringComparison.InvariantCultureIgnoreCase))
                currentProperty.Type.Name = "HttpCore";
            CrawlTree(currentElement, CorrectCodeType);
        }
        private void PatchResponseHandlerType(CodeElement current) {
            if(current is CodeMethod currentMethod && currentMethod.Name.Equals("defaultResponseHandler", StringComparison.InvariantCultureIgnoreCase)) 
                currentMethod.Parameters.First().Type.Name = "ReadableStream";
            if(current is CodeProperty currentProperty && currentProperty.PropertyKind == CodePropertyKind.ResponseHandler) {
                currentProperty.Type.Name = "(input: ReadableStream) => Promise<object>";
                currentProperty.DefaultValue = "this.defaultResponseHandler";
            }
            CrawlTree(current, PatchResponseHandlerType);
        }
    }
}
