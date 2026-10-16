using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs.Enrolments;
using RaceDay.API.Entities;
using RaceDay.Tests.Helpers;
using Xunit;
using RaceDay.API.Data;

namespace RaceDay.Tests.Controllers
{
    public class EnrolmentsControllerTests
    {
        private async Task<RaceDayDbContext> SeedEventAndParticipantAsync()
        {
            var context = TestDbContextFactory.Create();

            var orgUser = new User { UserId = 1, Email = "o@t.com", PasswordHash = "x", FullName = "Org", Role = "Organiser" };
            var organiser = new Organiser { OrganiserId = 1, UserId = 1, OrganisationName = "O", ContactPhone = "0" };
            var ev = new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Test Race",
                EventDate = DateTime.UtcNow.AddDays(30),
                RegistrationDeadline = DateTime.UtcNow.AddDays(15),
                Venue = "V",
                City = "C",
                Province = "P",
                IsActive = true
            };
            var cat = new Category
            {
                CategoryId = 1,
                EventId = 1,
                CategoryName = "10km",
                CategoryType = "Running",
                DistanceKm = 10,
                EntryFee = 50,
                MaxParticipants = 2
            };

            var partUser = new User { UserId = 2, Email = "p@t.com", PasswordHash = "x", FullName = "Part", Role = "Participant" };
            var participant = new Participant
            {
                ParticipantId = 1,
                UserId = 2,
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "Male",
                EmergencyContactName = "E",
                EmergencyContactPhone = "0"
            };

            context.Users.AddRange(orgUser, partUser);
            context.Organisers.Add(organiser);
            context.Events.Add(ev);
            context.Categories.Add(cat);
            context.Participants.Add(participant);
            await context.SaveChangesAsync();

            return context;
        }

        [Fact]
        public async Task EnrolInEvent_AsParticipant_ReturnsSuccess()
        {
            var context = await SeedEventAndParticipantAsync();
            var controller = new EnrolmentsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            var result = await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 1 });

            result.Should().BeOfType<OkObjectResult>();
            var enrolments = await context.Enrolments.ToListAsync();
            enrolments.Should().HaveCount(1);
            enrolments[0].Status.Should().Be("Pending");
            enrolments[0].ParticipantId.Should().Be(1);
        }

        [Fact]
        public async Task EnrolInEvent_WhenAlreadyEnrolled_ReturnsBadRequest()
        {
            var context = await SeedEventAndParticipantAsync();
            var controller = new EnrolmentsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 1 });
            var result = await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 1 });

            result.Should().BeOfType<BadRequestObjectResult>();
            (await context.Enrolments.ToListAsync()).Should().HaveCount(1);
        }

        [Fact]
        public async Task EnrolInEvent_WithInvalidCategory_ReturnsBadRequest()
        {
            var context = await SeedEventAndParticipantAsync();
            var controller = new EnrolmentsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            var result = await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 999 });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EnrolInEvent_WhenDeadlinePassed_ReturnsBadRequest()
        {
            var context = await SeedEventAndParticipantAsync();
            var ev = await context.Events.FindAsync(1);
            ev!.RegistrationDeadline = DateTime.UtcNow.AddDays(-1);
            await context.SaveChangesAsync();

            var controller = new EnrolmentsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            var result = await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 1 });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task EnrolInEvent_WhenCategoryFull_ReturnsBadRequest()
        {
            var context = await SeedEventAndParticipantAsync();

            // Fill category
            context.Enrolments.AddRange(
                new Enrolment { ParticipantId = 1, EventId = 1, CategoryId = 1, Status = "Confirmed" },
                new Enrolment { ParticipantId = 1, EventId = 1, CategoryId = 1, Status = "Confirmed" }
            );
            await context.SaveChangesAsync();

            var controller = new EnrolmentsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            var result = await controller.EnrolInEvent(1, new EnrolmentCreateDto { CategoryId = 1 });

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}