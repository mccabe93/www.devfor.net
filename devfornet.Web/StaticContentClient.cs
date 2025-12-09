namespace devfornet.Web
{
    public class StaticContentClient
    {
        private readonly HttpClient _client;

        public StaticContentClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<string> GetContentAsync(string relativePath)
        {
            var response = await _client.GetAsync(relativePath);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
