# Ithline.AspNetCore.Components
Simple package to generate Blazor RazorComponentResult typed factory methods using C# source generators.

## Installation

Add a [NuGet package](https://www.nuget.org/packages/Ithline.AspNetCore.Components) reference to the project:<br>
`<PackageReference Include="Ithline.AspNetCore.Components" Version="<version>" />`

## Usage example
```cs
using Ithline.AspNetCore.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;

// define a component
class ComponentA : ComponentBase
{
  [Parameter]
  public int Parameter1 { get; set; }
}

// add component factory method to a class
[GeneratedRazorResult<ComponentA>]
static partial class TypedResultExtensions
{
}

// consume factory method
class Consumer
{
  void Consume()
  {
    TypedResults.Extensions.ComponentA(10);
  }
}
```
