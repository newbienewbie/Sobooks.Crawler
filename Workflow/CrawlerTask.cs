using System;
using System.Linq;
using System.Threading.Tasks;
using App.Api;
using Itminus.Middleware;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;

namespace SoBooksCrawler.Workflow{


    public class CrawlerTask: ICrawlerTask{

        public CrawlerTask(string name,IServiceProvider sp ,Func<CrawlerContext,Task> initialize  ,Func<CrawlerContext,Task<bool>> shouldTerminate){
            if(sp== null ){
                throw new ArgumentNullException(nameof(sp));
            }
            
            if(initialize == null ){
                throw new ArgumentNullException(nameof(initialize));
            }
            if(shouldTerminate == null ){
                throw new ArgumentNullException(nameof(shouldTerminate));
            }
            this.Name = name;
            this.ServiceProvider = sp;
            this._initialize = initialize;
            this._shouldTerminate = shouldTerminate;
        }


        public virtual string Name {get;set;}

        public virtual IServiceProvider ServiceProvider{get;set;}

        private Func<CrawlerContext,Task<bool>> _shouldTerminate;
        private Func<CrawlerContext,Task> _initialize;

        public virtual WorkDelegate<CrawlerContext> BuildDelegate(){

            var container =  new WorkContainer<CrawlerContext>();
            // initialize 
            return container.Use(async (ctx,next) =>{
                Console.WriteLine($"Crawler Task {this.Name} : starts !");
                await this.InitializeAsync(ctx);
                await next();
            })
            // loop and when to terminate
            .Use(async (ctx , next ) => {
                var flag = await this.ShouldTerminateAsync(ctx);
                while ( !flag ){
                    await next();
                    flag = await this.ShouldTerminateAsync(ctx);
                }
                System.Console.WriteLine($"Crawler Task {this.Name} : Done!");
            })
            // how to process list page
            .Use(async (ctx , next ) =>{
                Console.WriteLine($"Task={this.Name} : Crawling : "+ctx.NextPage);
                var api= this.ServiceProvider.GetRequiredService<ApiClient>();
                var response = api.GetResponse(ctx.NextPage);
                var parser = new CategoryPageParser(response.Content);
                var items = parser.ParseDetailPageLinks();
                var nextPage = parser.ParseNextPage();
                if( !String.IsNullOrEmpty(nextPage) && nextPage.StartsWith(CrawlerContext.BaseUrl+"/")){
                    nextPage = nextPage.Substring((CrawlerContext.BaseUrl+"/").Length);
                }
                Console.WriteLine($"Task={this.Name} : Next Page Found : "+nextPage);
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
                    detail.DownloadUrls= detail.DownloadUrls.Select(u => {
                        var uri = new Uri(u);
                        var qs = System.Web.HttpUtility.ParseQueryString(uri.Query);
                        return qs.Get("url");
                    }).ToList();
                    using( var db = new LiteDatabase(@"MyDataBase.db")){
                        var col = db.GetCollection<ItemDetail>("itemDetail");
                        col.EnsureIndex(x => x.ISBN);
                        col.EnsureIndex(x=>x.PageUrl);
                        col.EnsureIndex(x=>x.Title);
                        col.Upsert(detail);
                    }
                    Console.WriteLine($"Task={this.Name} Got: {detail.Title}:{detail.ISBN}-{detail.Password}");
                }
            })
            .Build();
        }

        public async Task InitializeAsync(CrawlerContext ctx)
        {
            if(ctx == null){ throw new ArgumentNullException(nameof(ctx)); }
            await this._initialize(ctx);
        }

        public Task<bool> ShouldTerminateAsync(CrawlerContext ctx)
        {
            if(ctx == null){ throw new ArgumentNullException(nameof(ctx)); }
            return this._shouldTerminate(ctx);
        }
    }
}