using HotelReservationSystem.MCP.Server.Utils;
using OpenAI.Chat;

namespace HotelReservationSystem.MCP.Server.Guardrails;

public sealed class TopicalGuardrail : ITopicalGuardrail
{
    private readonly ChatClient _chatClient;
    private readonly string _judgePrompt;

    public const string DefaultRefusal =
        "Przepraszam, jestem wirtualną recepcjonistką Hotelu Aurora i odpowiadam wyłącznie na pytania związane z naszym hotelem, pobytem oraz rezerwacjami. W czym mogę pomóc w kwestii Hotelu Aurora?";

    public TopicalGuardrail(ChatClient chatClient)
    {
        _chatClient = chatClient;
        _judgePrompt = McpServerUtils.LoadPromptFromYaml("TopicalGuardrailV1.yaml");
    }

    public async Task<TopicalGuardrailResult> EvaluateAsync(string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new TopicalGuardrailResult(true);
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(_judgePrompt),
            new UserChatMessage(message)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.0f
        };

        try
        {
            ChatCompletion completion = await _chatClient.CompleteChatAsync(messages, options, cancellationToken);
            string judgement = completion.Content[0].Text?.Trim().ToUpperInvariant() ?? string.Empty;

            if (judgement.Contains("OFF_TOPIC"))
            {
                return new TopicalGuardrailResult(false, DefaultRefusal);
            }

            return new TopicalGuardrailResult(true);
        }
        catch
        {
            return new TopicalGuardrailResult(true);
        }
    }
}
