
using System;
using System.ComponentModel.Design;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using GitReposFinder;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main(string[] args)
    {
        using HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

        Console.WriteLine("Enter GitHub username:");
        var username = Console.ReadLine();

        if (!string.IsNullOrEmpty(username))
        {
            var repositories = await ProcessRepositoriesAsync(client, username);

            foreach (var repo in repositories)
            {
                Console.WriteLine($"Name: {repo.Name}");
                Console.WriteLine($"Homepage: {repo.Homepage}");
                Console.WriteLine($"GitHub: {repo.GitHubHomeUrl}");
                Console.WriteLine($"Description: {repo.Description}");
                Console.WriteLine($"Watchers: {repo.Watchers:#,0}");
                Console.WriteLine($"{repo.LastPush}");
                Console.WriteLine();
            }
        }
    }

    static async Task<List<Repository>> ProcessRepositoriesAsync(HttpClient client, string username)
    {
        var requestUri = $"https://api.github.com/users/{username}/repos";
        int maxRetries = 3;
        int delay = 2000;

        for (int i = 0; i <= maxRetries; i++)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();
                await using Stream stream = await response.Content.ReadAsStreamAsync();
                var repositories = await JsonSerializer.DeserializeAsync<List<Repository>>(stream);
                return repositories ?? new List<Repository>();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request failed: {e.Message} - Retry {i + 1} of {maxRetries}");
                if (i == maxRetries) throw; 
                await Task.Delay(delay);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}");
                throw; 
            }
        }

        return new List<Repository>(); 
    }
}