using System;
using System.Collections.Generic;

namespace Kiota.Builder.Refiners;
public class CurlReservedNamesProvider : IReservedNamesProvider
{
    // https://zsh.sourceforge.io/Doc/Release/Shell-Grammar.html#Reserved-Words
    private readonly Lazy<HashSet<string>> _reservedNames = new(() => new(StringComparer.OrdinalIgnoreCase)
    {
        "do", "done", "esac", "then", "elif", "else", "fi", "for", "case", "if", "while", "function", "repeat",
        "time", "until", "select", "coproc", "nocorrect", "foreach", "end", /*! [[ { } */ "declare", "export",
        "float", "integer", "local", "readonly", "typeset"
    });
    public HashSet<string> ReservedNames => _reservedNames.Value;
}
