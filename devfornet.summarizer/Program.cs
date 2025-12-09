using devfornet.onnx;
using devfornet.Shared.Models;
using WebReaper.Builders;
using WebReaper.Core;
using WebReaper.Domain;
using WebReaper.Sinks.Abstract;
using WebReaper.Sinks.Models;

namespace devfornet.summarizer
{
    /// <summary>
    /// Server that creates gists from articles.
    /// </summary>
    internal static class Program
    {
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        private static OnnxT5Summarizer _onnxT5Summarizer = new OnnxT5Summarizer();
        private static SemaphoreSlim _processingArticle = new SemaphoreSlim(1, 1);
        private static ScraperEngine _scraperEngine;
        private static SummarySink _summarySink;

        static async Task Main(string[] args)
        {

        }

        private static async Task<ScraperEngine> ProcessArticleAsync(
            RssFeed feed,
            RssArticle article
        )
        {
            await _processingArticle.WaitAsync();
            
            try
            {
                if (_scraperEngine != null)
                {
                    ScraperConfig config = new ConfigBuilder()
                        .Get(article.Url)
                        .WithScheme(
                            new() { new(feed.ContentElementType, feed.ContentElementClass) }
                        )
                        .Build();
                    _ = Task.Factory.StartNew(async () => await _scraperEngine.ReconfigureAsync(config));
                    return _scraperEngine;
                }

                SummarySink sink = new SummarySink(article.Title, feed.ContentElementType)
                {
                    DataCleanupOnStart = true,
                };

                _scraperEngine = await new ScraperEngineBuilder()
                    .Get(article.Url)
                    .Parse(new() { new(feed.ContentElementType, feed.ContentElementClass) })
                    .AddSink(_summarySink)
                    .LogToConsole()
                    .BuildAsync();

                _ = Task.Factory.StartNew(async () =>
                {
                    await _scraperEngine.RunAsync();
                });

                return _scraperEngine;
            }
            catch
            {
                _processingArticle.Release();
            }
            throw new InvalidDataException("Unhandled exception processing article.");
        }

        public class SummarySink(string articleTitle, string element) : IScraperSink
        {
            public string ArticleTitle { get; set; } = articleTitle;
            public string Element { get; set; } = element;
            public bool DataCleanupOnStart { get; set; }

            public Task EmitAsync(ParsedData entity, CancellationToken cancellationToken = default)
            {
                if (entity.Data.TryGetValue(Element, out var data))
                {
                    _onnxT5Summarizer.GetSummary(
                        ArticleTitle,
                        data.ToString() ?? string.Empty,
                        256
                    );
                }
                _processingArticle.Release();
                return Task.CompletedTask;
            }
        }
    }
}
