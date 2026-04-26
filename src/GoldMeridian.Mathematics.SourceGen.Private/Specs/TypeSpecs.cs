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
        new("float", "float", false, true, true, false, true),
        new("double", "double", false, true, true, false, true),
        new("int", "int", true, true, false, false, true),
        new("uint", "uint", true, false, false, false, true),
        new("long", "long", true, true, false, false, true),
        new("ulong", "ulong", true, false, false, false, true),
        new("short", "short", true, true, false, false, true),
        new("ushort", "ushort", true, false, false, false, true),
        new("sbyte", "sbyte", true, true, false, false, true),
        new("byte", "byte", true, false, false, false, true),
        new("bool", "bool", false, false, false, true, false),
    ];

    public static VectorSpec[] Vectors { get; } = MakeVectors().ToArray();

    public static MatrixSpec[] Matrices { get; } = MakeMatrices().ToArray();

    private static IEnumerable<VectorSpec> MakeVectors()
    {
        foreach (var scalar in Scalars)
        {
            // Including 1-dimensional values is a little awkward but fits with
            // matrices.
            yield return new VectorSpec(scalar, Lanes.One);
            yield return new VectorSpec(scalar, Lanes.Two);
            yield return new VectorSpec(scalar, Lanes.Three);
            yield return new VectorSpec(scalar, Lanes.Four);
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
