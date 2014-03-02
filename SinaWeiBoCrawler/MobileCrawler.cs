using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Web;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Nodes;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;

namespace SinaWeiBoCrawler
{
    class MobileCrawler : ICrawler
    {
        private static string loginUrl = "http://weibo.cn/?gsid=4u3w110b19JjKJOUzGaw3acLn6m&vt=4";//登录URL
        private static string queryUrl = "http://weibo.cn/0xiaoyu1?&vt=4&st=e89c&page=";//查询URL
        private static CookieContainer cc = null;//移动版微博登录cookie
        private static string cookieStr;//cookie字符串

        public string Name
        {
            get;
            set;
        }
        public CookieContainer Cc
        {
            get { return MobileCrawler.cc; }
        }
        public string Cookie//静态成员cookieStr的获取器
        {
            get
            {
                return cookieStr;
            }
        }
        public string LoginUrl//静态成员loginUrl的获取器
        {
            get { return loginUrl; }
        }
        public string QueryUrl//静态成员queryUrl的获取器
        {
            get { return queryUrl; }
        }
        public string htmlContent;//当前爬取的网页
        private int startPage;//查询起始页
        private int queryRange;//查询页数范围
        public User user;//被爬取微博的用户


        /// <summary>
        /// 设置登录移动版微博相关的cookie信息；方法类似MakeCookieForWeb
        /// </summary>
        private static void ReadInCookieFromFile()
        {
            cc = new CookieContainer();
            StreamReader reader = new StreamReader("CookieForMobile.txt");
            while (!reader.EndOfStream)
            {
                string rawStr = reader.ReadLine();
                string[] strPair = rawStr.Split(' ');
                if (strPair.Length == 2)
                {
                    cookieStr += strPair[0] + "=" + strPair[1] + ";";
                    cc.Add(new Uri("http://www.weibo.com"), new Cookie(strPair[0], strPair[1]));
                }
                else
                {
                    Console.WriteLine("cookie信息有错！");
                }
            }
            reader.Close();
            //cc = new CookieContainer();
            //cc.Add(new System.Uri("http://www.weibo.com "), new Cookie("gsid_CTandWM", "4u3w110b19JjKJOUzGaw3acLn6m"));
        }

        /// <summary>
        /// 静态构造函数，从本地文件读取cookie信息填充cc
        /// </summary>
        static MobileCrawler()
        {
            ReadInCookieFromFile();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="user">被爬取微博的用户</param>
        /// <param name="startPage">爬取起始页</param>
        /// <param name="queryRange">爬取范围</param>
        public MobileCrawler(User user, int startPage, int queryRange)
        {
            this.Name = "Mobile";
            this.startPage = startPage;
            this.queryRange = queryRange;
            this.user = user;
        }

        /// <summary>
        /// 登录移动版微博
        /// </summary>
        public void LoginWeiBo()
        {
            //设置httpWebRequest请求的HTTP头
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(loginUrl);
            httpWebRequest.CookieContainer = cc;
            httpWebRequest.Referer = loginUrl;
            httpWebRequest.Accept = HttpHeader.Accept;
            httpWebRequest.UserAgent = HttpHeader.UserAgent;
            httpWebRequest.Method = HttpHeader.Method;

            httpWebRequest.GetResponse();
        }

        /// <summary>
        /// 从移动版微博中获取网页
        /// </summary>
        /// <param name="index">要获取页面的页面序号</param>
        public void GetHtmlFromWeiBo(int index)
        {
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(queryUrl + index);
            httpWebRequest.CookieContainer = cc;
            StreamReader reader = new StreamReader(new BufferedStream(httpWebRequest.GetResponse().GetResponseStream(), 4 * 200 * 1024));
            string content = reader.ReadToEnd();
            StreamWriter writer = new StreamWriter("weibo" + index + ".html");
            writer.Write(content);
            writer.Close();
            htmlContent = content;
        }

        /// <summary>
        /// 从移动版微博中获取微博信息
        /// </summary>
        /// <param name="index">要获取页面的页面序号</param>
        /// <param name="feedList">保存微博的Feed数组</param>
        public void GetInfoFromHtml(int index, List<Feed> feedList)
        {
            Lexer lexer = new Lexer(htmlContent);
            Parser parser = new Parser(lexer);
            //移动版网页中，爬取个人主页的微博，过滤出包含用户名称和信息的div
            HasAttributeFilter userFilter = new HasAttributeFilter("class", "u");
            //移动版网页中，每条微博的div都含有class=c的属性
            HasAttributeFilter feedFilter = new HasAttributeFilter("class", "c");
            //移动版网页中，每条转发微博的第一个子div中都含有带class=c的属性的span标记
            HasAttributeFilter refeedFilter = new HasAttributeFilter("class", "cmt");
            //移动版网页中，每条微博内容都存于带class="ctt"的属性的span标记内
            HasAttributeFilter feedContentFilter = new HasAttributeFilter("class", "ctt");
            //移动版网页中，每条微博的发送时间和发送方式都存于带class="ct"的属性的span标记内
            HasAttributeFilter feedTimeFilter = new HasAttributeFilter("class", "ct");
            //在移动版网页中过滤出包含每条微博的转发理由的div。注意：内层的HasChildFilter只过滤出了包含文字“转发理由:”的span标记，所以需要再套一层HasChildFilter才能得到包含span标记的div
            HasChildFilter reFeedReasonFilter = new HasChildFilter(new HasChildFilter(new StringFilter("转发理由:")));

            //若user.NickName为空，则说明是第一次爬取该个人主页的微博，需要获得用户信息
            if (user.NickName.Equals(""))
            {
                #region 爬取个人主页的微博，首先获得用户信息
                NodeList userNodeList = parser.Parse(userFilter);
                if (userNodeList.Size() == 1)
                {
                    NodeList userDetailNodeList = userNodeList[0].Children.ExtractAllNodesThatMatch(feedContentFilter, true);//此处只是借用feedContentFilter过滤器，因为要过滤的节点正好符合这个过滤器
                    if (userDetailNodeList.Size() >= 2)
                    {
                        //获取微博用户名
                        if (userDetailNodeList[0].Children[0].GetType().Equals(typeof(TextNode)))
                        {
                            string nickName = ((TextNode)userDetailNodeList[0].Children[0]).ToPlainTextString();
                            //尝试把备注名提取出来
                            if (nickName.Contains("("))
                            {
                                int start = nickName.IndexOf('(');
                                int end = nickName.IndexOf(')');
                                if (end > start)
                                {
                                    string remarkName = nickName.Substring(start + 1, end - start - 1);
                                    user.RemarkName = remarkName;
                                }
                                user.NickName = nickName.Substring(0, start);
                            }
                            else
                            {
                                user.NickName = nickName;
                            }
                        }
                        else
                        {
                            Console.WriteLine("获取微博用户名出错！");
                        }
                        //获取自我描述
                        user.SelfIntroduction = ((Span)userDetailNodeList[1]).StringText;
                    }
                    else
                    {
                        Console.WriteLine("获取包含微博用户名和自我描述的div出错！");
                    }
                }
                else
                {
                    Console.WriteLine("获取包含微博用户信息的div出错！");
                }
                //注意：重复使用parser前一定要调用Reset方法
                parser.Reset();
                #endregion
            }
            NodeList feedNodeList = parser.Parse(feedFilter);
            int count = 0;
            for (int i = 0; i < feedNodeList.Size(); i++)
            {
                //保存该条微博
                Feed feed = new Feed();
                feed.Page = index;
                feed.Number = i + 1;
                //记录微博条数
                count++;

                //取得第i条微博的div；
                //把一个node转为具体的TagNode，以便取得其中的属性值
                TagNode feedNode = (TagNode)feedNodeList[i];
                //注意：获取某个属性的值时，作为键值的属性需要大写，如“ID”
                if (feedNode.Attributes.Contains("ID"))//若ID属性不存在，则说明不是这个节点不是微博内容
                {
                    //通过分析移动版网页可知，
                    //每条微博的div中的一个子div中一般包含微博内容；
                    //第二个子div包含图片和发送时间等
                    //若是转发微博，则有第三个子div，其中包含转发理由、转发来源和时间等

                    //第一个子div
                    TagNode feedFirstDiv = (TagNode)feedNode.Children[0];
                    //找出包含转发微博的标记
                    NodeList reFeedList = feedFirstDiv.Children.ExtractAllNodesThatMatch(refeedFilter, true);
                    if (reFeedList.Size() > 0)//实践表明，class="cmt"属性往往不止被转发微博所使用
                    {
                        if (HttpUtility.HtmlDecode(((TextNode)reFeedList[0].Children[0]).ToPlainTextString()).Substring(0, 2).Equals("转发"))//为了保证取到的是转发微博的来源，故加这一条辅助判断
                        {
                            feed.ReFeedOrNot = true;
                            feed.OriginalAuthor = HttpUtility.HtmlDecode(((ATag)reFeedList[0].Children[1]).StringText);
                            //找到包含转发理由的子div
                            NodeList reFeedReasonList = feedNode.Children.ExtractAllNodesThatMatch(reFeedReasonFilter, true);
                            if (reFeedReasonList.Size() == 1)
                            {
                                TagNode reFeedReasonDiv = (TagNode)reFeedReasonList[0];
                                //在包含转发理由的子div中，第一个子节点总为span标记，为文本“转发理由”四字
                                //第二个子节点开始的一些系列子节点组成保存转发理由的内容，可能有文本，有链接（@某人）
                                //判断转发理由结束的几个条件：若为文本节点，则最后两个字符应为“//”；若为链接节点，则其文本应为“赞[X]”（或其链接为“http://weibo.cn/attitude/……”）
                                for (int j = 1; j < reFeedReasonDiv.Children.Size(); j++)
                                {
                                    Type t = reFeedReasonDiv.Children[j].GetType();
                                    if (t.Equals(typeof(TextNode)))
                                    {
                                        string str = HttpUtility.HtmlDecode(((TextNode)reFeedReasonDiv.Children[j]).ToPlainTextString());
                                        if (str.Length >= 2 && str.Substring(str.Length - 2, 2).Equals("//"))
                                        {
                                            feed.ReFeedReason += str.Substring(0, str.Length - 2);
                                            feed.ReFeedFrom = HttpUtility.HtmlDecode(((ATag)reFeedReasonDiv.Children[j + 1]).StringText);
                                            if (feed.ReFeedFrom.Substring(0, 1).Equals("@"))//去掉上一个转发者前的@符号
                                            {
                                                feed.ReFeedFrom = feed.ReFeedFrom.Substring(1);
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            feed.ReFeedReason += str;
                                        }
                                        continue;
                                    }
                                    if (t.Equals(typeof(ATag)))
                                    {
                                        string str = HttpUtility.HtmlDecode(((ATag)reFeedReasonDiv.Children[j]).StringText);
                                        if (str.Substring(0, 1).Equals("赞"))
                                        {
                                            feed.ReFeedFrom = feed.OriginalAuthor;
                                            break;
                                        }
                                        else
                                        {
                                            feed.ReFeedReason += str;
                                        }
                                        continue;
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("好像找到不止一个转发理由？！");
                            }
                        }
                        else
                        {
                            Console.WriteLine("糟糕！第" + count + "条微博中，找不到转发微博的来源！");
                        }
                    }
                    //找出包含微博正文的标记
                    NodeList feedContentList = feedFirstDiv.Children.ExtractAllNodesThatMatch(feedContentFilter, true);
                    switch (feedContentList.Size())
                    {
                        case 1:
                            //微博正文包含在一个span标记内
                            Span feedContentListNode = (Span)feedContentList[0];
                            //因为微博正文是不确定数量的文本和链接（如@某人）的组合，因此对于span的每个子节点，根据其类型（是文本节点还是链接节点），分别处理
                            for (int j = 0; j < feedContentListNode.Children.Size(); j++)
                            {
                                Type t = feedContentListNode.Children[j].GetType();
                                if (t.Equals(typeof(TextNode)))
                                {
                                    feed.Content += HttpUtility.HtmlDecode(((TextNode)feedContentListNode.Children[j]).ToPlainTextString());
                                    continue;
                                }
                                if (t.Equals(typeof(ATag)))
                                {
                                    feed.Content += HttpUtility.HtmlDecode(((ATag)feedContentListNode.Children[j]).StringText);
                                    continue;
                                }
                            }
                            break;
                        default:
                            Console.WriteLine("糟糕！第" + count + "条微博中，取得微博正文的判断标准出错了！");
                            break;
                    }

                    //从整个feed的范围内，找出包含微博发送时间的标记
                    NodeList feedTimeList = feedNode.Children.ExtractAllNodesThatMatch(feedTimeFilter, true);
                    switch (feedTimeList.Size())
                    {
                        case 1:
                            string time = HttpUtility.HtmlDecode(((TextNode)((Span)feedTimeList[0]).Children[0]).ToHtml());
                            feed.Time = Program.GetTime(time);
                            if (feedTimeList[0].Children.Size() > 1)
                            {
                                feed.Device = HttpUtility.HtmlDecode(((ATag)((Span)feedTimeList[0]).Children[1]).StringText);
                            }
                            //从包含微博发送时间的标记往前推，便是“赞”、“转发”和“评论”的标记
                            INode node = feedTimeList[0];
                            for (int j = 0; j < 9; j++)
                            {
                                node = node.PreviousSibling;
                                switch (j)
                                {
                                    case 4:
                                        //评论
                                        string strCommentCount = ((ATag)node).StringText;
                                        feed.CommentCount = Int32.Parse(strCommentCount.Substring(3, strCommentCount.Length - 4));
                                        break;
                                    case 6:
                                        //转发
                                        string strReFeedCount = ((ATag)node).StringText;
                                        feed.ReFeedCount = Int32.Parse(strReFeedCount.Substring(3, strReFeedCount.Length - 4));
                                        break;
                                    case 8:
                                        //赞
                                        string strLikeCount = ((ATag)node).StringText;
                                        feed.LikeCount = Int32.Parse(strLikeCount.Substring(2, strLikeCount.Length - 3));
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        default:
                            Console.WriteLine("糟糕！第" + count + "条微博中，取得微博时间的判断标准出错了！");
                            break;
                    }
                    feedList.Add(feed);
                }
            }
        }

        /// <summary>
        /// 调用移动版微博爬虫程序
        /// </summary>
        /// <param name="feedList">保存微博的Feed数组</param>
        public void RunCrawler(List<Feed> feedList)
        {
            LoginWeiBo();
            int index = 0;
            for (int i = 0; i < queryRange; i++)
            {
                index = startPage + i;
                GetHtmlFromWeiBo(index);
                GetInfoFromHtml(i + 1, feedList);
                Console.WriteLine("第" + index + "个页面处理完毕！");
            }
        }
    }
}
