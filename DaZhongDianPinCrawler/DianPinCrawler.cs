using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Nodes;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;

namespace DaZhongDianPinCrawler
{
    class DianPinCrawler
    {
        #region 静态成员
        static string basicQueryUrl = "http://www.dianping.com/search/category/1/10/";//查找上海的餐馆
        private static AndFilter poiListFilter;//过滤参观列表的过滤器
        private static NodeClassFilter poiFilter;//过滤出每家餐馆的过滤器
        private static HasAttributeFilter tasteFilter;//过滤出口味评分的过滤器
        private static HasAttributeFilter environmentFilter;//过滤出环境评分的过滤器
        private static HasAttributeFilter serviceFilter;//过滤出服务评分的过滤器
        private static HasAttributeFilter averageFilter;//过滤出平均消费的过滤器
        private static AndFilter commentFilter;//过滤出点评数的过滤器
        private static AndFilter nameFilter;//过滤出店名的过滤器
        private static HasAttributeFilter addressFilter;//过滤出地址的过滤器
        private static HasAttributeFilter tagsFilter;//过滤出标签的过滤器
        #endregion

        private int startPage;
        private int queryRange;
        public string currentHtml;
        List<POI> poiList;

        private static void MakeFilters()
        {
            NodeClassFilter dlFilter = new NodeClassFilter(typeof(DefinitionList));
            HasAttributeFilter searchListFilter = new HasAttributeFilter("id", "searchList");
            poiListFilter = new AndFilter(new HasParentFilter(searchListFilter, false), dlFilter);
            poiFilter = new NodeClassFilter(typeof(DefinitionListBullet));
            tasteFilter = new HasAttributeFilter("class", "score1");
            environmentFilter = new HasAttributeFilter("class", "score2");
            serviceFilter = new HasAttributeFilter("class", "score3");
            averageFilter = new HasAttributeFilter("class", "average");
            commentFilter = new AndFilter(new HasAttributeFilter("class", "B"), new HasAttributeFilter("module", "list-readreview"));
            HasAttributeFilter nameFilterByParent = new HasAttributeFilter("class", "shopname");
            nameFilter = new AndFilter(new HasParentFilter(nameFilterByParent, false), new HasAttributeFilter("class", "BL"));
            addressFilter = new HasAttributeFilter("class", "address");
            tagsFilter = new HasAttributeFilter("class", "tags");
        }

        static DianPinCrawler()
        {
            MakeFilters();
        }

        public DianPinCrawler(List<POI> poiList, int startPage, int queryRange)
        {
            this.startPage = startPage;
            this.queryRange = queryRange;
            currentHtml = "";
            this.poiList = poiList;
        }

        public void GetHtml(int index)
        {
            string queryUrl = basicQueryUrl;
            if (index > 1)
            {
                queryUrl = basicQueryUrl + "p" + index;
            }
            HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(queryUrl);
            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.95";
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();//取得响应
            if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))//若响应状态码为200，说明成功，可以分析得到的数据
            {
                StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                currentHtml = reader.ReadToEnd();
                reader.Close();
                //调试用
                //SaveAsFile(currentHtml, index);
            }
            else
            {
                Console.WriteLine("请求第" + index + "个页面出错！");
            }
        }

        public void GetInfoFromHtml(int currentPage)
        {
            Lexer lexer = new Lexer(currentHtml);
            Parser parser = new Parser(lexer);
            NodeList poiHeadList = parser.Parse(poiListFilter);
            if (poiHeadList.Count == 1)
            {
                NodeList poiNodeList = poiHeadList[0].Children.ExtractAllNodesThatMatch(poiFilter, false);
                int numCount = 0;
                for (int i = 0; i < poiNodeList.Count; i++)
                {
                    POI poi = new POI();
                    DefinitionListBullet poiNode = (DefinitionListBullet)poiNodeList[i];
                    if (poiNode.TagName.Equals("DD"))
                    {
                        numCount++;
                        poi.Page = currentPage;
                        poi.Number = numCount;
                        #region 获取口味、环境和服务评分，以及获取星级
                        NodeList tasteNodeList = poiNode.Children.ExtractAllNodesThatMatch(tasteFilter, true);
                        NodeList environmentNodeList = poiNode.Children.ExtractAllNodesThatMatch(environmentFilter, true);
                        NodeList serviceNodeList = poiNode.Children.ExtractAllNodesThatMatch(serviceFilter, true);
                        if (tasteNodeList.Count == 1 && environmentNodeList.Count == 1 && serviceNodeList.Count == 1)
                        {
                            Span spanNode = (Span)tasteNodeList[0];
                            if (!spanNode.ToPlainTextString().Equals("-"))
                            {
                                poi.TasteRemark = Int32.Parse(spanNode.ToPlainTextString());
                            }
                            spanNode = (Span)environmentNodeList[0];
                            if (!spanNode.ToPlainTextString().Equals("-"))
                            {
                                poi.EnvironmentRemark = Int32.Parse(spanNode.ToPlainTextString());
                            }
                            spanNode = (Span)serviceNodeList[0];
                            if (!spanNode.ToPlainTextString().Equals("-"))
                            {
                                poi.ServiceRemark = Int32.Parse(spanNode.ToPlainTextString());
                            }
                            #region 获取星级
                            INode rankNodeOfParent = spanNode.Parent.NextSibling.NextSibling;
                            if (rankNodeOfParent.Children != null && rankNodeOfParent.Children.Count >= 1)
                            {
                                INode rankNodeCandidate = rankNodeOfParent.Children[0];
                                if (rankNodeCandidate.GetType().Equals(typeof(Span)))
                                {
                                    Span rankNode = (Span)rankNodeCandidate;
                                    string rank = rankNode.GetAttribute("TITLE");
                                    if (rank.Contains("五"))
                                    {
                                        poi.Rank = 5;
                                    }
                                    else
                                    {
                                        if (rank.Contains("四"))
                                        {
                                            poi.Rank = 4;
                                        }
                                        else
                                        {
                                            if (rank.Contains("三"))
                                            {
                                                poi.Rank = 3;
                                            }
                                            else
                                            {
                                                if (rank.Contains("二"))
                                                {
                                                    poi.Rank = 2;
                                                }
                                                else
                                                {
                                                    if (rank.Contains("一"))
                                                    {
                                                        poi.Rank = 1;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断口味、环境和服务的标准出错！");
                        }
                        #endregion
                        #region 获取平均消费
                        NodeList averageNodeList = poiNode.Children.ExtractAllNodesThatMatch(averageFilter, true);
                        if (averageNodeList.Count == 1)
                        {
                            INode averageNode = averageNodeList[0];
                            if (averageNode.NextSibling.NextSibling.GetType().Equals(typeof(TextNode)))
                            {
                                string cost = ((TextNode)averageNode.NextSibling.NextSibling).ToPlainTextString();
                                poi.AverageCost = Int32.Parse(cost);
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断平均消费的标准出错！");
                        }
                        #endregion
                        #region 获取点评数
                        NodeList commentNodeList = poiNode.Children.ExtractAllNodesThatMatch(commentFilter, true);
                        if (commentNodeList.Count == 1)
                        {
                            INode commentNode = commentNodeList[0];
                            if (commentNode.GetType().Equals(typeof(ATag)))
                            {
                                string commentNum = ((ATag)commentNode).StringText;
                                if (commentNum.Substring(commentNum.Length - 3, 3).Equals("封点评"))
                                {
                                    commentNum = commentNum.Substring(0, commentNum.Length - 3);
                                }
                                poi.CommentCount = Int32.Parse(commentNum);
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断点评数的标准出错！");
                        }
                        #endregion
                        #region 获取店名
                        NodeList nameNodeList = poiNode.Children.ExtractAllNodesThatMatch(nameFilter, true);
                        if (nameNodeList.Count == 1)
                        {
                            INode nameNode = nameNodeList[0];
                            if (nameNode.GetType().Equals(typeof(ATag)))
                            {
                                poi.Name = ((ATag)nameNode).StringText;
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断店名的标准出错！");
                        }
                        #endregion
                        #region 获取地址和电话
                        NodeList addressNodeList = poiNode.Children.ExtractAllNodesThatMatch(addressFilter, true);
                        if (addressNodeList.Count == 1)
                        {
                            NodeList districtNodeList = addressNodeList[0].Children.ExtractAllNodesThatMatch(new NodeClassFilter(typeof(ATag)));
                            if (districtNodeList.Count == 1)
                            {
                                ATag districtTag = (ATag)districtNodeList[0];
                                string address = districtTag.ToPlainTextString();
                                if (districtTag.NextSibling.GetType().Equals(typeof(TextNode)))
                                {
                                    TextNode detailAddressNode = (TextNode)districtTag.NextSibling;
                                    string detailAddress = detailAddressNode.ToPlainTextString();
                                    detailAddress = detailAddress.Trim();
                                    string phoneStr = detailAddress.Substring(detailAddress.Length - 8, 8);
                                    poi.Phone = phoneStr;
                                    address += detailAddress.Substring(0, detailAddress.Length - 8);
                                }
                                char[] removeChrVector = { ' ', '\n', '\t' };
                                address = address.Trim(removeChrVector);
                                foreach (char c in removeChrVector)
                                {
                                    address = address.Replace(c.ToString(), "");
                                }
                                poi.Address = address;
                            }
                            else
                            {
                                Console.WriteLine("第" + i + "条POI中，判断含地址的<a>标记的标准出错！");
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断地址的标准出错！");
                        }
                        #endregion
                        #region 获取标签
                        NodeList tagsNodeList = poiNode.Children.ExtractAllNodesThatMatch(tagsFilter, true);
                        if (tagsNodeList.Count == 1)
                        {
                            INode tagsNode = tagsNodeList[0];
                            if (tagsNode.Children != null)
                            {
                                for (int j = 0; j < tagsNode.Children.Count; j++)
                                {
                                    INode node = tagsNode.Children[j];
                                    if (node.GetType().Equals(typeof(ATag)))
                                    {
                                        poi.Tags.Add(node.ToPlainTextString());
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "条POI中，判断标签的标准出错！");
                        }
                        #endregion
                        poiList.Add(poi);
                    }
                }
            }
            else
            {
                Console.WriteLine("获取POI列表出错");
            }
        }

        /// <summary>
        /// 调用大众点评网爬虫程序
        /// </summary>
        public void RunCrawler()
        {
            int index = 0;
            for (int i = 0; i < queryRange; i++)
            {
                index = startPage + i;
                Console.WriteLine("获取网页……");
                GetHtml(index);
                Console.WriteLine("析取网页……");
                GetInfoFromHtml(index);
                Console.WriteLine("第" + index + "个页面处理完毕！");
                if (i + 1 < queryRange)
                {
                    Thread.Sleep(7000);
                }
            }
        }

        public void SaveAsFile(string content, int index)
        {
            StreamWriter writer = new StreamWriter("html" + index + ".html");
            writer.Write(content);
            writer.Close();
        }
    }
}
