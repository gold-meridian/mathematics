using System;
using System.Collections.Generic;

namespace GoldMeridian.Mathematics.SourceGen.Specs;

internal enum Lanes
{
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
}

internal readonly record struct VectorSpec(
    ScalarSpec Scalar,
    Lanes Lanes
)
{
    public string Name => $"{Scalar.Name}{(int)Lanes}";

    public bool IsBoolVector => Scalar.IsBool;

    public string BoolVectorName { get; } = $"bool{(int)Lanes}";

    public int StructSizeBytes => (int)Lanes * Scalar.ByteSize;

    public int SimdWidthBytes
    {
        get
        {
            if (!Scalar.SupportsIntrinsics)
            {
                return 0;
            }

            var dataBytes = StructSizeBytes;
            foreach (var width in simd_widths)
            {
                if (width >= dataBytes)
                {
                    return width;
                }
            }

            throw new InvalidOperationException($"No SIMD register wide enough for {Name} ({dataBytes} bytes).");
        }
    }

    public string SimdTypeName => SimdWidthBytes switch
    {
        8 => "Vector64",
        16 => "Vector128",
        32 => "Vector256",
        64 => "Vector512",
        _ => throw new InvalidOperationException($"No SIMD type for {Name}."),
    };

    public int Alignment => Scalar.SupportsIntrinsics ? SimdWidthBytes : Scalar.ByteSize;

    public bool EmitDotnetVectorConversions => Scalar.Keyword == "float" && Lanes is not Lanes.One;

    public IReadOnlyList<string> InterfaceList => BuildInterfaceList();

    private static readonly int[] simd_widths = [8, 16, 32, 64];

    private IReadOnlyList<string> BuildInterfaceList()
    {
        var list = new List<string>();
        var t = Name;
        var s = Scalar.Keyword;

        list.Add($"IEquatable<{t}>");

        if (IsBoolVector)
        {
            list.Add($"IBoolVector<{t}>");
            list.Add(DimensionInterface(t, s));
            return list;
        }

        // Choose the most-derived numeric interface that applies.
        // The hierarchy is:
        //   IFloatingPointVector    (floats)
        //   ISignedIntegerVector    (signed integers)
        //   IUnsignedIntegerVector  (unsigned integers)
        // These all transitively include INumberVector, ISignedVector, etc.

        if (Scalar.IsFloatingPoint)
        {
            var bv = BoolVectorName;

            // Dimension-specific floating-point combo interface
            list.Add(
                Lanes switch
                {
                    Lanes.One => $"IFloatingPointVector1<{t}, {s}, {bv}>",
                    Lanes.Two => $"IFloatingPointVector2<{t}, {s}, {bv}>",
                    Lanes.Three => $"IFloatingPointVector3<{t}, {s}, {bv}>",
                    Lanes.Four => $"IFloatingPointVector4<{t}, {s}, {bv}>",
                    _ => throw new InvalidOperationException($"Invalid number of lanes: {Lanes}"),
                }
            );
            return list;
        }

        if (Scalar.IsInteger)
        {
            if (Scalar.IsSigned)
            {
                list.Add($"ISignedIntegerVector<{t}, {s}>");
            }
            else
            {
                list.Add($"IUnsignedIntegerVector<{t}, {s}>");
            }

            list.Add(DimensionInterface(t, s));
            return list;
        }

        throw new InvalidOperationException($"Scalar '{s}' does not map to any known vector interfaces.");
    }

    /// <summary>
    ///     Returns the structural dimension interface (IVector1/2/3/4) for
    ///     non-float vectors.  Float vectors already include the dimension via
    ///     IFloatingPointVector2/3/4, so they don't add it separately.
    /// </summary>
    private string DimensionInterface(string typeName, string scalarKeyword) => Lanes switch
    {
        Lanes.One => $"IVector1<{typeName}, {scalarKeyword}>",
        Lanes.Two => $"IVector2<{typeName}, {scalarKeyword}>",
        Lanes.Three => $"IVector3<{typeName}, {scalarKeyword}>",
        Lanes.Four => $"IVector4<{typeName}, {scalarKeyword}>",
        _ => throw new InvalidOperationException($"Invalid number of lanes: {Lanes}"),
    };
}
