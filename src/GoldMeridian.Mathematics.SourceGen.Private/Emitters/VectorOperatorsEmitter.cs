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
        EmitUnary(w, "~", ScalarCapabilities.OnesComplement, spec, bitwise: true);

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
    }

    private static void EmitUnary(
        CodeWriter w,
        string op,
        ScalarCapabilities cap,
        VectorSpec spec,
        bool bitwise = false
    )
    {
        if (!spec.Scalar.Capabilities.HasFlag(cap))
        {
            return;
        }

        var typeName = spec.Name;
        var lanes = spec.Lanes;
        var scalar = spec.Scalar.Keyword;

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {spec.Name} operator {op}({spec.Name} v)");
        w.WriteLine("{");
        {
            w.Indent();

            if (spec.Scalar.SupportsIntrinsics && bitwise)
            {
                var vecType = IntrinsicHelpers.GetIntrinsicVectorType(spec);
                w.WriteLine($"var vv = Unsafe.As<{typeName}, {vecType}<{scalar}>>(ref v);");
                w.WriteLine($"var vr = {op}vv;");
                w.WriteLine($"return Unsafe.As<{vecType}<{scalar}>, {typeName}>(ref vr);");
            }
            else
            {
                var expr = string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})({op}v.{n})"));
                w.WriteLine($"return new {typeName}({expr});");
            }

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();
    }

    private static void EmitBinary(
        CodeWriter w,
        string op,
        ScalarCapabilities cap,
        VectorSpec spec,
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
        var returnType = typeName;

        // Vector op Vector
        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {returnType} operator {op}({typeName} a, {typeName} b)");
        w.WriteLine("{");
        {
            w.Indent();

            if (spec.Scalar.SupportsIntrinsics)
            {
                var vecType = IntrinsicHelpers.GetIntrinsicVectorType(spec);

                w.WriteLine($"var va = Unsafe.As<{typeName}, {vecType}<{scalar}>>(ref a);");
                w.WriteLine($"var vb = Unsafe.As<{typeName}, {vecType}<{scalar}>>(ref b);");

                w.WriteLine($"var vr = va {op} vb;");
                w.WriteLine($"return Unsafe.As<{vecType}<{scalar}>, {typeName}>(ref vr);");
            }
            else
            {
                var expr = string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a.{n} {op} b.{n})"));

                w.WriteLine($"return new {typeName}({expr});");
            }

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();

        // Vector op Scalar

        // TODO: For intrinsics, look into just creating the vectors with the
        //       the scalar value directly instead of spreading it over our
        //       constructors.  Would probably be marginally faster...

        w.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
        w.WriteLine($"public static {typeName} operator {op}({typeName} a, {scalar} b)");
        w.WriteLine("{");
        {
            w.Indent();

            if (spec.Scalar.SupportsIntrinsics)
            {
                w.WriteLine($"var bv = new {typeName}(b);");
                w.WriteLine($"return a {op} bv;");
            }
            else
            {
                w.WriteLine($"return new {typeName}({string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a.{n} {op} b)"))});");
            }

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

            if (spec.Scalar.SupportsIntrinsics)
            {
                w.WriteLine($"var av = new {typeName}(a);");
                w.WriteLine($"return av {op} b;");
            }
            else
            {
                w.WriteLine($"return new {typeName}({string.Join(", ", LaneNames.Take(lanes).Select(n => $"({scalar})(a {op} b.{n})"))});");
            }

            w.Outdent();
        }
        w.WriteLine("}");
        w.WriteLine();
    }
}
