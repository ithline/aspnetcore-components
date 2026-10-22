using Microsoft.AspNetCore.Components;

namespace Ithline.AspNetCore.Components.Tests;

[GeneratedRazorResult]
public partial class ChildComponent : ParentComponent
{
    [Parameter]
    public required int ChildA { get; set; }

    [Parameter, EditorRequired]
    public int ChildB { get; set; }

    [Parameter]
    public int ChildC { get; set; }

    public static void Run()
    {
        var c = ChildComponent.CreateComponentResult(parentA: "a", parentB: "b", childA: 0, childB: 1);
    }
}

public abstract class ParentComponent : ComponentBase
{
    [Parameter]
    public required string ParentA { get; set; }

    [Parameter, EditorRequired]
    public string ParentB { get; set; } = string.Empty;

    [Parameter]
    public string? ParentC { get; set; }
}
