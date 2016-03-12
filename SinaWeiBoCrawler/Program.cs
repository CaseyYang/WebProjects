using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace SinaWeiBoCrawler
{
    /// <summary>
    /// 表示要爬取的页面类型
    /// </summary>
    enum PageType
    {
        /// <summary>
        /// 爬取某个人的个人主页
        /// </summary>
        PersonalPage = 0,
        /// <summary>
        /// 爬取自己首页上所有关注的微博
        /// </summary>
        HomePage = 1
    }
    /// <summary>
    /// 表示要爬取的关系类型
    /// </summary>
    enum RelateType
    {
        /// <summary>
        /// 爬取某个人关注的用户
        /// </summary>
        Follow = 0,
        /// <summary>
        /// 爬取某个人的粉丝
        /// </summary>
        Fan = 1
    }
    class Program
    {
        /// <summary>
        /// 调试用：从本地文件中读入HTML文本
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <returns>读取到的HTML文本</returns>
        static string GetHtmlFromLocalFile(string filepath)
        {
            StreamReader reader = new StreamReader(filepath);
            string str = reader.ReadToEnd();
            reader.Close();
            return str;
        }

        /// <summary>
        /// 输出
        /// </summary>
        /// <param name="user">爬取得到的用户</param>
        /// <param name="fileName">文件名</param>
        public static void Output(Object result, string fileName)
        {
            #region 输出到csv文件：优点：使用Excel查看、直观；缺点：对内容中的逗号和引号需做转义处理
            //StreamWriter writer = new StreamWriter("weibo.csv");
            //writer.WriteLine("微博内容,是否转发微博,转发微博来源,发送时间,发送设备");
            //foreach (Feed feed in feedList)
            //{
            //    writer.WriteLine(feed.Content + "," + feed.ReFeedOrNot + "," + feed.ReFeedFrom + "," + feed.Time + "," + feed.Device);
            //}
            //writer.Close();
            #endregion

            #region 输出到XML文件
            StreamWriter writer = new StreamWriter(fileName + ".xml");
            XmlSerializer sr = new XmlSerializer(result.GetType());
            sr.Serialize(writer, result);
            writer.Close();
            #endregion
        }

        /// <summary>
        /// 辅助函数：处理时间字符串
        /// </summary>
        /// <param name="time">原始时间字符串</param>
        /// <returns>处理过的时间字符串</returns>
        public static string GetTime(string time)
        {
            string result = "";
            for (int i = 0; i < time.Length; i++)
            {
                if ((int)time[i] == 160)
                {
                    result = time.Substring(0, i);
                    break;
                }
            }
            return result;
        }

        static void Main(string[] args)
        {
            #region 调试程序
            //FansCrawler fansCrawler = new FansCrawler(1, 1);
            ////fansCrawler.GetHtmlFromWeiBo(1);
            //fansCrawler.ReadInHtmlContent();
            //List<Fan> fansList = new List<Fan>();
            //StreamReader reader = new StreamReader("content.html");
            //string content = reader.ReadToEnd();
            //fansCrawler.currentHtmlContent = content;
            //fansCrawler.GetInfoFromHtml(fansList);
            //OutputFansList(fansList);
            #endregion

            #region 正式爬取微博程序
            User user = new User();
            ICrawler crawler = null;
            int taskCount = 278;
            for (int i = 1; i <= taskCount; i = i + 25)
            {
                if (i + 24 <= taskCount)
                {
                    //第二个参数0表示爬取个人主页的微博，1表示爬取自己首页的微博
                    //25是通过实践得到的比较可靠的数字，当单个爬取程序爬取页面数大于25时可能被服务器屏蔽
                    crawler = new WebCrawler(user, PageType.PersonalPage, i, 25);
                }
                else
                {
                    crawler = new WebCrawler(user, PageType.PersonalPage, i, taskCount - i + 1);
                }
                crawler.RunCrawler(user.FeedList);
            }
            Output(user, crawler.Name);
            #endregion

            #region 正式爬取粉丝程序

            //List<Fan> fansList = new List<Fan>();
            //FansAndFollowCrawler fansCrawler = null;
            //int taskCountForFansPage =58;
            //RelateType type = RelateType.Follow;

            //for (int i = 1; i <= taskCountForFansPage; i = i + 25)
            //{
            //    if (i + 24 <= taskCountForFansPage)
            //    {
            //        fansCrawler = new FansAndFollowCrawler(type, i, 25);
            //    }
            //    else
            //    {
            //        fansCrawler = new FansAndFollowCrawler(type, i, taskCountForFansPage - i + 1);
            //    }
            //    fansCrawler.RunCrawler(fansList);
            //}
            //Output(fansList, fansCrawler.Name);
            //Console.WriteLine("关注总数：" + fansList.Count);
            #endregion
        }
    }
}
