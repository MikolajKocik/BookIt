using System.ClientModel;
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

string openAiProvider = builder.Configuration["OpenAI:Provider"]
    ?? builder.Configuration["OPENAI_PROVIDER"]
    ?? "OpenAI";
string openAiModel = builder.Configuration["OpenAI:Model"]
    ?? builder.Configuration["OPENAI_MODEL"]
    ?? "gpt-4o-mini";

if (openAiProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
{
    var options = new OpenAIClientOptions
    {
        Endpoint = new Uri("http://host.docker.internal:11434/v1")
    };
    var client = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama"), options);
    builder.Services.AddSingleton(client.GetChatClient(openAiModel));
}
else if (openAiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
         openAiProvider.Equals("Google", StringComparison.OrdinalIgnoreCase) ||
         (!string.IsNullOrWhiteSpace(builder.Configuration["Gemini:ApiKey"]) && string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"])) ||
         (!string.IsNullOrWhiteSpace(builder.Configuration["GEMINI_API_KEY"]) && string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"])))
{
    string geminiKey = builder.Configuration["Gemini:ApiKey"]
        ?? builder.Configuration["GEMINI_API_KEY"]
        ?? builder.Configuration["OpenAI:ApiKey"]
        ?? throw new InvalidOperationException("Gemini / Google API Key not found in configuration ('Gemini:ApiKey' or 'GEMINI_API_KEY')");

    string geminiModel = builder.Configuration["Gemini:Model"]
        ?? builder.Configuration["GEMINI_MODEL"]
        ?? (!string.IsNullOrWhiteSpace(openAiModel) && !openAiModel.StartsWith("gpt", StringComparison.OrdinalIgnoreCase) ? openAiModel : "gemini-2.5-flash");

    var options = new OpenAIClientOptions
    {
        Endpoint = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/")
    };
    var client = new OpenAIClient(new ApiKeyCredential(geminiKey), options);
    builder.Services.AddSingleton(client.GetChatClient(geminiModel));
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
