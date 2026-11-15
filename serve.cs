// Previews the built site at http://localhost:5317/stream/, the same path it has on GitHub Pages.
// Usage: dotnet run serve.cs [folder] [port]. The folder defaults to _site, next to this file.
#:sdk Microsoft.NET.Sdk.Web
#:property PublishAot=false

using Microsoft.Extensions.FileProviders;

var repo = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? ".";
var root = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine(repo, "_site"));
var files = new PhysicalFileProvider(root);

var app = WebApplication.CreateBuilder().Build();
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files, RequestPath = "/stream" });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = files,
    RequestPath = "/stream",
    // Always the latest version while editing.
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-store",
});
app.MapGet("/", () => Results.Redirect("/stream/"));

var port = args.Length > 1 ? int.Parse(args[1]) : 5317;
Console.WriteLine($"Serving {root} at http://localhost:{port}/stream/");
app.Run($"http://localhost:{port}");
