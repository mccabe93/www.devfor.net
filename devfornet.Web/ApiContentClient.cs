using devfornet.Shared.Http.Api;
using devfornet.Shared.Models;

namespace devfornet.Web;

public class ApiContentClient(HttpClient httpClient)
{
    public async Task<HeadlinesResponse> GetHeadlinesAsync(
        int top = 3,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/RssArticles/headlines/{top}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HeadlinesResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException($"Invalid {nameof(GetHeadlinesAsync)} response data");
    }

    public async Task<DevForNetArticlesResponse> GetLatestDevForNetArticlesAsync(
        int top = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/DevForNetArticles/latest/{top}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DevForNetArticlesResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(DevForNetArticlesResponse)} response data"
            );
    }

    public async Task<CommunityLatestResponse> GetCommunitiesLatest(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Communities/latest/{count}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommunityLatestResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(CommunityLatestResponse)} response data"
            );
    }

    public async Task<CommunityCountResponse> GetCommunityCount(
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Communities/count",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommunityCountResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(CommunityCountResponse)} response data"
            );
    }

    public async Task<CommunityHistoryPageResponse> GetCommunityHistoryPageAsync(
        int page = 0,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Communities/page/{page}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CommunityHistoryPageResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(CommunityHistoryPageResponse)} response data"
            );
    }

    public async Task<RepoLatestResponse> GetReposLatestCreated(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/latest/created/{count}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoLatestResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(RepoLatestResponse)} response data"
            );
    }

    public async Task<RepoLatestResponse> GetReposLatestUpdated(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/latest/updated/{count}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoLatestResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(RepoLatestResponse)} response data"
            );
    }

    public async Task<RepoLatestResponse> GetReposHelpWanted(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/latest/help-wanted/{count}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoLatestResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(RepoLatestResponse)} response data"
            );
    }

    public async Task<RepoLatestResponse> GetReposNoobHelpWanted(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/latest/noob-friendly/{count}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoLatestResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(RepoLatestResponse)} response data"
            );
    }

    public async Task<RepoCountResponse> GetRepoCount(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/count",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoCountResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException($"Invalid {nameof(RepoCountResponse)} response data");
    }

    public async Task<RepoHistoryPageResponse> GetRepoHistoryPageAsync(
        int page = 0,
        CancellationToken cancellationToken = default
    )
    {
        HttpResponseMessage response = await httpClient.GetAsync(
            $"/api/v1/Repos/page/{page}",
            cancellationToken
        );
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepoHistoryPageResponse>(
                cancellationToken: cancellationToken
            )
            ?? throw new InvalidDataException(
                $"Invalid {nameof(RepoHistoryPageResponse)} response data"
            );
    }
}
