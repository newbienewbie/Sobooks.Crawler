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
            
            var mw= p.BuildDelegate();
            mw(new CrawlerContext())
                .Wait();
        }

        public IServiceCollection ConfigureServices(){
            var sc = new ServiceCollection();
            sc.AddSingleton<RestClient>(sp => new RestClient(CrawlerContext.BaseUrl));
            sc.AddSingleton<ApiClient>();
            return sc;
        }


        public WorkDelegate<CrawlerContext> BuildDelegate(){

            var container =  new WorkContainer<CrawlerContext>();
            // initialize 
            return container.Use(async (ctx,next) =>{
                Console.WriteLine("Crawler starts !");
                ctx.NextPage = "/";
                await next();
            })
            // loop and when to terminate
            .Use(async (ctx , next ) => {
                while (! String.IsNullOrEmpty( ctx.NextPage )){
                    await next();
                }
                System.Console.WriteLine("Done!");
            })
            // how to process list page
            .Use(async (ctx , next ) =>{
                Console.WriteLine(ctx.NextPage);
                var api= this.ServiceProvider.GetRequiredService<ApiClient>();
                var response = api.GetResponse(ctx.NextPage);
                var parser = new CategoryPageParser(response.Content);
                var items = parser.ParseDetailPageLinks();
                var nextPage = parser.ParseNextPage();
                if( !String.IsNullOrEmpty(nextPage) && nextPage.StartsWith(CrawlerContext.BaseUrl+"/")){
                    nextPage = nextPage.Substring((CrawlerContext.BaseUrl+"/").Length);
                }
                Console.WriteLine(nextPage);
                ctx.NextPage = nextPage;
                ctx.ItemLinks = items.Select(i => i.PageUrl).ToList();
                await next();
            })
            // how to deal with detail page
            .Use(async(ctx ,next)=>{
                var api= this.ServiceProvider.GetRequiredService<ApiClient>();
                var nextPage = ctx.NextPage;
                foreach(var href in ctx.ItemLinks){
                    var response= api.PostToRetrievePassword(href ,"2018919");
                    var parser = new DetailPageParser(response.Content);
                    var detail = parser.ParseDetail();
                    var password = parser.ParsePasswrod();
                    detail.PageUrl = href;
                    detail.Password = password;
                    using( var db = new LiteDatabase(@"MyDataBase.db")){
                        var col = db.GetCollection<ItemDetail>("itemDetail");
                        col.EnsureIndex(x => x.ISBN);
                        col.EnsureIndex(x=>x.PageUrl);
                        col.EnsureIndex(x=>x.Title);
                        col.Upsert(detail);
                    }
                    Console.WriteLine($"Got: {detail.Title}:{detail.ISBN}-{detail.Password}");
                }
            })
            .Build();

        }
    }
}
