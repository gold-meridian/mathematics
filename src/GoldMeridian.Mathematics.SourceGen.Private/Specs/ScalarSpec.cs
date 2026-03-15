using System;

namespace GoldMeridian.Mathematics.SourceGen.Specs;

[Flags]
internal enum ScalarCapabilities
{
    None = 0,
    Add = 1 << 0,
    Subtract = 1 << 1,
    Multiply = 1 << 2,
    Divide = 1 << 3,
    Negate = 1 << 4,
    Plus = 1 << 5,
    Equality = 1 << 6,
    Inequality = 1 << 7,
    BitwiseAnd = 1 << 8,
    BitwiseOr = 1 << 9,
    ExclusiveOr = 1 << 10,
    OnesComplement = 1 << 11,
    LeftShift = 1 << 12,
    RightShift = 1 << 13,
    UnsignedRightShift = 1 << 14,
}

internal readonly record struct ScalarSpec(
    string Name,
    string Keyword,
    bool IsInteger,
    bool IsSigned,
    bool IsFloatingPoint,
    bool SupportsIntrinsics
)
{
    public ScalarCapabilities Capabilities { get; } = InferCapabilities(
        IsInteger,
        IsSigned,
        IsFloatingPoint
    );

    public bool SupportsArithmetic => IsInteger || IsFloatingPoint;

    private static ScalarCapabilities InferCapabilities(
        bool isInteger,
        bool isSigned,
        bool isFloatingPoint
    )
    {
        // Some of the conditions are split up for intent in case we need to
        // later support types that don't encompass everything under
        // supportsArithmetic.
        
        var cap = ScalarCapabilities.None;
        var supportsArithmetic = isInteger || isFloatingPoint;

        // Any numeric types...
        if (supportsArithmetic)
        {
            cap |= ScalarCapabilities.Add | ScalarCapabilities.Subtract | ScalarCapabilities.Multiply | ScalarCapabilities.Divide;

            // Signed only really matters for integers since it's implied by
            // floats.
            if (isSigned || isFloatingPoint)
            {
                cap |= ScalarCapabilities.Negate | ScalarCapabilities.Plus;
            }
        }

        // Everything supports equality.
        cap |= ScalarCapabilities.Equality | ScalarCapabilities.Inequality;

        // TODO: In case we ever want to support comparisons?!  Useful for
        //       lanes, but should probably be handled exclusively by them.
        // Arithmetic here implies numbers that can be compared (vs. booleans,
        // which can't be).
        /*
        if (supportsArithmetic)
        {
            cap |= ScalarCapabilities.Less | ScalarCapabilities.LessEqual | ScalarCapabilities.Greater | ScalarCapabilities.GreaterEqual;
        }
        */

        // Booleans are the only type without significant bitness that can be
        // manipulated.
        if (supportsArithmetic)
        {
            cap |= ScalarCapabilities.BitwiseAnd | ScalarCapabilities.BitwiseOr | ScalarCapabilities.ExclusiveOr | ScalarCapabilities.OnesComplement;
        }

        // Another case of significant bitness.
        if (supportsArithmetic)
        {
            cap |= ScalarCapabilities.LeftShift | ScalarCapabilities.RightShift | ScalarCapabilities.UnsignedRightShift;
        }

        return cap;
    }
}
