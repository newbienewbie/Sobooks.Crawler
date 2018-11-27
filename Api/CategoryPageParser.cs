

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using LiteDB;

namespace App.Api{

    public class ItemDetail{

        [BsonId]
        public string PageUrl{get;set;}
        public string Title {get;set;}
        public string Authors {get;set;}

        public string PosterSrc{get;set;}

        public double? RateByDouBan {get;set;}

        public IList<string> Tags{get;set;}

        public string ISBN {get;set;}
        public IList<string> DownloadUrls{get;set;}
        public string Description {get;set;}
        public string Password {get;set;} 
    }
    public class CategoryPageParser{

        public HtmlParser Parser{get;}
        public IHtmlDocument Document { get; }

        public CategoryPageParser(string doc){
            this.Parser =new HtmlParser();
            this.Document= this.Parser.Parse(doc);
        }

        public IList<ItemDetail> ParseDetailPageLinks(){
            var details = new List<ItemDetail>();
            var items = this.Document.QuerySelectorAll("#cardslist .card");
            foreach(var item in items ){
                var h3 = item.QuerySelector("h3 a");
                var pageUrl = h3?.GetAttribute("href");
                var title = h3?.GetAttribute("title");
                var author = item.QuerySelector(".shop-item >p a")?.TextContent;
                var rate = item.QuerySelector(".shop-item >p b.dbpf_i")?.ClassList
                    .Where(c => c.StartsWith("dbpf_i"))
                    .Select(c => c.Substring(6, c.Length-6))
                    .Where(c =>!String.IsNullOrEmpty(c))
                    .Select(c => Convert.ToDouble(c))
                    .FirstOrDefault();

                var detail = new ItemDetail(){
                    PageUrl = pageUrl,
                    Title = title,
                    Authors  = author,
                };

                details.Add(detail);
            }
            return details;
        }

        public string ParseNextPage(){
            var nextPage= this.Document.QuerySelector(".content .pagination .next-page a")?
                .GetAttribute("href");
            return nextPage;
        }
    }
}