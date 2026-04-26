using System;

namespace GoldMeridian.Mathematics.SourceGen.Specs;

internal readonly record struct ScalarSpec(
    string Name,
    string Keyword,
    bool IsInteger,
    bool IsSigned,
    bool IsFloatingPoint,
    bool IsBool,
    bool SupportsIntrinsics
)
{
    public bool SupportsArithmetic => IsInteger || IsFloatingPoint;

    /// <summary>
    ///     Size of one scalar in bytes.
    /// </summary>
    public int ByteSize { get; } = Keyword switch
    {
        "double" => 8,
        "long" => 8,
        "ulong" => 8,
        "float" => 4,
        "int" => 4,
        "uint" => 4,
        "short" => 2,
        "ushort" => 2,
        "bool" => 1,
        "sbyte" => 1,
        "byte" => 1,
        _ => throw new NotSupportedException($"Unknown scalar: {Keyword}"),
    };
}
