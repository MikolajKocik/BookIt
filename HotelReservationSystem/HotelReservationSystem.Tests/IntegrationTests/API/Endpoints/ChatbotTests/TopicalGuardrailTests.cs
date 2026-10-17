using System.Net;
using System.Net.Http.Json;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using FluentAssertions;
using HotelReservationSystem.Tests.IntegrationTests.API.Common.Fixture;
using HotelReservationSystem.Web.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace HotelReservationSystem.Tests.IntegrationTests.API.Endpoints.ChatbotTests;

public sealed class TopicalGuardrailTests : ControllerFixture
{
    public TopicalGuardrailTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }


    [Fact]
    public async Task ProcessMessage_WhenQuestionIsOffTopic_ShouldReturnRefusalWithoutCallingTools()
    {
        // Arrange
        string offTopicMsg = "Napisz skrypt w Pythonie do odwracania stringa oraz wyjaśnij teorię względności Einsteina.";
        AskRequest request = new()
        {
            Message = offTopicMsg,
            SessionId = Guid.NewGuid()
        };

        QueueServiceClient queueServiceClient = Factory.Services.GetRequiredService<QueueServiceClient>();
        QueueClient queueClient = queueServiceClient.GetQueueClient("hotel-staff-mutations");
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.ClearMessagesAsync();

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync("/Agent/Ask", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        jsonResponse.Should().NotBeNull();
        jsonResponse!.ContainsKey("answer").Should().BeTrue();

        string answer = jsonResponse["answer"];
        answer.Should().Contain("Hotelu Aurora");

        // Ensure no staff notifications were triggered for an off-topic question
        QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages: 1);
        messages.Should().BeEmpty("Narzędzia nie powinny być wywoływane dla pytań niezwiązanych z hotelem.");
    }

    [Fact]
    public async Task ProcessMessage_WhenGuestAsksAboutHotelSPA_ShouldAnswerPositivelyInDomain()
    {
        // Arrange
        string inDomainMsg = "W jakich godzinach czynny jest basen i strefa SPA w hotelu Aurora?";
        AskRequest request = new()
        {
            Message = inDomainMsg,
            SessionId = Guid.NewGuid()
        };

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync("/Agent/Ask", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jsonResponse = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        jsonResponse.Should().NotBeNull();
        jsonResponse!.ContainsKey("answer").Should().BeTrue();

        string answer = jsonResponse["answer"];
        answer.Should().NotBeNullOrWhiteSpace();
    }
}
