using GoldMeridian.Mathematics.SourceGen.Emitters;
using Microsoft.CodeAnalysis;

namespace GoldMeridian.Mathematics.SourceGen;

[Generator]
public sealed class Generator : IIncrementalGenerator
{
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        VectorDefinitionEmitter.Register(context);
        // VectorBindingEmitter.Register(context);

        context.RegisterPostInitializationOutput(
            ctx =>
            {
                /*
                foreach (var vector in TypeSpecs.Vectors)
                {
                    ctx.AddSource(
                        $"{vector.Name}.g.cs",
                        VectorEmitter.Generate(vector)
                    );

                    ctx.AddSource(
                        $"{vector.Name}.interfaces.g.cs",
                        VectorInterfaceEmitter.Generate(vector)
                    );
                }
                */

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
