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
        new("float", "float", true),
        new("double", "double", true),
        new("int", "int", true),
        new("uint", "uint", true),
        new("long", "long", true),
        new("ulong", "ulong", true),
        new("short", "short", true),
        new("ushort", "ushort", true),
        new("byte", "byte", true),
        new("sbyte", "sbyte", true),
        new("bool", "bool", false),
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
