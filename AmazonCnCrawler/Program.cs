using System.IO;
using System.Net;
using Winista.Text.HtmlParser.Util;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Tags;
using System.Web;
using Winista.Text.HtmlParser.Filters;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

namespace Spider
{
    class Program
    {
        public static List<string> HtmlContents;//保存待分析的html文本序列
        public static List<Commodity> TradeList;//保存得到的商品信息
        public static int Range;//要爬取的页面数
        public static int NumOfThread;//线程数
        public static string QueryKeyWord;//要查询的关键词
        private static int index;//标识当前分析的html文本的索引
        private static int badRecordCount;//记录无法处理的条目数

        //保存第page页的物品搜索结果
        public static void CrawlHtml(int page)
        {
            HttpWebRequest wrq = HttpWebRequest.CreateHttp("http://www.amazon.cn/s/field-keywords=" + QueryKeyWord + "&page=" + page);
            StreamReader reader = new StreamReader(new BufferedStream(wrq.GetResponse().GetResponseStream(), 4 * 200 * 1024));
            HtmlContents.Add(reader.ReadToEnd());
            reader.Close();
        }

        //分析HtmlContents中给定索引条目的内容，提取信息
        private static void GetInfoFromHtml(int index)
        {
            //使用Winista.HtmlParser库解析HTML
            //建立HTML分析工具对象
            Lexer lexer = new Lexer(HtmlContents[index]);
            Parser parser = new Parser(lexer);
            //按属性的过滤器：两个参数分别代表要过滤的属性和属性值
            HasAttributeFilter nameFilter = new HasAttributeFilter("class", "lrg");
            HasAttributeFilter priceFilter = new HasAttributeFilter("class", "bld lrg red");
            //获得所有满足过滤条件的HTML节点
            NodeList nameList = parser.Parse(nameFilter);
            for (int j = 0; j < nameList.Size(); j++)
            {
                //确定节点nameList[j]为Span类型的标签；HttpUtility.HtmlDecode方法把HTML编码转为文本编码，使中文正常显示
                string name = HttpUtility.HtmlDecode(((Span)nameList[j]).StringText);
                //Parent表示该HTML节点的父节点
                //NextSobling表示该HTML节点的下一个兄弟节点
                //Children表示该HTML节点的所有孩子节点组成的集合
                //ExtractAllNodesThatMatch表示获取所有满足给定过滤器条件的节点，两个参数分别代表过滤器和是否进入孩子节点中迭代查找
                //注意：对Winista.HtmlParser来说，“空文本节点”也是一个节点（在IE的开发者工具中显示“空文本节点”，而Chrome则不显示）；形似<del>内容</ del>在Children中会表达成三个节点
                NodeList priceList = nameList[j].Parent.Parent.NextSibling.NextSibling.Children.ExtractAllNodesThatMatch(priceFilter, true);
                if (priceList.Size() == 1)
                {
                    string priceStr = ((Span)priceList[0]).StringText;
                    double price = Double.Parse(priceStr.Substring(2, priceStr.Length - 2));
                    TradeList.Add(new Commodity(name, price, "RMB"));
                }
                else
                {
                    badRecordCount++;
                }
            }
            Console.WriteLine("第" + (index + 1) + "个页面处理完成！");
            //保存当前页面到本地文件
            //StreamWriter writer = new StreamWriter("searchresult"+i+".html");
            //writer.Write(s);
            //writer.Close();
        }

        //消费者线程
        private static void Consumer()
        {
            while (Producer.iCount != NumOfThread)
            {
                while (index < HtmlContents.Count)
                {
                    GetInfoFromHtml(index);
                    index++;
                }
                Thread.Sleep(10);
            }
        }

        static void Main(string[] args)
        {
            #region 初始化
            Range = 20;//爬取页面数
            NumOfThread = 2;//线程数
            QueryKeyWord = "摩恩";//查询关键词
            index = 0;//对于HtmlContents当前处理到的位置
            badRecordCount = 0;//错误的记录条数（调试用）
            HtmlContents = new List<string>(Range);//记录HTML页面的集合
            TradeList = new List<Commodity>();//爬取的物品集合
            Stopwatch sw = new Stopwatch();//计时器
            #endregion

            sw.Start();//开始计时

            #region 多线程
            //ManualResetEvent eventX = new ManualResetEvent(false);
            //Producer tManager = new Producer(NumOfThread, eventX, 1);
            //Thread consumer = new Thread(new ThreadStart(Consumer));
            //consumer.Start();
            //for (int i = 0; i < NumOfThread; i++)
            //{
            //    ThreadPool.QueueUserWorkItem(new WaitCallback(tManager.Product), Ranger / NumOfThread);
            //}
            //eventX.WaitOne(Timeout.Infinite, true);
            //while (index < HtmlContents.Count)
            //{
            //    GetInfoFromHtml(index);
            //    index++;
            //}
            #endregion

            #region 单线程
            //建立连接，读取网页
            //WebClient client = new WebClient();
            for (int i = 1; i < 21; i++)
            {
                CrawlHtml(i);
                GetInfoFromHtml(i - 1);
            }
            #endregion

            sw.Stop();//计时结束
            Console.WriteLine("用时：" + sw.ElapsedMilliseconds);
            Console.WriteLine("保存的总条目数：" + TradeList.Count);
            Console.WriteLine("跳过的总条目数：" + badRecordCount);
        }
    }
}
