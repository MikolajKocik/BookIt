using System.ComponentModel;
using System.Security.Claims;
using System.Text;
using HotelReservationSystem.Application.CQRS.Abstractions;
using HotelReservationSystem.Application.CQRS.Guests.Commands;
using HotelReservationSystem.Application.CQRS.Reservations.Queries;
using HotelReservationSystem.Application.CQRS.Rooms.Queries;
using HotelReservationSystem.Application.Dtos.Reservation;
using HotelReservationSystem.Application.Dtos.Room;
using HotelReservationSystem.Core.Domain.Enums;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace HotelReservationSystem.MCP.Server.Tools;

public sealed class GuestTools
{
    private readonly ICQRSMediator _mediator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GuestTools(
        ICQRSMediator mediator,
        IHttpContextAccessor httpContextAccessor)
    {
        _mediator = mediator;
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool(Name = "search_available_rooms")]
    [Description("Wyszukuje dostępne pokoje w hotelu w podanym terminie. Użyj tego narzędzia ZAWSZE przed próbą rezerwacji (book_room), aby uzyskać ID dostępnego pokoju i przedstawić ofertę gościowi.")]
    public async Task<string> SearchAvailableRoomsAsync(
        [Description("Data przyjazdu (YYYY-MM-DD)")] DateTime arrival,
        [Description("Data wyjazdu (YYYY-MM-DD)")] DateTime departure,
        [Description("Dla ilu osób ma być pokój?")] int guests,
        CancellationToken cancellationToken)
    {
        if (guests < 0 || guests.Equals(default) || guests == 0) return "Proszę podać liczbę gości.";

        var query = new GetAvailableRoomsQuery(arrival, departure);
        IQueryable<RoomDto> availableRooms = await _mediator.SendAsync(query, cancellationToken);

        if (!availableRooms.Any())
        {
            return "Brak dostępnych pokoi w tym terminie. Poinformuj o tym gościa i zaproponuj zmianę daty.";
        }
        
        var responseBuilder = new StringBuilder("Znaleziono następujące wolne pokoje:\n");

        int totalNights = (int)(departure.Date - arrival.Date).TotalDays;

        foreach (var room in availableRooms)
        {
            decimal totalPrice = room.PricePerNight * totalNights;
            responseBuilder.AppendLine(
                @$"- ID: {room.Id} | Typ pokoju: {(guests > 2 ? RoomType.Double : RoomType.Single)} | Cena za noc: {room.PricePerNight} PLN | Łączna cena za {totalNights} noc/nocy: {totalPrice} PLN");
        }

        return responseBuilder.ToString();
    }

    [McpServerTool(Name = "get_my_reservations")]
    [Description("Pobiera listę aktualnych rezerwacji dla gościa, z którym rozmawiasz. Używaj tego ZAWSZE, gdy gość pyta o szczegóły swojego pobytu, daty, przydzielony pokój lub status płatności.")]
    public async Task<string> GetMyReservationsAsync(CancellationToken cancellationToken)
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            return "BŁĄD: Użytkownik nie jest zalogowany. Poinformuj go, że musi się zalogować, aby przeglądać swoje rezerwacje.";
        }

        string email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity.Name!;

        try
        {
            var query = new GetReservationsByGuestEmailQuery(email);
            
            IEnumerable<ReservationDto> reservations = await _mediator.SendAsync(query, cancellationToken);

            if (!reservations.Any())
            {
                return "System: Ten gość nie ma obecnie żadnych zapisanych rezerwacji.";
            }

            var responseBuilder = new StringBuilder("System: Znaleziono następujące rezerwacje tego gościa:\n");
            
            foreach (var res in reservations)
            {
                responseBuilder.AppendLine($"- ID Rezerwacji: {res.Id}");
                 
                if (res.RoomNumber != null && !res.RoomType.Equals(default))
                {
                    responseBuilder.AppendLine($" Numer pokoju: {res.RoomNumber} (Typ: {res.RoomType})");
                }
                
                responseBuilder.AppendLine($"  Przyjazd: {res.ArrivalDate:yyyy-MM-dd}");
                responseBuilder.AppendLine($"  Wyjazd: {res.DepartureDate:yyyy-MM-dd}");
                
                if (res.PaymentStatus != null)
                {
                    responseBuilder.AppendLine($"  Status płatności: {res.PaymentStatus}");
                }
                responseBuilder.AppendLine("---");
            }

            return responseBuilder.ToString();
        }
        catch (Exception)
        {
            return $"Wystąpił błąd techniczny podczas pobierania rezerwacji. Poinformuj gościa, aby spróbował ponownie później.";
        }
    }

    [McpServerTool(Name = "save_guest_preference")]
    [Description("Zapisuje ważną preferencję lub fakt o gościu w pamięci długoterminowej. Używaj ZAWSZE, gdy gość wspomni o swoich wymaganiach (np. alergie, ulubione piętro, potrzeba faktury na firmę).")]
    public async Task<string> SavePreferenceAsync(
        [Description("Kategoria faktu. Dozwolone: 'diet', 'room_preference', 'general'")] string category,
        [Description("Treść faktu, np. 'Alergia na orzechy', 'Woli miękkie poduszki'")] string value,
        CancellationToken cancellationToken)
    {
        ClaimsPrincipal? user = _httpContextAccessor.HttpContext?.User;
        if (user == null || !user.Identity!.IsAuthenticated)
        {
            return "BŁĄD: Gość jest niezalogowany. Nie mogę zapisać preferencji.";
        }

        string email = user.FindFirst(ClaimTypes.Email)?.Value ?? user.Identity.Name!;

        var command = new SaveGuestPreferenceCommand(email, category, value);
        await _mediator.SendAsync(command, cancellationToken);

        return "Fakt został pomyślnie zapisany w profilu gościa na przyszłość.";
    }
}