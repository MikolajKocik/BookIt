using FluentAssertions;
using HotelReservationSystem.MCP.Server.Utils;

namespace HotelReservationSystem.Tests.UnitTests;

public sealed class PromptLoadingUnitTests
{
    [Fact]
    public void LoadPromptFromYaml_WhenLoadingAuroraBaseV1_ShouldLoadPromptSuccessfully()
    {
        // Act
        string prompt = McpServerUtils.LoadPromptFromYaml("AuroraBaseV1.yaml");

        // Assert
        prompt.Should().NotBeNullOrWhiteSpace();
        prompt.Should().Contain("Hotelu Aurora");
        prompt.Should().Contain("TOPICAL GUARDRAIL");
        prompt.Should().Contain("Doba hotelowa");
    }

    [Fact]
    public void LoadPromptFromYaml_WhenLoadingTopicalGuardrailV1_ShouldLoadPromptSuccessfully()
    {
        // Act
        string prompt = McpServerUtils.LoadPromptFromYaml("TopicalGuardrailV1.yaml");

        // Assert
        prompt.Should().NotBeNullOrWhiteSpace();
        prompt.Should().Contain("Topical Guardrail");
        prompt.Should().Contain("OFF_TOPIC");
        prompt.Should().Contain("IN_DOMAIN");
    }
}
