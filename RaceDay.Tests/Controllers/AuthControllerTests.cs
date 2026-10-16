
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RaceDay.API.Controllers;
using RaceDay.API.DTOs.Auth;
using RaceDay.Tests.Helpers;
using Xunit;
using RaceDay.API.Data;

namespace RaceDay.Tests.Controllers
{
    public class AuthControllerTests
    {
        // --- REGISTRATION ---

        [Fact]
        public async Task Register_WithValidParticipantData_ReturnsOkAndCreatesUser()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            var request = new RegisterDto
            {
                Email = "participant@test.com",
                Password = "Password123",
                FullName = "Test Participant",
                Role = "Participant"
            };

            var result = await controller.Register(request);

            result.Should().BeOfType<OkObjectResult>();
            context.Users.Count().Should().Be(1);
            context.Users.First().Role.Should().Be("Participant");
            context.Participants.Count().Should().Be(1);
            context.Organisers.Count().Should().Be(0);
        }

        [Fact]
        public async Task Register_WithValidOrganiserData_CreatesOrganiserProfile()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            var request = new RegisterDto
            {
                Email = "organiser@test.com",
                Password = "Password123",
                FullName = "Test Organiser",
                Role = "Organiser"
            };

            var result = await controller.Register(request);

            result.Should().BeOfType<OkObjectResult>();
            context.Users.Count().Should().Be(1);
            context.Organisers.Count().Should().Be(1);
            context.Participants.Count().Should().Be(0);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            var request = new RegisterDto
            {
                Email = "duplicate@test.com",
                Password = "Password123",
                FullName = "First User",
                Role = "Participant"
            };
            await controller.Register(request);

            var result = await controller.Register(request);

            result.Should().BeOfType<BadRequestObjectResult>();
            context.Users.Count().Should().Be(1);
        }

        [Fact]
        public async Task Register_PasswordIsHashed_NotStoredInPlainText()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            var request = new RegisterDto
            {
                Email = "test@test.com",
                Password = "MySecretPassword",
                FullName = "Test User",
                Role = "Participant"
            };

            await controller.Register(request);

            var user = context.Users.First();
            user.PasswordHash.Should().NotBe("MySecretPassword");
            user.PasswordHash.Should().StartWith("$2");
        }

        // --- LOGIN ---

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkAndSetsSession()
        {
            var context = TestDbContextFactory.Create();
            var setupController = new AuthController(context);
            await setupController.Register(new RegisterDto
            {
                Email = "login@test.com",
                Password = "Password123",
                FullName = "Login User",
                Role = "Participant"
            });

            var controller = new AuthController(context);
            ControllerTestHelpers.SetSession(controller, null);

            var result = await controller.Login(new LoginDto
            {
                Email = "login@test.com",
                Password = "Password123"
            });

            result.Should().BeOfType<OkObjectResult>();
            controller.HttpContext.Session.GetInt32("UserId").Should().NotBeNull();
            controller.HttpContext.Session.GetString("Role").Should().Be("Participant");
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            var context = TestDbContextFactory.Create();
            var setupController = new AuthController(context);
            await setupController.Register(new RegisterDto
            {
                Email = "user@test.com",
                Password = "CorrectPassword",
                FullName = "Test User",
                Role = "Participant"
            });

            var controller = new AuthController(context);
            ControllerTestHelpers.SetSession(controller, null);

            var result = await controller.Login(new LoginDto
            {
                Email = "user@test.com",
                Password = "WrongPassword"
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            ControllerTestHelpers.SetSession(controller, null);

            var result = await controller.Login(new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "AnyPassword"
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // --- GET CURRENT USER ---

        [Fact]
        public void GetCurrentUser_WithNoSession_ReturnsUnauthorized()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            ControllerTestHelpers.SetSession(controller, null);

            var result = controller.GetCurrentUser();

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void GetCurrentUser_WithActiveSession_ReturnsUserDetails()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AuthController(context);
            ControllerTestHelpers.SetSession(controller, userId: 1, role: "Organiser", fullName: "John", email: "john@test.com");

            var result = controller.GetCurrentUser();

            result.Should().BeOfType<OkObjectResult>();
            var dto = (result as OkObjectResult)!.Value as UserResponseDto;
            dto!.Role.Should().Be("Organiser");
            dto.FullName.Should().Be("John");
        }
    }
}