# www.devfor.net
devfor.net website source code. The best source on the internet for staying up to date with what's happening in .NET. We try to keep this site as up-to-date and close to best practices as possible.

## Features
- Pure C# website and backend utilizing:
  - .NET 10 and C# 14 with practical uses of their new features.
  - Aspire 13 orchestrator
  - Blazor frontend with [FluentUI](https://github.com/microsoft/fluentui-blazor) theming
  - ASP.NET WebAPI
  - EFCore code-first database
- Fully Dockerized setup for deployment.
- Example of external servers for populating data which then propogate to the website.
- Simple CLI tool for a CMS (Content Management System)

## AI Model
There is a working example of an ONNX runtime model for summarization. I could use help fixing this up! Its purpose is to summarize the articles in the RSS feed. Check out the devfornet.ai solution folder. The current methodology for large articles is to essentially summarize segments, then summarize the summarized segments, and continue to do that until our segmenets collectively can fit in the model to be summarized.

## Documentation
[Full Documentation](https://deepwiki.com/mccabe93/www.devfor.net)

**Building devfor.net**
1. Create all of the .env and launchsettings/appsettings.json files that are currently marked with .example. You can use the .example values with the exception of the GitHub token -- that is required for the search performed by the repo server.
2. Spin up the database container with compose in the devfornet.db folder.
```sh
cd devfornet.db/Docker
docker-compose up
```
4. Build the solution.

**Dockerizing devfor.net**
If you want to build the project to docker containers simply run up.ps1 or up.sh, and tear it down with down.ps1 or down.sh.
