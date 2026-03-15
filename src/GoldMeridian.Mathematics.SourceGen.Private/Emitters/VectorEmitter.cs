using System;
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
        w.WriteLine($"public partial struct {spec.Name}");
        w.WriteLine("{");
        {
            w.Indent();

            EmitFields(w, spec);
            EmitIndexer(w, spec);
            EmitConstructors(w, spec);
            EmitVectorConstructors(w, spec);
            EmitScalarConversions(w, spec);
            EmitDotnetVectorConversions(w, spec);
            EmitIntrinsicConversions(w, spec);

            w.WriteLine("#region Swizzles", skipIndent: true);
            SwizzleEmitter.Emit(w, spec);
            w.WriteLine("#endregion", skipIndent: true);

            w.Outdent();
        }
        w.WriteLine("}");

        return w.ToString();
    }

    private static void EmitFields(CodeWriter w, VectorSpec spec)
    {
        foreach (var lane in LaneNames.Take(spec.Lanes))
        {
            w.WriteLine($"public {spec.Scalar.Keyword} {lane};");
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

    private static readonly (string Type, int Bits)[] intrinsic_sizes =
    [
        ("Vector64", 64),
        ("Vector128", 128),
        ("Vector256", 256),
        ("Vector512", 512),
    ];

    private static void EmitIntrinsicConversions(CodeWriter w, VectorSpec spec)
    {
        if (!spec.Scalar.SupportsIntrinsics)
        {
            return;
        }

        var scalar = spec.Scalar.Keyword;
        var scalarBits = ScalarBitSize(spec.Scalar.Keyword);
        var lanes = spec.Lanes;

        foreach (var (vectorType, bits) in intrinsic_sizes)
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

        return;

        static int ScalarBitSize(string keyword) => keyword switch
        {
            "float" => 32,
            "double" => 64,
            "int" => 32,
            "uint" => 32,
            "long" => 64,
            "ulong" => 64,
            "short" => 16,
            "ushort" => 16,
            "byte" => 8,
            "sbyte" => 8,
            _ => throw new NotSupportedException($"Unknown scalar type: {keyword}"),
        };
    }
}
