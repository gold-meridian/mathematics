using System.Collections.Generic;
using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Emitters;

namespace GoldMeridian.Mathematics.SourceGen.Specs;

internal static class TypeSpecs
{
    // TODO: Figure out which options can feasibly support SIMD, look into
    //       (U)Int24.
    public static ScalarSpec[] Scalars { get; } =
    [
        new("float", "float", IsInteger: false, IsSigned: true, IsFloatingPoint: true, SupportsIntrinsics: true),
        new("double", "double", IsInteger: false, IsSigned: true, IsFloatingPoint: true, SupportsIntrinsics: true),
        new("int", "int", IsInteger: true, IsSigned: true, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("uint", "uint", IsInteger: true, IsSigned: false, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("long", "long", IsInteger: true, IsSigned: true, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("ulong", "ulong", IsInteger: true, IsSigned: false, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("short", "short", IsInteger: true, IsSigned: true, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("ushort", "ushort", IsInteger: true, IsSigned: false, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("sbyte", "sbyte", IsInteger: true, IsSigned: true, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("byte", "byte", IsInteger: true, IsSigned: false, IsFloatingPoint: false, SupportsIntrinsics: true),
        new("bool", "bool", IsInteger: false, IsSigned: false, IsFloatingPoint: false, SupportsIntrinsics: false),
    ];

    public static VectorSpec[] Vectors { get; } = MakeVectors().ToArray();

    public static MatrixSpec[] Matrices { get; } = MakeMatrices().ToArray();

    private static IEnumerable<VectorSpec> MakeVectors()
    {
        foreach (var scalar in Scalars)
        {
            // Including 1-dimensional values is a little awkward but fits with
            // matrices.
            for (var i = 1; i <= VectorEmitter.MaxLanes; i++)
            {
                yield return new VectorSpec(scalar, i);
            }
        }
    }

    private static IEnumerable<MatrixSpec> MakeMatrices()
    {
        /*
        foreach (var scalar in Scalars)
        {
            for (var x = 1; x <= 4; x++)
            for (var y = 1; y <= 4; y++)
            {
                yield return new MatrixSpec(scalar, x, y);
            }
        }
        */
        yield break;
    }
}
