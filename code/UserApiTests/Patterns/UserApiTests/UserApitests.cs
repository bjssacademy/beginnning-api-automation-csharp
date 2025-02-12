using System.Net.Http.Json;
using UserApiTests.Factories;
using UserApiTests.Models;
using UserApiTests.Services;

namespace UserApiTests
{
    [TestFixture]
    public class UserApiTests
    {
        private HttpClient _client;
        private UserService _userService;

        [SetUp]
        public async Task SetUp()
        {
            _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/") };  //replace URL with whatever your URL is
            _userService = new UserService(_client);
        }

        [Test]
        public async Task CreateUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();

            var response = await _client.PostAsJsonAsync("api/Users", newUser);
            response.EnsureSuccessStatusCode();
            var createdUser = await response.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(createdUser);
            Assert.Greater(createdUser.Id, 0);
            Assert.That(createdUser.Name, Is.EqualTo(newUser.Name));
            Assert.That(createdUser.Password, Is.EqualTo(newUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);

        }

        [Test]
        public async Task GetAllUsers_ShouldReturnUsers()
        {

            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.GetAsync("api/Users");
            response.EnsureSuccessStatusCode();

            var users = await response.Content.ReadFromJsonAsync<User[]>();

            Assert.NotNull(users);
            Assert.IsNotEmpty(users);

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task GetUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.GetAsync($"api/Users/{createdUser.Id}");
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<User>();

            Assert.NotNull(user);
            Assert.That(user.Id, Is.EqualTo(createdUser.Id));
            Assert.That(user.Name, Is.EqualTo(createdUser.Name));
            Assert.That(user.Password, Is.EqualTo(createdUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task UpdateUser_ShouldModifyUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var updatedUser = new User {Id = createdUser.Id, Name = "JohnDoeUpdated", Password = "NewPass123" };

            var response = await _client.PutAsJsonAsync($"api/Users/{createdUser.Id}", updatedUser);
            response.EnsureSuccessStatusCode();

            var fetchedUser = await _userService.GetUserByIdAsync(createdUser.Id); 

            Assert.NotNull(fetchedUser);
            Assert.That(fetchedUser.Name, Is.EqualTo(updatedUser.Name));
            Assert.That(fetchedUser.Password, Is.EqualTo(updatedUser.Password));

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task DeleteUser_ShouldRemoveUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.DeleteAsync($"api/Users/{createdUser.Id}");
            response.EnsureSuccessStatusCode();

            var getUserResponse = await _client.GetAsync($"api/Users/{createdUser.Id}");
            Assert.That(getUserResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }

        [Test]
        public async Task LoginUser_ShouldBeSuccessful()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var response = await _client.PostAsJsonAsync($"api/Users/login", newUser);
            response.EnsureSuccessStatusCode();

            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();

            Assert.NotNull(loginResponse);
            Assert.That(loginResponse.Id, Is.EqualTo(createdUser.Id));

            await _userService.DeleteUserAsync(createdUser.Id);

        }

        [TearDown]
        public async Task TearDown()
        {
            _client.Dispose();
        }
    }
}