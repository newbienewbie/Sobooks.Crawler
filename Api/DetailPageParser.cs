

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Itminus.Hunters;

namespace App.Api{

    public class DetailPageParser{

        public HtmlParser Parser{get;}
        public IHtmlDocument Document { get; }

        private Regex _passwordRegex;

        public object NodeType { get; private set; }

        public DetailPageParser(string doc){
            this.Parser =new HtmlParser();
            this.Document= this.Parser.Parse(doc);
            this._passwordRegex = new Regex(@"\b(?<tip>提取密码：)(?<pswd>.*)$");
        }

        public string ParsePasswrod(){
            var details = new List<ItemDetail>();
            var password= this.Document.QuerySelector("div.e-secret strong")?
                .GetFirstTextContext()
                .Trim();
            if(String.IsNullOrEmpty(password)){
                return null;
            }
            var matches=this._passwordRegex.Match(password);
            var pswd=matches.Groups["pswd"]?.Value;
            return pswd;
        }


        public ItemDetail ParseDetail(){
            var container =this.Document.QuerySelector(".content-wrap .book-info .book-left .item");
            var imgSrc= container?.QuerySelector(".bookpic img")?
                .GetAttribute("src")
                ?.Trim();
            var bookTitle = container?.QuerySelector(".bookinfo ul li:first-of-type")?
                .GetFirstTextContext()
                ?.Trim();
            var author = container?.QuerySelector(".bookinfo ul li:nth-of-type(2)")?
                .GetFirstTextContext()
                ?.Trim();
            var tags = container?.QuerySelectorAll(".bookinfo ul li:nth-of-type(5) a")?
                .OfType<IHtmlAnchorElement>()
                .Select(a => a.TextContent)
                .ToList();
            var isbn = container?.QuerySelector(".bookinfo ul li:last-of-type")?
                .GetFirstTextContext()
                ?.Trim();
            var downloadUrls = this.Document.QuerySelectorAll("table.dltable tr td a")?
                .OfType<IHtmlAnchorElement>()
                .Select(d => d.Href)
                .ToList();
            
            StringBuilder description = new StringBuilder();
            this.Document.QuerySelector(".article-content > h2:first-of-type")?
                .NextSibling?
                .ForEachUntil(
                    n => {
                        if(n.NodeType == AngleSharp.Dom.NodeType.Element){
                            var node = n as IElement;
                            if(node.NodeName.ToLower() == "table"){
                                return true;
                            }
                            var className= node.GetAttribute("class");
                            if(className !=null && className.Trim().Contains("dltable")){
                                return true;
                            }
                        }
                        return false;
                    },
                    n =>{
                        description.Append(n.TextContent);
                        description.Append("\r\n");
                    }
                );

            return new ItemDetail{
                PageUrl = this.Document.Url,
                Authors = author,
                Title = bookTitle,
                Tags = tags,
                ISBN = isbn,
                PosterSrc= imgSrc,
                DownloadUrls = downloadUrls,
                Description = description.ToString()
            };
        }
    }
}