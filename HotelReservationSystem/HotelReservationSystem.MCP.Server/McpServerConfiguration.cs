using HotelReservationSystem.MCP.Server.Extensions;
using HotelReservationSystem.MCP.Server.Guardrails;
using HotelReservationSystem.MCP.Server.Interfaces;
using HotelReservationSystem.MCP.Server.Resources;
using HotelReservationSystem.MCP.Server.Services;
using HotelReservationSystem.MCP.Server.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace HotelReservationSystem.MCP.Server;

public static class McpServerConfiguration
{
    public static IServiceCollection AddHotelMcpServer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation
            {
                Name = "Aurora Hotel Server",
                Version = "1.0.0"
            };
        })
        .AddToolsFromAssembly(typeof(ReceptionTools).Assembly)
        .AddResourcesFromAssembly(typeof(HotelInfoResources).Assembly);

        services.AddScoped<ITopicalGuardrail, TopicalGuardrail>();
        services.AddScoped<IAgentService, AgentService>();

        return services;
    }
}
