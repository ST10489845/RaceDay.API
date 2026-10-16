using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs.Events;
using RaceDay.API.Entities;
using RaceDay.Tests.Helpers;
using Xunit;
using RaceDay.API.Data;

namespace RaceDay.Tests.Controllers
{
    public class EventsControllerTests
    {
        private async Task<(RaceDayDbContext context, Organiser organiser)> SeedOrganiserAsync(int userId = 1)
        {
            var context = TestDbContextFactory.Create();
            var user = new User { UserId = userId, Email = $"org{userId}@test.com", PasswordHash = "x", FullName = "Organiser", Role = "Organiser" };
            var organiser = new Organiser { OrganiserId = userId, UserId = userId, OrganisationName = "Org", ContactPhone = "0" };
            context.Users.Add(user);
            context.Organisers.Add(organiser);
            await context.SaveChangesAsync();
            return (context, organiser);
        }

        // --- ROLE REJECTION ---

        [Fact]
        public async Task CreateEvent_WhenNotAuthenticated_ReturnsUnauthorized()
        {
            var context = TestDbContextFactory.Create();
            var controller = new EventsController(context);
            ControllerTestHelpers.SetSession(controller, null);

            var result = await controller.CreateEvent(new EventCreateDto
            {
                EventName = "Test Event",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "V",
                City = "C",
                Province = "P"
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // --- SUCCESS ---

        [Fact]
        public async Task CreateEvent_WhenLoggedInAsOrganiser_ReturnsCreated()
        {
            var (context, organiser) = await SeedOrganiserAsync(1);
            var controller = new EventsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 1, role: "Organiser");

            var result = await controller.CreateEvent(new EventCreateDto
            {
                EventName = "Cape Town Marathon",
                EventDescription = "Annual marathon",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "Cape Town Stadium",
                City = "Cape Town",
                Province = "Western Cape"
            });

            result.Should().BeOfType<CreatedAtActionResult>();
            var events = await context.Events.ToListAsync();
            events.Should().HaveCount(1);
            events[0].EventName.Should().Be("Cape Town Marathon");
            events[0].OrganiserId.Should().Be(organiser.OrganiserId);
        }

        [Fact]
        public async Task CreateEvent_WithDeadlineAfterEventDate_ReturnsBadRequest()
        {
            var (context, _) = await SeedOrganiserAsync(1);
            var controller = new EventsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 1, role: "Organiser");

            var result = await controller.CreateEvent(new EventCreateDto
            {
                EventName = "Invalid Event",
                EventDate = DateTime.UtcNow.AddDays(10),
                RegistrationDeadline = DateTime.UtcNow.AddDays(20),
                Venue = "V",
                City = "C",
                Province = "P"
            });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        // --- OWNERSHIP ---

        [Fact]
        public async Task UpdateEvent_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
        {
            var context = TestDbContextFactory.Create();

            var o1User = new User { UserId = 1, Email = "o1@t.com", PasswordHash = "x", FullName = "O1", Role = "Organiser" };
            var o1 = new Organiser { OrganiserId = 1, UserId = 1, OrganisationName = "O1", ContactPhone = "0" };
            var o2User = new User { UserId = 2, Email = "o2@t.com", PasswordHash = "x", FullName = "O2", Role = "Organiser" };
            var o2 = new Organiser { OrganiserId = 2, UserId = 2, OrganisationName = "O2", ContactPhone = "0" };

            var ev = new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Original",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "V",
                City = "C",
                Province = "P",
                IsActive = true
            };

            context.Users.AddRange(o1User, o2User);
            context.Organisers.AddRange(o1, o2);
            context.Events.Add(ev);
            await context.SaveChangesAsync();

            var controller = new EventsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Organiser");

            var result = await controller.UpdateEvent(1, new EventCreateDto
            {
                EventName = "Hacked",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "V",
                City = "C",
                Province = "P"
            });

            var objResult = result as ObjectResult;
            objResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task DeleteEvent_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
        {
            var context = TestDbContextFactory.Create();

            var o1User = new User { UserId = 1, Email = "o1@t.com", PasswordHash = "x", FullName = "O1", Role = "Organiser" };
            var o1 = new Organiser { OrganiserId = 1, UserId = 1, OrganisationName = "O1", ContactPhone = "0" };
            var o2User = new User { UserId = 2, Email = "o2@t.com", PasswordHash = "x", FullName = "O2", Role = "Organiser" };
            var o2 = new Organiser { OrganiserId = 2, UserId = 2, OrganisationName = "O2", ContactPhone = "0" };

            var ev = new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Event",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "V",
                City = "C",
                Province = "P",
                IsActive = true
            };

            context.Users.AddRange(o1User, o2User);
            context.Organisers.AddRange(o1, o2);
            context.Events.Add(ev);
            await context.SaveChangesAsync();

            var controller = new EventsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Organiser");

            var result = await controller.DeleteEvent(1);

            (result as ObjectResult)!.StatusCode.Should().Be(403);
        }
    }
}