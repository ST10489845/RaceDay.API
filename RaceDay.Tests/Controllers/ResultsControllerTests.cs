using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs.Results;
using RaceDay.API.Entities;
using RaceDay.Tests.Helpers;
using Xunit;
using RaceDay.API.Data;

namespace RaceDay.Tests.Controllers
{
    public class ResultsControllerTests
    {
        private async Task<RaceDayDbContext> SeedScenarioAsync()
        {
            var context = TestDbContextFactory.Create();

            var orgUser = new User { UserId = 1, Email = "o@t.com", PasswordHash = "x", FullName = "Org", Role = "Organiser" };
            var organiser = new Organiser { OrganiserId = 1, UserId = 1, OrganisationName = "O", ContactPhone = "0" };
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
            var ev = new Event
            {
                EventId = 1,
                OrganiserId = 1,
                EventName = "Race",
                EventDate = DateTime.UtcNow.AddDays(-1),
                RegistrationDeadline = DateTime.UtcNow.AddDays(-10),
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
                EntryFee = 50
            };
            var enrolment = new Enrolment
            {
                EnrolmentId = 1,
                ParticipantId = 1,
                EventId = 1,
                CategoryId = 1,
                Status = "Paid",
                Paid = true
            };

            context.Users.AddRange(orgUser, partUser);
            context.Organisers.Add(organiser);
            context.Participants.Add(participant);
            context.Events.Add(ev);
            context.Categories.Add(cat);
            context.Enrolments.Add(enrolment);
            await context.SaveChangesAsync();

            return context;
        }

        [Fact]
        public async Task CaptureResult_AsOwnerOrganiser_ReturnsSuccess()
        {
            var context = await SeedScenarioAsync();
            var controller = new ResultsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 1, role: "Organiser");

            var result = await controller.CaptureResult(new ResultCreateDto
            {
                EnrolmentId = 1,
                FinishTime = new TimeSpan(0, 45, 30),
                OverallRank = 5,
                CategoryRank = 2
            });

            result.Should().BeOfType<OkObjectResult>();
            var results = await context.Results.ToListAsync();
            results.Should().HaveCount(1);
            results[0].IsVerified.Should().BeTrue();
            results[0].VerifiedBy.Should().Be(1);
        }

        [Fact]
        public async Task CaptureResult_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
        {
            var context = await SeedScenarioAsync();

            var otherUser = new User { UserId = 3, Email = "o2@t.com", PasswordHash = "x", FullName = "O2", Role = "Organiser" };
            var otherOrg = new Organiser { OrganiserId = 2, UserId = 3, OrganisationName = "O2", ContactPhone = "0" };
            context.Users.Add(otherUser);
            context.Organisers.Add(otherOrg);
            await context.SaveChangesAsync();

            var controller = new ResultsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 3, role: "Organiser");

            var result = await controller.CaptureResult(new ResultCreateDto
            {
                EnrolmentId = 1,
                FinishTime = TimeSpan.FromMinutes(45)
            });

            (result as ObjectResult)!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CaptureResult_WhenResultAlreadyExists_ReturnsBadRequest()
        {
            var context = await SeedScenarioAsync();
            var controller = new ResultsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 1, role: "Organiser");

            await controller.CaptureResult(new ResultCreateDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(45) });
            var result = await controller.CaptureResult(new ResultCreateDto { EnrolmentId = 1, FinishTime = TimeSpan.FromMinutes(50) });

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetMyResults_AsParticipant_ReturnsOk()
        {
            var context = await SeedScenarioAsync();
            context.Results.Add(new Result
            {
                ResultId = 1,
                EnrolmentId = 1,
                ParticipantId = 1,
                EventId = 1,
                CategoryId = 1,
                FinishTime = TimeSpan.FromMinutes(45),
                IsVerified = true,
                VerifiedBy = 1
            });
            await context.SaveChangesAsync();

            var controller = new ResultsController(context);
            ControllerTestHelpers.SetSession(controller, userId: 2, role: "Participant");

            var result = await controller.GetMyResults();

            result.Should().BeOfType<OkObjectResult>();
        }
    }
}