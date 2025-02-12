using RestSharp;
using System.Net.Http.Json;
using UserApiTests.Factories;
using UserApiTests.Models;
using UserApiTests.Services;

namespace UserApiTests
{
    [TestFixture]
    public class UserApiTests
    {
        private RestClient _client;
        private UserService _userService;

        [SetUp]
        public async Task SetUp()
        {
            var options = new RestClientOptions("https://localhost:7098/");  //replace URL with whatever your URL is
            _client = new RestClient(options);
            _userService = new UserService(_client);
        }

        [Test]
        public async Task CreateUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();

            var request = new RestRequest("api/Users").AddJsonBody(newUser);
            var response = await _client.ExecutePostAsync<User>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                throw new Exception($"Failed to create user: {response.StatusCode} - {response.ErrorMessage}");
            }

            var createdUser = response.Data;

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

            var request = new RestRequest("api/Users");
            var response = await _client.ExecuteGetAsync<List<User>>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                throw new Exception($"Failed to get users: {response.StatusCode} - {response.ErrorMessage}");
            }

            var users = response.Data;

            Assert.NotNull(users);
            Assert.IsNotEmpty(users);

            await _userService.DeleteUserAsync(createdUser.Id);
        }

        [Test]
        public async Task GetUser_ShouldReturnUser()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var request = new RestRequest($"api/Users/{createdUser.Id}");
            var response = await _client.ExecuteGetAsync<User>(request);

            if (!response.IsSuccessful || response.Data == null)
            {
                throw new Exception($"Failed to get user: {response.StatusCode} - {response.ErrorMessage}");
            }

            var user = response.Data;

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

            var request = new RestRequest($"api/Users/{createdUser.Id}").AddJsonBody(updatedUser);

            var response = await _client.ExecutePutAsync<User>(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to update user: {response.StatusCode} - {response.ErrorMessage}");
            }

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

            var request = new RestRequest($"api/Users/{createdUser.Id}");
            var response = await _client.ExecuteDeleteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to delete user: {response.StatusCode} - {response.ErrorMessage}");
            }

            var getUserResponse = await _client.ExecuteGetAsync(request);
            Assert.That(getUserResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound));
        }

        [Test]
        public async Task LoginUser_ShouldBeSuccessful()
        {
            var newUser = UserFactory.CreateUser();
            var createdUser = await _userService.CreateUserAsync(newUser);

            var request = new RestRequest($"api/Users/login").AddJsonBody(createdUser);
            var response = await _client.ExecutePostAsync<LoginResponse>(request);

            if (!response.IsSuccessful)
            {
                throw new Exception($"Failed to login user: {response.StatusCode} - {response.ErrorMessage}");
            }

            var loginResponse = response.Data;

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