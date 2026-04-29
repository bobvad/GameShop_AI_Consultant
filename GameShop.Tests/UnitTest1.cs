using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Game_Shop_AI_Assistent.Controllers;
using GameShop.Context;

namespace Game_Shop_AI_Assistent.Tests.Controllers
{
    public class UsersControllerTests : IDisposable
    {
        private readonly string _dbName;
        private GameShopContext gameShopContext;
        public UsersControllerTests()
        {
            _dbName = $"TestDb_{Guid.NewGuid()}";
        }

        public void Dispose()
        {
            using var context = new GameShopContext(
                new DbContextOptionsBuilder<GameShopContext>()
                    .UseInMemoryDatabase(_dbName)
                    .Options);
            context.Database.EnsureDeleted();
        }

        private DbContextOptions<GameShopContext> CreateContextOptions()
        {
            return new DbContextOptionsBuilder<GameShopContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;
        }

        private void SeedTestData(GameShopContext context)
        {
            if (!context.Users.Any())
            {
                context.Users.Add(new Users
                {
                    Id = 1,
                    Login = "test",
                    Email = "test@mail.com",
                    Password = "1234",
                    DateTimeCreated = DateTime.UtcNow,
                    IsGuest = false
                });
                context.SaveChanges();
            }
        }

        private UsersController CreateController()
        {
            var options = CreateContextOptions();
            var context = new GameShopContext(options);
            SeedTestData(context);
            return new UsersController(gameShopContext);
        }

        #region SingIn Tests

        [Fact]
        public void SingIn_ValidUser_ReturnsJsonWithUser()
        {
            var controller = CreateController();

            var result = controller.SingIn("test", "1234");

            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public void SingIn_EmptyLogin_Returns403()
        {
            var controller = CreateController();
            var result = controller.SingIn("", "1234");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public void SingIn_EmptyPassword_Returns403()
        {
            var controller = CreateController();
            var result = controller.SingIn("test", "");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public void SingIn_WrongPassword_Returns403()
        {
            var controller = CreateController();
            var result = controller.SingIn("test", "wrongpass");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        [Fact]
        public void SingIn_NonExistentUser_Returns403()
        {
            var controller = CreateController();
            var result = controller.SingIn("nouser", "1234");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(403, statusCodeResult.StatusCode);
        }

        #endregion

        #region GetStats Tests

        [Fact]
        public void GetStats_UserExists_ReturnsJsonWithStats()
        {
            var controller = CreateController();
            var result = controller.GetStats(1);
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public void GetStats_UserNotFound_Returns404()
        {
            var controller = CreateController();
            var result = controller.GetStats(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region RegIn Tests

        [Fact]
        public void RegIn_NewUser_ReturnsJsonWithUser()
        {
            var controller = CreateController();
            var result = controller.RegIn("newuser", "new@mail.com", "pass123", DateTime.UtcNow);
            var jsonResult = Assert.IsType<JsonResult>(result);
            var returnedUser = Assert.IsType<Users>(jsonResult.Value);
            Assert.Equal("newuser", returnedUser.Login);
            Assert.False(returnedUser.IsGuest);
        }

        [Fact]
        public void RegIn_DuplicateLogin_Returns409()
        {
            var controller = CreateController();
            controller.RegIn("duplicate", "mail@mail.com", "1234", DateTime.UtcNow);
            var result = controller.RegIn("duplicate", "other@mail.com", "1234", DateTime.UtcNow);
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, statusCodeResult.StatusCode);
        }

        [Fact]
        public void RegIn_TrimmedLogin_Email()
        {
            var controller = CreateController();
            var result = controller.RegIn("  spaced  ", "  test@mail.com  ", "pass", DateTime.UtcNow);
            var jsonResult = Assert.IsType<JsonResult>(result);
            var user = Assert.IsType<Users>(jsonResult.Value);
            Assert.Equal("spaced", user.Login);
            Assert.Equal("test@mail.com", user.Email);
        }

        #endregion

        #region DeleteById Tests

        [Fact]
        public void DeleteById_ValidId_ReturnsOk()
        {
            var controller = CreateController();
            var result = controller.DeleteById(1);
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("успешно удален", okResult.Value?.ToString());
        }

        [Fact]
        public void DeleteById_NonExistentId_Returns404()
        {
            var controller = CreateController();
            var result = controller.DeleteById(999);
            Assert.IsType<NotFoundObjectResult>(result);
        }

        #endregion

        #region RegInAdmin Tests

        [Fact]
        public void RegInAdmin_NewAdmin_ReturnsJsonWithAdmin()
        {
            var controller = CreateController();
            var result = controller.RegInAdmin("admin", "admin@mail.com", "adminpass");
            var jsonResult = Assert.IsType<JsonResult>(result);
            var admin = Assert.IsType<Users>(jsonResult.Value);
            Assert.Equal("Admin", admin.Role);
        }

        [Fact]
        public void RegInAdmin_DuplicateLogin_Returns409()
        {
            var controller = CreateController();
            controller.RegInAdmin("existingadmin", "a@mail.com", "pass");
            var result = controller.RegInAdmin("existingadmin", "b@mail.com", "pass");
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, statusCodeResult.StatusCode);
        }

        #endregion
    }
}