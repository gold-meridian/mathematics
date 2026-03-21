using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GoldMeridian.Mathematics;

/* Important design notes/considerations:
 *
 * The interface hierarchy is modeled after the .NET System.Numerics.Vector2/3/4
 * API surfaces, lifted into generic contracts based on the .NET 7 Generic Math
 * interfaces where possible.
 *
 * Key derivations from INumber<T>:
 * - no IComparable<T> (vectors have no total ordering);
 * - comparison operators return TVector (bitmasks),
 *   - use *All/*Any/*None for scalar boolean reductions;
 * - shifts on floats are included but mirror Vector2/3/4 <<, >>, and >>>
 *   operators (which delegate to Vector128<float>, etc.).  These are the SIMD
 *   semantics, not arithmetic shift semantics.
 *
 * Boolean vectors:
 * - bool1/2/3/4 use packed 8-bit C# booleans;
 * - they are not layout-compatible with standard float/int vectors of the same
 *   dimension due to not being 32-bit booleans (duh), so we can't use
 *   Unsafe.BitCast for zero-cost conversions (we can't use this in general due
 *   to the differences in sizes);
 * - classification methods on IFloatingPointVector take on two forms:
 *   - IsNan(TVector): TVector -> (scalar bitmask, mirrors Vector2/3/4),
 *   - IsNanMask(Vector): TBoolVector -> (component-wise, concrete impl).
 * The *Mask variants are abstract because there is no general zero-cost way to
 * convert a scalar bitmask to a packed bool vector (see earlier comments).
 * Concrete generated types implement them with explicit per-component checks
 * (unfortunate and slow).  We can't parallelize lane conversion because we
 * always just get bitmasked vectors, which we're explicitly trying to convert
 * from.
 *
 * Memory layout contract:
 * - all generated vector types must be sequential (StructLayout.Sequential);
 * - fields are laid out contiguously in component order (X, Y, Z, W).
 */

/// <summary>
///     Base contract for all vector types.  Provides equality, lane count,
///     zero/one constants, creation, and span interop.
/// </summary>
public interface IVector<TVector, TScalar> : IEquatable<TVector>,
                                             IEqualityOperators<TVector, TVector, bool>,
                                             IFormattable
    where TVector : IVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    /// <summary>
    ///     The number of components in this vector type.
    /// </summary>
    static abstract int Lanes { get; }

    /// <summary>
    ///     The required memory alignment in bytes for aligned load/store
    ///     operations.  Matches the smallest SIMD vector boundary that fits
    ///     this type.
    /// </summary>
    static abstract int Alignment { get; }

    /// <summary>
    ///     All components zero (or false for bool vectors).
    /// </summary>
    static abstract TVector Zero { get; }

    /// <summary>
    ///     All components one (or true for bool vectors).
    /// </summary>
    static abstract TVector One { get; }

#region Creation
    /// <summary>
    ///     Broadcast a scalar value to all components.
    /// </summary>
    static abstract TVector Create(TScalar value);

    /// <summary>
    ///     Create from a span.
    ///     <br />
    ///     Must contain at least <see cref="Lanes"/> elements.
    /// </summary>
    static virtual TVector Create(ReadOnlySpan<TScalar> values)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(values.Length, TVector.Lanes);
        return Unsafe.ReadUnaligned<TVector>(ref Unsafe.As<TScalar, byte>(ref MemoryMarshal.GetReference(values)));
    }

    /// <summary>
    ///     Creates a vector with X set to <paramref name="x"/> and all other
    ///     components zero.
    /// </summary>
    static abstract TVector CreateScalar(TScalar x);

    /// <summary>
    ///     Creates a vector with X set to <paramref name="x"/> and all other
    ///     components uninitialized.
    /// </summary>
    static abstract TVector CreateScalarUnsafe(TScalar x);
#endregion

    static abstract ref TScalar GetReference(in TVector v);
}

/// <summary>
///     A vector whose scalar is a number.  Covers both integer and float
///     vectors.
///     <br />
///     Mirrors the full arithmetic + bitwise + shift + comparison operator set
///     of <see cref="Vector2"/>/<see cref="Vector3"/>/<see cref="Vector4"/>.
/// </summary>
/// <remarks>
///     Unary negation is included here because even unsigned integer types
///     expose it at the SIMD level (two's complement negation).  If you need to
///     distinguish signed from unsigned at the constraint level, use
///     <see cref="ISignedVector{TVector,TScalar}"/> or
///     <see cref="IUnsignedIntegerVector{TVector,TScalar}"/>.
/// </remarks>
public interface INumberVector<TVector, TScalar> : IVector<TVector, TScalar>,
                                                   IAdditionOperators<TVector, TVector, TVector>,
                                                   ISubtractionOperators<TVector, TVector, TVector>,
                                                   IMultiplyOperators<TVector, TVector, TVector>,
                                                   IDivisionOperators<TVector, TVector, TVector>,
                                                   IModulusOperators<TVector, TVector, TVector>,
                                                   IUnaryPlusOperators<TVector, TVector>,
                                                   IUnaryNegationOperators<TVector, TVector>,
                                                   IBitwiseOperators<TVector, TVector, TVector>,
                                                   IShiftOperators<TVector, int, TVector>,
                                                   IAdditiveIdentity<TVector, TVector>,
                                                   IMultiplicativeIdentity<TVector, TVector>
    where TVector : INumberVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, INumberBase<TScalar>
{
#region Identity defaults
    static TVector IAdditiveIdentity<TVector, TVector>.AdditiveIdentity => TVector.Zero;

    static TVector IMultiplicativeIdentity<TVector, TVector>.MultiplicativeIdentity => TVector.One;
#endregion

#region Constants
    /// <summary>
    ///     All bits set in every component.  Mirrors
    ///     <see cref="Vector2.AllBitsSet"/>.
    /// </summary>
    static virtual TVector AllBitsSet => ~TVector.Zero;
#endregion

#region Scalar operator overloads
    static virtual TVector operator *(TVector left, TScalar right)
    {
        return left * TVector.Create(right);
    }

    static virtual TVector operator *(TScalar left, TVector right)
    {
        return TVector.Create(left) * right;
    }

    static virtual TVector operator /(TVector left, TScalar right)
    {
        return left / TVector.Create(right);
    }

    static virtual TVector operator %(TVector left, TScalar right)
    {
        return left % TVector.Create(right);
    }
#endregion

#region Named operator aliases
    static virtual TVector Add(TVector left, TVector right)
    {
        return left + right;
    }

    static virtual TVector Subtract(TVector left, TVector right)
    {
        return left - right;
    }

    static virtual TVector Multiply(TVector left, TVector right)
    {
        return left * right;
    }

    static virtual TVector Multiply(TVector left, TScalar right)
    {
        return left * right;
    }

    static virtual TVector Multiply(TScalar left, TVector right)
    {
        return left * right;
    }

    static virtual TVector Divide(TVector left, TVector right)
    {
        return left / right;
    }

    static virtual TVector Divide(TVector left, TScalar right)
    {
        return left / right;
    }

    static virtual TVector Negate(TVector value)
    {
        return -value;
    }
#endregion

#region Component-wise math
    /// <summary>
    ///     Component-wise absolute value.
    /// </summary>
    static abstract TVector Abs(TVector value);

    /// <summary>
    ///     Component-wise minimum (NaN propagating).
    /// </summary>
    static abstract TVector Min(TVector left, TVector right);

    /// <summary>
    ///     Component-wise maximum (NaN propagating).
    /// </summary>
    static abstract TVector Max(TVector left, TVector right);

    /// <summary>
    ///     Component-wise minimum, NaN suppressing; returns the number.
    /// </summary>
    static abstract TVector MinNumber(TVector left, TVector right);

    /// <summary>
    ///     Component-wise maximum, NaN suppressing; returns the number.
    /// </summary>
    static abstract TVector MaxNumber(TVector left, TVector right);

    /// <summary>
    ///     Component-wise clamp (NaN propagating).
    /// </summary>
    static virtual TVector Clamp(TVector value, TVector min, TVector max)
    {
        return TVector.Min(TVector.Max(value, min), max);
    }

    /// <summary>
    ///     Component-wise clamp using native hardware semantics.
    ///     <br />
    ///     May not propagate NaN.  Mirrors <see cref="Vector2.ClampNative"/>.
    /// </summary>
    static abstract TVector ClampNative(TVector value, TVector min, TVector max);
#endregion

#region Bitwise named methods
    static virtual TVector AndNot(TVector left, TVector right)
    {
        return left & ~right;
    }

    static virtual TVector BitwiseAnd(TVector left, TVector right)
    {
        return left & right;
    }

    static virtual TVector BitwiseOr(TVector left, TVector right)
    {
        return left | right;
    }

    static virtual TVector OnesComplement(TVector value)
    {
        return ~value;
    }

    static virtual TVector Xor(TVector left, TVector right)
    {
        return left ^ right;
    }
#endregion

#region Comparison (TVector bitmask returning)
    static abstract TVector Equals(TVector left, TVector right);

    static abstract TVector LessThan(TVector left, TVector right);

    static abstract TVector LessThanOrEqual(TVector left, TVector right);

    static abstract TVector GreaterThan(TVector left, TVector right);

    static abstract TVector GreaterThanOrEqual(TVector left, TVector right);
#endregion

#region Scalar boolean reductions
    static abstract bool EqualsAll(TVector left, TVector right);

    static abstract bool EqualsAny(TVector left, TVector right);

    static abstract bool LessThanAll(TVector left, TVector right);

    static abstract bool LessThanAny(TVector left, TVector right);

    static abstract bool LessThanOrEqualAll(TVector left, TVector right);

    static abstract bool LessThanOrEqualAny(TVector left, TVector right);

    static abstract bool GreaterThanAll(TVector left, TVector right);

    static abstract bool GreaterThanAny(TVector left, TVector right);

    static abstract bool GreaterThanOrEqualAll(TVector left, TVector right);

    static abstract bool GreaterThanOrEqualAny(TVector left, TVector right);
#endregion

#region Value-based scalar reductions
    // Value equality
    static abstract bool All(TVector vector, TScalar value);

    static abstract bool Any(TVector vector, TScalar value);

    static abstract bool None(TVector vector, TScalar value);

    // Bitwise all-bits-set
    static abstract bool AllWhereAllBitsSet(TVector vector);

    static abstract bool AnyWhereAllBitsSet(TVector vector);

    static abstract bool NoneWhereAllBitsSet(TVector vector);

    // Index / count
    static abstract int Count(TVector vector, TScalar value);

    static abstract int CountWhereAllBitsSet(TVector vector);

    static abstract int IndexOf(TVector vector, TScalar value);

    static abstract int IndexOfWhereAllBitsSet(TVector vector);

    static abstract int LastIndexOf(TVector vector, TScalar value);

    static abstract int LastIndexOfWhereAllBitsSet(TVector vector);
#endregion

#region Horizontal reduction
    /// <summary>
    ///     Sum of all components.  Mirrors <see cref="Vector2.Sum"/>.
    /// </summary>
    static abstract TScalar Sum(TVector value);
#endregion
}

/// <summary>
///     A numeric vector whose scalar is signed.  Adds <see cref="NegativeOne"/>
///     and <see cref="CopySign"/>.
/// </summary>
public interface ISignedVector<TVector, TScalar> : INumberVector<TVector, TScalar>
    where TVector : ISignedVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, INumberBase<TScalar>, ISignedNumber<TScalar>
{
    /// <summary>
    ///     All components set to negative one.
    /// </summary>
    static abstract TVector NegativeOne { get; }

    /// <summary>
    ///     Component-wise CopySign.  Mirrors <see cref="Vector2.CopySign"/>.
    /// </summary>
    static abstract TVector CopySign(TVector value, TVector sign);
}

/// <summary>
///     Shared base for signed and unsigned integer vectors.
///     <br />
///     Provides integer-specific operations: <see cref="DivRem"/>,
///     <see cref="Log2"/>, bit counting, rotation.
/// </summary>
public interface IBinaryIntegerVector<TVector, TScalar> : INumberVector<TVector, TScalar>
    where TVector : IBinaryIntegerVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, IBinaryInteger<TScalar>
{
    static virtual (TVector Quotient, TVector Remainder) DivRem(TVector left, TVector right)
    {
        var q = left / right;
        return (q, left - q * right);
    }

    static abstract TVector Log2(TVector value);

    static abstract TVector LeadingZeroCount(TVector value);

    static abstract TVector TrailingZeroCount(TVector value);

    static abstract TVector PopCount(TVector value);

    static abstract TVector RotateLeft(TVector value, int count);

    static abstract TVector RotateRight(TVector value, int count);
}

/// <summary>
///     Signed binary integer vector (<see cref="sbyte"/>, <see cref="short"/>,
///     <see cref="int"/>, <see cref="long"/>, <see cref="Int128"/>,
///     <see cref="nint"/>).
/// </summary>
public interface ISignedIntegerVector<TVector, TScalar> : IBinaryIntegerVector<TVector, TScalar>,
                                                          ISignedVector<TVector, TScalar>
    where TVector : ISignedIntegerVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, IBinaryInteger<TScalar>, ISignedNumber<TScalar>;

/// <summary>
///     Unsigned binary integer vector (<see cref="byte"/>,
///     <see cref="ushort"/>, <see cref="uint"/>, <see cref="ulong"/>,
///     <see cref="UInt128"/>, <see cref="nuint"/>).
/// </summary>
public interface IUnsignedIntegerVector<TVector, TScalar> : IBinaryIntegerVector<TVector, TScalar>
    where TVector : IUnsignedIntegerVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, IBinaryInteger<TScalar>, IUnsignedNumber<TScalar>;

/// <summary>
///     A vector whose scalar is an IEEE 754 floating-point type.
///     <br />
///     Mirrors the full
///     <see cref="Vector2"/>/<see cref="Vector3"/>/<see cref="Vector4"/> API
///     surface.
/// </summary>
/// <typeparam name="TVector"></typeparam>
/// <typeparam name="TScalar"></typeparam>
/// <typeparam name="TBoolVector">
///     The same-size boolean vector type.  Used as the typed return of
///     <c>*Mask</c> classification methods and
///     <see cref="ConditionalSelect(TVector, TVector, TVector)"/>/<see cref="ConditionalSelect(TBoolVector, TVector, TVector)"/>.
///     <br />
///     E.g. for <see cref="float2"/> this is <see cref="bool2"/>.
/// </typeparam>
public interface IFloatingPointVector<TVector, TScalar, TBoolVector> : ISignedVector<TVector, TScalar>
    where TVector : struct, IFloatingPointVector<TVector, TScalar, TBoolVector>
    where TScalar : IEquatable<TScalar>, IFloatingPointIeee754<TScalar>
    where TBoolVector : struct, IBoolVector<TBoolVector>
{
#region IFloatingPointConstants defaults
    static virtual TVector E
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.E);
    }

    static virtual TVector Pi
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.Pi);
    }

    static virtual TVector Tau
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.Tau);
    }
#endregion

#region IFloatingPointIeee754 constants
    static virtual TVector Epsilon => TVector.Create(TScalar.Epsilon);

    static virtual TVector NaN => TVector.Create(TScalar.NaN);

    static virtual TVector NegativeInfinity => TVector.Create(TScalar.NegativeInfinity);

    static virtual TVector PositiveInfinity => TVector.Create(TScalar.PositiveInfinity);

    static virtual TVector NegativeZero => TVector.Create(TScalar.NegativeZero);
#endregion

#region Rounding
    static abstract TVector Floor(TVector value);

    static abstract TVector Ceiling(TVector value);

    static abstract TVector Truncate(TVector value);

    static abstract TVector Round(TVector value);

    static abstract TVector Round(TVector value, MidpointRounding mode);
#endregion

#region Component-wise float math
    // Exponential

    static abstract TVector Sqrt(TVector value);

    // Sqrt is a more recognized name, but Vector2/3/4 use SquareRoot.
    static virtual TVector SquareRoot(TVector value)
    {
        return TVector.Sqrt(value);
    }

    static abstract TVector Hypot(TVector x, TVector y);

    static abstract TVector Exp(TVector value);

    static abstract TVector Log(TVector value);

    static abstract TVector Log2(TVector value);

    // Trigonometric
    static abstract TVector Sin(TVector value);

    static abstract TVector Cos(TVector value);

    static abstract (TVector Sin, TVector Cos) SinCos(TVector value);

    // Rotation
    static abstract TVector DegreesToRadians(TVector degrees);

    static abstract TVector RadiansToDegrees(TVector radians);
#endregion

    static abstract TVector FusedMultiplyAdd(TVector left, TVector right, TVector addend);

    static abstract TVector MultiplyAddEstimate(TVector left, TVector right, TVector addend);

    static abstract TVector MaxMagnitude(TVector left, TVector right);

    static abstract TVector MinMagnitude(TVector left, TVector right);

    static abstract TVector MaxMagnitudeNumber(TVector left, TVector right);

    static abstract TVector MinMagnitudeNumber(TVector left, TVector right);

    static abstract TVector MaxNative(TVector left, TVector right);

    static abstract TVector MinNative(TVector left, TVector right);

#region Classification (TVector bitmask-returning)
    static abstract TVector IsNaN(TVector value);

    static abstract TVector IsInfinity(TVector value);

    static abstract TVector IsFinite(TVector value);

    static abstract TVector IsNegative(TVector value);

    static abstract TVector IsPositive(TVector value);

    static abstract TVector IsZero(TVector value);

    static abstract TVector IsPositiveInfinity(TVector value);

    static abstract TVector IsNegativeInfinity(TVector value);

    static abstract TVector IsNormal(TVector value);

    static abstract TVector IsSubnormal(TVector value);

    static abstract TVector IsInteger(TVector value);

    static abstract TVector IsEvenInteger(TVector value);

    static abstract TVector IsOddInteger(TVector value);
#endregion

#region Typed classification (bool-mask returning)
    // There is no general, zero-cost conversion to boolean vectors since they
    // come in different sizes.  Concrete generated types implement each as an
    // explicit per-component check (slow!):
    //   static bool2 IsNaNMask(float2 v) => new (float.IsNaN(v.X), float.IsNaN(v.Y));

    static abstract TBoolVector IsNaNMask(TVector value);

    static abstract TBoolVector IsInfinityMask(TVector value);

    static abstract TBoolVector IsFiniteMask(TVector value);

    static abstract TBoolVector IsNegativeMask(TVector value);

    static abstract TBoolVector IsPositiveMask(TVector value);

    static abstract TBoolVector IsZeroMask(TVector value);

    static abstract TBoolVector IsPositiveInfinityMask(TVector value);

    static abstract TBoolVector IsNegativeInfinityMask(TVector value);

    static abstract TBoolVector IsNormalMask(TVector value);

    static abstract TBoolVector IsSubnormalMask(TVector value);

    static abstract TBoolVector IsIntegerMask(TVector value);

    static abstract TBoolVector IsEvenIntegerMask(TVector value);

    static abstract TBoolVector IsOddIntegerMask(TVector value);
#endregion

#region Geometric
    /// <summary>
    ///     Dot product.  Returns scalar.
    /// </summary>
    static abstract TScalar Dot(TVector left, TVector right);

    /// <summary>
    ///     Squared Euclidean length.
    /// </summary>
    static virtual TScalar LengthSquared(TVector value)
    {
        return TVector.Dot(value, value);
    }

    /// <summary>
    ///     Euclidean length.
    /// </summary>
    static abstract TScalar Length(TVector value);

    /// <summary>
    ///     Squared distance.
    /// </summary>
    static virtual TScalar DistanceSquared(TVector a, TVector b)
    {
        return TVector.LengthSquared(a - b);
    }

    /// <summary>
    ///     Euclidean distance.
    /// </summary>
    static virtual TScalar Distance(TVector a, TVector b)
    {
        return TVector.Length(a - b);
    }

    /// <summary>
    ///     Unit vector.  Returns NaN components if input has zero length.
    /// </summary>
    static abstract TVector Normalize(TVector value);

    /// <summary>
    ///     Returns the reflection of a vector off a surface that has the
    ///     specified normal.
    /// </summary>
    static virtual TVector Reflect(TVector incident, TVector normal)
    {
        var tmp = TVector.Create(TVector.Dot(incident, normal));
        tmp += tmp;
        return TVector.MultiplyAddEstimate(-tmp, normal, incident);
    }
#endregion

#region Interpolation
    static abstract TVector Lerp(TVector a, TVector b, TVector t);

    static virtual TVector Lerp(TVector a, TVector b, TScalar t)
    {
        return TVector.Lerp(a, b, TVector.Create(t));
    }
#endregion

    /// <summary>
    ///     Selects components from <paramref name="left"/> or
    ///     <paramref name="right"/> based on the bits in
    ///     <paramref name="condition"/> (all-bits-set selects left).
    ///     <br />
    ///     Mirrors <see cref="Vector2.ConditionalSelect"/>.
    /// </summary>
    static abstract TVector ConditionalSelect(TVector condition, TVector left, TVector right);

    /// <summary>
    ///     Selects components using a packed bool vector as the condition.
    ///     <br />
    ///     Concrete types implement the bool-to-mask expansion internally.
    /// </summary>
    static abstract TVector ConditionalSelect(TBoolVector condition, TVector left, TVector right);

#region Instance methods
    TScalar Length();

    TScalar LengthSquared();
#endregion
}

/// <summary>
///     A vector of booleans.  Used for mask results and conditional selection.
///     <br />
///     Supports logical bitwise operations and
///     <see cref="Any"/>/<see cref="All"/>/<see cref="None"/> reductions.
///     <br />
///     Does NOT support arithmetic.
/// </summary>
public interface IBoolVector<TVector> : IVector<TVector, bool>,
                                        IBitwiseOperators<TVector, TVector, TVector>,
                                        IUnaryNegationOperators<TVector, TVector> // logical NOT (~)
    where TVector : IBoolVector<TVector>
{
    /// <summary>
    ///     All components true.
    /// </summary>
    static abstract TVector False { get; }

    /// <summary>
    ///     All components false.
    /// </summary>
    static abstract TVector True { get; }

    static TVector IVector<TVector, bool>.Zero => TVector.False;

    static TVector IVector<TVector, bool>.One => TVector.True;

    static abstract bool Any(TVector value);

    static abstract bool All(TVector value);

    static virtual bool None(TVector value)
    {
        return !TVector.Any(value);
    }
}

public interface IVectorComponent1<out TVector, TScalar>
{
    static abstract TVector UnitX { get; }

    ref TScalar X { get; }
}

public interface IVectorComponent2<out TVector, TScalar> : IVectorComponent1<TVector, TScalar>
{
    static abstract TVector UnitY { get; }

    ref TScalar Y { get; }
}

public interface IVectorComponent3<out TVector, TScalar> : IVectorComponent2<TVector, TScalar>
{
    static abstract TVector UnitZ { get; }

    ref TScalar Z { get; }
}

public interface IVectorComponent4<out TVector, TScalar> : IVectorComponent3<TVector, TScalar>
{
    static abstract TVector UnitW { get; }

    ref TScalar W { get; }
}

public interface IVector1<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent1<TVector, TScalar>
    where TVector : IVector1<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 1;

    new static abstract TVector Create(TScalar x);

    static TVector IVector<TVector, TScalar>.Create(TScalar value)
    {
        return TVector.Create(value);
    }

    static TVector IVector<TVector, TScalar>.CreateScalar(TScalar x)
    {
        return TVector.Create(x);
    }

    static TVector IVector<TVector, TScalar>.CreateScalarUnsafe(TScalar x)
    {
        return TVector.CreateScalarUnsafe(x);
    }

    static ref TScalar IVector<TVector, TScalar>.GetReference(in TVector v)
    {
        return ref v.X;
    }
}

public interface IVector2<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent2<TVector, TScalar>
    where TVector : IVector2<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 2;

    static abstract TVector Create(TScalar x, TScalar y);

    static TVector IVector<TVector, TScalar>.CreateScalar(TScalar x)
    {
        return TVector.Create(x, default(TScalar)!);
    }

    static TVector IVector<TVector, TScalar>.CreateScalarUnsafe(TScalar x)
    {
        Unsafe.SkipInit(out TVector value);
        value.X = x;
        return value;
    }

    static ref TScalar IVector<TVector, TScalar>.GetReference(in TVector v)
    {
        return ref v.X;
    }
}

public interface IVector3<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent3<TVector, TScalar>
    where TVector : IVector3<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 3;

    static abstract TVector Create(TScalar x, TScalar y, TScalar z);

    static TVector IVector<TVector, TScalar>.CreateScalar(TScalar x)
    {
        return TVector.Create(x, default(TScalar)!, default(TScalar)!);
    }

    static TVector IVector<TVector, TScalar>.CreateScalarUnsafe(TScalar x)
    {
        Unsafe.SkipInit(out TVector value);
        value.X = x;
        return value;
    }

    static ref TScalar IVector<TVector, TScalar>.GetReference(in TVector v)
    {
        return ref v.X;
    }
}

public interface IVector4<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent4<TVector, TScalar>
    where TVector : IVector4<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 4;

    static abstract TVector Create(TScalar x, TScalar y, TScalar z, TScalar w);

    static TVector IVector<TVector, TScalar>.CreateScalar(TScalar x)
    {
        return TVector.Create(x, default(TScalar)!, default(TScalar)!, default(TScalar)!);
    }

    static TVector IVector<TVector, TScalar>.CreateScalarUnsafe(TScalar x)
    {
        Unsafe.SkipInit(out TVector value);
        value.X = x;
        return value;
    }

    static ref TScalar IVector<TVector, TScalar>.GetReference(in TVector v)
    {
        return ref v.X;
    }
}

/// <summary>
///     2-component floating-point vector.  Adds 2D <see cref="Cross"/> and
///     <see cref="Shuffle"/>.</summary>
public interface IFloatingPointVector2<TVector, TScalar, TBoolVector> : IFloatingPointVector<TVector, TScalar, TBoolVector>,
                                                                        IVector2<TVector, TScalar>
    where TVector : struct, IFloatingPointVector2<TVector, TScalar, TBoolVector>
    where TScalar : IEquatable<TScalar>, IFloatingPointIeee754<TScalar>
    where TBoolVector : struct, IBoolVector<TBoolVector>
{
    /// <summary>
    ///     2D cross product; the Z component of the 3D cross product.
    ///     <br />
    ///     Returns value1.X * value2.Y - value1.Y * value2.X.
    /// </summary>
    static abstract TScalar Cross(TVector left, TVector right);

    /// <summary>
    ///     Shuffle components by index.  Mirrors <see cref="Vector2.Shuffle"/>.
    /// </summary>
    static abstract TVector Shuffle(TVector vector, byte xIndex, byte yIndex);
}

/// <summary>
///     3-component floating-point vector.  Adds 3D <see cref="Cross"/> and
///     <see cref="Shuffle"/>.
/// </summary>
public interface IFloatingPointVector3<TVector, TScalar, TBoolVector> : IFloatingPointVector<TVector, TScalar, TBoolVector>,
                                                                        IVector3<TVector, TScalar>
    where TVector : struct, IFloatingPointVector3<TVector, TScalar, TBoolVector>
    where TScalar : IEquatable<TScalar>, IFloatingPointIeee754<TScalar>
    where TBoolVector : struct, IBoolVector<TBoolVector>
{
    /// <summary>
    ///     3D cross product.
    /// </summary>
    static abstract TVector Cross(TVector left, TVector right);

    /// <summary>
    ///     Shuffle components by index.
    /// </summary>
    static abstract TVector Shuffle(TVector vector, byte xIndex, byte yIndex, byte zIndex);
}

/// <summary>
///     4-component floating-point vector.  Adds <see cref="Shuffle"/>.
/// </summary>
public interface IFloatingPointVector4<TVector, TScalar, TBoolVector> : IFloatingPointVector<TVector, TScalar, TBoolVector>,
                                                                        IVector4<TVector, TScalar>
    where TVector : struct, IFloatingPointVector4<TVector, TScalar, TBoolVector>
    where TScalar : IEquatable<TScalar>, IFloatingPointIeee754<TScalar>
    where TBoolVector : struct, IBoolVector<TBoolVector>
{
    /// <summary>
    ///     Shuffle components by index.
    /// </summary>
    static abstract TVector Shuffle(TVector vector, byte xIndex, byte yIndex, byte zIndex, byte wIndex);
}
