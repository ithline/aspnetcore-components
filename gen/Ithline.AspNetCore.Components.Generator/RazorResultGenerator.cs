using System.Collections.Immutable;
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

        var componentGenerationSpecs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: Constants.GeneratedRazorComponentAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: (context, ct) =>
                {
                    var parser = new Parser();
                    var spec = parser.ParseComponentGenerationSpec(context, ct);
                    var diagnostics = parser.GetDiagnostics();

                    return (spec, diagnostics);
                })
            .WithTrackingName("parsingResult");

        // Project the combined pipeline result to just the equatable model, discarding diagnostics.
        var sourceGenerationSpecs = componentGenerationSpecs.Select(static (t, _) => t.spec).WithTrackingName("componentSpecs");
        context.RegisterSourceOutput(sourceGenerationSpecs, EmitComponents);

        // Project to just the diagnostics, discarding the model. ImmutableArray<Diagnostic> does not
        // implement value equality, so Roslyn's incremental pipeline uses reference equality for these
        // values — the callback fires on every compilation change. This is by design: diagnostic
        // emission is cheap, and we need fresh SourceLocation instances that are pragma-suppressible
        // (cf. https://github.com/dotnet/runtime/issues/92509).
        // No source code is generated from this pipeline — it exists solely to report diagnostics.
        var diagnostics = componentGenerationSpecs.Select(static (t, _) => t.diagnostics).WithTrackingName("diagnostics");
        context.RegisterSourceOutput(diagnostics, EmitDiagnostics);
    }

    private static void EmitComponents(SourceProductionContext context, ComponentGenerationSpec? spec)
    {
        if (spec is null)
        {
            return;
        }

        using var sw = new StringWriter();
        using var writer = new CodeWriter(sw, 0);

        writer.WriteLine(Constants.FileHeader);

        if (!string.IsNullOrWhiteSpace(spec.Namespace))
        {
            writer.WriteLine($"namespace {spec.Namespace}");
            writer.StartBlock();
        }

        writer.WriteLine($"partial class {spec.TypeName}");
        writer.StartBlock();

        var emitLine = false;
        foreach (var syntax in spec.Components)
        {
            if (emitLine)
            {
                writer.WriteLine();
            }
            emitLine = true;

            // emit signature
            var parameterCount = 0;
            writer.WriteLine($"[global::System.CodeDom.Compiler.{_generatedCodeAttribute}]");
            writer.WriteLine($"public static {syntax.ReturnType} {syntax.ComponentName}(");

            writer.Indent++;
            writer.Write($"this {spec.ResultExtensions} _");
            foreach (var p in syntax.Parameters.OrderBy(t => t.IsRequired ? 0 : 1))
            {
                writer.WriteLine(",");

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
                writer.WriteLine($"{spec.DictionaryStringObject} {dictionaryName} = new {spec.DictionaryStringObject}();");

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
        }

        writer.EndBlock();
        if (!string.IsNullOrWhiteSpace(spec.Namespace))
        {
            writer.EndBlock();
        }

        context.AddSource($"{spec.TypeName}.g.cs", sw.ToString());
    }

    private static void EmitDiagnostics(SourceProductionContext context, ImmutableList<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private sealed class Parser
    {
        private readonly List<Diagnostic> _diagnostics = [];

        public Parser()
        {
        }

        public ComponentGenerationSpec? ParseComponentGenerationSpec(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            var semanticModel = context.SemanticModel;
            var compilation = semanticModel.Compilation;
            var targetSyntax = (ClassDeclarationSyntax)context.TargetNode;

            if (compilation is not CSharpCompilation { LanguageVersion: > LanguageVersion.CSharp11 })
            {
                this.ReportDiagnostic(DiagnosticDescriptors.LimitedSourceGeneration, targetSyntax);
                return null;
            }

            var symbolRazorComponentResult = compilation.GetBestTypeByMetadataName(Constants.RazorComponentResultOfT);
            var symbolResultExtensions = compilation.GetBestTypeByMetadataName(Constants.IResultExtensions);
            var symbolParameterAttribute = compilation.GetBestTypeByMetadataName(Constants.ParameterAttribute);
            var symbolEditorRequiredAttribute = compilation.GetBestTypeByMetadataName(Constants.EditorRequiredAttribute);
            var symbolDictionary = compilation.GetBestTypeByMetadataName(Constants.Dictionary);

            if (symbolRazorComponentResult is null
                || symbolResultExtensions is null
                || symbolParameterAttribute is null
                || symbolEditorRequiredAttribute is null
                || symbolDictionary is null)
            {
                this.ReportDiagnostic(DiagnosticDescriptors.RequiredTypesMissing, targetSyntax);
                return null;
            }

            if (context.TargetSymbol is not INamedTypeSymbol targetSymbol)
            {
                this.ReportDiagnostic(DiagnosticDescriptors.ClassMustHaveValidName, targetSyntax);
                return null;
            }

            if (!targetSymbol.IsStatic || !Helpers.IsPartial(targetSyntax))
            {
                this.ReportDiagnostic(DiagnosticDescriptors.ClassMustBeStaticPartial, targetSyntax);
                return null;
            }

            if (targetSymbol.ContainingType is not null)
            {
                this.ReportDiagnostic(DiagnosticDescriptors.ClassCannotBeNested, targetSyntax);
                return null;
            }

            var components = new List<ComponentSyntax>();
            foreach (var targetAttribute in context.Attributes)
            {
                if (targetAttribute.AttributeClass is not { TypeArguments: [var componentSymbol] } || componentSymbol is null)
                {
                    this.ReportDiagnostic(DiagnosticDescriptors.TypeIsNotValidComponent, targetAttribute);
                    continue;
                }

                List<ComponentParameter> parameters = [];
                foreach (var propertySymbol in componentSymbol
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

                var returnTypeRef = TypeRef.Create(symbolRazorComponentResult.Construct(componentSymbol));
                components.Add(new ComponentSyntax
                {
                    ComponentName = componentSymbol.Name,
                    ReturnType = returnTypeRef,
                    StatusCode = targetAttribute.TryGetNamedArgumentValue<int>("StatusCode", out var statusCode) && statusCode is > 0 ? statusCode : null,
                    ContentType = targetAttribute.TryGetNamedArgumentValue<string?>("ContentType", out var contentType) ? contentType : null,
                    Parameters = [.. parameters],
                });
            }

            return new ComponentGenerationSpec
            {
                TypeName = Helpers.GetTypeName(targetSyntax),
                Namespace = Helpers.GetNamespace(targetSyntax),
                Components = components.ToEquatableArray(),

                ResultExtensions = TypeRef.Create(symbolResultExtensions),
                DictionaryStringObject = TypeRef.Create(symbolDictionary.Construct(
                    [compilation.GetSpecialType(SpecialType.System_String), compilation.GetSpecialType(SpecialType.System_Object)],
                    [NullableAnnotation.NotAnnotated, NullableAnnotation.Annotated]))
            };
        }

        public ImmutableList<Diagnostic> GetDiagnostics()
        {
            return _diagnostics.ToImmutableList();
        }

        private void ReportDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode syntax, object? args = null)
        {
            var location = syntax.GetLocation();
            _diagnostics.Add(Diagnostic.Create(descriptor, location, args));
        }

        private void ReportDiagnostic(DiagnosticDescriptor descriptor, AttributeData attributeData, object? args = null)
        {
            var reference = attributeData.ApplicationSyntaxReference;
            var location = reference?.SyntaxTree.GetLocation(reference.Span);

            _diagnostics.Add(Diagnostic.Create(descriptor, location, args));
        }
    }

    private sealed record ComponentGenerationSpec
    {
        public required string TypeName { get; init; }
        public required string? Namespace { get; init; }
        public required EquatableArray<ComponentSyntax> Components { get; init; }

        public required TypeRef ResultExtensions { get; init; }
        public required TypeRef DictionaryStringObject { get; init; }
    }

    private sealed record ComponentSyntax
    {
        public required string ComponentName { get; init; }
        public required TypeRef ReturnType { get; init; }
        public required int? StatusCode { get; init; }
        public required string? ContentType { get; init; }
        public required EquatableArray<ComponentParameter> Parameters { get; init; }
    }

    private sealed record ComponentParameter
    {
        public required string PropertyName { get; init; }
        public required string ArgumentName { get; init; }
        public required TypeRef Type { get; init; }
        public required bool IsRequired { get; init; }
    }
}
