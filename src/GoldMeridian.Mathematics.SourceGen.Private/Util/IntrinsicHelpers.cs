using System;
using GoldMeridian.Mathematics.SourceGen.Emitters;
using GoldMeridian.Mathematics.SourceGen.Specs;

namespace GoldMeridian.Mathematics.SourceGen.Util;

internal static class IntrinsicHelpers
{
    public static (string Type, int Bits)[] IntrinsicSizes { get; } =
    [
        ("Vector64", 64),
        ("Vector128", 128),
        ("Vector256", 256),
        ("Vector512", 512),
    ];

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

    public static string GetIntrinsicVectorType(VectorSpec spec)
    {
        var scalarBits = spec.Scalar.Keyword switch
        {
            "byte" => 8,
            "sbyte" => 8,
            "short" => 16,
            "ushort" => 16,
            "int" => 32,
            "uint" => 32,
            "long" => 64,
            "ulong" => 64,
            "float" => 32,
            "double" => 64,
            _ => throw new NotSupportedException(),
        };

        var neededBits = spec.Lanes * scalarBits;

        foreach (var (vectorType, bits) in IntrinsicSizes)
        {
            if (bits >= neededBits)
            {
                return vectorType;
            }
        }

        throw new InvalidOperationException($"No suitable intrinsic for {spec.Name}");
    }
}
