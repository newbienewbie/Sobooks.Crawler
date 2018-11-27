
using System;
using System.Collections.Generic;

namespace SoBooksCrawler{
    public class CrawlerContext{

        public static string BaseUrl =  "https://sobooks.cc";


        public string NextPage {get;set; }

        public IList<string> ItemLinks{get;set;}

    }
}