using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

namespace Ithline.AspNetCore.Components.Tests;

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
        TypedResults.Extensions.ChildComponent("abc", "def", 1, 2, childC: 3);
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

[GeneratedRazorResult<ChildComponent>]
[GeneratedRazorResult<ParentComponent>]
public static partial class ComponentExtensions
{

}
