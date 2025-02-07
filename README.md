# WebForms on ASP.NET Core

The goal of this project is to explore building some of the basic building blocks of the WebForms on ASP.NET Core. This will isolate out the actual components needed to build a functional page.

Supported so far:

- `System.Web.IHttpHandler`
- `System.Web.UI.Page`
- `System.Web.UI.HtmlTextWriter`
- `System.Web.UI.HtmlControls.*`
- `System.Web.UI.WebControls.*`
- Master pages
- Compilation of `aspx` pages (both VB and C#)
- Binary compatibility (via the `WebForms.SystemWebShim` package) - limited support; needs testing

What is *NOT* supported:

- Designer support
- `System.Web` hosting model
- `System.Web` membership model
- Any `System.Web` concept not called out as in scope

This will make use of `Microsoft.AspNetCore.SystemWebAdapters` to provide the `System.Web.HttpContext` that is at the core of the WebForms pipeline.

## Packages

- `WebForms` - Contains the majority of the page/control/etc methods required for WebForms
- `WebForms.Compiler` - Build time compiler that will generate a `.dll` for each page in the project
- `WebForms.Compiler.Dynamic` - Run time compiler that will allow for updating `aspx` at run time and generating a new in-memory assembly
- `WebForms.HttpHandler` - Contains `IHttpHandler` and related helpers to enable hooking them up to ASP.NET Core
- `WebForms.SystemWebShim` - A package with a `System.Web.dll` assembly that will type forward to the new locations. This is build for .NET 6+ and would help with
   controls/assemblies/etc that are compiled and cannot be recompiled for some reason. If they use members/types not supported, they will throw at runtime, but can
   be a helpful step in migrating old projects (see the `TypeDumper` tool to regenerate the available types for the shim)

## Get Started

1. Add a `nuget.config` or update yours to have the ci feed:

    ```xml
    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
      <packageSources>
        <!--To inherit the global NuGet package sources remove the <clear/> line below -->
        <clear />
        <add key="nuget" value="https://api.nuget.org/v3/index.json" />
        <add key="webforms" value="https://webformsfeed.blob.core.windows.net/feed/index.json" />
      </packageSources>
    </configuration>
    ```

1. Add `WebForms` and `WebForms.Compiler` to your project
1. Add System.Web adapter services and session, i.e.

    ```cs
    builder.Services.AddSystemWebAdapters()
        .WrapAspNetCoreSession()
        .AddWebForms();
    
    builder.Services.AddSession();
    builder.Services.AddDistributedMemoryCache();
    ```

1. Add the System.Web middleware and map the webforms pages:

    ```cs
    ...
    app.UseSystemWebAdapters();
    ...
    app.MapWebForms();
    ```

1. Add any `.aspx` or `.aspx.cs`/`.aspx.vb` files to your project. They should be served up as expected when you run.

### Alternate

There is a dynamic compilation method that can be enabled by doing the following (continued from the above sample):

1. Add `WebForms.Compiler.Dynamic`
1. Add `<EnableRuntimeAspxCompilation>true</EnableRuntimeAspxCompilation>` to a property group in the project file
1. Add dynamic compilation to the services and configure where to look for the files:

    ```csharp
    builder.Services.AddSystemWebAdapters()
        ...
        .AddDynamicWebForms(options =>
        {
            options.Files = builder.Environment.ContentRootFileProvider;
        });
    ``````

## Try it out!

Clone the project and run the sample. Two pages are available:
  - `https://localhost:7226/dynamic_page.aspx` - Change the text in `dynamic_page.aspx` and watch it reload
  - `https://localhost:7226/test.aspx` - A static page that just implements `System.Web.UI.Page`

# Architecture

![Architecture](./docs/images/ui-arch.png)

## IHttpHandler/IHttpModule infrastructure

This project has some initial support for `IHttpHandler`. However, this ideally would move into the main [dotnet/systemweb-adapters](https://github.com/dotnet/systemweb-adapters) repo (see [here](https://github.com/dotnet/systemweb-adapters/tree/tasou/http-application) for what that might look like). All that's needed here is `IHttpHandler` support, although there may be a need for some aspects of `IHttpModule` that would be out of scope for this project.

## HtmlTextWriter

`HtmlTextWriter` is how custom controls would write to the response. The majority of that infrastructure was copied from [referencesource](https://referencesource.microsoft.com/#System.Web/UI/HTMLTextWriter.cs,671c476a45af082b) and is (for the most part) as-is from .NET Framework.

## Controls

The majority of pages rely on built-in controls that derive from `System.Web.UI.Controls`. This includes the collection of controls derived from `System.Web.UI.WebControl` as well as server side HTML controls (deriving from `System.Web.UI.HtmlControl`).

## Pages

Pages are the main container of a collection of user controls. This is an implementation of `System.Web.UI.Page` or the contents of an `.aspx` page. This implements `IHttpHandler` (more specifically for our implementation, we'll use `IAsyncHttpHandler` which was not used for some reason ASP.NET Framework).

To map a page to be served, you can use the following extension methods:

```csharp

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSystemWebAdapters()
    .AddDynamicPages();

var app = builder.Build();

app.UseSystemWebAdapters();

app.MapAspxPages();
app.MapAspxPage<MyPage>("/some/path.aspx");
app.MapDynamicAspxPages(app.Environment.ContentRootFileProvider);
```

Pages can by dynamically or statically served. Dynamic is similar to the process of ASP.NET Framework WebForms, while statically would be similar to using `aspnet_compiler`.

In the case of static compilation (i.e. the `Compile` process below is done at build time):

```mermaid
graph TD
    ashx[.ashx] -->|Compile| handler(IHttpHandler)
    aspx[.aspx] -->|Compile| page(Page)
    page --> handler
    handler --> endpoint(ASP.NET Core Endpoint)
```

However, in the dynamic compilation mode (i.e. an `.aspx` page is found in the directory at run time and the code is generated on the fly), it would look more like this:

```mermaid
graph TD
    watcher[IFileProvider.Watch] --> ashx
    watcher --> aspx
    ashx[.ashx] -->|Compile| handler(IHttpHandler)
    aspx[.aspx] -->|Compile| page(Page)
    page --> handler
    handler --> alc[AssemblyLoadContext]
    alc --> datasource[EndpointDataSource]
    datasource --> endpoint
```

## Compilation

Compilation in `System.Web` made use of `MSBuild` and `CodeDom` technologies and was tightly coupled to the `Page`, `Control` and other types. Going forward, this implementation will build the compilation infrastructure as a layer above the types in `System.Web.UI` namespace.

There are a few ways to handle compilation:

- **[Source Generators](./gen/UI.Generator)**: This would allow for static compilation and happen as part of the build process. This would include the pages and handlers as part of the assembly they're in instead of as additional assemblies like the other options. *NOTE: Not support for Visual Basic*
- **aspnet_compiler**: This was the entry point for compilation that customers would use to manually compile their pages into controls. This would use Roslyn behind the scenes to generate an assembly.
- **[Runtime Compilation](./src/RuntimeCompilation)**: This was the default for many people and allowed them to put an `.aspx` page in the deployed directory and have it be served up. This would utilize the same logic as `aspnet_compiler`, but also load up the assemblies in a custom unloadable AssemblyLoadContext and a custom `EndpointDataSource` that allows for updates

These can all be supported by using a single [library](./gen/AspxParser/) to parse and generated the source files. Initially with C#, but Visual Basic support is a high priority.
