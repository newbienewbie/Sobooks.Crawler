using System;
using System.Threading.Tasks;
using Itminus.Middleware;

namespace SoBooksCrawler.Workflow
{
    public interface ICrawlerTask
    {

        string Name {get;set;}

        IServiceProvider ServiceProvider {get;set;}
        

        Task InitializeAsync(CrawlerContext ctx);

        Task<bool>  ShouldTerminateAsync(CrawlerContext ctx);

        WorkDelegate<CrawlerContext> BuildDelegate();

    }
}