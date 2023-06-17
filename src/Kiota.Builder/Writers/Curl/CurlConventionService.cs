using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.Curl;
public class CurlConventionService : CommonLanguageConventionService
{
    public override string StreamTypeName => "ArrayBuffer";
    public override string VoidTypeName => "void";
    public override string DocCommentPrefix => " * ";
    public override string ParseNodeInterfaceName => "ParseNode";
    internal string DocCommentStart = "/**";
    internal string DocCommentEnd = " */";
#pragma warning disable CA1822 // Method should be static
    public override string TempDictionaryVarName => "urlTplParams";

#pragma warning restore CA1822 // Method should be static
    public override string GetAccessModifier(AccessModifier access)
    {
        return "";
    }

    public override string GetParameterSignature(CodeParameter parameter, CodeElement targetElement, LanguageWriter? writer = null)
    {
        return "";
    }
    public override string GetTypeString(CodeTypeBase code, CodeElement targetElement, bool includeCollectionInformation = true, LanguageWriter? writer = null)
    {
        return "";
    }

    public override string TranslateType(CodeType type)
    {
        return "";
    }

    public override void WriteShortDescription(string description, LanguageWriter writer)
    {
        throw new NotImplementedException();
    }
}
