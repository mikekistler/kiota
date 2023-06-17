using System;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Curl;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, CurlConventionService>
{
    public CodePropertyWriter(CurlConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
    }
}
