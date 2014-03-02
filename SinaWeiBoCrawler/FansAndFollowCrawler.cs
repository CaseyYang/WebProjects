using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Nodes;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;

namespace SinaWeiBoCrawler
{
    class FansAndFollowCrawler
    {
        #region 静态成员
        private static string cookieStr;//保存cookie字符串
        private static string loginUrl = "http://www.weibo.com/u/2432345394?wvr=5&";//登录URL
        public static string UserID = "1798541762";//爬取个人主页时需要的用户ID；该值只能在这里改，因为后面有静态成员依赖于该值，当程序运行时那些静态成员不会随该值改变而改变
        private static string[] queryUrl = { "http://weibo.com/p/100505" + UserID + "/follow?page=", "http://weibo.com/p/100505" + UserID + "/follow?relate=fans&page=" };
        private static AndFilter fanFilter;//过滤出包含一个粉丝信息的<li>标记
        private static AndFilter portraitFilter;//过滤出包含该粉丝用户头像照片的<div>标记
        private static AndFilter fanNameFilter;//过滤出包含用户名和地点的<div>标记
        private static AndFilter fanConnectFilter;//过滤出包含该粉丝用户的关注数/粉丝数/微博数的<div>标记
        private static AndFilter fanInfoFilter;//过滤出包含该粉丝用户简介的<div>标记
        private static AndFilter followMethodFilter;//过滤出包含关注该粉丝的方式的<div>标记
        #endregion

        public string Name
        {
            get;
            set;
        }//标识该爬虫名称
        public RelateType Type;//标识是爬取用户关注的人的列表还是用户的粉丝列表
        public string LoginUrl
        {
            get
            {
                return loginUrl;
            }
        }//静态成员loginUrl的获取器
        public string QueryUrl
        {
            get
            {
                return queryUrl[(int)Type];
            }
        }//静态成员queryUrl的获取器
        public string currentHtmlContent;//当前处理的网页
        private int startPage;//查询起始页
        private int queryRange;//查询页数范围
        private int page = 0;//当前访问的微博页面

        /// <summary>
        /// 设置登录网页版微博相关的cookie信息；内容从浏览器登录新浪微博时生成的cookie中获得
        /// </summary>
        private static void ReadInCookieFromFile()
        {
            StreamReader reader = new StreamReader("CookieForWeb.txt");
            while (!reader.EndOfStream)
            {
                string rawStr = reader.ReadLine();
                string[] strPair = rawStr.Split(' ');
                if (strPair.Length == 2)
                {
                    cookieStr += strPair[0] + "=" + strPair[1] + ";";
                }
                else
                {
                    Console.WriteLine("cookie信息有错！");
                }
            }
            reader.Close();
        }

        /// <summary>
        /// 配置各种HTML节点过滤器
        /// </summary>
        private static void MakeFilters()
        {
            HasAttributeFilter fansListFilterByClass = new HasAttributeFilter("class", "cnfList");
            HasAttributeFilter fanListFilterByNodeType = new HasAttributeFilter("node-type", "userListBox");
            AndFilter fansListFilter = new AndFilter(fanListFilterByNodeType, fansListFilterByClass);
            fanFilter = new AndFilter(new HasParentFilter(fansListFilter, false), new HasAttributeFilter("class", "clearfix S_line1"));
            HasAttributeFilter portraitFilterByParent = new HasAttributeFilter("class", "left");
            portraitFilter = new AndFilter(new HasParentFilter(portraitFilterByParent, false), new HasAttributeFilter("class", "face mbspace"));
            HasAttributeFilter fanNameFilterByParent = new HasAttributeFilter("class", "con_left");
            fanNameFilter = new AndFilter(new HasParentFilter(fanNameFilterByParent, false), new HasAttributeFilter("class", "name"));
            fanConnectFilter = new AndFilter(new HasParentFilter(fanNameFilterByParent, false), new HasAttributeFilter("class", "connect"));
            fanInfoFilter = new AndFilter(new HasParentFilter(fanNameFilterByParent, false), new HasAttributeFilter("class", "info"));
            followMethodFilter = new AndFilter(new HasParentFilter(fanNameFilterByParent, false), new HasAttributeFilter("class", "from W_textb"));
        }

        /// <summary>
        /// 静态构造函数，从本地文件读取cookie信息填充ccString；生成各种HTML节点过滤器
        /// </summary>
        static FansAndFollowCrawler()
        {
            ReadInCookieFromFile();
            MakeFilters();
        }

        /// <summary>
        /// 构造函数，设置、查询URL、查询起始页、查询范围等
        /// </summary>
        /// <param name="user">被爬取微博的用户</param>
        /// <param name="startPage">爬取起始页</param>
        /// <param name="queryRange">爬取范围</param>
        public FansAndFollowCrawler(RelateType type, int startPage, int queryRange)
        {
            Type = type;
            this.Name = Type + UserID;
            this.startPage = startPage;
            this.queryRange = queryRange;
        }

        /// <summary>
        /// 登录网页版微博
        /// </summary>
        public void LoginWeiBo()
        {
            //设置httpWebRequest请求的HTTP头
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(loginUrl);
            httpWebRequest.Referer = loginUrl;
            httpWebRequest.Accept = HttpHeader.Accept;
            httpWebRequest.UserAgent = HttpHeader.UserAgent;
            httpWebRequest.Method = HttpHeader.Method;
            httpWebRequest.Headers["Cookie"] = cookieStr;
            httpWebRequest.GetResponse();
        }

        /// <summary>
        /// 从网页版微博中获取网页
        /// </summary>
        /// <param name="index">要获取页面的页面序号</param>
        public void GetHtmlFromWeiBo(int index)
        {
            page = index;
            //获取页面的第一段微博
            string queryUrlComplete = queryUrl[(int)Type] + page;
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(queryUrlComplete);
            httpWebRequest.Headers["Cookie"] = cookieStr;
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
            {
                StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                //从Http响应请求流中获得包含用户粉丝的HTML和微博内容的HTML
                GetHtmlFromHttpResponse(reader);
                reader.Close();
            }
            else
            {
                Console.WriteLine("请求第" + index + "个页面出错！");
            }
            httpWebResponse.Close();
        }

        /// <summary>
        /// 从网页版微博中获取微博信息
        /// </summary>
        /// <param name="fansList">保存爬得的粉丝数组</param>
        public void GetInfoFromHtml(List<Fan> fansList)
        {
            Lexer lexer = new Lexer(currentHtmlContent);
            Parser parser = new Parser(lexer);
            //获取包含每条微博的div标记列表
            NodeList fansNodeList = parser.Parse(fanFilter);
            for (int i = 0; i < fansNodeList.Size(); i++)
            {
                Fan fan = new Fan();
                //获取包含一个粉丝的<li>标记
                Bullet fanBullet = (Bullet)fansNodeList[i];

                #region 获取该粉丝头像
                NodeList fanPortraitNodeList = fanBullet.Children.ExtractAllNodesThatMatch(portraitFilter, true);
                if (fanPortraitNodeList.Size() == 1)
                {
                    Div fanPortraitDiv = (Div)fanPortraitNodeList[0];
                    NodeList imgNodeList = fanPortraitDiv.Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ImageTag)), true);
                    if (imgNodeList.Size() == 1)
                    {
                        ImageTag imgNode = (ImageTag)imgNodeList[0];
                        if (imgNode.Attributes.ContainsKey("SRC") && imgNode.Attributes.ContainsKey("ALT"))
                        {
                            string imgUrl = imgNode.GetAttribute("SRC");
                            string imgName = imgNode.GetAttribute("ALT");
                            fan.Name = imgName;
                            WebClient wc = new WebClient();//使用WebClient是因为下载用户头像不用登录cookie
                            wc.DownloadFileAsync(new Uri(imgUrl), @"portrait\" + imgName + ".jpg");
                            wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "个粉丝中，<img>标记缺少必要的属性！");
                        }

                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个粉丝中，获取img标记出错！");
                    }
                }
                else
                {
                    Console.WriteLine("第" + i + "个粉丝中，获取粉丝头像的标准出错！");
                }
                #endregion

                #region 获取该粉丝的关注数/粉丝数/微博数
                NodeList fanConnectNodeList = fanBullet.Children.ExtractAllNodesThatMatch(fanConnectFilter, true);
                if (fanConnectNodeList.Size() == 1)
                {
                    NodeList ATagList = fanConnectNodeList[0].Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ATag)), true);
                    if (ATagList.Size() == 3)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            ATag aTag = (ATag)ATagList[j];
                            switch (j)
                            {
                                case 0:
                                    if (aTag.Attributes.ContainsKey("HREF") && aTag.GetAttribute("HREF").Contains("follow"))
                                    {
                                        fan.FollowCount = Int32.Parse(aTag.StringText);
                                    }
                                    else
                                    {
                                        Console.WriteLine("第" + i + "个粉丝中，获取粉丝的关注数出错！");
                                    }
                                    break;
                                case 1:
                                    if (aTag.Attributes.ContainsKey("HREF") && aTag.GetAttribute("HREF").Contains("fans"))
                                    {
                                        fan.FansCount = Int32.Parse(aTag.StringText);
                                    }
                                    else
                                    {
                                        Console.WriteLine("第" + i + "个粉丝中，获取粉丝的粉丝数出错！");
                                    }
                                    break;
                                default:
                                    fan.FeedsCount = Int32.Parse(aTag.StringText);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个粉丝中，获取粉丝关注数/粉丝数/微博数的数量出错！");
                    }
                }
                else
                {
                    Console.WriteLine("第" + i + "个粉丝中，获取粉丝关注数/粉丝数/微博数的标准出错！");
                }
                #endregion

                #region 获取该粉丝的简介信息
                NodeList fanInfoNodeList = fanBullet.Children.ExtractAllNodesThatMatch(fanInfoFilter, true);
                if (fanInfoNodeList.Size() == 1)
                {
                    //Console.WriteLine(fanInfoNodeList[0].Parent.ToHtml());
                    Div fanInfoDiv = (Div)fanInfoNodeList[0];
                    string intro = fanInfoDiv.StringText;
                    if (intro.Substring(0, 2).Equals("简介"))
                    {
                        fan.Introduction = intro.Substring(3, intro.Length - 3).Replace("\n", " ").Replace("\t", " ");
                    }
                }
                else
                {
                    if (fanInfoNodeList.Size() == 0)
                    {
                        fan.Introduction = "";
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个粉丝中，获取粉丝简介的标准出错！");
                    }
                }
                #endregion

                #region 获取该粉丝的UserID、地点和性别信息；校验该粉丝的用户名信息
                NodeList fanLocationNodeList = fanBullet.Children.ExtractAllNodesThatMatch(fanNameFilter, true);
                if (fanLocationNodeList.Size() == 1)
                {
                    //获取粉丝的UserID信息；校验该粉丝的用户名信息
                    NodeList aTagNodeList = fanLocationNodeList[0].Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ATag)), true);
                    if (aTagNodeList.Size() >= 1)
                    {
                        ATag nameNode = (ATag)aTagNodeList[0];
                        if (nameNode.Attributes.ContainsKey("USERCARD") && nameNode.Attributes.ContainsKey("HREF"))
                        {
                            //获取粉丝的UserID信息
                            string uidStr = nameNode.GetAttribute("USERCARD");
                            if (uidStr.Substring(0, 3).Equals("id="))
                            {
                                fan.UserID = uidStr.Substring(3, uidStr.Length - 3);
                            }

                            //获取粉丝的微博链接
                            string linkUrl = nameNode.GetAttribute("HREF");
                            fan.LinkURL = "http://www.weibo.com" + linkUrl;
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "个粉丝中，包含用户id和链接的<a>标记中缺少必要的属性！");
                        }
                        //校验该粉丝的用户名信息
                        if (!nameNode.StringText.Equals(fan.Name))
                        {
                            Console.WriteLine("第" + i + "个粉丝中，用户名与用户头像文字描述不一致！");
                        }
                    }

                    //获取粉丝的性别和地点信息
                    NodeList locationNodeList = fanLocationNodeList[0].Children.ExtractAllNodesThatMatch(new HasAttributeFilter("class", "addr"), true);
                    if (locationNodeList.Size() == 1)
                    {
                        string locationStr = "";
                        for (int j = 0; j < locationNodeList[0].Children.Size(); j++)
                        {
                            INode node = locationNodeList[0].Children[j];
                            if (node.GetType().Equals(typeof(TextNode)))
                            {
                                TextNode tNode = (TextNode)node;
                                locationStr += tNode.ToPlainTextString();
                            }
                            if (node.GetType().Equals(typeof(TagNode)))
                            {
                                TagNode tNode = (TagNode)node;
                                if (tNode.Attributes.ContainsKey("CLASS"))
                                {
                                    if (tNode.GetAttribute("CLASS").Contains("female"))//必须先female，因为female中也含有male，如果male在前，则所有用户均符合该条件了= =
                                    {
                                        fan.Gender = "female";
                                    }
                                    else
                                    {
                                        if (tNode.GetAttribute("CLASS").Contains("male"))
                                        {
                                            fan.Gender = "male";
                                        }
                                        else
                                        {
                                            fan.Gender = "unknown";
                                            Console.WriteLine("第" + i + "个粉丝性别不明！");
                                        }
                                    }
                                }
                            }
                        }
                        fan.Location = locationStr.Trim();
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个粉丝中，获取粉丝地点的标准出错！");
                    }
                }
                else
                {
                    Console.WriteLine("第" + i + "个粉丝中，获取该粉丝的UserID、地点和性别信息的标准出错！");
                }
                #endregion

                #region 获取该粉丝关注用户的方式
                NodeList followMethodNodeList = fanBullet.Children.ExtractAllNodesThatMatch(followMethodFilter, true);
                if (followMethodNodeList.Size() == 1)
                {
                    NodeList methodNodeList = followMethodNodeList[0].Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ATag)));
                    if (methodNodeList.Size() == 1)
                    {
                        ATag methodNode = (ATag)methodNodeList[0];
                        fan.FollowMethod = methodNode.StringText.Trim();
                    }
                    else
                    {
                        Console.WriteLine("第" + i + "个粉丝中，获取该粉丝关注用户的方式的数量出错！");
                    }
                }
                else
                {
                    Console.WriteLine("第" + i + "个粉丝中，获取该粉丝关注用户的方式的标准出错！");
                }
                #endregion

                fansList.Add(fan);
            }
        }

        /// <summary>
        /// WebClient完成下载文件后调用的操作
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WebClient wc = (WebClient)sender;
            wc.Dispose();
        }

        /// <summary>
        /// 辅助函数：从原始HTML中提取包含用户信息和微博内容的HTML代码
        /// </summary>
        /// <param name="reader">传输HTTP返回内容的流</param>
        private void GetHtmlFromHttpResponse(StreamReader reader)
        {
            string contentForFollowOrFan = "";
            //解析json数组
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JsonJsPart));
            MemoryStream ms = null;
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                //如果user为空，则获取用户信息
                if (str.Contains("<script>FM.view({\"ns\":\"pl.content.followTab.index\",\"domid"))
                {
                    int start = str.IndexOf('{');
                    int last = str.LastIndexOf('}');
                    contentForFollowOrFan = str.Substring(start, last - start + 1);
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(contentForFollowOrFan));
                    JsonJsPart json = (JsonJsPart)serializer.ReadObject(ms);
                    currentHtmlContent = json.html;
                    break;
                }
            }
            if (ms != null)
            {
                ms.Close();
            }
        }

        /// <summary>
        /// 调用爬取微博粉丝程序
        /// </summary>
        /// <param name="fansList">保存爬得的粉丝数组</param>
        public void RunCrawler(List<Fan> fansList)
        {
            LoginWeiBo();
            int index = 0;
            for (int i = 0; i < queryRange; i++)
            {
                index = startPage + i;
                Console.WriteLine("获取网页……");
                GetHtmlFromWeiBo(index);
                Console.WriteLine("析取微博……");
                GetInfoFromHtml(fansList);
                Console.WriteLine("第" + index + "个页面处理完毕！");
                if (i + 1 < queryRange)
                {
                    Thread.Sleep(7000);
                }
            }
        }

        /// <summary>
        /// 调试用：保存微博网页到文件
        /// </summary>
        /// <param name="htmlContent">HTML文本</param>
        /// <param name="index">页面序号</param>
        /// <param name="part">微博段序号</param>
        private static void SaveWebpageAsFile(string htmlContent, int index, int part)
        {
            StreamWriter writer;
            writer = new StreamWriter("weibo" + index + "-" + (part + 1) + ".html");
            writer.Write(htmlContent);
            writer.Close();
        }
    }
}
