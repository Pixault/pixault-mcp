using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using Pixault.Client;
using Pixault.Mcp;

var builder = Host.CreateApplicationBuilder(args);

// Configuration accepts two equivalent styles so every documented setup binds:
//   - .NET section style:  Pixault__BaseUrl, Pixault__DefaultProject, ...
//   - flat env-var style:  PIXAULT_BASE_URL, PIXAULT_PROJECT, PIXAULT_API_KEY, PIXAULT_HMAC_SECRET
string? Setting(string configKey, string envVar)
    => builder.Configuration[configKey] ?? Environment.GetEnvironmentVariable(envVar);

bool Flag(string configKey, string envVar, bool defaultValue)
    => bool.TryParse(Setting(configKey, envVar), out var v) ? v : defaultValue;

builder.Services.AddPixault(options =>
{
    options.BaseUrl = Setting("Pixault:BaseUrl", "PIXAULT_BASE_URL") ?? "https://img.pixault.io";
    options.DefaultProject = Setting("Pixault:DefaultProject", "PIXAULT_PROJECT");
    options.ApiKey = Setting("Pixault:ApiKey", "PIXAULT_API_KEY");
    options.HmacSecret = Setting("Pixault:HmacSecret", "PIXAULT_HMAC_SECRET");
});

// Agent scoping: operators may restrict what the AI assistant can do through this server.
// Defaults mirror Pixault.Client's AgentPermissions — writes on, deletes and plugin changes off.
builder.Services.AddSingleton(new AgentPermissions
{
    AllowWrite = Flag("Pixault:AllowWrite", "PIXAULT_ALLOW_WRITE", defaultValue: true),
    AllowDelete = Flag("Pixault:AllowDelete", "PIXAULT_ALLOW_DELETE", defaultValue: false),
    AllowPluginManagement = Flag("Pixault:AllowPlugins", "PIXAULT_ALLOW_PLUGINS", defaultValue: false),
});
builder.Services.AddSingleton<AgentScope>();

// Named client for raw API reads the SDK doesn't wrap (e.g. the JSON-LD document body).
// Mirrors the SDK's own client config: API base URL + X-Api-Key header.
builder.Services.AddHttpClient("PixaultApi", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PixaultOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    if (!string.IsNullOrEmpty(options.ApiKey))
        client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly()
    .WithResourcesFromAssembly();

await builder.Build().RunAsync();
