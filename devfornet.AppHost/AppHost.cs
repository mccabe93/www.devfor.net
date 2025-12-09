var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder
    .AddProject<Projects.devfornet_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder
    .AddProject<Projects.devfornet_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder
    .AddProject<Projects.devfornet_rss>("rssserver")
    .WaitForStart(apiService, WaitBehavior.StopOnResourceUnavailable);

builder
    .AddProject<Projects.devfornet_repos>("reposerver")
    .WaitForStart(apiService, WaitBehavior.StopOnResourceUnavailable);

await builder.Build().RunAsync();
