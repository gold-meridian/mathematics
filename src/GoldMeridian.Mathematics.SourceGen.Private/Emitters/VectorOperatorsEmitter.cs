using System.Linq;
using GoldMeridian.Mathematics.SourceGen.Specs;
using GoldMeridian.Mathematics.SourceGen.Util;

namespace GoldMeridian.Mathematics.SourceGen.Emitters;

internal static class VectorOperatorsEmitter
{
    public static string[] LaneNames => VectorEmitter.LaneNames;

    public static void Emit(CodeWriter w, VectorSpec spec)
    {
        // Unary operators
        EmitUnary(w, "-", ScalarCapabilities.Negate, spec);
        EmitUnary(w, "+", ScalarCapabilities.Plus, spec);
        EmitUnary(w, "~", ScalarCapabilities.OnesComplement, spec);

        // Binary operators
        EmitBinary(w, "+", ScalarCapabilities.Add, spec);
        EmitBinary(w, "-", ScalarCapabilities.Subtract, spec);
        EmitBinary(w, "*", ScalarCapabilities.Multiply, spec);
        EmitBinary(w, "/", ScalarCapabilities.Divide, spec);

        EmitBinary(w, "&", ScalarCapabilities.BitwiseAnd, spec);
        EmitBinary(w, "|", ScalarCapabilities.BitwiseOr, spec);
        EmitBinary(w, "^", ScalarCapabilities.ExclusiveOr, spec);

        EmitBinary(w, "<<", ScalarCapabilities.LeftShift, spec, onlyOwningLeft: true);
        EmitBinary(w, ">>", ScalarCapabilities.RightShift, spec, onlyOwningLeft: true);
        EmitBinary(w, ">>>", ScalarCapabilities.UnsignedRightShift, spec, onlyOwningLeft: true);

        // Handled in EmitEquatable for now.
        /*
        // Equality operators
        EmitBinary(w, "==", ScalarCapabilities.Equality, spec, returnsBool: true);
        EmitBinary(w, "!=", ScalarCapabilities.Inequality, spec, returnsBool: true);
        */
    }

    private static void EmitUnary(
        CodeWriter w,
        string op,
        ScalarCapabilities cap,
        VectorSpec spec
    )
    {
        if (!spec.Scalar.Capabilities.HasFlag(cap))
        {
            return;
        }

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {spec.Name} operator {op}({spec.Name} v)");
        w.WriteLine("{");
        w.Indent();
        w.WriteLine($"return new {spec.Name}({string.Join(", ", LaneNames.Take(spec.Lanes).Select(n => $"({spec.Scalar.Keyword})({op}v.{n})"))});");
        w.Outdent();
        w.WriteLine("}");
        w.WriteLine();
    }

    private static void EmitBinary(
        CodeWriter w,
        string op,
        ScalarCapabilities cap,
        VectorSpec spec,
        bool returnsBool = false,
        bool onlyOwningLeft = false
    )
    {
        if (!spec.Scalar.Capabilities.HasFlag(cap))
        {
            return;
        }

        var typeName = spec.Name;
        var lanes = spec.Lanes;
        var scalar = spec.Scalar.Keyword;
        var returnType = returnsBool ? "bool" : typeName;

        // Vector op Vector
        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {returnType} operator {op}({typeName} a, {typeName} b)");
        w.WriteLine("{");
        {
            w.Indent();

            var expr = returnsBool
                ? string.Join(" && ", LaneNames.Take(lanes).Select(n => $"a.{n} {op} b.{n}"))
                : string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a.{n} {op} b.{n})"));

            w.WriteLine($"return {(returnsBool ? expr : $"new {typeName}({expr})")};");

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        // Vector op Scalar
        if (returnsBool)
        {
            return;
        }

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {typeName} operator {op}({typeName} a, {scalar} b)");
        w.WriteLine("{");
        {
            w.Indent();

            w.WriteLine($"return new {typeName}({string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a.{n} {op} b)"))});");

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        // Scalar op Vector
        if (onlyOwningLeft)
        {
            return;
        }

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {typeName} operator {op}({scalar} a, {typeName} b)");
        w.WriteLine("{");
        {
            w.Indent();

            w.WriteLine($"return new {typeName}({string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a {op} b.{n})"))});");

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();
    }
}
