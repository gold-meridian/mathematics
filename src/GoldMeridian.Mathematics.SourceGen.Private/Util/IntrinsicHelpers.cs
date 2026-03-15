using System;

namespace GoldMeridian.Mathematics.SourceGen.Util;

internal static class IntrinsicHelpers
{
    public static int ScalarBitSize(string keyword) => keyword switch
    {
        "float" => 32,
        "double" => 64,
        "int" => 32,
        "uint" => 32,
        "long" => 64,
        "ulong" => 64,
        "short" => 16,
        "ushort" => 16,
        "byte" => 8,
        "sbyte" => 8,
        _ => throw new NotSupportedException($"Unknown scalar type: {keyword}"),
    };
}
