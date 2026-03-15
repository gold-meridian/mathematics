using System.Linq;

namespace GoldMeridian.Mathematics.SourceGen.Generators;

internal static class VectorEmitter
{
    public static readonly string[] LANE_NAMES = ["X", "Y", "Z", "W"];

    public static string Generate(VectorSpec spec)
    {
        var lanes = LANE_NAMES.Take(spec.Lanes).ToArray();
        var fields = string.Join("\n", lanes.Select(x => $"public {spec.Scalar.TypeOrKeywordName} {x} = {x};"));
        var ctorParams = string.Join(", ", lanes.Select(x => $"{spec.Scalar.TypeOrKeywordName} {x}"));

        return $$"""
                 namespace GoldMeridian.Mathematics;
                 
                 public partial struct {{spec.TypeName}}({{ctorParams}})
                 {
                 {{fields}}
                 }
                 """;
    }
}
