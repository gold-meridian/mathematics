using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GoldMeridian.Mathematics;

/// <summary>
///     Generic extension methods for all vector types.
///     <br />
///     All implementations rely on the memory layout contract: every generated
///     vector type uses <see cref="System.Runtime.InteropServices.StructLayoutAttribute"/>
///     (<see cref="LayoutKind.Sequential"/>), with components laid out
///     contiguously in field order (X, Y, Z, W, ...).  This makes
///     <see cref="MemoryMarshal"/> and <see cref="Unsafe"/> operations safe
///     and correct for all generated types without per-type overrides.
/// </summary>
public static class VectorExtensions
{
    // TODO: How necessary is `this ref` in each extension?  `this in` may be
    //       generally preferred but isn't allowed with generics (same for
    //       `this ref readonly`).
    
    /// <summary>
    ///     Gets the element at <paramref name="index"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TScalar GetElement<TVector, TScalar>(
        this ref TVector vector,
        int index
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if ((uint)index >= (uint)TVector.Lanes)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return MemoryMarshal.CreateReadOnlySpan(
            ref TVector.GetReference(in vector),
            TVector.Lanes
        )[index];
    }

    /// <summary>
    ///     Returns a new vector with the element at <paramref name="index"/>
    ///     replaced by <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TVector WithElement<TVector, TScalar>(
        this ref TVector vector,
        int index,
        TScalar value
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if ((uint)index >= (uint)TVector.Lanes)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        MemoryMarshal.CreateSpan(ref TVector.GetReference(in vector), TVector.Lanes)[index] = value;
        return vector;
    }

    /// <summary>
    ///     Copies all components into <paramref name="destination"/>.
    ///     <br />
    ///     Destination must have at least
    ///     <see cref="IVector{TVector,TScalar}.Lanes"/> elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyTo<TVector, TScalar>(
        this ref TVector vector,
        Span<TScalar> destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>, IVectorComponent1<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if (destination.Length < TVector.Lanes)
        {
            throw new ArgumentException("Destination span is too short.");
        }

        /*
        MemoryMarshal.CreateReadOnlySpan(
            ref TVector.GetReference(in vector),
            TVector.Lanes
        ).CopyTo(destination);
        */

        Unsafe.WriteUnaligned(ref Unsafe.As<TScalar, byte>(ref MemoryMarshal.GetReference(destination)), vector);
    }

    /// <summary>
    ///     Attempts to copy all components into <paramref name="destination"/>.
    ///     <br />
    ///     Returns <see langword="false"/> if the destination is too short.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryCopyTo<TVector, TScalar>(
        this ref TVector vector,
        Span<TScalar> destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>, IVectorComponent1<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if (destination.Length < TVector.Lanes)
        {
            return false;
        }

        /*
        MemoryMarshal.CreateReadOnlySpan(
            ref TVector.GetReference(in vector),
            TVector.Lanes
        ).CopyTo(destination);
        */

        Unsafe.WriteUnaligned(ref Unsafe.As<TScalar, byte>(ref MemoryMarshal.GetReference(destination)), vector);
        return true;
    }

    /// <summary>
    ///     Loads a vector from the memory at <paramref name="source"/>.
    ///     <br />
    ///     The source need not be aligned.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TVector Load<TVector, TScalar>(TScalar* source)
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        return LoadUnsafe<TVector, TScalar>(in *source);
    }

    /// <summary>
    ///     Loads a vector from the memory starting at
    ///     <paramref name="source"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TVector LoadUnsafe<TVector, TScalar>(ref readonly TScalar source)
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        ref readonly var address = ref Unsafe.As<TScalar, byte>(ref Unsafe.AsRef(in source));
        return Unsafe.ReadUnaligned<TVector>(in address);
    }

    /// <summary>
    ///     Loads a vector from the memory at <paramref name="source"/> offset
    ///     by <paramref name="elementOffset"/> elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TVector LoadUnsafe<TVector, TScalar>(
        ref readonly TScalar source,
        nuint elementOffset
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        ref readonly var address = ref Unsafe.As<TScalar, byte>(ref Unsafe.Add(ref Unsafe.AsRef(in source), (nint)elementOffset));
        return Unsafe.ReadUnaligned<TVector>(in address);
    }

    /// <summary>
    ///     Loads a vector from aligned memory at <paramref name="source"/>.
    ///     Throws <see cref="AccessViolationException"/> if the pointer is not
    ///     aligned to <see cref="IVector{TVector,TScalar}.Alignment"/> bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TVector LoadAligned<TVector, TScalar>(TScalar* source)
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if ((nuint)source % (nuint)TVector.Alignment != 0)
        {
            throw new AccessViolationException($"Pointer must be aligned to {TVector.Alignment} bytes.");
        }

        return *(TVector*)source;
    }

    // This is identical to LoadAligned in terms of functionality.  The JIT can
    // optimize this to bypass the cache if the hardware allows, but we can't
    // take advantage of that because we are not an intrinsic function with
    // these features implemented.
    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe TVector LoadAlignedNonTemporal<TVector, TScalar>(TScalar* source)
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        return LoadAligned<TVector, TScalar>(source);
    }
    */

    // =========================================================================
    // Store (unmanaged pointer)
    // =========================================================================

    /// <summary>
    ///     Stores <paramref name="vector"/> to the memory at
    ///     <paramref name="destination"/>.  The destination need not be
    ///     aligned.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void Store<TVector, TScalar>(
        this ref TVector vector,
        TScalar* destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        vector.StoreUnsafe(ref *destination);
    }

    /// <summary>
    ///     Stores <paramref name="vector"/> to the memory starting at
    ///     <paramref name="destination"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StoreUnsafe<TVector, TScalar>(
        this ref TVector vector,
        ref TScalar destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        ref var address = ref Unsafe.As<TScalar, byte>(ref destination);
        Unsafe.WriteUnaligned(ref address, vector);
    }

    /// <summary>
    ///     Stores <paramref name="vector"/> to the memory at
    ///     <paramref name="destination"/> offset by
    ///     <paramref name="elementOffset"/> elements.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StoreUnsafe<TVector, TScalar>(
        this ref TVector vector,
        ref TScalar destination,
        nuint elementOffset
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        destination = ref Unsafe.Add(ref destination, (nint)elementOffset);
        Unsafe.WriteUnaligned(ref Unsafe.As<TScalar, byte>(ref destination), vector);
    }

    /// <summary>
    ///     Stores <paramref name="vector"/> to aligned memory at
    ///     <paramref name="destination"/>.
    ///     <br />
    ///     Throws <see cref="AccessViolationException"/> if the pointer is not
    ///     aligned to <see cref="IVector{TVector,TScalar}.Alignment"/> bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void StoreAligned<TVector, TScalar>(
        this ref TVector vector,
        TScalar* destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        if ((nuint)destination % (nuint)TVector.Alignment != 0)
        {
            throw new AccessViolationException($"Pointer must be aligned to {TVector.Alignment} bytes.");
        }

        *(TVector*)destination = vector;
    }

    // See LoadAlignedNonTemporal comment.
    /*
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void StoreAlignedNonTemporal<TVector, TScalar>(
        this ref TVector vector,
        TScalar* destination
    )
        where TVector : unmanaged, IVector<TVector, TScalar>
        where TScalar : unmanaged, IEquatable<TScalar>
    {
        vector.StoreAligned(destination);
    }
    */
}
