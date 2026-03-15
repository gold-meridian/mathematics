using System.Text;

namespace GoldMeridian.Mathematics.SourceGen.Util;

/// <summary>
///     Utility to make writing code with indentation easier.
/// </summary>
public sealed class CodeWriter
{
    /// <summary>
    ///     The current level of indentation to emit.
    /// </summary>
    public int IndentLevel
    {
        get;

        set
        {
            field = value;
            indentString = new string(' ', value * 4);
        }
    } = 0;

    private string indentString = "";

    private readonly StringBuilder sb = new();

    public void Indent(int amount = 1)
    {
        IndentLevel += amount;

        // TODO: Worthy of throwing an exception?  Probably not...
        if (IndentLevel < 0)
        {
            IndentLevel = 0;
        }
    }

    public void Outdent(int amount = 1)
    {
        Indent(-amount);
    }

    public void WriteLine(string line = "", bool skipIndent = false)
    {
        if (!skipIndent && !string.IsNullOrEmpty(line))
        {
            sb.Append(indentString);
        }

        sb.AppendLine(line);
    }

    public override string ToString()
    {
        return sb.ToString();
    }
}
