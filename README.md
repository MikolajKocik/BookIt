# BookIt

BookIt is a modern hotel reservation system, scalable application built on .NET 8, designed for comprehensive management of hotel industry processes. The project combines Clean Architecture with the performance of the CQRS pattern and an innovative approach to AI integration via the Model Context Protocol (MCP).

---

## What problem does this project solve?

Modern hospitality requires speed, reliability, and automation. Traditional systems often suffer from a lack of flexibility and difficulty integrating with modern tools. BookIt addresses these challenges by:

1. Automating the reservation cycle: Eliminates manual errors from the moment of room selection, through discount calculation, up to payment finalization.
2. Financial security: Integration with Stripe ensures secure transaction processing, handling of refunds, and webhooks without the need to store sensitive payment data.
3. Staff work optimization: Thanks to the notification system (Discord integration) and dedicated background workers, staff are informed about key events in real-time.
4. AI Readiness (Future-Proof): Implementing MCP (Model Context Protocol) allows interaction with the system through intelligent AI agents, opening the way for modern customer service and automated reception.
5. Scalability and maintainability: By using Clean Architecture and CQRS, the system is easy to expand and test, which is crucial in a dynamically changing business environment.

---

## Key Features

- Reservation Management: Full CRUD process, dynamic room availability checking, discount code system.
- Stripe Integration: Support for Checkout sessions, Payment Intents, and automatic webhook processing.
- Review System: Ability for guests to leave reviews after their stay.
- Reporting: Generating advanced reports regarding occupancy and revenue.
- Background Workers: Asynchronous processing of tasks (e.g., reservation queue, notification delivery).
- AI MCP Server: Dedicated MCP server enabling Large Language Models (LLMs) to execute tools (e.g., notify_staff, book_room).

![chatbot-animals-response](docs/chatbot-animals-response.PNG)

---

## Video

[![Watch the video](https://img.youtube.com/vi/Fi9cs86jCNw/maxresdefault.jpg)](https://youtu.be/Fi9cs86jCNw)

---

## Architecture and Patterns

The project is designed with software engineering best practices in mind:

- Clean Architecture: Clear separation into layers (Core, Application, Infrastructure, Web) ensures independence from frameworks and databases.
- CQRS (Command Query Responsibility Segregation): Separation of read and write operations increases performance and business logic readability.
- DDD (Domain-Driven Design): Business logic is concentrated within domain entities, ensuring consistency and correctness of processes.
- Repository Pattern: Abstraction of the data access layer facilitates testing and replacement of database providers.
- Mediator Pattern: Decomposition of dependencies in the application layer using MediatR-style dispatching.

---

## Request Flow (AI Agent -> Reception)

One of the most innovative elements of the system is the automation of communication between the guest (served by AI) and the hotel staff. Example request flow:

![Staff Notification Flow](docs/Staff%20Notification%20Flow.PNG)

1. Interaction: A guest asks the AI Agent (e.g., via chat) for extra towels.
2. Tool Invocation: The AI Agent, using the MCP protocol, invokes the `notify_staff` tool provided by the MCP server.
3. Processing: The MCP server in the `HotelReservationSystem.MCP.Server` layer receives the request and forwards it to the `ReceptionTools` service.
4. External Integration: The system builds a message and sends it asynchronously to a dedicated Discord channel using `DiscordClient`.
5. Staff Reaction: The reception or housekeeping staff receives a real-time notification on Discord and can immediately respond to the guest's request.

---

## Tech Stack

- Backend: ASP.NET Core 8, C#
- Persistence: Entity Framework Core, SQL Server
- Payments: Stripe API
- AI/LLM Integration: Model Context Protocol (MCP)
- Messaging: Discord API
- Testing: xUnit, Moq
- Frontend: ASP.NET Core MVC (Razor)

---

## Quick Start

1. Clone and Build:
   ```bash
   dotnet restore "HotelReservationSystem/HotelReservationSystem.sln"
   dotnet build "HotelReservationSystem/HotelReservationSystem.sln"
   ```

2. Database:
   Ensure you have a Connection String configured in appsettings.json, then run:
   ```bash
   dotnet ef database update --project HotelReservationSystem/HotelReservationSystem.Infrastructure --startup-project HotelReservationSystem/HotelReservationSystem.Web
   ```

3. Run:
   ```bash
   dotnet run --project HotelReservationSystem/HotelReservationSystem.Web
   ```
