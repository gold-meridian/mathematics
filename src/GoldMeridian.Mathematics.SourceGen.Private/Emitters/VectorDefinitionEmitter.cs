using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;
using Microsoft.CodeAnalysis;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class VectorDefinitionEmitter
{
    // public static string[] LaneNames => VectorEmitter.LaneNames;
    public static string[] LaneNames { get; } = ["X", "Y", "Z", "W"];
    
    public static void Register(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(GenerateBaseVectorDefinitions);
    }

    private static void GenerateBaseVectorDefinitions(
        IncrementalGeneratorPostInitializationContext ctx
    )
    {
        foreach (var vectorSpec in TypeSpecs.Vectors)
        {
            var defFile = GenerateSpec(vectorSpec);
            ctx.AddSource($"{vectorSpec.Name}.def.g.cs", defFile);
        }
    }

    private static string GenerateSpec(VectorSpec spec)
    {
        var w = new CodeWriter();
        {
            var definitionPart = $"public partial struct {spec.Name} : ";
            var interfaceIndentation = new string(' ', definitionPart.Length);
            var interfaces = spec.InterfaceList.ToArray();

            w.WriteLine("using System;");
            w.WriteLine("using System.Runtime.InteropServices;");
            w.WriteLine();
            w.WriteLine("namespace GoldMeridian.Mathematics;");
            w.WriteLine();
            w.WriteLine("[StructLayout(StructLayoutKind.Sequential)]");
            w.Write(definitionPart);

            for (var i = 0; i < interfaces.Length; i++)
            {
                if (i != 0)
                {
                    w.Write(interfaceIndentation);
                }

                w.Write(interfaces[i]);

                if (i < interfaces.Length - 1)
                {
                    w.WriteLine(",");
                }
                else
                {
                    w.WriteLine();
                }
            }

            w.WriteLine("{");
            {
                w.Indent();

                for (var i = 0; i < (int)spec.Lanes; i++)
                {
                    w.WriteLine($"public ref {spec.Scalar.Name} {LaneNames[i]} => {LaneNames[i].ToLower()}");
                }

                w.WriteLine();

                for (var i = 0; i < (int)spec.Lanes; i++)
                {
                    w.WriteLine($"private {spec.Scalar.Name} {LaneNames[i].ToLower()};");
                }
                
                w.Outdent();
            }
            w.WriteLine("}");
        }
        return w.ToString();
    }
}
