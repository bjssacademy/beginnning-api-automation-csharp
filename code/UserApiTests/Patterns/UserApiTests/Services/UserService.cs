using System.Net.Http.Json;
using UserApiTests.Models;

namespace UserApiTests.Services
{
    public class UserService
    {
        private readonly HttpClient _client;

        public UserService(HttpClient client)
        {
            _client = client;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            var response = await _client.PostAsJsonAsync("api/Users", user);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
            var response = await _client.GetAsync($"api/Users/{userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<User>();
        }

        public async Task DeleteUserAsync(long userId)
        {
            var response = await _client.DeleteAsync($"api/Users/{userId}");
            response.EnsureSuccessStatusCode();
        }
    }

}
