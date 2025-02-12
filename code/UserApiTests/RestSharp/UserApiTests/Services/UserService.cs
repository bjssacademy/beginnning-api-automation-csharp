using RestSharp;
using System.Net.Http.Json;
using UserApiTests.Models;

namespace UserApiTests.Services
{
    public class UserService
    {
        private readonly RestClient _client;

        public UserService(RestClient client)
        {
            _client = client;
        }

        public async Task<User> CreateUserAsync(User user)
        {
            var request = new RestRequest("api/Users").AddJsonBody(user);
            return await _client.PostAsync<User>(request);
        }

        public async Task<User> GetUserByIdAsync(long userId)
        {
            var request = new RestRequest($"api/Users/{userId}");
            return await _client.GetAsync<User>(request);
        }

        public async Task DeleteUserAsync(long userId)
        {
            var request = new RestRequest($"api/Users/{userId}");
            await _client.DeleteAsync(request);
        }
    }

}
