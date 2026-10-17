using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HotelReservationSystem.Application.CQRS.Abstractions;
using HotelReservationSystem.Application.CQRS.Guests.Queries;
using HotelReservationSystem.MCP.Server.Guardrails;
using HotelReservationSystem.MCP.Server.Interfaces;
using HotelReservationSystem.MCP.Server.Tools;
using HotelReservationSystem.MCP.Server.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using OpenAI.Chat;

namespace HotelReservationSystem.MCP.Server.Services;

public record ChatMessageDto(string Role, string Content);

public sealed class AgentService : IAgentService
{
    private readonly ReceptionTools _receptionTools;
    private readonly GuestTools _guestTools;
    private readonly ChatClient _chatClient;
    private readonly string _systemPrompt;
    private readonly List<ChatTool> _tools;
    private readonly IDistributedCache _cache;
    private readonly IHttpContextAccessor _httpContext;
    private readonly ICQRSMediator _mediator;
    private readonly ITopicalGuardrail _topicalGuardrail;

    public AgentService(
        ReceptionTools receptionTools,
        GuestTools guestTools,
        ChatClient chatClient,
        IDistributedCache cache,
        IHttpContextAccessor httpContext,
        ICQRSMediator mediator,
        ITopicalGuardrail topicalGuardrail
        )
    {
        _guestTools = guestTools;
        _receptionTools = receptionTools;
        _chatClient = chatClient;
        _cache = cache;
        _httpContext = httpContext;
        _mediator = mediator;
        _topicalGuardrail = topicalGuardrail;

        _systemPrompt = McpServerUtils.LoadPromptFromYaml("AuroraBaseV1.yaml");


        _tools = McpServerUtils.GenerateToolsFrom<ReceptionTools>();
        _tools.AddRange(McpServerUtils.GenerateToolsFrom<GuestTools>());
    }

    /// <summary>
    /// Executes the available tools for chatbot by reading the props from Json Document
    /// and implement these parameters to dedicated tool.
    /// </summary>
    /// <param name="toolCall"></param>
    /// <returns>The relevant tool for chat message request</returns>
    private async Task<string> ExecuteToolAsync(ChatToolCall toolCall, CancellationToken cancellationToken)
    {
        try
        {
            using JsonDocument args = JsonDocument.Parse(toolCall.FunctionArguments);
            JsonElement root = args.RootElement;

            return toolCall.FunctionName switch
            {
                "notify_staff" => await _receptionTools.NotifyStaffAsync(
                    root.GetProperty("roomId").GetInt32(),
                    root.GetProperty("message").GetString()!,
                    root.GetProperty("category").GetString()!,
                    cancellationToken),

                "book_room" => await _receptionTools.BookRoomAsync(
                    DateTime.Parse(root.GetProperty("arrival").GetString()!),
                    DateTime.Parse(root.GetProperty("departure").GetString()!),
                    root.GetProperty("roomId").GetInt32(),
                    root.GetProperty("guests").GetInt32(),
                    cancellationToken),

                "search_available_rooms" => await _guestTools.SearchAvailableRoomsAsync(
                    DateTime.Parse(root.GetProperty("arrival").GetString()!),
                    DateTime.Parse(root.GetProperty("departure").GetString()!),
                    root.GetProperty("guests").GetInt32(),
                    cancellationToken),

                "save_guest_preference" => await _guestTools.SavePreferenceAsync(
                    root.GetProperty("category").GetString()!,
                    root.GetProperty("value").GetString()!,
                    cancellationToken
                ),

                "get_my_reservations" => await _guestTools.GetMyReservationsAsync(cancellationToken),

                _ => $"Nieznane narzędzie: {toolCall.FunctionName}"
            };
        }
        catch (Exception ex)
        {
            return $"Błąd wykonania narzędzia {toolCall.FunctionName}: {ex.Message}";
        }
    }

    /// <summary>
    /// Map chat data transfer object to OpenAI chat message class
    /// depends on chat role
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>OpenAI chat message role</returns>
    private ChatMessage MapToOpenAiMessage(ChatMessageDto dto)
    {
        return dto.Role.ToLowerInvariant() switch
        {
            "system" => new SystemChatMessage(dto.Content),
            "assistant" => new AssistantChatMessage(dto.Content),
            "user" => new UserChatMessage(dto.Content),
            _ => new UserChatMessage(dto.Content)
        };
    }

    /// <summary>
    /// Process the chat bot answer for user's request
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="message"></param>
    /// <returns>The answer based on user's request</returns>
    public async Task<string> ProcessMessageAsync(Guid sessionId, string message, CancellationToken cancellationToken)
    {
        TopicalGuardrailResult guardrailResult = await _topicalGuardrail.EvaluateAsync(message, cancellationToken);
        if (!guardrailResult.IsAllowed)
        {
            return guardrailResult.RefusalResponse ?? TopicalGuardrail.DefaultRefusal;
        }

        string cacheKey = $"session_{sessionId}";
        List<ChatMessageDto> historyDto = new();

        string? cachedHistory = await _cache.GetStringAsync(cacheKey, cancellationToken);

        if (!string.IsNullOrEmpty(cachedHistory))
        {
            historyDto = JsonSerializer.Deserialize<List<ChatMessageDto>>(cachedHistory) ?? new();
        }
        else
        {
            string personalizedSystemPrompt = _systemPrompt;

            string? email = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

            if (!string.IsNullOrEmpty(email))
            {
                var preferences = await _mediator.SendAsync(new GetGuestPreferenceQuery(email), cancellationToken);

                if (preferences.Any())
                {
                    var memoryBuilder = new StringBuilder();
                    memoryBuilder.AppendLine("\n--- Pamięć długoterminowa gościa ---");
                    memoryBuilder.AppendLine(@"Wykorzystaj te informacje, aby spersonalizować obsługę.Jeśli gość rezerwuje pokój lub zamawia jedzenie, od razu weź pod uwagę te fakty:");

                    foreach (var pref in preferences)
                    {
                        memoryBuilder.AppendLine($"- [{pref.Category.ToUpper()}]: {pref.Value}");
                    }
                    memoryBuilder.AppendLine("-----------------------------------------");

                    personalizedSystemPrompt += memoryBuilder.ToString();
                }
            }

            historyDto.Add(new ChatMessageDto("system", personalizedSystemPrompt));

        }

        historyDto.Add(new ChatMessageDto("user", message));

        List<ChatMessage> openAiMessages = historyDto.Select(MapToOpenAiMessage).ToList();

        ChatCompletionOptions options = new()
        {
            Temperature = 0.2f,
        };

        foreach (var tool in _tools)
        {
            options.Tools.Add(tool);
        }

        string finalAnswer = "Przepraszam, wystąpił błąd.";
        const int maxToolRounds = 5;

        for (int i = 0; i < maxToolRounds; i++)
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(openAiMessages, options, cancellationToken);

            if (completion.FinishReason == ChatFinishReason.ToolCalls)
            {
                openAiMessages.Add(new AssistantChatMessage(completion));

                foreach (ChatToolCall toolCall in completion.ToolCalls)
                {
                    string toolResult = await this.ExecuteToolAsync(toolCall, cancellationToken);
                    openAiMessages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                }
            }
            else
            {
                finalAnswer = completion.Content[0].Text ?? finalAnswer;
                historyDto.Add(new ChatMessageDto("assistant", finalAnswer));
                break;
            }
        }

        if (historyDto.Count > 15)
        {
            // Skip(0) because its system prompt message
            var oldMessages = historyDto.Skip(1).Take(8).ToList();

            string chatLogToSummarize = string.Join("\n", oldMessages.Select(m => $"{m.Role}: {m.Content}"));

            string summaryPrompt = $"Streszcz w jednym krótkim zdaniu najważniejsze fakty z tej rozmowy, zachowując kontekst ustaleń: \n{chatLogToSummarize}";
            var summary = await _chatClient.CompleteChatAsync(new[] { new SystemChatMessage(summaryPrompt) });

            historyDto.RemoveRange(1, 8);

            historyDto.Insert(1, new ChatMessageDto("system", $"[Streszczenie wcześniejszej części rozmowy]: {summary.Value.Content[0].Text}"));
        }

        var cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(60)
        };

        string updatedHistoryJson = JsonSerializer.Serialize(historyDto);
        await _cache.SetStringAsync(cacheKey, updatedHistoryJson, cacheOptions, cancellationToken);

        return finalAnswer;
    }
}
