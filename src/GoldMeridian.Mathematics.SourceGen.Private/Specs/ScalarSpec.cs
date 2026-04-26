using System;

namespace GoldMeridian.Mathematics.SourceGen.Specs;

[Flags]
internal enum ScalarCapabilities
{
    None = 0,

    // Arithmetic operators
    Add = 1 << 0,
    Subtract = 1 << 1,
    Multiply = 1 << 2,
    Divide = 1 << 3,
    Modulus = 1 << 4,
    Negate = 1 << 5,
    Plus = 1 << 6,

    // Equality (all types)
    Equality = 1 << 7,
    Inequality = 1 << 8,

    // Bitwise: integers + floats (via SIMD reinterpret), and bool (logical).
    BitwiseAnd = 1 << 9,
    BitwiseOr = 1 << 10,
    ExclusiveOr = 1 << 11,
    OnesComplement = 1 << 12,

    // Shifts: integers only at the scalar level.
    // Float vector shifts are handled separately in the emitter via the SIMD
    // path (Vector128<float> << int) and do not come from scalar capabilities
    LeftShift = 1 << 13,
    RightShift = 1 << 14,
    UnsignedRightShift = 1 << 15,

    // Semantic flags: used by the emitter to select interface declarations
    // and gate method emission per capability group.
    IsFloatingPoint = 1 << 16,
    IsSigned = 1 << 17,
    IsBinaryInteger = 1 << 18,
    IsBool = 1 << 19,
}

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
    public ScalarCapabilities Capabilities { get; } = InferCapabilities(
        IsInteger,
        IsSigned,
        IsFloatingPoint,
        IsBool,
        SupportsIntrinsics
    );

    public bool SupportsArithmetic => IsInteger || IsFloatingPoint;

    /// <summary>
    ///     Size of one scalar in bytes.
    /// </summary>
    public int ByteSize => Keyword switch
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

    /// <summary>
    ///     Returns the name of the corresponding bool vector type for this
    ///     scalar at a given lane count (e.g. float -> "bool2" for lanes=2).
    ///     <br />
    ///     Only meaningful for arithmetic scalars.
    /// </summary>
    public string BoolVectorName(int lanes)
    {
        return $"bool{lanes}";
    }

    private static ScalarCapabilities InferCapabilities(
        bool isInteger,
        bool isSigned,
        bool isFloatingPoint,
        bool isBool,
        bool supportsIntrinsics
    )
    {
        var cap = ScalarCapabilities.None;
        var supportsArithmetic = isInteger || isFloatingPoint;

        // Equality: every type including bool.
        cap |= ScalarCapabilities.Equality | ScalarCapabilities.Inequality;

        // Arithmetic: integers and floats only.
        if (supportsArithmetic)
        {
            cap |= ScalarCapabilities.Add
                 | ScalarCapabilities.Subtract
                 | ScalarCapabilities.Multiply
                 | ScalarCapabilities.Divide
                 | ScalarCapabilities.Modulus
                 | ScalarCapabilities.Plus;

            if (isSigned || isFloatingPoint)
            {
                cap |= ScalarCapabilities.Negate;
            }
        }

        // Bitwise: intrinsic types (floats + integers) get it via SIMD
        // reinterpret, matching Vector2/3/4.  Bool gets logical bitwise without
        // needing intrinsics.
        if (supportsIntrinsics || isBool)
        {
            cap |= ScalarCapabilities.BitwiseAnd
                 | ScalarCapabilities.BitwiseOr
                 | ScalarCapabilities.ExclusiveOr
                 | ScalarCapabilities.OnesComplement;
        }

        // Shifts: integers only.
        if (isInteger)
        {
            cap |= ScalarCapabilities.LeftShift
                 | ScalarCapabilities.RightShift
                 | ScalarCapabilities.UnsignedRightShift;
        }

        // Semantic tier flags.
        if (isFloatingPoint)
        {
            cap |= ScalarCapabilities.IsFloatingPoint;
        }

        if (isSigned || isFloatingPoint)
        {
            cap |= ScalarCapabilities.IsSigned;
        }

        if (isInteger)
        {
            cap |= ScalarCapabilities.IsBinaryInteger;
        }

        if (isBool)
        {
            cap |= ScalarCapabilities.IsBool;
        }

        return cap;
    }
}
