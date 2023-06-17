using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Curl;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, CurlConventionService>
{
    public CodeEnumWriter(CurlConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
    }
}
