using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using App.Api;
using Itminus.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using RestSharp;
using LiteDB;
using System.IO;
using SoBooksCrawler.Workflow;

namespace SoBooksCrawler
{

    class Program
    {

        private IServiceProvider ServiceProvider;

        static void Main(string[] args)
        {

            var config= new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("applicationSettings.json")
                .Build();
                ;

            var p = new Program();

            var services = p.ConfigureServices();
            p.ServiceProvider = services.BuildServiceProvider();

            var factory = new SoBooksCrawler.Workflow.CrawlerTaskFactory(p.ServiceProvider,"https://sobooks.cc");
            List<CrawlerTask> crawlerTaskList = new List<CrawlerTask>();
            crawlerTaskList.Add(factory.Create("task1",1,2));
            crawlerTaskList.Add(factory.Create("task2",2,32));
            crawlerTaskList.Add(factory.Create("task3",32,62));
            crawlerTaskList.Add(factory.Create("task4",62,92));
            crawlerTaskList.Add(factory.Create("task5",92,122));
            crawlerTaskList.Add(factory.Create("task6",122,152));
            crawlerTaskList.Add(factory.Create("task7",152,null));

            Parallel.ForEach(crawlerTaskList.Select(t => t.BuildDelegate()),(d)=>{
                d(new CrawlerContext());
            });

        }

        public IServiceCollection ConfigureServices(){
            var sc = new ServiceCollection();
            sc.AddSingleton<RestClient>(sp => new RestClient(CrawlerContext.BaseUrl));
            sc.AddSingleton<ApiClient>();
            return sc;
        }

    }
}
