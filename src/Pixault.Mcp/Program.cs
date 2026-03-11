using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using Pixault.Client;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPixault(options =>
{
    options.BaseUrl = builder.Configuration["Pixault:BaseUrl"] ?? "https://img.pixault.io";
    options.DefaultProject = builder.Configuration["Pixault:DefaultProject"];
    options.ApiKey = builder.Configuration["Pixault:ApiKey"];
    options.HmacSecret = builder.Configuration["Pixault:HmacSecret"];
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
