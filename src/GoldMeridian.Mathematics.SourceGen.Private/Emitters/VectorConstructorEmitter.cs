using System.Collections.Generic;
using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;
using Microsoft.CodeAnalysis;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class VectorConstructorEmitter
{
    private record struct ParamInfo(
        string TypeName,
        string ParamName,
        int PartSize
    );

    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateVectorConstructorDefinitions);
    }

    private static void GenerateVectorConstructorDefinitions(
        IncrementalGeneratorPostInitializationContext ctx
    )
    {
        foreach (var vectorSpec in TypeSpecs.Vectors)
        {
            var defFile = GenerateConstructors(vectorSpec);
            ctx.AddSource($"{vectorSpec.Name}.constructors.g.cs", defFile);
        }
    }

    private static string GenerateConstructors(VectorSpec spec)
    {
        var totalLanes = (int)spec.Lanes;
        if (totalLanes == 1)
        {
            return "// No constructors to generate for single-lane vectors.";
        }

        var scalarKeyword = spec.Scalar.Keyword;
        var scalarName = spec.Scalar.Name;

        var vectors =
            TypeSpecs.Vectors
                     .Where(v => v.Scalar.Name == scalarName)
                     .ToDictionary(v => (int)v.Lanes);

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

                var firstPartition = true;
                foreach (var partition in OrderedPartitions(totalLanes))
                {
                    // Skip all-scalar (will be the component-wise ctor); e.g.,
                    // float4(float, float, float, float)
                    if (partition.All(p => p == 1))
                    {
                        continue;
                    }

                    if (!firstPartition)
                    {
                        w.WriteLine();
                    }
                    else
                    {
                        firstPartition = false;
                    }

                    EmitPartitionConstructor(w, spec, partition, scalarKeyword, vectors);
                }

                w.Outdent();
            }
            w.WriteLine("}");
        }
        return w.ToString();
    }

    private static void EmitPartitionConstructor(
        CodeWriter w,
        VectorSpec spec,
        List<int> partition,
        string scalarKeyword,
        Dictionary<int, VectorSpec> vectors
    )
    {
        var typeName = spec.Name;
        var parameters = BuildParameters(partition, scalarKeyword, vectors);
        var assignments = BuildAssignments(partition, parameters);

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public {typeName}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.ParamName}"))})");
        w.WriteLine("{");
        {
            w.Indent();

            foreach (var (lhs, rhs) in assignments)
            {
                w.WriteLine($"{lhs} = {rhs};");
            }

            w.Outdent();
        }
        w.WriteLine("}");
    }

    private static List<ParamInfo> BuildParameters(
        List<int> partition,
        string scalarKeyword,
        Dictionary<int, VectorSpec> siblings
    )
    {
        var result = new List<ParamInfo>();
        var laneOffset = 0;
        var laneNames = VectorDefinitionEmitter.LaneNames;

        foreach (var size in partition)
        {
            var typeName = size == 1 ? scalarKeyword : siblings[size].Name;

            // Name the parameter after the lanes it covers, e.g. "xy", "zw",
            // "x"
            var coveredLanes = laneNames.Skip(laneOffset).Take(size);
            var paramName = string.Concat(coveredLanes).ToLowerInvariant();

            result.Add(new ParamInfo(typeName, paramName, size));
            laneOffset += size;
        }

        return result;
    }

    private static List<(string Lhs, string Rhs)> BuildAssignments(
        List<int> partition,
        List<ParamInfo> parameters
    )
    {
        var laneNames = VectorDefinitionEmitter.LaneNames;

        var assignments = new List<(string, string)>();
        var laneOffset = 0;

        for (var i = 0; i < partition.Count; i++)
        {
            var param = parameters[i];
            var size = param.PartSize;

            if (size == 1)
            {
                assignments.Add((laneNames[laneOffset], param.ParamName));
            }
            else
            {
                for (var j = 0; j < size; j++)
                {
                    assignments.Add((laneNames[laneOffset + j], $"{param.ParamName}.{laneNames[j]}"));
                }
            }

            laneOffset += size;
        }

        return assignments;
    }

    private static IEnumerable<List<int>> OrderedPartitions(int remaining, List<int>? current = null)
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

            foreach (var p in OrderedPartitions(remaining - i, current))
            {
                yield return p;
            }

            current.RemoveAt(current.Count - 1);
        }
    }
}
