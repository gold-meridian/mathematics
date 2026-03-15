using System.Numerics;

namespace GoldMeridian.Mathematics;

// TODO: IFormattable
/// <summary>
///     The base definition for a vector. Has no specified components.
/// </summary>
/// <typeparam name="TVector"></typeparam>
/// <typeparam name="TData"></typeparam>
public interface IVector<TVector, TData> : IEquatable<TVector>,
                                           IEqualityOperators<TVector, TVector, bool>,
                                           ISpanFormattable,
                                           ISpanParsable<TVector>,
                                           IUtf8SpanFormattable,
                                           IUtf8SpanParsable<TVector>
    where TVector : IVector<TVector, TData>
{
    /// <summary>
    ///     The number of lanes in the vector.
    /// </summary>
    static abstract int Lanes { get; }

    // Constructors:
    // Vector(T broadcast)
    // Vector(ROS<T> values);
    // (Create overloads)
}

/// <summary>
///     A vector whose data is arithmetic (integral or floating point numbers).
/// </summary>
/// <typeparam name="TVector"></typeparam>
/// <typeparam name="TData"></typeparam>
public interface IArithmeticVector<TVector, TData> : IVector<TVector, TData>,
                                                     INumberBase<TVector>
    where TVector : IArithmeticVector<TVector, TData>;

/// <summary>
///     A vector which can represent both positive and negative values.
/// </summary>
public interface ISignedVector<TVector, TData> : IArithmeticVector<TVector, TData>,
                                                 ISignedNumber<TVector>
    where TVector : ISignedVector<TVector, TData>;

/// <summary>
///     A vector that is represented in a base-2 format.
/// </summary>
public interface IBinaryVector<TVector, TData> : IVector<TVector, TData>,
                                                 IBinaryInteger<TVector>
    where TVector : IBinaryVector<TVector, TData>;

public interface IBinaryFloatingPointIeee754Vector<TVector, TData> : IBinaryVector<TVector, TData>,
                                                                     IFloatingPointIeee754<TVector>
    where TVector : IBinaryFloatingPointIeee754Vector<TVector, TData>;

#region Components
/// <summary>
///     The X component.
/// </summary>
public interface IVectorComponent1<TVector, TData> : IVector<TVector, TData>
    where TVector : IVectorComponent1<TVector, TData>
{
    ref TData X { get; }
}

/// <summary>
///     The Y component.
/// </summary>
public interface IVectorComponent2<TVector, TData> : IVectorComponent1<TVector, TData>
    where TVector : IVectorComponent2<TVector, TData>
{
    ref TData Y { get; }
}

/// <summary>
///     The Z component.
/// </summary>
public interface IVectorComponent3<TVector, TData> : IVectorComponent2<TVector, TData>
    where TVector : IVectorComponent3<TVector, TData>
{
    ref TData Z { get; }
}

/// <summary>
///     The W component.
/// </summary>
public interface IVectorComponent4<TVector, TData> : IVectorComponent3<TVector, TData>
    where TVector : IVectorComponent4<TVector, TData>
{
    ref TData W { get; }
}
#endregion

#region Vector sizes
/// <summary>
///     A vector with a concrete dimension of 1.
/// </summary>
public interface IVector1<TVector, TData> : IVectorComponent1<TVector, TData>
    where TVector : IVector1<TVector, TData>
{
    static int IVector<TVector, TData>.Lanes => 1;
}

/// <summary>
///     A vector with a concrete dimension of 2.
/// </summary>
public interface IVector2<TVector, TData> : IVectorComponent2<TVector, TData>
    where TVector : IVector2<TVector, TData>
{
    static int IVector<TVector, TData>.Lanes => 2;
}

/// <summary>
///     A vector with a concrete dimension of 3.
/// </summary>
public interface IVector3<TVector, TData> : IVectorComponent3<TVector, TData>
    where TVector : IVector3<TVector, TData>
{
    static int IVector<TVector, TData>.Lanes => 3;
}

/// <summary>
///     A vector with a concrete dimension of 4.
/// </summary>
public interface IVector4<TVector, TData> : IVectorComponent4<TVector, TData>
    where TVector : IVector4<TVector, TData>
{
    static int IVector<TVector, TData>.Lanes => 4;
}
#endregion
