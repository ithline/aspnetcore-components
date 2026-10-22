using Microsoft.CodeAnalysis;

namespace Ithline.AspNetCore.Components;

internal static class DiagnosticDescriptors
{
    private const string Category = "Performance";

    public static DiagnosticDescriptor LimitedSourceGeneration { get; } = new DiagnosticDescriptor(
        id: "ITH0001",
        title: "Source generator limitation reached.",
        messageFormat: "Source generator limitation reached.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RequiredTypesMissing { get; } = new DiagnosticDescriptor(
        id: "ITH0002",
        title: "Required types are not present.",
        messageFormat: "Required types are not present.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClassMustBeStaticPartial { get; } = new DiagnosticDescriptor(
        id: "ITH0100",
        title: "Class must be static and partial.",
        messageFormat: "Class must be static and partial.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClassCannotBeNested { get; } = new DiagnosticDescriptor(
        id: "ITH0101",
        title: "Class cannot be nested.",
        messageFormat: "Class cannot be nested.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ClassMustHaveValidName { get; } = new DiagnosticDescriptor(
        id: "ITH0102",
        title: "Class must have valid name.",
        messageFormat: "Class must have valid name.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeIsNotValidComponent { get; } = new DiagnosticDescriptor(
        id: "ITH0104",
        title: "Type is not valid for a component.",
        messageFormat: "Type is not valid for a component.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor Dummy { get; } = new DiagnosticDescriptor(
        id: "ITH9999",
        title: "Dummy.",
        messageFormat: "Dummy.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
