using HotelReservationSystem.Application.Extensions;
using HotelReservationSystem.Infrastructure.Data;
using HotelReservationSystem.Infrastructure.Data.Extensions;
using HotelReservationSystem.Infrastructure.Extensions;
using HotelReservationSystem.MCP.Server;
using HotelReservationSystem.Web.Configuration;
using HotelReservationSystem.Web.Extensions;
using HotelReservationSystem.Web.Filters;
using HotelReservationSystem.Web.Middleware.MiddlewareExtensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);


var dpBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("HotelAuroraApp");

var keysPath = builder.Configuration["DataProtectionKeysPath"];

if (!string.IsNullOrEmpty(keysPath))
{
    dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddScoped<PromptInjectionFilter>();
builder.Services.Configure<StaffSettings>(builder.Configuration.GetSection("StaffSettings"));

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddWebServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();

string openAiProvider = builder.Configuration["OpenAI:Provider"] ?? "OpenAI";
string openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-4o-mini";

if (openAiProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    var options = new OpenAIClientOptions
    {
        Endpoint = new Uri("http://host.docker.internal:11434/v1")
    };
    var client = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama"), options);
    builder.Services.AddSingleton(client.GetChatClient(openAiModel));
}
else
{
    string openAiKey = builder.Configuration["OpenAI:ApiKey"]
        ?? throw new InvalidOperationException("OpenAI Key not found in configuration");
    builder.Services.AddSingleton(new ChatClient(openAiModel, openAiKey));
}

builder.Services.AddHotelMcpServer(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    try
    {
        context.Database.Migrate();
        context.Seed();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during database migration/seeding: {ex.Message}");
        throw;
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedRolesAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during role seeding: {ex.Message}");
        throw;
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseErrorHandlingMiddleware();
app.UseCultureHandling();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();

public partial class Program { }
