using CDO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;

namespace CYMBlogCrawler
{
    class Program
    {
        static HasAttributeFilter articleFilter = new HasAttributeFilter("class", "article");
        static AndFilter wrapFilterByParent = new AndFilter(new NodeClassFilter(typeof(Span)), new HasAttributeFilter("class", "wrap"));
        static NodeClassFilter wrapFilterByNodeClass = new NodeClassFilter(typeof(HeadingTag));
        static AndFilter wrapFilter = new AndFilter(new HasParentFilter(wrapFilterByParent, false), wrapFilterByNodeClass);
        static NodeClassFilter titleFilter = new NodeClassFilter(typeof(TitleTag));
        static List<string> blogLinkList = new List<string>();

        static void GetBlogLink(string htmlContent)
        {
            Lexer lexer = new Lexer(htmlContent);
            Parser parser = new Parser(lexer);
            NodeList articleList = parser.Parse(articleFilter);
            if (articleList.Count == 1)
            {
                NodeList candidateNodeList = articleList[0].Children.ExtractAllNodesThatMatch(wrapFilter, true);
                for (int i = 0; i < candidateNodeList.Count; i++)
                {
                    NodeList linkNodeList = candidateNodeList[i].Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ATag)), false);
                    if (linkNodeList.Count == 1)
                    {
                        string blogLink = ((ATag)linkNodeList[0]).ExtractLink();
                        blogLinkList.Add(blogLink);
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个条目中，判断链接出错！");
                    }
                }
            }
            else
            {
                Console.WriteLine("获取包含日志列表出错！");
            }
        }

        static string GetBlogTitle(string htmlContent)
        {
            string result = "";
            Lexer lexer = new Lexer(htmlContent);
            Parser parser = new Parser(lexer);
            NodeList titleList = parser.Parse(titleFilter);
            if (titleList.Count == 1)
            {
                TitleTag titleTag = (TitleTag)titleList[0];
                result = titleTag.Title;
            }
            else
            {
                Console.WriteLine("获取标题信息出错！");
            }
            return result;
        }

        static void Main(string[] args)
        {
            #region 获取包含所有日志链接的HTML文本
            //string baseQueryUrl = "http://www.douban.com/people/oranjeruud/notes?start=";
            //for (int i = 0; i < 23; i++)
            //{
            //    string queryUrl = baseQueryUrl + (i * 10);
            //    HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(queryUrl);
            //    httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.95";
            //    HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
            //    if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
            //    {
            //        StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
            //        GetBlogLink(reader.ReadToEnd());
            //        reader.Close();
            //    }
            //    Thread.Sleep(3000);
            //}
            #endregion

            #region 根据获得的日志链接获取日志网页和mht文件
            Message msg = new CDO.MessageClass();
            CDO.Configuration c = new CDO.ConfigurationClass();
            msg.Configuration = c;
            StreamReader reader = new StreamReader("links.txt");
            int i = 0;
            while (!reader.EndOfStream)
            {
                i++;
                string link = reader.ReadLine();
                HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(link);
                httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.95";
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
                if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
                {
                    StreamReader httpReader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                    string htmlContent = httpReader.ReadToEnd();
                    httpReader.Close();
                    string title = GetBlogTitle(htmlContent);
                    #region 保存为html文件
                    StreamWriter writer = new StreamWriter(i + ".html");
                    writer.Write(htmlContent);
                    writer.Close();
                    #endregion
                    #region 保存为mht文件
                    //msg.HTMLBody = htmlContent;
                    msg.CreateMHTMLBody(link,CDO.CdoMHTMLFlags.cdoSuppressNone,"","");
                    ADODB.Stream stream = msg.GetStream();
                    stream.SaveToFile(title + ".mht", ADODB.SaveOptionsEnum.adSaveCreateOverWrite);
                    #endregion
                }
                if (i == 1) break;
            }
            reader.Close();
            #endregion
        }
    }
}
