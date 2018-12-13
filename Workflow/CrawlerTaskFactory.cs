

using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace SoBooksCrawler.Workflow{
    public  class CrawlerTaskFactory{
        public CrawlerTaskFactory(IServiceProvider serviceProvider,string hostUri){
            this.ServiePrivider = serviceProvider;
            this.HostUri = hostUri;
        }

        public IServiceProvider ServiePrivider { get; }
        public string HostUri { get; private set; }


        // [startPage,endPage)
        public CrawlerTask Create(string name ,int startPage, int? endPage){
            var baseUri = new Uri(this.HostUri);

            Func<CrawlerContext,Task> initialize = ctx =>{
                ctx.NextPage = $"/page/{startPage}";
                return Task.CompletedTask;
            };


            // here's a trap. 
            // Note that the retrieved nextPage always < endPage , in that case it will result in endless loop
            Func<CrawlerContext,Task<bool>> shouldTerminate= async ctx =>{
                if(string.IsNullOrEmpty( ctx.NextPage )){
                    return true;
                }
                if(endPage != null){
                    try{
                        Uri relativeUri = new Uri(ctx.NextPage, UriKind.RelativeOrAbsolute);
                        var uri = new Uri(baseUri,relativeUri);
                        var q = uri.Segments;
                        if(q[0] == "/" && q[1]=="page/"){
                            var page = Convert.ToInt32(q[2]);
                            if(page < endPage ){
                                return false;
                            }
                            return true;
                        }
                        return true;
                    }catch(Exception e){
                        return true;
                    }
                }
                else{
                    return false; // if there's no endPage,  use the ctx.NextPage to determine 
                }
            };

            return new CrawlerTask(name,ServiePrivider,initialize,shouldTerminate);
        }
        

    }
}