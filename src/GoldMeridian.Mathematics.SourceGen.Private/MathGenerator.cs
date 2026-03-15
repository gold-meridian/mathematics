using GoldMeridian.Mathematics.SourceGen.Emitters;
using GoldMeridian.Mathematics.SourceGen.Specs;
using Microsoft.CodeAnalysis;

namespace GoldMeridian.Mathematics.SourceGen;

[Generator]
public sealed class MathGenerator : IIncrementalGenerator
{
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(
            ctx =>
            {
                foreach (var vector in TypeSpecs.Vectors)
                {
                    ctx.AddSource(
                        $"{vector.Name}.g.cs",
                        VectorEmitter.Generate(vector)
                    );
                }

                /*
                foreach (var matrix in TypeSpecs.Matrices)
                {
                    ctx.AddSource(
                        $"{matrix.TypeName}.matrix.g.cs",
                        MatrixEmitter.Generate(matrix)
                    );
                }
                */
            }
        );
    }
}
