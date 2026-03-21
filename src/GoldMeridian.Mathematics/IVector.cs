using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace GoldMeridian.Mathematics;

/* TODO:
 *   ISpanFormattable,
 *   ISpanParsable<TVector>,
 *   IUtf8SpanFormattable,
 *   IUtf8SpanParsable<TVector>
 */
/// <summary>
///     The base definition for a vector. Has no specified components.
/// </summary>
public interface IVector<TVector, TScalar> : IEquatable<TVector>,
                                             IEqualityOperators<TVector, TVector, bool>
    where TVector : IVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    /// <summary>
    ///     The number of lanes in the vector.
    /// </summary>
    static abstract int Lanes { get; }

    static abstract TVector One { get; }

    static abstract TVector Zero { get; }

    // Constructors:
    // Vector(T broadcast)
    // Vector(ROS<T> values);
    // (Create overloads)

    static abstract TVector Create(TScalar value);

    static abstract TVector Create(ReadOnlySpan<TScalar> values);
}

/// <summary>
///     A vector whose data is arithmetic (integral or floating point numbers).
/// </summary>
public interface INumberVector<TVector, TScalar> : IVector<TVector, TScalar>,
                                                   IAdditionOperators<TVector, TVector, TVector>,
                                                   ISubtractionOperators<TVector, TVector, TVector>,
                                                   IMultiplyOperators<TVector, TVector, TVector>,
                                                   IDivisionOperators<TVector, TVector, TVector>,
                                                   IAdditiveIdentity<TVector, TVector>,
                                                   IMultiplicativeIdentity<TVector, TVector>,
                                                   IUnaryPlusOperators<TVector, TVector>,
                                                   IUnaryNegationOperators<TVector, TVector>
    where TVector : INumberVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static abstract TVector Abs(TVector value);

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
}

/// <summary>
///     A vector which can represent both positive and negative values.
/// </summary>
public interface ISignedVector<TVector, TScalar> : INumberVector<TVector, TScalar>
    where TVector : ISignedVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static abstract TVector NegativeOne { get; }
}

/// <summary>
///     A vector that is represented in a base-2 format.
/// </summary>
public interface IBinaryNumberVector<TVector, TScalar> : INumberVector<TVector, TScalar>,
                                                         IBitwiseOperators<TVector, TVector, TVector>,
                                                         IShiftOperators<TVector, int, TVector>
    where TVector : IBinaryNumberVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, IBinaryInteger<TScalar>
{
    static virtual TVector AllBitsSet => ~TVector.Zero;

    static abstract TVector Log2(TVector value);

    static virtual (TVector Quotient, TVector Remainder) DivRem(TVector left, TVector right)
    {
        var quotient = left / right;
        return (quotient, left - quotient * right);
    }

    // TODO: LeadingZeroCount
    // TODO: PopCount
    // TODO: RotateLeft
    // TODO: RotateRight
    // TODO: TrailingZeroCount
    // TODO: GetByteCount (Maybe)
    // TODO: GetShortestBitLength (Maybe)
}

public interface IFloatingPointVector<TVector, TScalar> : IBinaryNumberVector<TVector, TScalar>,
                                                          IFloatingPointConstants<TVector>
    where TVector : IFloatingPointVector<TVector, TScalar>
    where TScalar : IEquatable<TScalar>, IBinaryInteger<TScalar>, IFloatingPointIeee754<TScalar>
{
    static TVector IFloatingPointConstants<TVector>.E
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.E);
    }

    static TVector IFloatingPointConstants<TVector>.Pi
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.Pi);
    }

    static TVector IFloatingPointConstants<TVector>.Tau
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TVector.Create(TScalar.Tau);
    }
}

#region Components
/// <summary>
///     The X component.
/// </summary>
public interface IVectorComponent1<out TVector, TScalar>
{
    static abstract TVector UnitX { get; }

    ref TScalar X { get; }
}

/// <summary>
///     The Y component.
/// </summary>
public interface IVectorComponent2<out TVector, TScalar> : IVectorComponent1<TVector, TScalar>
{
    static abstract TVector UnitY { get; }

    ref TScalar Y { get; }
}

/// <summary>
///     The Z component.
/// </summary>
public interface IVectorComponent3<out TVector, TScalar> : IVectorComponent2<TVector, TScalar>
{
    static abstract TVector UnitZ { get; }

    ref TScalar Z { get; }
}

/// <summary>
///     The W component.
/// </summary>
public interface IVectorComponent4<out TVector, TScalar> : IVectorComponent3<TVector, TScalar>
{
    static abstract TVector UnitW { get; }

    ref TScalar W { get; }
}
#endregion

#region Vector sizes
/// <summary>
///     A vector with a concrete dimension of 1.
/// </summary>
public interface IVector1<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent1<TVector, TScalar>
    where TVector : IVector1<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 1;
}

/// <summary>
///     A vector with a concrete dimension of 2.
/// </summary>
public interface IVector2<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent2<TVector, TScalar>
    where TVector : IVector2<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 2;
}

/// <summary>
///     A vector with a concrete dimension of 3.
/// </summary>
public interface IVector3<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent3<TVector, TScalar>
    where TVector : IVector3<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 3;
}

/// <summary>
///     A vector with a concrete dimension of 4.
/// </summary>
public interface IVector4<TVector, TScalar> : IVector<TVector, TScalar>,
                                              IVectorComponent4<TVector, TScalar>
    where TVector : IVector4<TVector, TScalar>
    where TScalar : IEquatable<TScalar>
{
    static int IVector<TVector, TScalar>.Lanes => 4;
}
#endregion
