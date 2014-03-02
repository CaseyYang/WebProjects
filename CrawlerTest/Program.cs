
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;
namespace CrawlerTest
{
    class Program
    {
        static void GetStoryOfRevolution()
        {
            StreamReader reader = new StreamReader("catalogue.htm");
            Lexer lexer = new Lexer(reader.ReadToEnd());
            Parser parser = new Parser(lexer);
            HasAttributeFilter linkFilterByParent = new HasAttributeFilter("class", "row zhangjieUl");
            HasAttributeFilter linkFilterByClass = new HasAttributeFilter("class", "fontStyle2 colorStyleLink");
            AndFilter linkFilter = new AndFilter(new HasParentFilter(linkFilterByParent, true), linkFilterByClass);
            NodeList linkNodeList = parser.Parse(linkFilter);
            List<string> linkUrlList = new List<string>(linkNodeList.Size());
            List<string> chapterHtmlContentList = new List<string>(linkNodeList.Size());
            HttpWebRequest httpWebRequest;
            StreamReader chapterReader = null;
            for (int i = 0; i < linkNodeList.Size(); i++)
            {
                ATag linkNode = (ATag)linkNodeList[i];
                linkUrlList.Add(linkNode.Link);
                httpWebRequest = HttpWebRequest.CreateHttp("http://www.mlxiaoshuo.com" + linkUrlList[linkUrlList.Count - 1]);
                chapterReader = new StreamReader(new BufferedStream(httpWebRequest.GetResponse().GetResponseStream(), 4 * 200 * 1024));
                string chapterHtmlContent = chapterReader.ReadToEnd();
                chapterHtmlContentList.Add(chapterHtmlContent);
                Console.WriteLine("第" + (i + 1) + "个页面获取完毕！");
            }
            chapterReader.Close();
            HasAttributeFilter praghFilter = new HasAttributeFilter("class", "textP fontStyle2 colorStyleText");
            StreamWriter writer = new StreamWriter("革命逸事.txt");
            for (int i = 0; i < chapterHtmlContentList.Count; i++)
            {
                writer.WriteLine("第" + (i + 1) + "章");
                lexer = new Lexer(chapterHtmlContentList[i]);
                parser = new Parser(lexer);
                NodeList praghNodeList = parser.Parse(praghFilter);
                if (praghNodeList.Size() == 1)
                {
                    for (int j = 0; j < praghNodeList[0].Children.Size(); j++)
                    {
                        if (praghNodeList[0].Children[j].GetType().Equals(typeof(ParagraphTag)))
                        {
                            ParagraphTag praghTag = (ParagraphTag)praghNodeList[0].Children[j];
                            writer.WriteLine("    " + praghTag.StringText);
                        }
                    }
                    writer.WriteLine();
                }
                else
                {
                    Console.WriteLine("第" + (i + 1) + "页中，判断段落的标准出错！");
                }
            }
            writer.Close();
        }

        static void Main(string[] args)
        {
            StreamReader reader = new StreamReader("weibo1-0.html");
            string content="";
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                if (str.Contains("<script>STK && STK.pageletM && STK.pageletM.view({\"pid\":\"pl_content_hisFeed"))
                {
                    int start = str.IndexOf('{');
                    int last = str.LastIndexOf('}');
                    content = str.Substring(start, last - start + 1);
                    break;
                }
            }
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Json));
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            Json json = (Json)serializer.ReadObject(ms);
            ms.Close();
            StreamWriter writer = new StreamWriter("weiboGetFromJson.html");
            writer.Write(json.html);
            writer.Close();
        }
    }
}
