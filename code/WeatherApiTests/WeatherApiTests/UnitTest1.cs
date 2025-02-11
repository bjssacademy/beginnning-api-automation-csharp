using System.Net.Http.Json;

namespace WeatherApiTests
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }
        public int TemperatureC { get; set; }
        public string Summary { get; set; }
    }

    [TestFixture]
    public class WeatherApiTests
    {
        private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new HttpClient { BaseAddress = new Uri("https://localhost:7098/") };
        }

        [Test]
        public async Task GetWeatherForecast_ShouldReturnData()
        {
            var forecasts = await _client.GetFromJsonAsync<WeatherForecast[]>("weatherforecast");

            Assert.NotNull(forecasts, "API response should not be null");
            Assert.IsNotEmpty(forecasts, "API should return at least one forecast");

            foreach (var forecast in forecasts)
            {
                Console.WriteLine($"Date: {forecast.Date}, Temp (C): {forecast.TemperatureC}, Summary: {forecast.Summary}");
                Assert.IsNotNull(forecast.Summary, "Summary should not be null");
            }
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }
    }
}