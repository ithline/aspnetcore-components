# Ithline.AspNetCore.Components
Simple package to generate Blazor RazroComponentResult typed factory method using C# source generators.

## Installation

Add a [NuGet package](https://www.nuget.org/packages/Ithline.AspNetCore.Components) reference to the project:<br>
`<PackageReference Include="Ithline.AspNetCore.Components" Version="<version>" />`

## Usage example
```cs
using Ithline.AspNetCore.Components;

[GeneratedRazorResult]
partial class ComponentA
{
  [Parameter]
  public int Parameter1 { get; set; }
}
```
