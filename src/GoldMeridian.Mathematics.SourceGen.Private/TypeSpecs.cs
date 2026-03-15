using System.Collections.Generic;
using System.Linq;

namespace GoldMeridian.Mathematics.SourceGen;

internal readonly record struct ScalarSpec(
    string TypeOrKeywordName,
    string SimplifiedName,
    bool IsFloat,
    bool SupportsSimd
);

internal readonly record struct VectorSpec(
    ScalarSpec Scalar,
    int Lanes
)
{
    public string TypeName => $"{Scalar.TypeOrKeywordName}{Lanes}";
}

internal readonly record struct MatrixSpec(
    ScalarSpec Scalar,
    int Rows,
    int Columns
)
{
    public string TypeName => $"{Scalar.TypeOrKeywordName}{Rows}x{Columns}";
}

internal static class TypeSpecs
{
    // TODO: Figure out which options can feasibly support SIMD, look into
    //       (U)Int24.
    public static ScalarSpec[] Scalars { get; } =
    [
        new("sbyte", "i8", IsFloat: false, SupportsSimd: false),
        new("byte", "u8", IsFloat: false, SupportsSimd: false),
        new("short", "i16", IsFloat: false, SupportsSimd: false),
        new("ushort", "u16", IsFloat: false, SupportsSimd: false),
        new("int", "i32", IsFloat: false, SupportsSimd: false),
        new("uint", "u32", IsFloat: false, SupportsSimd: false),
        new("long", "i64", IsFloat: false, SupportsSimd: false),
        new("ulong", "u64", IsFloat: false, SupportsSimd: false),
        new("bool", "bool", IsFloat: false, SupportsSimd: false),
        new("float", "f32", IsFloat: true, SupportsSimd: true),
        new("double", "f64", IsFloat: true, SupportsSimd: false),
    ];

    public static VectorSpec[] Vectors { get; } = MakeVectors().ToArray();

    public static MatrixSpec[] Matrices { get; } = MakeMatrices().ToArray();

    private static IEnumerable<VectorSpec> MakeVectors()
    {
        foreach (var scalar in Scalars)
        {
            // Including 1-dimensional values is a little awkward but fits with
            // matrices.
            for (var i = 1; i <= 4; i++)
            {
                yield return new VectorSpec(scalar, i);
            }
        }
    }

    private static IEnumerable<MatrixSpec> MakeMatrices()
    {
        foreach (var scalar in Scalars)
        {
            for (var x = 1; x <= 4; x++)
            for (var y = 1; y <= 4; y++)
            {
                yield return new MatrixSpec(scalar, x, y);
            }
        }
    }
}
