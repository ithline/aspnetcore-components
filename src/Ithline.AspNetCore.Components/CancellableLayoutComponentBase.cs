using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;

namespace Ithline.AspNetCore.Components;

/// <summary>
/// Optional base class for components that represent a layout and need to support a <see cref="CancellationToken"/> based lifetime.
/// </summary>
public abstract class CancellableLayoutComponentBase : CancellableComponentBase
{
    /// <summary>
    /// Gets the content to be rendered inside the layout.
    /// </summary>
    [Parameter]
    public RenderFragment? Body { get; set; }

    /// <inheritdoc/>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(CancellableLayoutComponentBase))]
    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }
}
