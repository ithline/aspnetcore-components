using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Ithline.AspNetCore.Components;

/// <summary>
/// Instructs the source generator to generate a <see cref="RazorComponentResult"/> factory method.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GeneratedRazorResultAttribute<T> : Attribute where T: IComponent
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GeneratedRazorResultAttribute{T}"/> class.
    /// </summary>
    public GeneratedRazorResultAttribute()
    {
    }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response.
    /// </summary>
    public string? ContentType { get; set; }
}
