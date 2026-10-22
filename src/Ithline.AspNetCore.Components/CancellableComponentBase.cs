using Microsoft.AspNetCore.Components;

namespace Ithline.AspNetCore.Components;

/// <summary>
/// A base class for components that need to support a <see cref="CancellationToken"/> based lifetime.
/// </summary>
public abstract class CancellableComponentBase : ComponentBase, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="CancellableComponentBase"/> class.
    /// </summary>
    protected CancellableComponentBase()
    {
    }

    /// <inheritdoc/>
    protected sealed override Task OnInitializedAsync()
    {
        return this.OnInitializedAsync(_cts.Token);
    }

    /// <summary>
    /// Method invoked when the component is ready to start, having received its initial parameters from its parent in the render tree. Override this method if you will perform an asynchronous operation and want the component to refresh when that operation is completed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    protected virtual Task OnInitializedAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task OnParametersSetAsync()
    {
        return this.OnParametersSetAsync(_cts.Token);
    }

    /// <summary>
    /// Method invoked when the component has received parameters from its parent in the render tree, and the incoming values have been assigned to properties.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    protected virtual Task OnParametersSetAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected sealed override Task OnAfterRenderAsync(bool firstRender)
    {
        return this.OnAfterRenderAsync(firstRender, _cts.Token);
    }

    /// <summary>
    /// Method invoked after each time the component has been rendered interactively and the UI has finished updating (for example, after elements have been added to the browser DOM). Any <see cref="ElementReference"/> fields will be populated by the time this runs. This method is not invoked during prerendering or server-side rendering, because those processes are not attached to any live browser DOM and are already complete before the DOM is updated. Note that the component does not automatically re-render after the completion of any returned <see cref="Task"/>, because that would cause an infinite render loop.
    /// </summary>
    /// <param name="firstRender">Set to true if this is the first time <see cref="ComponentBase.OnAfterRender(bool)"/> has been invoked on this component instance; otherwise false.</param>
    /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
    /// <returns>A <see cref="Task"/> representing any asynchronous operation.</returns>
    protected virtual Task OnAfterRenderAsync(bool firstRender, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _cts.Cancel();
            _disposedValue = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
