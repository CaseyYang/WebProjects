using System;
using System.Collections.Generic;
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
    class WebCrawler : ICrawler
    {
        #region 静态成员
        private static string cookieStr;//保存cookie字符串
        private static string loginUrl = "http://www.weibo.com/u/2432345394?wvr=5&";//登录URL
        private static string userID = "1870396370";//爬取个人主页时需要的用户ID
        private static string[] queryUrl = { "http://weibo.com/p/100505" + userID + "/weibo?page=", "http://www.weibo.com/u/2432345394?wvr=5&page=" };//查询URL，第一个是爬取个人主页的，第二个是爬取自己首页的
        private static string[] pagebarQueryUrl = { "http://weibo.com/p/aj/mblog/mbloglist?domain=100505&id=100505" + userID + "&count=15&page=", "http://weibo.com/aj/mblog/fsearch?_wv=5&count=15page=" };//分段加载微博的查询URL，得到的是一个json数组；和上面对应，第一个是爬取个人主页的，第二个是爬取自己首页的
        private static List<HasAttributeFilter> idFilter;//过滤出包含mid属性的div，mid给maxid和endid供值
        private static HasAttributeFilter feedFilter;//过滤出每条微博的div
        private static AndFilter feedAuthorFilter;//过滤出包含微博发送者的div
        private static AndFilter feedContentFilter;//过滤出包含微博内容的div
        private static HasAttributeFilter reFeedFilter;//过滤出包含转发微博的div
        private static AndFilter reFeedAuthorFilter;//过滤出转发微博的原发送者的div
        private static AndFilter reFeedContentFilter;//过滤出转发微博的内容
        private static HasAttributeFilter refeedDeletedFilter1;//转发微博已被删除(适用于该div位于reFeedFilter过滤出的div下的情况)
        private static AndFilter refeedDeletedFilter2;//转发微博已被删除(适用于该div位于<div class="WB_datail">下的情况)
        private static AndFilter similarFeedCountFilter;//过滤出包含对原微博转发数的<b>标记
        private static AndFilter similarFeedFilter;//过滤出包含对原微博类似转发的标记
        private static AndFilter feedLocationFilter;//过滤出包含微博发送地点的div
        private static AndFilter feedFromFilter;//过滤出包含发送时间和发送方式的div
        private static AndFilter feedLikeFilter;//过滤出包含“赞”数的链接标记
        private static AndFilter feedForwardFilter;//过滤出包含转发数的链接标记
        private static AndFilter feedCommentFilter;//过滤出包含评论数的链接标记
        private static AndFilter feedTimeFilter;//过滤出包含微博发送时间的链接标记
        private static AndFilter feedSendTypeFilter;//过滤出包含微博发送方式的链接标记
        #endregion

        public string Name
        {
            get;
            set;
        }//标识该爬虫是爬取网页版微博还是移动版微博：网页版微博设该值为"Web";移动版微博设该值为"Mobile"
        public PageType Type;//标识是爬取个人主页上的微博还是自己首页上的微博；0代表爬取个人主页上的微博；1代表爬取自己首页上的微博；可以作为下标选择queryUrlWeb和pagebarQueryUrl中的元素
        public string LoginUrl//静态成员loginUrl的获取器
        {
            get
            {
                return loginUrl;
            }
        }
        public string QueryUrl//静态成员queryUrl的获取器
        {
            get
            {
                return queryUrl[(int)Type];
            }
        }
        public string Cookie//静态成员cookieStr的获取器
        {
            get
            {
                return cookieStr;
            }
        }
        public List<string> htmlContentList = new List<string>();//爬取的网页集合
        private string currentUserHtml;//当前获取到的用户信息的HTML代码
        private string currentHtmlContent;//当前处理的网页
        private int startPage;//查询起始页
        private int queryRange;//查询页数范围
        private string firstPartQueryUrl;//查询第一部分微博的URL
        private string end_id = "";//end_id是指当前页面第一条微博的mid值
        private string max_id = "";//max_id是指当前页面最后一条微博的mid值
        private int pre_page = 1;//pre_page指访问的上一个微博页面
        private int page = 0;//当前访问的微博页面
        private int pagebar = 0;//当前已获取到的微博段：当一个页面加载后，请求第二段微博时pagebar=0；请求第三段微博时pagebar=1
        private int serialNumber = 0;//记录该条微博在该页中的序号，一般情况下serialNumberInThisPage和下标相等，但当存在“还有X条对原微博的转发”时，serialNumberInThisPage大于下标，所以需要特别记录
        public User user;//被爬取微博的用户

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
            //爬取个人主页时，使用如下过滤器得到包含mid属性的div；mid和maid以及endid相关
            idFilter = new List<HasAttributeFilter>();
            idFilter.Add(new HasAttributeFilter("class", "WB_feed_type SW_fun  "));
            //过滤出每条微博的div
            feedFilter = new HasAttributeFilter("class", "WB_feed_datail S_line2 clearfix");
            idFilter.Add(feedFilter);
            //过滤出包含微博发送者的div：因为转发微博的div也包含属性class="WB_info"，所以使用两个过滤器更为可靠
            HasAttributeFilter wbDetailFilter = new HasAttributeFilter("class", "WB_detail");
            feedAuthorFilter = new AndFilter(new HasAttributeFilter("class", "WB_info"), new HasParentFilter(wbDetailFilter, false));
            //过滤出包含微博内容的div：因为转发微博的div也包含属性class="WB_text"，所以使用两个过滤器更为可靠
            feedContentFilter = new AndFilter(new HasAttributeFilter("class", "WB_text"), new HasAttributeFilter("node-type", "feed_list_content"));
            //过滤出包含转发微博的div
            reFeedFilter = new HasAttributeFilter("node-type", "feed_list_forwardContent");
            //过滤出转发微博的原发送者的div：因为类似的原因，所以需要两个过滤器
            reFeedAuthorFilter = new AndFilter(new HasAttributeFilter("class", "WB_info"), new HasParentFilter(reFeedFilter, true));
            //过滤出转发微博的内容：因为类似的原因，所以需要两个过滤器
            reFeedContentFilter = new AndFilter(new HasAttributeFilter("class", "WB_text"), new HasAttributeFilter("node-type", "feed_list_reason"));
            //过滤出已被删除的转发微博(适用于该div位于reFeedFilter过滤出的div下的情况)
            refeedDeletedFilter1 = new HasAttributeFilter("class", "WB_deltxt");
            //过滤出已被删除的转发微博(适用于该div位于<div class="WB_datail">下的情况)
            refeedDeletedFilter2 = new AndFilter(new HasParentFilter(wbDetailFilter, true), refeedDeletedFilter1);
            //过滤出包含对原微博转发数的<b>标记
            similarFeedCountFilter = new AndFilter(new HasAttributeFilter("class", "S_spetxt"), new HasAttributeFilter("node-type", "followNum"));
            //过滤出包含对原微博类似转发的标记
            HasAttributeFilter similarFeedFilterByParent = new HasAttributeFilter("class", "WB_feed_datail S_line2 clearfix WB_feed_noLine");
            similarFeedFilter = new AndFilter(wbDetailFilter, new HasParentFilter(similarFeedFilterByParent, false));
            //过滤出包含微博发送地点的div
            feedLocationFilter = new AndFilter(new HasAttributeFilter("class", "map_data"), new HasParentFilter(wbDetailFilter, false));
            //过滤出包含微博发送时间、发送方式、转发数和评论数的div
            AndFilter feedMetaDataFilter = new AndFilter(new NotFilter(new HasParentFilter(new HasAttributeFilter("class", "WB_media_expand SW_fun2 S_line1 S_bg1"), true)), new HasAttributeFilter("class", "WB_func clearfix"));
            //过滤出包含转发数和评论数的div
            AndFilter feedHandleFilter = new AndFilter(new HasParentFilter(feedMetaDataFilter, false), new HasAttributeFilter("class", "WB_handle"));
            //过滤出包含发送时间和发送方式的div
            feedFromFilter = new AndFilter(new HasParentFilter(feedMetaDataFilter, false), new HasAttributeFilter("class", "WB_from"));
            //过滤出包含“赞”数的链接标记
            feedLikeFilter = new AndFilter(new HasParentFilter(feedHandleFilter, false), new HasAttributeFilter("action-type", "fl_like"));
            //过滤出包含转发数的链接标记
            feedForwardFilter = new AndFilter(new HasParentFilter(feedHandleFilter, false), new HasAttributeFilter("action-type", "fl_forward"));
            //过滤出包含评论数的链接标记
            feedCommentFilter = new AndFilter(new HasParentFilter(feedHandleFilter, false), new HasAttributeFilter("action-type", "fl_comment"));
            //过滤出包含微博发送时间的链接标记
            feedTimeFilter = new AndFilter(new HasParentFilter(feedFromFilter, false), new HasAttributeFilter("class", "S_link2 WB_time"));
            //过滤出包含微博发送方式的链接标记
            feedSendTypeFilter = new AndFilter(new HasParentFilter(feedFromFilter, false), new HasAttributeFilter("class", "S_link2"));
        }

        /// <summary>
        /// 静态构造函数，从本地文件读取cookie信息填充ccString；生成各种HTML节点过滤器
        /// </summary>
        static WebCrawler()
        {
            ReadInCookieFromFile();
            MakeFilters();
        }

        /// <summary>
        /// 构造函数，设置、查询URL、查询起始页、查询范围等
        /// </summary>
        /// <param name="user">被爬取微博的用户</param>
        /// <param name="type">要爬取的页面类型</param>
        /// <param name="startPage">爬取起始页</param>
        /// <param name="queryRange">爬取范围</param>
        public WebCrawler(User user, PageType type, int startPage, int queryRange)
        {
            this.Name = "Web";
            this.Type = type;
            this.startPage = startPage;
            this.queryRange = queryRange;
            this.user = user;
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
            httpWebRequest.Headers["Cookie"] = cookieStr;//设置cookie不能使用CookieContainer，因为在获取json时后者无效，原因不明= =
            httpWebRequest.GetResponse();
        }

        /// <summary>
        /// 从网页版微博中获取网页
        /// </summary>
        /// <param name="index">要获取页面的页面序号</param>
        public void GetHtmlFromWeiBo(int index)
        {
            //清空相关成员
            htmlContentList.Clear();
            serialNumber = 0;

            page = index;
            //获取页面的第一段微博
            firstPartQueryUrl = queryUrl[(int)Type] + page;// +"&pre_page=" + pre_page;
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(firstPartQueryUrl);
            httpWebRequest.Headers["Cookie"] = cookieStr;
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
            {
                StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                //从Http响应请求流中获得包含用户信息的HTML和微博内容的HTML
                GetHtmlFromHttpResponse(reader);
                //当爬取类型为“爬取个人主页上的微博”时，解析含有用户信息的HTML代码，填充user相关字段
                if (Type.Equals(PageType.PersonalPage) && user.NickName.Equals(""))
                {
                    GetUserInfoFromHtml(currentUserHtml);
                }
                httpWebResponse.Close();
                //调试用
                //SaveWebpageAsFile(currentHtmlContent, index, -1);
                htmlContentList.Add(currentHtmlContent);
                //调试用
                //Console.WriteLine("请求第1段微博地址：" + firstPartQueryUrl);
                //解析第一段微博，获取end_id
                if (GetEndIdFromHtml(htmlContentList[htmlContentList.Count - 1]))
                {
                    //pagebar递增，获取该页面的后面数段微博；具体循环次数视实际情况而定，暂定为2
                    for (pagebar = 0; pagebar < 2; pagebar++)
                    {
                        //解析最后一段微博，获取max_id
                        if (GetMaxIdFromHtml(htmlContentList[htmlContentList.Count - 1]))
                        {
                            string queryUrlForPagebar = pagebarQueryUrl[(int)Type] + page + "&pre_page=" + page + "&end_id=" + end_id + "&max_id=" + max_id + "&pagebar=" + pagebar;
                            //构造获取后两段微博的请求的HTTP头：真是痛苦的过程= =
                            httpWebRequest = HttpWebRequest.CreateHttp(queryUrlForPagebar);
                            httpWebRequest.Method = "GET";
                            httpWebRequest.Headers["Cookie"] = cookieStr;//不清楚原因，但看起来在请求json时只能如此添加cookie，而不能用httpWebRequest.CookieContainer
                            httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
                            {
                                reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                                //由于后面几段微博都是以json数组形式获得的，因此需要对json进行解析，获取其中的HTML代码内容
                                htmlContentList.Add(GetHtmlFromJson(reader.ReadToEnd()));
                                //调试用
                                //Console.WriteLine("请求第" + (pagebar + 2) + "段微博地址：" + queryUrlForPagebar);
                                //SaveWebpageAsFile(htmlContentList[htmlContentList.Count - 1], index, pagebar);
                            }
                            else
                            {
                                Console.WriteLine("请求第" + index + "个页面的第" + (pagebar + 2) + "段微博出错！");
                            }
                            httpWebResponse.Close();
                        }
                        else
                        {
                            Console.WriteLine("第" + index + "个页面获取max_id出错！");
                        }
                    }
                    reader.Close();
                }
                else
                {
                    Console.WriteLine("第" + index + "个页面获取end_id出错！");
                }
            }
            else
            {
                Console.WriteLine("请求第" + index + "个页面出错！");
            }
            //最后，更新pre_page
            pre_page = page;
        }

        /// <summary>
        /// 从网页版微博中获取微博信息
        /// </summary>
        /// <param name="currentPage">爬得的微博所在页面序号</param>
        /// <param name="feedList">保存爬得的微博的数组</param>
        public void GetInfoFromHtml(int currentPage, List<Feed> feedList)
        {
            foreach (string htmlContent in htmlContentList)
            {
                Lexer lexer = new Lexer(htmlContent);
                Parser parser = new Parser(lexer);
                //获取包含每条微博的div标记列表
                NodeList feedNodeList = parser.Parse(feedFilter);
                for (int i = 0; i < feedNodeList.Size(); i++)
                {
                    serialNumber++;
                    Feed feed = new Feed();
                    feed.Page = currentPage;
                    feed.Number = serialNumber;
                    //类似微博转发的数量
                    int similarFeedCount = 0;

                    //取得第i条微博的div
                    TagNode feedDiv = (TagNode)feedNodeList[i];

                    //判断是否含有“还有X条对原微博的转发”
                    NodeList similarfeedCountNodeList = feedDiv.Children.ExtractAllNodesThatMatch(similarFeedCountFilter, true);
                    switch (similarfeedCountNodeList.Size())
                    {
                        case 1:
                            //说明存在“还有X条对原微博的转发”的div；此处看起来此HTML解析器不认<b>标记，而把其中包含的内容作为其下一个兄弟节点= =
                            similarFeedCount = Int32.Parse(((TextNode)(similarfeedCountNodeList[0].NextSibling)).ToPlainTextString());
                            break;
                        case 0:
                            //说明不存在“还有X条对原微博的转发”
                            similarFeedCount = 0;
                            break;
                        default:
                            Console.WriteLine("第" + i + "条微博中，判断是否含有类似微博转发的标准出错！");
                            break;
                    }

                    #region 获取微博作者
                    NodeList feedAuthorNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedAuthorFilter, true);
                    //在整个一条微博的范围（即一个feedDiv）内，满足feedAuthorFilter过滤器的div节点数量应该是本条微博作者加上转发类似微博的作者（如果有的话），所以是(1 + similarFeedCount)
                    if (feedAuthorNodeList.Size() == (1 + similarFeedCount))
                    {
                        ATag feedAuthorTag = (ATag)feedAuthorNodeList[0].Children[0];
                        string author = feedAuthorTag.GetAttribute("TITLE");
                        feed.Author = author;
                        //如果存在，则获取该作者的备注名
                        INode remarkNameNode = feedAuthorTag.NextSibling;
                        if (remarkNameNode.GetType().Equals(typeof(Span)))
                        {
                            string remarkName = ((Span)remarkNameNode).StringText;
                            //去掉前后括号
                            remarkName = remarkName.Substring(1, remarkName.Length - 2);
                            feed.RemarkName = remarkName;
                        }
                    }
                    else
                    {
                        //从首页爬取微博时，微博来自不同的被关注者，所以是有微博作者的；而从个人主页爬取微博时，由于所有微博作者都是该用户，所以是没有微博作者相关节点的
                        if (!user.NickName.Equals(""))
                        {
                            feed.Author = user.NickName;
                            feed.RemarkName = user.RemarkName;
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条微博中，判断微博作者的标准出错！");
                        }
                    }
                    #endregion

                    #region 获取转发微博
                    NodeList reFeedNodeList = feedDiv.Children.ExtractAllNodesThatMatch(reFeedFilter, true);
                    //转发微博；(1 + similarFeedCount)的理由和获取微博作者时相同
                    if (reFeedNodeList.Size() == (1 + similarFeedCount))
                    {
                        //获取转发微博的div
                        TagNode reFeedDiv = (TagNode)reFeedNodeList[0];
                        //先获取本次转发微博的相关信息
                        GetReFeedInfo(i, feed, reFeedDiv, feedDiv);
                        #region 考虑“还有X条对原微博的转发”的情况
                        if (similarFeedCount > 0)
                        {
                            NodeList similarFeedNodeList = feedDiv.Children.ExtractAllNodesThatMatch(similarFeedFilter, true);
                            if (similarFeedNodeList.Size() == similarFeedCount)
                            {
                                for (int j = 0; j < similarFeedCount; j++)
                                {
                                    feedList.Add(GetSimilarFeed(currentPage, i, (TagNode)similarFeedNodeList[j], feed.OriginalAuthor, feed.Content));
                                }
                            }
                            else
                            {
                                Console.WriteLine("第" + i + "条微博中，获取转发微博的数量出错！");
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        if (reFeedNodeList.Size() == 0)
                        {
                            //获取本条微博内容作为微博内容
                            NodeList feedContentNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedContentFilter, true);
                            if (feedContentNodeList.Size() == 1)
                            {
                                feed.Content = GetContentFromChildren(feed, feedContentNodeList[0], false);
                                #region 由于存在某些情况，转发微博被删除后更不过滤不到reFeedDiv，所以需要再次检查是否存在已删除的转发微博
                                NodeList deletedFeedList = feedDiv.Children.ExtractAllNodesThatMatch(refeedDeletedFilter2, true);
                                if (deletedFeedList.Size() > 0)
                                {
                                    feed.OriginalAuthor = "Unknown";
                                    feed.ReFeedOrNot = true;
                                    feed.ReFeedReason = feed.Content;
                                    feed.Content = "微博已删除";
                                }
                                #endregion
                            }
                            else
                            {
                                Console.WriteLine("第" + i + "条微博中，判断微博内容的标准出错！");
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条微博中，判断转发微博的标准出错！");
                        }
                    }
                    #endregion

                    //获取包含微博发送地点的div
                    feed.Location = GetLocationInfo(i, feedDiv);
                    //获取包含微博“赞”数的标记
                    feed.LikeCount = GetFeedLikeInfo(i, feedDiv);
                    //获取包含微博转发数的链接标记
                    feed.ReFeedCount = GetFeedForwardCount(i, feedDiv);
                    //获取包含微博评论数的链接标记
                    feed.CommentCount = GetFeedCommentCount(i, feedDiv);
                    //获取包含微博发送时间的链接标记
                    feed.Time = GetFeedTimeInfo(i, feedDiv);
                    //获取包含微博发送设备的链接标记
                    feed.Device = GetFeedSendTypeInfo(i, feedDiv);

                    feedList.Add(feed);
                }
            }
        }

        /// <summary>
        /// 给定根结点，填充转发微博相关的各种信息
        /// </summary>
        /// <param name="i">该微博在所在页面中的流水号</param>
        /// <param name="feed">保存该微博的Feed实例</param>
        /// <param name="reFeedDiv">包含转发微博的div标记</param>
        /// <param name="feedDiv">包含原微博的div标记</param>
        private void GetReFeedInfo(int i, Feed feed, TagNode reFeedDiv, TagNode feedDiv)
        {
            //标记ReFeedOrNot为true，表明是转发微博
            feed.ReFeedOrNot = true;
            //标识是否出现“转发微博已被删除”的情况
            bool reFeedIsDeleted = false;

            #region 获取原微博作者
            NodeList reFeedOriginalAuthorNodeList = reFeedDiv.Children.ExtractAllNodesThatMatch(reFeedAuthorFilter, true);
            if (reFeedOriginalAuthorNodeList.Size() == 1)
            {
                INode reFeedOriginalAuthorNode = reFeedOriginalAuthorNodeList[0];
                //由于包含原微博作者的链接标记与得到的子div相对位置不定（某些情况下可能会有空文本标记，很奇怪= =），所以采用遍历判断标记类型的办法
                for (int j = 0; j < reFeedOriginalAuthorNode.Children.Size(); j++)
                {
                    INode reFeedOriginalAuthorCandidate = reFeedOriginalAuthorNode.Children[j];
                    if (reFeedOriginalAuthorCandidate.GetType().Equals(typeof(ATag)))
                    {
                        feed.OriginalAuthor = ((ATag)reFeedOriginalAuthorCandidate).GetAttribute("TITLE");
                        break;
                    }
                }
            }
            else
            {
                NodeList deletedFeedList = reFeedDiv.Children.ExtractAllNodesThatMatch(refeedDeletedFilter1, true);
                if (deletedFeedList.Size() > 0)
                {
                    reFeedIsDeleted = true;
                    feed.OriginalAuthor = "Unknown";
                }
                else
                {
                    Console.WriteLine("第" + i + "条微博中，判断转发微博作者的标准出错！");
                }
            }
            #endregion

            #region 获取原微博内容
            NodeList reFeedContentNodeList = reFeedDiv.Children.ExtractAllNodesThatMatch(reFeedContentFilter, true);
            if (reFeedContentNodeList.Size() == 1)
            {
                //不清楚em是什么类型的节点，所以直接传递reFeedContentNodeList.Children给函数，让函数对其中每个元素进行遍历处理
                //在个人主页中，好像又没有em节点了= =瞎了……
                feed.Content = GetContentFromChildren(feed, reFeedContentNodeList[0], false);
            }
            else
            {
                if (reFeedIsDeleted)
                {
                    feed.Content = "微博已删除";
                }
                else
                {
                    Console.WriteLine("第" + i + "条微博中，判断转发微博内容的标准出错！");
                }
            }
            #endregion

            #region 获取本条微博内容作为转发理由
            NodeList reFeedReasonNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedContentFilter, true);
            //注意：如果含有“还有X条对原微博的转发”的内容，那么此处reFeedReasonNodeList的数量应该等于(1 + similarFeedCount)，但是考虑到之前已经有了多次判断相等的过程，所以此处直接调用第一个（即下标为0的元素）即可
            feed.ReFeedReason = GetContentFromChildren(feed, reFeedReasonNodeList[0], false);
            #endregion
        }

        /// <summary>
        /// 给定根节点，填充相似转发微博的信息
        /// </summary>
        /// <param name="currentPage">微博所在页面序号</param>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="similarFeedDiv">包含相似微博的div标记</param>
        /// <param name="originalAuthor">转发微博的原作者</param>
        /// <param name="feedContent">转发微博的内容</param>
        /// <returns>返回填充好的一个Feed实例</returns>
        private Feed GetSimilarFeed(int currentPage, int i, TagNode similarFeedDiv, string originalAuthor, string feedContent)
        {
            Feed feed = new Feed();
            serialNumber++;
            feed.Page = currentPage;
            feed.Number = serialNumber;

            feed.ReFeedOrNot = true;

            #region 获取转发相似微博的作者
            NodeList feedAuthorNodeList = similarFeedDiv.Children.ExtractAllNodesThatMatch(feedAuthorFilter, true);
            if (feedAuthorNodeList.Size() == 1)
            {
                ATag feedAuthorTag = (ATag)feedAuthorNodeList[0].Children[1];
                string author = feedAuthorTag.GetAttribute("TITLE");
                feed.Author = author;
                //如果存在，则获取该作者的备注名
                INode remarkNameNode = feedAuthorTag.NextSibling;
                if (remarkNameNode.GetType().Equals(typeof(Span)))
                {
                    string remarkName = ((Span)remarkNameNode).StringText;
                    //去掉前后括号
                    remarkName = remarkName.Substring(1, remarkName.Length - 2);
                    feed.RemarkName = remarkName;
                }
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断转发类似微博的作者的标准出错！");
            }
            #endregion

            #region 获取转发相似微博的转发理由
            NodeList reFeedReasonNodeList = similarFeedDiv.Children.ExtractAllNodesThatMatch(feedContentFilter, true);
            if (reFeedReasonNodeList.Size() == 1)
            {
                feed.ReFeedReason = GetContentFromChildren(feed, reFeedReasonNodeList[0], false);
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断转发类似微博的转发理由的标准出错！");
            }
            #endregion

            //因为是转发相似微博，所以原微博的作者和内容就通过参数传入
            feed.OriginalAuthor = originalAuthor;
            feed.Content = feedContent;

            //获取转发类似微博的发送地点信息
            feed.Location = GetLocationInfo(i, similarFeedDiv);
            //获取转发类似微博赞数
            feed.LikeCount = GetFeedLikeInfo(i, similarFeedDiv);
            //获取转发类似微博的转发数
            feed.ReFeedCount = GetFeedForwardCount(i, similarFeedDiv);
            //获取转发类似微博的评论数
            feed.CommentCount = GetFeedCommentCount(i, similarFeedDiv);
            //获取转发类似微博的发送时间
            feed.Time = GetFeedTimeInfo(i, similarFeedDiv);
            //获取转发类似微博的发送方式
            feed.Device = GetFeedSendTypeInfo(i, similarFeedDiv);

            return feed;
        }

        /// <summary>
        /// 给定根节点，返回微博发送地点信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博发送地点信息</returns>
        private string GetLocationInfo(int i, INode feedDiv)
        {
            string result = "";
            NodeList feedLocationNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedLocationFilter, true);
            if (feedLocationNodeList.Size() > 0)
            {
                result = ((TextNode)(feedLocationNodeList[0].Children[1])).ToPlainTextString();
            }
            return result;
        }

        /// <summary>
        /// 给定根节点，返回微博赞数信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博赞数信息</returns>
        private int GetFeedLikeInfo(int i, INode feedDiv)
        {
            int result = 0;
            NodeList feedLikeNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedLikeFilter, true);
            if (feedLikeNodeList.Size() == 1)
            {
                ATag feedLikeTag = (ATag)feedLikeNodeList[feedLikeNodeList.Size() - 1];
                if (feedLikeTag.Children.Size() > 2)
                {
                    string str = ((TextNode)feedLikeTag.Children[2]).ToPlainTextString();
                    //去掉头尾的括号
                    result = Int32.Parse(str.Substring(1, str.Length - 2));
                }
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断微博赞数的标准出错！");
            }
            return result;
        }

        /// <summary>
        /// 给定根节点，返回微博转发数信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博转发数信息</returns>
        private int GetFeedForwardCount(int i, INode feedDiv)
        {
            int result = 0;
            NodeList feedForwardNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedForwardFilter, true);
            if (feedForwardNodeList.Size() == 1)
            {
                string str = ((ATag)(feedForwardNodeList[0])).StringText;
                if (!str.Equals("转发"))
                {
                    result = Int32.Parse(str.Substring(3, str.Length - 4));
                }
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断微博转发数的标准出错！");
            }
            return result;
        }

        /// <summary>
        /// 给定根节点，返回微博评论数信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博评论数信息</returns>
        private int GetFeedCommentCount(int i, INode feedDiv)
        {
            int result = 0;
            NodeList feedCommentNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedCommentFilter, true);
            if (feedCommentNodeList.Size() == 1)
            {
                string str = ((ATag)(feedCommentNodeList[0])).StringText;
                if (!str.Equals("评论"))
                {
                    result = Int32.Parse(str.Substring(3, str.Length - 4));
                }
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断微博评论数的标准出错！");
            }
            return result;
        }

        /// <summary>
        /// 给定根结点，返回微博发送时间信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博发送时间信息</returns>
        private string GetFeedTimeInfo(int i, INode feedDiv)
        {
            string result = "";
            NodeList feedTimeNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedTimeFilter, true);
            if (feedTimeNodeList.Size() == 1)
            {
                result = ((ATag)(feedTimeNodeList[0])).GetAttribute("TITLE");
            }
            else
            {
                Console.WriteLine("第" + i + "条微博中，判断微博发送时间的标准出错！");
            }
            return result;
        }

        /// <summary>
        /// 给定根节点，返回微博发送设备信息
        /// </summary>
        /// <param name="i">微博在所在页面中的流水号</param>
        /// <param name="feedDiv">包含微博的div标记</param>
        /// <returns>返回微博发送设备信息</returns>
        private string GetFeedSendTypeInfo(int i, INode feedDiv)
        {
            string result = "";
            NodeList feedSendTypeNodeList = feedDiv.Children.ExtractAllNodesThatMatch(feedSendTypeFilter, true);
            if (feedSendTypeNodeList.Size() == 1)
            {
                result = ((ATag)(feedSendTypeNodeList[0])).StringText;
            }
            else
            {
                //某些情况下，会显示“来自未经审核的应用”
                AndFilter fromFilter = new AndFilter(new HasParentFilter(feedFromFilter, true), new NodeClassFilter(typeof(TextNode)));
                NodeList textNodeList = feedDiv.Children.ExtractAllNodesThatMatch(fromFilter, true);
                for (int j = 0; j < textNodeList.Size(); j++)
                {
                    if (textNodeList[j].ToPlainTextString().Equals("来自"))
                    {
                        if (j < textNodeList.Size())//以防万一出现存在“来自”字符串而没有设备字符串的奇葩情况……
                        {
                            result = textNodeList[j + 1].ToPlainTextString();
                        }
                        break;
                    }
                }
                if (result.Equals(""))
                {
                    Console.WriteLine("第" + i + "条微博中，微博发送设备为空");
                }
            }
            char[] shouldRemove = { ' ', (char)10, '\r', '\n' };
            result = result.TrimStart(shouldRemove);
            result = result.TrimEnd(shouldRemove);
            return result;
        }

        /// <summary>
        /// 调用网页版微博爬虫程序
        /// </summary>
        /// <param name="feedList">保存微博内容的Feed数组</param>
        public void RunCrawler(List<Feed> feedList)
        {
            LoginWeiBo();
            int index = 0;
            for (int i = 0; i < queryRange; i++)
            {
                index = startPage + i;
                Console.WriteLine("获取网页……");
                GetHtmlFromWeiBo(index);
                Console.WriteLine("析取微博……");
                GetInfoFromHtml(index, user.FeedList);
                Console.WriteLine("第" + index + "个页面处理完毕！");
                if (i + 1 < queryRange)
                {
                    Thread.Sleep(7000);
                }
            }
        }

        /// <summary>
        /// 调试用：运行整个WebCrawler
        /// </summary>
        /// <param name="feedList">保存微博内容的Feed数组</param>
        public void DebugCrawler(List<Feed> feedList)
        {
            StreamReader reader = new StreamReader("weibo1-0.html");
            htmlContentList.Add(reader.ReadToEnd());
            reader = new StreamReader("weibo1-1.html");
            htmlContentList.Add(reader.ReadToEnd());
            reader = new StreamReader("weibo1-2.html");
            htmlContentList.Add(reader.ReadToEnd());
            reader.Close();
            GetInfoFromHtml(0, feedList);
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

        /// <summary>
        /// 辅助函数：以给定的HTML节点为根节点，把其子节点均作为微博内容提取出来
        /// </summary>
        /// <param name="feed">保存微博内容的Feed实例</param>
        /// <param name="node">作为根节点的HTML节点</param>
        /// <param name="hasEmTag">子节点中是否含有em标签（只有转发微博中含有em标签），若有em标签，hasEmTag为true，则start初始为false；反之为true</param>
        /// <returns>返回微博内容字符串</returns>
        private static string GetContentFromChildren(Feed feed, INode node, bool hasEmTag)
        {
            string content = "";
            bool start = !hasEmTag;
            for (int i = 0; i < node.Children.Size(); i++)
            {
                Type t = node.Children[i].GetType();
                if (start)
                {
                    if (t.Equals(typeof(TextNode)))
                    {
                        string str = ((TextNode)node.Children[i]).ToPlainTextString();
                        //遇到“//”说明微博内容提取完成；同时，还要提取“//”之后的一系列转发者
                        if (str.Length >= 2 && str.Substring(str.Length - 2).Equals("//"))
                        {
                            //去掉“//”
                            str = str.Substring(0, str.Length - 2);
                            content += str;
                            //string reFeedFrom = ((ATag)node.Children[i + 1]).StringText;
                            //if (reFeedFrom[0].Equals('@'))
                            //{
                            //    //去掉“@”
                            //    reFeedFrom = reFeedFrom.Substring(1, reFeedFrom.Length - 1);
                            //}
                            //获取转发链
                            string reFeedFrom = "";
                            for (int j = i + 1; j < node.Children.Size(); j++)
                            {
                                Type t2 = node.Children[j].GetType();
                                if (t2.Equals(typeof(ATag)) && ((ATag)node.Children[j]).Attributes.ContainsKey("USERCARD"))
                                {
                                    string oneReFeeder = ((ATag)node.Children[j]).StringText;
                                    if (oneReFeeder[0].Equals('@'))
                                    {
                                        //去掉“@”
                                        oneReFeeder = oneReFeeder.Substring(1, oneReFeeder.Length - 1);
                                        reFeedFrom = reFeedFrom.Insert(0, oneReFeeder + " ");
                                    }
                                    else
                                    {
                                        Console.WriteLine("获取转发链时出现错误！此前的转发链为" + reFeedFrom);
                                    }
                                }
                            }
                            //最后，把reFeedFrom赋给feed.ReFeedFrom
                            feed.ReFeedFrom = reFeedFrom;
                            break;
                        }
                        content += str;
                        continue;
                    }
                    if (t.Equals(typeof(ATag)))
                    {
                        ATag aTagNode = (ATag)node.Children[i];
                        //某些情况下，链接标记中不仅仅含有文本节点，还有span标记（以后说不定还会碰到跟奇葩的……）,所以提取aTagNode的孩子节点中所有文本节点信息
                        NodeClassFilter textNodeFilter = new NodeClassFilter(typeof(TextNode));
                        NodeList nodeList = aTagNode.Children.ExtractAllNodesThatMatch(textNodeFilter, true);
                        for (int j = 0; j < nodeList.Size(); j++)
                        {
                            content += ((TextNode)nodeList[j]).ToPlainTextString();
                        }
                        continue;
                    }
                    if (t.Equals(typeof(TagNode)))
                    {
                        content += ((TagNode)node.Children[i]).ToPlainTextString();
                        continue;
                    }
                    if (t.Equals(typeof(ImageTag)))
                    {
                        content += ((ImageTag)node.Children[i]).GetAttribute("TITLE");
                        continue;
                    }
                }
                else
                {
                    if (t.Equals(typeof(TagNode)) && (((TagNode)(node.Children[i])).TagName.Equals("EM")))
                    {
                        start = true;
                    }
                }
            }
            //某些情况下最先/后数个字符竟然会是空格和换行符（ASCII码10），瞎了……
            char[] shouldRemove = { ' ', (char)10, '\r', '\n' };
            content = content.TrimStart(shouldRemove);
            content = content.TrimEnd(shouldRemove);
            return content;
        }

        /// <summary>
        /// 辅助函数：从json数组中获得HTML内容
        /// </summary>
        /// <param name="jsonContent">json数组内容</param>
        /// <returns>返回提取得到的HTML文本</returns>
        private static string GetHtmlFromJson(string jsonContent)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JsonWeiboPart));
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            JsonWeiboPart json = (JsonWeiboPart)serializer.ReadObject(ms);
            ms.Close();
            return json.data;
        }

        /// <summary>
        /// 辅助函数：从原始HTML中提取包含用户信息和微博内容的HTML代码
        /// </summary>
        /// <param name="reader">传输HTTP返回内容的流</param>
        private void GetHtmlFromHttpResponse(StreamReader reader)
        {
            string contentForUser = "";
            string contentForWeiBoFeed = "";
            //解析json数组
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(JsonJsPart));
            MemoryStream ms = null;
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                //如果user为空，则获取用户信息
                if (user.NickName.Equals("") && str.Contains("<script>FM.view({\"ns\":\"pl.header.head.index"))
                {
                    int start = str.IndexOf('{');
                    int last = str.LastIndexOf('}');
                    contentForUser = str.Substring(start, last - start + 1);
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(contentForUser));
                    JsonJsPart json = (JsonJsPart)serializer.ReadObject(ms);
                    currentUserHtml = json.html;
                    continue;
                }
                //爬取个人主页时获取的包含微博信息的HTML代码
                if (str.Contains("<script>FM.view({\"ns\":\"pl.content.homeFeed.index"))
                {
                    int start = str.IndexOf('{');
                    int last = str.LastIndexOf('}');
                    contentForWeiBoFeed = str.Substring(start, last - start + 1);
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(contentForWeiBoFeed));
                    JsonJsPart json = (JsonJsPart)serializer.ReadObject(ms);
                    currentHtmlContent = json.html;
                    break;
                }
                //爬取自己首页时获取的包含微博信息的HTML代码
                if (str.Contains("<script>FM.view({\"pid\":\"pl_content_homeFeed"))
                {
                    int start = str.IndexOf('{');
                    int last = str.LastIndexOf('}');
                    contentForWeiBoFeed = str.Substring(start, last - start + 1);
                    ms = new MemoryStream(Encoding.UTF8.GetBytes(contentForWeiBoFeed));
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
        /// 辅助函数：从HTML中获得用户信息
        /// </summary>
        /// <param name="currentUserHtml">包含微博用户信息的HTML文本</param>
        private void GetUserInfoFromHtml(string currentUserHtml)
        {
            //配置相关的过滤器
            HasAttributeFilter nickNameFilter = new HasAttributeFilter("class", "name");
            HasAttributeFilter remarkNameFilter = new HasAttributeFilter("class", "CH");
            HasAttributeFilter linkUrlFilter = new HasAttributeFilter("class", "pf_lin S_link1");
            HasAttributeFilter selfIntroFilter = new HasAttributeFilter("class", "pf_intro bsp");
            HasAttributeFilter tagsFilter = new HasAttributeFilter("class", "S_func1");
            HasAttributeFilter profileFilter = new HasAttributeFilter("class", "tags");

            Lexer lexer = new Lexer(currentUserHtml);
            Parser parser = new Parser(lexer);

            //获取微博名
            NodeList nickNameNodeList = parser.ExtractAllNodesThatMatch(nickNameFilter);

            if (nickNameNodeList.Size() == 1)
            {
                user.NickName = ((Span)nickNameNodeList[0]).ToPlainTextString();
            }
            else
            {
                Console.WriteLine("判断微博名的标准出错！");
            }
            //注意此处：如果要重复使用parser，一定要在本次使用“完”、下次使用前调用reset，否则会出错
            parser.Reset();
            //获取备注名称
            NodeList remarkNameNodeList = parser.ExtractAllNodesThatMatch(remarkNameFilter);

            if (remarkNameNodeList.Size() == 1 && remarkNameNodeList[0].GetType().Equals(typeof(Span)))
            {
                string str = ((Span)remarkNameNodeList[0]).ToPlainTextString();
                //去掉头尾的括号
                user.RemarkName = str.Substring(1, str.Length - 2);
            }
            else
            {
                Console.WriteLine("判断微博备注名称的标准出错！");
            }
            parser.Reset();
            //获取微博链接地址
            NodeList linkUrlNodeList = parser.ExtractAllNodesThatMatch(linkUrlFilter);
            if (linkUrlNodeList.Size() == 1 && linkUrlNodeList[0].GetType().Equals(typeof(ATag)))
            {
                user.LinkURL = ((ATag)linkUrlNodeList[0]).StringText;
            }
            else
            {
                Console.WriteLine("判断微博链接地址的标准出错！");
            }
            parser.Reset();
            //获取自我描述
            NodeList selfIntroNodeList = parser.ExtractAllNodesThatMatch(selfIntroFilter);
            if (selfIntroNodeList.Size() == 1 && selfIntroNodeList[0].Children[1].GetType().Equals(typeof(Span)))
            {
                user.SelfIntroduction = ((Span)selfIntroNodeList[0].Children[1]).GetAttribute("TITLE");
            }
            else
            {
                Console.WriteLine("判断自我描述的标准出错！");
            }
            parser.Reset();
            //获取标签
            NodeList tagsNodeList = parser.ExtractAllNodesThatMatch(tagsFilter);
            string str2 = "";
            for (int i = 0; i < tagsNodeList.Size(); i++)
            {
                if (tagsNodeList[i].GetType().Equals(typeof(Span)))
                {
                    str2 += ((Span)tagsNodeList[i]).ToPlainTextString() + " ";
                }
            }
            user.Tags = str2;
            parser.Reset();
            //获取属性信息
            NodeList profileNodeList = parser.ExtractAllNodesThatMatch(profileFilter);
            if (profileNodeList.Size() == 1)
            {
                //通过分析发现，有用的信息均处于<a>标记中，所以按<a>标记取。然后再分析是其中的文本还是<em>中的title
                NodeClassFilter aTagFilter = new NodeClassFilter(typeof(ATag));
                NodeList profileList = profileNodeList[0].Children.ExtractAllNodesThatMatch(aTagFilter, true);
                for (int j = 0; j < profileList.Size(); j++)
                {
                    ATag aTag = (ATag)profileList[j];
                    if (aTag.Attributes.Contains("TITLE"))
                    {
                        user.Profile += aTag.GetAttribute("TITLE") + " ";
                    }
                    else
                    {
                        //遇到含有node-type="infoSlide"的节点说明所有属性遍历结束
                        if (aTag.Attributes.Contains("NODE-TYPE") && aTag.GetAttribute("NODE-TYPE").Equals("infoSlide"))
                        {
                            break;
                        }
                        else
                        {
                            //包含<em>子节点的情况
                            if (aTag.Children[0].GetType().Equals(typeof(TagNode)))
                            {
                                TagNode tagNode = (TagNode)aTag.Children[0];
                                user.Profile += tagNode.GetAttribute("TITLE") + " ";
                            }
                            else
                            {
                                //直接把<a>标记包含的文本输出
                                user.Profile += aTag.StringText + " ";
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("判断用户属性信息的标准出错！");
            }
        }

        /// <summary>
        /// 辅助函数：从HTML中获得end_id
        /// </summary>
        /// <param name="htmlContent">HTML文本</param>
        /// <returns></returns>
        private bool GetEndIdFromHtml(string htmlContent)
        {
            Lexer lexer = new Lexer(htmlContent);
            Parser parser = new Parser(lexer);
            NodeList feedNodeList = parser.Parse(idFilter[(int)Type]);
            if (feedNodeList.Size() >= 1 && feedNodeList[0].GetType().Equals(typeof(Div)) && ((TagNode)feedNodeList[0]).Attributes.ContainsKey("MID"))
            {
                end_id = ((TagNode)feedNodeList[0]).GetAttribute("MID");
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 辅助函数：从HTML中获得max_id
        /// </summary>
        /// <param name="htmlContent">HTML文本</param>
        /// <returns></returns>
        private bool GetMaxIdFromHtml(string htmlContent)
        {
            Lexer lexer = new Lexer(htmlContent);
            Parser parser = new Parser(lexer);
            NodeList feedNodeList = parser.Parse(idFilter[(int)Type]);
            if (feedNodeList.Size() >= 1)
            {
                max_id = ((TagNode)feedNodeList[feedNodeList.Size() - 1]).GetAttribute("MID");
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
