using System;
using System.Collections.Generic;
using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class SwizzleEmitter
{
    public static string[] LaneNames => VectorEmitter.LaneNames;

    public static void Emit(CodeWriter w, VectorSpec spec)
    {
        var lanes = spec.Lanes;
        var scalar = spec.Scalar.Name;

        var laneSubset = LaneNames.Take(lanes).ToArray();

        for (var length = 2; length <= Math.Min(4, lanes); length++)
        {
            foreach (var combination in EnumerateCombinations(laneSubset, length))
            {
                var name = string.Concat(combination);
                var typeName = $"{scalar}{length}";

                w.WriteLine($"public {typeName} {name}");
                w.WriteLine("{");
                {
                    w.Indent();

                    w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    w.WriteLine($"get => new({string.Join(", ", combination)});");

                    if (combination.Distinct().Count() == combination.Length)
                    {
                        w.WriteLine();
                        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                        w.WriteLine("set");
                        w.WriteLine("{");
                        {
                            w.Indent();

                            for (var i = 0; i < length; i++)
                            {
                                w.WriteLine($"{combination[i]} = value.{LaneNames[i]};");
                            }

                            w.Outdent();
                        }
                        w.WriteLine("}");
                    }

                    w.Outdent();
                }
                w.WriteLine("}");

                w.WriteLine();
            }
        }
    }

    private static IEnumerable<string[]> EnumerateCombinations(string[] lanes, int length)
    {
        if (length == 1)
        {
            foreach (var lane in lanes)
            {
                yield return [lane];
            }

            yield break;
        }

        foreach (var head in lanes)
        {
            foreach (var tail in EnumerateCombinations(lanes, length - 1))
            {
                var result = new string[length];
                result[0] = head;
                Array.Copy(tail, 0, result, 1, length - 1);
                yield return result;
            }
        }
    }
}
