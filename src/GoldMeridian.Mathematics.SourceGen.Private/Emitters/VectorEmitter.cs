using System.Collections.Generic;
using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class VectorEmitter
{
    public static int MaxLanes => 4;

    public static string[] LaneNames { get; } = ["X", "Y", "Z", "W"];

    public static string Generate(VectorSpec spec)
    {
        var w = new CodeWriter();

        w.WriteLine("using System;");
        w.WriteLine("using System.Diagnostics.CodeAnalysis;");
        w.WriteLine("using System.Numerics;");
        w.WriteLine("using System.Runtime.CompilerServices;");
        w.WriteLine("using System.Runtime.InteropServices;");
        w.WriteLine("using System.Runtime.Intrinsics;");
        w.WriteLine();
        w.WriteLine("namespace GoldMeridian.Mathematics;");
        w.WriteLine();

        if (spec.Scalar.SupportsIntrinsics)
        {
            var scalarBits = IntrinsicHelpers.ScalarBitSize(spec.Scalar.Keyword);
            var lanes = spec.Lanes;

            var neededBits = lanes * scalarBits;
            var vectorWidth = int.MaxValue;
            foreach (var v in IntrinsicHelpers.IntrinsicSizes)
            {
                if (v.Bits >= neededBits && v.Bits < vectorWidth)
                {
                    vectorWidth = v.Bits;
                }
            }

            var structSizeBytes = vectorWidth / 8;

            w.WriteLine($"[StructLayout(LayoutKind.Sequential, Size = {structSizeBytes})]");
        }

        w.WriteLine($"public partial struct {spec.Name} : IEquatable<{spec.Name}>");
        w.WriteLine("{");
        {
            w.Indent();

            EmitPublicProperties(w, spec);
            EmitFields(w, spec);
            EmitIndexer(w, spec);
            EmitConstructors(w, spec);
            EmitVectorConstructors(w, spec);
            EmitScalarConversions(w, spec);
            EmitDotnetVectorConversions(w, spec);
            EmitIntrinsicConversions(w, spec);

            EmitEquatable(w, spec);

            w.WriteLine("#region Operators", skipIndent: true);
            VectorOperatorsEmitter.Emit(w, spec);
            w.WriteLine("#endregion", skipIndent: true);

            w.WriteLine();

            w.WriteLine("#region Swizzles", skipIndent: true);
            SwizzleEmitter.Emit(w, spec);
            w.WriteLine("#endregion", skipIndent: true);

            w.Outdent();
        }
        w.WriteLine("}");

        return w.ToString();
    }

    private static void EmitPublicProperties(CodeWriter w, VectorSpec spec)
    {
        var t = spec.Scalar.Keyword;
        var lanes = spec.Lanes;

        if (spec.Scalar.SupportsArithmetic)
        {
            w.WriteLine($"public static {spec.Name} One => new({string.Join(", ", Enumerable.Repeat("1", lanes))});");

            w.WriteLine($"public static {spec.Name} Zero => new({string.Join(", ", Enumerable.Repeat("0", lanes))});");

            if (spec.Scalar.IsSigned || spec.Scalar.IsFloatingPoint)
            {
                w.WriteLine($"public static {spec.Name} NegativeZero => new({string.Join(", ", Enumerable.Repeat("-0", lanes))});");
            }

            if (spec.Scalar.IsFloatingPoint)
            {
                w.WriteLine($"public static {spec.Name} E => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.Epsilon", lanes))});");

                w.WriteLine($"public static {spec.Name} NaN => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.NaN", lanes))});");

                w.WriteLine($"public static {spec.Name} NegativeInfinity => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.NegativeInfinity", lanes))});");

                w.WriteLine($"public static {spec.Name} Pi => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.Pi", lanes))});");

                w.WriteLine($"public static {spec.Name} PositiveInfinity => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.PositiveInfinity", lanes))});");

                w.WriteLine($"public static {spec.Name} Tau => new({string.Join(", ", Enumerable.Repeat($"{spec.Scalar.Keyword}.Tau", lanes))});");
            }

            if (lanes >= 1)
            {
                w.WriteLine($"public static {spec.Name} UnitX => new(1{(lanes > 1 ? ", " + string.Join(", ", Enumerable.Repeat("0", lanes - 1)) : "")});");
            }

            if (lanes >= 2)
            {
                w.WriteLine($"public static {spec.Name} UnitY => new(0, 1, {string.Join(", ", Enumerable.Repeat("0", lanes - 2))});");
            }

            if (lanes >= 3)
            {
                w.WriteLine($"public static {spec.Name} UnitZ => new(0, 0, 1, {string.Join(", ", Enumerable.Repeat("0", lanes - 3))});");
            }

            if (lanes >= 4)
            {
                w.WriteLine($"public static {spec.Name} UnitW => new(0, 0, 0, 1);");
            }
        }

        // Special support for booleans because they're dumb and unique.
        if (t == "bool")
        {
            if (lanes >= 1)
            {
                w.WriteLine($"public static {spec.Name} UnitX => new(true{(lanes > 1 ? ", " + string.Join(", ", Enumerable.Repeat("false", lanes - 1)) : "")});");
                w.WriteLine($"public static {spec.Name} TrueX => UnitX;");
            }

            if (lanes >= 2)
            {
                w.WriteLine($"public static {spec.Name} UnitY => new(false, true, {string.Join(", ", Enumerable.Repeat("false", lanes - 2))});");
                w.WriteLine($"public static {spec.Name} TrueY => UnitY;");
            }

            if (lanes >= 3)
            {
                w.WriteLine($"public static {spec.Name} UnitZ => new(false, false, true, {string.Join(", ", Enumerable.Repeat("false", lanes - 3))});");
                w.WriteLine($"public static {spec.Name} TrueZ => UnitZ;");
            }

            if (lanes >= 4)
            {
                w.WriteLine($"public static {spec.Name} UnitW => new(false, false, false, true);");
                w.WriteLine($"public static {spec.Name} TrueW => UnitW;");
            }

            w.WriteLine($"public static {spec.Name} One => new({string.Join(", ", Enumerable.Repeat("true", lanes))});");
            w.WriteLine($"public static {spec.Name} True => One;");

            w.WriteLine($"public static {spec.Name} Zero => new({string.Join(", ", Enumerable.Repeat("false", lanes))});");
            w.WriteLine($"public static {spec.Name} False => Zero;");
        }

        w.WriteLine();
    }

    private static void EmitFields(CodeWriter w, VectorSpec spec)
    {
        var lanes = spec.Lanes;
        var scalar = spec.Scalar.Keyword;

        foreach (var lane in LaneNames.Take(lanes))
        {
            w.WriteLine($"public {scalar} {lane};");
        }

        if (spec.Scalar.SupportsIntrinsics)
        {
            var scalarBits = IntrinsicHelpers.ScalarBitSize(spec.Scalar.Keyword);

            var neededBits = lanes * scalarBits;
            var vectorWidth = int.MaxValue;
            foreach (var v in IntrinsicHelpers.IntrinsicSizes)
            {
                if (v.Bits >= neededBits && v.Bits < vectorWidth)
                {
                    vectorWidth = v.Bits;
                }
            }

            // May be larger than lanes -> pad
            var totalLanes = vectorWidth / scalarBits;

            if (totalLanes > lanes)
            {
                for (var i = lanes; i < totalLanes; i++)
                {
                    w.WriteLine($"private {scalar} pad{i - lanes}; // padding for SIMD alignment");
                }
            }
        }

        w.WriteLine();
    }

    private static void EmitIndexer(CodeWriter w, VectorSpec spec)
    {
        var t = spec.Scalar.Keyword;

        w.WriteLine("[UnscopedRef]");
        w.WriteLine($"public ref {t} this[int index]");
        w.WriteLine("{");
        {
            w.Indent();

            w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            w.WriteLine("get");
            w.WriteLine("{");
            {
                w.Indent();

                if (spec.Lanes == 1)
                {
                    w.WriteLine("if (index != 0)");
                    w.WriteLine("{");
                    {
                        w.Indent();

                        w.WriteLine("throw new IndexOutOfRangeException();");

                        w.Outdent();
                    }
                    w.WriteLine("}");
                    w.WriteLine();
                    w.WriteLine("return ref X;");
                }
                else
                {
                    w.WriteLine($"if ((uint)index >= {spec.Lanes})");
                    w.WriteLine("{");
                    {
                        w.Indent();

                        w.WriteLine("throw new IndexOutOfRangeException();");

                        w.Outdent();
                    }
                    w.WriteLine("}");
                    w.WriteLine();
                    w.WriteLine("return ref Unsafe.Add(ref X, index);");
                }

                w.Outdent();
            }
            w.WriteLine("}");

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();
    }

    private static void EmitConstructors(CodeWriter w, VectorSpec spec)
    {
        var t = spec.Scalar.Keyword;

        var parameters = string.Join(", ", LaneNames.Take(spec.Lanes).Select(n => $"{t} {n.ToLower()}"));
        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public {spec.Name}({parameters})");
        w.WriteLine("{");
        {
            w.Indent();

            foreach (var lane in LaneNames.Take(spec.Lanes))
            {
                w.WriteLine($"{lane} = {lane.ToLower()};");
            }

            w.Outdent();
        }
        w.WriteLine("}");

        if (spec.Lanes <= 1)
        {
            return;
        }

        w.WriteLine();

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public {spec.Name}({t} value)");
        w.WriteLine("{");
        {
            w.Indent();

            foreach (var lane in LaneNames.Take(spec.Lanes))
            {
                w.WriteLine($"{lane} = value;");
            }

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();
    }

    private static void EmitVectorConstructors(CodeWriter w, VectorSpec spec)
    {
        if (spec.Lanes <= 1)
        {
            return;
        }

        var scalar = spec.Scalar.Name;
        var lanes = spec.Lanes;

        foreach (var partition in EnumeratePartitions(lanes))
        {
            if (partition.Count == 1)
            {
                continue;
            }

            var paramIndex = 0;
            var parameters = partition.Select(size => $"{scalar}{size} v{paramIndex++}").ToList();

            w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            w.WriteLine($"public {spec.Name}({string.Join(", ", parameters)})");
            w.WriteLine("{");
            {
                w.Indent();

                var lane = 0;
                var p = 0;

                foreach (var size in partition)
                {
                    for (var i = 0; i < size; i++)
                    {
                        w.WriteLine($"{LaneNames[lane]} = v{p}.{LaneNames[i]};");
                        lane++;
                    }

                    p++;
                }

                w.Outdent();
            }
            w.WriteLine("}");
            w.WriteLine();
        }

        return;

        static IEnumerable<List<int>> EnumeratePartitions(int remaining, List<int>? current = null)
        {
            current ??= [];

            if (remaining == 0)
            {
                yield return [..current];

                yield break;
            }

            for (var i = 1; i <= remaining; i++)
            {
                current.Add(i);

                foreach (var p in EnumeratePartitions(remaining - i, current))
                {
                    yield return p;
                }

                current.RemoveAt(current.Count - 1);
            }
        }
    }

    private static void EmitScalarConversions(CodeWriter w, VectorSpec spec)
    {
        if (spec.Lanes != 1)
        {
            return;
        }

        var t = spec.Scalar.Keyword;

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static implicit operator {t}({spec.Name} v) => v.X;");

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static implicit operator {spec.Name}({t} v) => new(v);");

        w.WriteLine();
    }

    private static readonly string[] dotnet_vector_names = ["", "Vector2", "Vector3", "Vector4"];

    private static void EmitDotnetVectorConversions(CodeWriter w, VectorSpec spec)
    {
        if (spec.Scalar.Keyword != "float")
        {
            return;
        }

        if (spec.Lanes is < 2 or > 4)
        {
            return;
        }

        var vectorName = dotnet_vector_names[spec.Lanes - 1];
        var fieldAccesses = string.Join(", ", LaneNames.Take(spec.Lanes).Select(n => $"v.{n}"));

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static implicit operator {vectorName}({spec.Name} v) => new({fieldAccesses});");

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static implicit operator {spec.Name}({vectorName} v) => new({fieldAccesses});");

        w.WriteLine();
    }

    private static void EmitIntrinsicConversions(CodeWriter w, VectorSpec spec)
    {
        if (!spec.Scalar.SupportsIntrinsics)
        {
            return;
        }

        var scalar = spec.Scalar.Keyword;
        var scalarBits = IntrinsicHelpers.ScalarBitSize(spec.Scalar.Keyword);
        var lanes = spec.Lanes;

        foreach (var (vectorType, bits) in IntrinsicHelpers.IntrinsicSizes)
        {
            var maxLanes = bits / scalarBits;
            if (lanes > maxLanes)
            {
                continue;
            }

            var ctorArgs = new List<string>();
            for (var i = 0; i < lanes; i++)
            {
                ctorArgs.Add($"v.{LaneNames[i]}");
            }

            for (var i = lanes; i < maxLanes; i++)
            {
                ctorArgs.Add("default(" + scalar + ")");
            }

            w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            w.WriteLine($"public static implicit operator {vectorType}<{scalar}>({spec.Name} v) => {vectorType}.Create({string.Join(", ", ctorArgs)});");

            var getters = string.Join(", ", Enumerable.Range(0, lanes).Select(i => $"v.GetElement({i})"));
            w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
            w.WriteLine($"public static implicit operator {spec.Name}({vectorType}<{scalar}> v) => new({getters});");

            w.WriteLine();
        }
    }

    private static void EmitEquatable(CodeWriter w, VectorSpec spec)
    {
        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public bool Equals({spec.Name} other)");
        w.WriteLine("{");
        {
            w.Indent();

            var comparisons = string.Join(" && ", LaneNames.Take(spec.Lanes).Select(x => $"{x} == other.{x}"));
            w.WriteLine($"return {comparisons};");

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine("public override bool Equals(object obj)");
        w.WriteLine("{");
        {
            w.Indent();
            w.WriteLine($"return obj is {spec.Name} other && Equals(other);");
            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        // TODO: Look into more optimized cases for common vectors.
        var hashArgs = string.Join(", ", LaneNames.Take(spec.Lanes));
        w.WriteLine("public override int GetHashCode()");
        w.WriteLine("{");
        {
            w.Indent();
            w.WriteLine($"return HashCode.Combine({hashArgs});");
            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static bool operator ==({spec.Name} a, {spec.Name} b) => a.Equals(b);");

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static bool operator !=({spec.Name} a, {spec.Name} b) => !a.Equals(b);");
        w.WriteLine();
    }
}
