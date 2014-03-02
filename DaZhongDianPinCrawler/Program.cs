using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace DaZhongDianPinCrawler
{
    class Program
    {
        public static void Output(Object result, string fileName)
        {
            #region 输出到XML文件
            StreamWriter writer = new StreamWriter(fileName + ".xml");
            XmlSerializer sr = new XmlSerializer(result.GetType());
            sr.Serialize(writer, result);
            writer.Close();
            #endregion
        }

        static void Main(string[] args)
        {
            #region 调试
            //List<POI> poiList = new List<POI>();
            //DianPinCrawler crawler = new DianPinCrawler(poiList, 1, 25);
            //StreamReader reader = new StreamReader("html1.html");
            //string str = reader.ReadToEnd();
            //crawler.currentHtml = str;
            //crawler.GetInfoFromHtml(1);
            //Output(poiList, "test");
            #endregion

            #region 正式爬取大众点评网
            List<POI> poiList = new List<POI>();
            int taskCount = 50;
            DianPinCrawler crawler = null;
            for (int i = 1; i <= taskCount; i = i + 25)
            {
                if (i + 24 <= taskCount)
                {
                    //第二个参数0表示爬取个人主页的微博，1表示爬取自己首页的微博
                    //25是通过实践得到的比较可靠的数字，当单个爬取程序爬取页面数大于25时可能被服务器屏蔽
                    crawler = new DianPinCrawler(poiList, i, 25);
                }
                else
                {
                    crawler = new DianPinCrawler(poiList, i, taskCount - i + 1);
                }
                crawler.RunCrawler();
            }
            Output(poiList, "dianpin");
            #endregion
        }
    }
}
