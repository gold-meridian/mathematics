namespace GoldMeridian.Mathematics.SourceGen.Specs;

internal readonly record struct VectorSpec(
    ScalarSpec Scalar,
    int Lanes
)
{
    public string Name => $"{Scalar.Name}{Lanes}";
}
