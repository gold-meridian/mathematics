using System;
using System.Collections.Generic;
using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;
using Microsoft.CodeAnalysis;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class VectorSwizzleEmitter
{
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateVectorSwizzleDefinitions);
    }

    private static void GenerateVectorSwizzleDefinitions(
        IncrementalGeneratorPostInitializationContext ctx
    )
    {
        foreach (var vectorSpec in TypeSpecs.Vectors)
        {
            var defFile = GenerateSwizzles(vectorSpec);
            ctx.AddSource($"{vectorSpec.Name}.swizzles.g.cs", defFile);
        }
    }

    private static string GenerateSwizzles(VectorSpec spec)
    {
        var w = new CodeWriter();
        {
            w.WriteLine("namespace GoldMeridian.Mathematics;");
            w.WriteLine();
            w.WriteLine("using System.Runtime.CompilerServices;");
            w.WriteLine();
            w.WriteLine($"partial struct {spec.Name}");
            w.WriteLine("{");
            {
                w.Indent();

                var sourceLanes = (int)spec.Lanes;
                var scalarName = spec.Scalar.Name;
                var sourceNames = VectorDefinitionEmitter.LaneNames.Take(sourceLanes).ToArray();

                var firstProperty = true;
                for (var length = 2; length <= 4; length++)
                {
                    var targetName = $"{scalarName}{length}";

                    foreach (var combo in Combinations(sourceNames, length))
                    {
                        var propName = string.Concat(combo);
                        var isDistinct = combo.Distinct().Count() == combo.Length;
                        var ctorArgs = string.Join(", ", combo);

                        if (!firstProperty)
                        {
                            w.WriteLine();
                        }
                        else
                        {
                            firstProperty = false;
                        }

                        w.WriteLine($"public {targetName} {propName}");
                        w.WriteLine("{");
                        {
                            w.Indent();

                            w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                            w.WriteLine($"get => new({ctorArgs});");

                            if (isDistinct)
                            {
                                w.WriteLine();
                                w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                                w.WriteLine("set");
                                w.WriteLine("{");
                                {
                                    w.Indent();

                                    for (var i = 0; i < length; i++)
                                    {
                                        w.WriteLine($"{combo[i]} = value.{sourceNames[i]};");
                                    }

                                    w.Outdent();
                                }
                                w.WriteLine("}");
                            }

                            w.Outdent();
                        }
                        w.WriteLine("}");
                    }
                }

                w.Outdent();
            }
            w.WriteLine("}");
        }
        return w.ToString();
    }

    private static IEnumerable<string[]> Combinations(string[] lanes, int length)
    {
        if (length == 0)
        {
            yield return [];

            yield break;
        }

        if (length == 1)
        {
            foreach (var lane in lanes)
            {
                yield return [lane];
            }

            yield break;
        }

        foreach (var head in lanes)
        foreach (var tail in Combinations(lanes, length - 1))
        {
            var result = new string[length];
            result[0] = head;
            Array.Copy(tail, 0, result, 1, length - 1);
            yield return result;
        }
    }
}
