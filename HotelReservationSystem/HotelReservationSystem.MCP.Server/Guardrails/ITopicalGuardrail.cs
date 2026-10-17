namespace HotelReservationSystem.MCP.Server.Guardrails;

public interface ITopicalGuardrail
{
    Task<TopicalGuardrailResult> EvaluateAsync(string message, CancellationToken cancellationToken = default);
}

public sealed record TopicalGuardrailResult(bool IsAllowed, string? RefusalResponse = null);
