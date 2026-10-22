using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Ithline.AspNetCore.Components.Generator;

[Generator]
public sealed class RazorResultGenerator : IIncrementalGenerator
{
    private static readonly string _generatedCodeAttribute = $"GeneratedCodeAttribute(\"{typeof(RazorResultGenerator).Assembly.GetName().Name}\", \"{typeof(RazorResultGenerator).Assembly.GetName().Version}\")";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if LAUNCH_DEBUGGER
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Launch();
            }
#endif

        var results = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: Constants.GeneratedRazorComponentAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: GetComponentSyntaxOrFailureDiagnostic)
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(results, static (ctx, results) =>
        {
            var onlyFailures = true;
            foreach (var obj in results)
            {
                if (obj is DiagnosticData d)
                {
                    ctx.ReportDiagnostic(d.ToDiagnostic());
                }
                else
                {
                    onlyFailures = false;
                }
            }

            if (onlyFailures)
            {
                return;
            }

            using var sw = new StringWriter();
            using var cw = new CodeWriter(sw, 0);

            cw.WriteLine(Constants.FileHeader);

            foreach (var obj in results)
            {
                if (obj is not ComponentSyntax componentSyntax)
                {
                    continue;
                }

                cw.WriteLine();
                EmitComponentSyntax(cw, componentSyntax);
            }

            ctx.AddSource("GeneratedRazorResults.g.cs", sw.ToString());
        });
    }

    /// <summary>
    /// Returns null if nothing to do, <see cref="DiagnosticData"/> if there's an error to report,
    /// or <see cref="ComponentSyntax"/> if the type was analyzed successfully.
    /// </summary>
    private static object? GetComponentSyntaxOrFailureDiagnostic(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var targetSyntax = (ClassDeclarationSyntax)context.TargetNode;
        var targetAttribute = context.Attributes[0];

        var semanticModel = context.SemanticModel;
        var compilation = semanticModel.Compilation;

        if (compilation is not CSharpCompilation { LanguageVersion: > LanguageVersion.CSharp11 })
        {
            return DiagnosticData.Create(DiagnosticDescriptors.LimitedSourceGeneration, targetSyntax);
        }

        var symbolRazorComponentResult = compilation.GetBestTypeByMetadataName(Constants.RazorComponentResultOfT);
        var symbolParameterAttribute = compilation.GetBestTypeByMetadataName(Constants.ParameterAttribute);
        var symbolEditorRequiredAttribute = compilation.GetBestTypeByMetadataName(Constants.EditorRequiredAttribute);
        var symbolDictionary = compilation.GetBestTypeByMetadataName(Constants.Dictionary);

        if (symbolRazorComponentResult is null
            || symbolParameterAttribute is null
            || symbolEditorRequiredAttribute is null
            || symbolDictionary is null)
        {
            // required types aren't available
            return null;
        }

        if (context.TargetSymbol is not INamedTypeSymbol targetSymbol)
        {
            return null;
        }

        if (!Helpers.IsPartial(targetSyntax))
        {
            return DiagnosticData.Create(DiagnosticDescriptors.ClassMustBePartial, targetSyntax);
        }

        if (targetSymbol.ContainingType is not null)
        {
            return DiagnosticData.Create(DiagnosticDescriptors.ClassCannotBeNested, targetSyntax);
        }

        if (targetSymbol.IsAbstract)
        {
            return DiagnosticData.Create(DiagnosticDescriptors.ClassCannotBeAbstract, targetSyntax);
        }

        List<ComponentParameter> parameters = [];
        foreach (var propertySymbol in targetSymbol
            .EnumerateMembers(includeParent: true)
            .OfType<IPropertySymbol>())
        {
            if (propertySymbol.IsStatic || propertySymbol.IsReadOnly)
            {
                continue;
            }

            var parameterAttribute = false;
            var editorRequiredAttribute = false;
            foreach (var attributeData in propertySymbol.GetAttributes())
            {
                parameterAttribute |= SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbolParameterAttribute);
                editorRequiredAttribute |= SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, symbolEditorRequiredAttribute);
            }

            // parameter attribute not found, value is not considered an attribute
            if (!parameterAttribute)
            {
                continue;
            }

            var propertyTypeRef = TypeRef.Create(propertySymbol.Type);

            parameters.Add(new ComponentParameter
            {
                PropertyName = propertySymbol.Name,
                ArgumentName = Helpers.ToCamelCase(propertySymbol.Name),
                Type = propertyTypeRef,
                IsRequired = propertySymbol.IsRequired || editorRequiredAttribute,
            });
        }

        var returnTypeRef = TypeRef.Create(symbolRazorComponentResult.Construct(targetSymbol));
        return new ComponentSyntax
        {
            TypeName = Helpers.GetTypeName(targetSyntax),
            Namespace = Helpers.GetNamespace(targetSyntax),
            ReturnType = returnTypeRef,

            StatusCode = targetAttribute.TryGetNamedArgumentValue<int>("StatusCode", out var statusCode) && statusCode is > 0 ? statusCode : null,
            ContentType = targetAttribute.TryGetNamedArgumentValue<string?>("ContentType", out var contentType) ? contentType : null,
            Parameters = [.. parameters],

            DictionaryStringObject = TypeRef.Create(symbolDictionary.Construct(
                [compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Object)],
                [NullableAnnotation.NotAnnotated, NullableAnnotation.Annotated]))
        };
    }

    private static void EmitComponentSyntax(CodeWriter writer, ComponentSyntax syntax)
    {
        if (!string.IsNullOrWhiteSpace(syntax.Namespace))
        {
            writer.WriteLine($"namespace {syntax.Namespace}");
            writer.StartBlock();
        }

        writer.WriteLine($"partial class {syntax.TypeName}");
        writer.StartBlock();

        // emit signature
        var parameterCount = 0;
        writer.WriteLine($"[global::System.CodeDom.Compiler.{_generatedCodeAttribute}]");
        writer.Write($"public static {syntax.ReturnType} CreateComponentResult(");

        writer.Indent++;
        foreach (var p in syntax.Parameters.OrderBy(t => t.IsRequired ? 0 : 1))
        {
            if (parameterCount > 0)
            {
                writer.Write(",");
            }
            writer.WriteLine();

            writer.Write($"{p.Type} {p.ArgumentName}");
            if (!p.IsRequired)
            {
                writer.Write(" = default");
            }

            parameterCount++;
        }
        writer.WriteLine(")");
        writer.Indent--;

        // emit body
        writer.StartBlock();

        string? dictionaryName = null;
        if (parameterCount > 0)
        {
            dictionaryName = "temp0";
            writer.WriteLine($"{syntax.DictionaryStringObject} {dictionaryName} = new {syntax.DictionaryStringObject}();");

            foreach (var p in syntax.Parameters)
            {
                if (!p.IsRequired)
                {
                    writer.WriteLine(p.Type.IsNullAssignable()
                        ? $"if ({p.ArgumentName} is not null)"
                        : $"if ({p.ArgumentName} != default)");
                    writer.StartBlock();
                }

                writer.WriteLine($"""{dictionaryName}["{p.PropertyName}"] = {p.ArgumentName};""");

                if (!p.IsRequired)
                {
                    writer.EndBlock();
                }
            }
        }

        writer.Write($"return new {syntax.ReturnType}({dictionaryName})");

        if (syntax.StatusCode is null && syntax.ContentType is null)
        {
            writer.WriteLine(";");
        }
        else
        {
            writer.WriteLine();
            writer.StartBlock();
            if (syntax.StatusCode is not null)
            {
                writer.WriteLine($"StatusCode = {syntax.StatusCode},");
            }
            if (syntax.ContentType is not null)
            {
                writer.WriteLine($"""ContentType = "{syntax.ContentType}",""");
            }
            writer.EndBlockWithSemicolon();
        }

        writer.EndBlock();
        // end body

        writer.EndBlock();
        //end class

        if (!string.IsNullOrWhiteSpace(syntax.Namespace))
        {
            writer.EndBlock();
        }
    }

    private sealed record ComponentSyntax
    {
        public required string TypeName { get; init; }
        public required string? Namespace { get; init; }
        public required TypeRef ReturnType { get; init; }

        public required int? StatusCode { get; init; }
        public required string? ContentType { get; init; }
        public required EquatableArray<ComponentParameter> Parameters { get; init; }

        public required TypeRef DictionaryStringObject { get; init; }
    }

    private sealed record ComponentParameter
    {
        public required string PropertyName { get; init; }
        public required string ArgumentName { get; init; }
        public required TypeRef Type { get; init; }
        public required bool IsRequired { get; init; }
    }
}
