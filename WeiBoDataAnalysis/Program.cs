using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WeiBoDataAnalysis
{
    class Program
    {
        enum DaysType
        {
            Weekdays = 0,
            Weekends = 1,
            Alldays = 2
        }
        static User user;
        static List<string> locationList;
        //反序列化
        static void ReadInWeiBoData(string fileName)
        {
            StreamReader reader = new StreamReader(fileName);
            XmlSerializer sr = new XmlSerializer(typeof(User));
            user = (User)sr.Deserialize(reader);
        }

        //统计转发微博占所有微博的比重
        static double OriginalFeedRate()
        {
            double result = 0;
            foreach (Feed feed in user.FeedList)
            {
                if (feed.ReFeedOrNot == false)
                {
                    result += 1;
                }
            }
            result = result / user.FeedList.Count;
            return result;
        }

        //统计微博平均受关注度（计算公式：平均受关注度=（转发数+评论数+赞数）/微博条数）
        static double AttentionRate(bool onlyConcernOriginalWeiBo)//参数表示是否统计只原创微博；true为只统计原创微博；false为统计所有微博
        {
            double result = 0;
            int count = 0;//由于考虑到可能只统计原创微博，所以不能简单地用FeedList.Count作为分母，而要另行统计
            foreach (Feed feed in user.FeedList)
            {
                if (!(onlyConcernOriginalWeiBo && feed.ReFeedOrNot))
                {
                    result += feed.CommentCount + feed.LikeCount + feed.ReFeedCount;
                    count++;
                }
            }
            result = result / count;
            return result;
        }

        #region 使用关联规则的方法，尝试发现微博转发者之间的关系
        static void FindRelationFromReFeed(double minsup)
        {
            List<List<SortedSet<string>>> resultSet = Priori(minsup);
            int i = 0;
            foreach (List<SortedSet<string>> kResult in resultSet)
            {
                i++;
                Console.WriteLine(i + "-频繁项集有" + kResult.Count + "个：");
                foreach (SortedSet<string> result in kResult)
                //Console.WriteLine(resultSet.Count+ "-频繁项集有" + resultSet[resultSet.Count - 1].Count + "个：");
                //foreach (SortedSet<string> result in resultSet[resultSet.Count - 1])
                {
                    foreach (string str in result)
                    {
                        Console.Write(str + " ");
                    }
                    Console.WriteLine("");
                }
            }
        }

        //使用Priori算法，得到转发微博中原作者、最后一个转发作者和用户间的关联度
        static List<List<SortedSet<string>>> Priori(double minsup)
        {
            //事务集
            List<List<string>> transactionSet = new List<List<string>>();
            //所有出现过的元素的集合
            Dictionary<string, int> elementSet = new Dictionary<string, int>();
            //保留所有的k-频繁子集
            List<List<SortedSet<string>>> resultSet = new List<List<SortedSet<string>>>();

            #region 获取事务集
            foreach (Feed feed in user.FeedList)
            {
                if (feed.ReFeedOrNot)
                {
                    List<string> transaction = new List<string>();
                    transaction.Add(feed.OriginalAuthor);

                    #region 原微博作者：转发微博链的第一节
                    if (elementSet.ContainsKey(feed.OriginalAuthor))
                    {
                        elementSet[feed.OriginalAuthor]++;
                    }
                    else
                    {
                        elementSet.Add(feed.OriginalAuthor, 1);
                    }
                    #endregion

                    #region 转发链
                    if (!feed.ReFeedFrom.Equals(""))
                    {
                        string[] ReFeederList = feed.ReFeedFrom.Split(' ');
                        foreach (string reFeeder in ReFeederList)
                        {
                            if (!reFeeder.Equals(""))//由于原始数据中每个转发者后面都有一个' '，包括最后一个，所以调用Split函数后会有一个“”元素产生
                            {
                                transaction.Add(reFeeder);
                                if (elementSet.ContainsKey(reFeeder))
                                {
                                    elementSet[reFeeder]++;
                                }
                                else
                                {
                                    elementSet.Add(reFeeder, 1);
                                }
                            }
                        }
                    }
                    #endregion

                    #region 本条微博作者：转发微博链的最后一节
                    transaction.Add(feed.Author);
                    if (elementSet.ContainsKey(feed.Author))
                    {
                        elementSet[feed.Author]++;
                    }
                    else
                    {
                        elementSet.Add(feed.Author, 1);
                    }
                    #endregion
                    transactionSet.Add(transaction);
                }
            }
            #endregion
            int transactionCount = transactionSet.Count;
            double minCount = minsup * transactionCount;

            //获取频繁项集
            #region 第一步：获取1-频繁项集
            List<SortedSet<string>> kFequentSet = new List<SortedSet<string>>();
            foreach (KeyValuePair<string, int> pair in elementSet)
            {
                if (pair.Value >= minCount)
                {
                    string element = pair.Key;
                    SortedSet<string> candidate = new SortedSet<string>();
                    candidate.Add(element);
                    kFequentSet.Add(new SortedSet<string>(candidate));
                }
            }
            #endregion

            #region 第二步：提取k-频繁项集
            do
            {
                resultSet.Add(new List<SortedSet<string>>(kFequentSet));
                List<SortedSet<string>> candidateSet = AprioriGen(kFequentSet);
                List<int> score = new List<int>(candidateSet.Count);
                for (int j = 0; j < candidateSet.Count; j++)
                {
                    score.Add(0);
                }
                foreach (List<string> transaction in transactionSet)
                {
                    for (int i = 0; i < candidateSet.Count; i++)
                    {
                        SortedSet<string> candidate = candidateSet[i];
                        List<string> result = candidate.Except<string>(transaction).ToList();
                        if (result.Count == 0)
                        {
                            score[i]++;
                        }
                    }
                }
                kFequentSet.Clear();
                for (int j = 0; j < score.Count; j++)
                {
                    if (score[j] > minCount)
                    {
                        kFequentSet.Add(new SortedSet<string>(candidateSet[j]));
                    }
                }
            } while (kFequentSet.Count > 0);
            #endregion

            return resultSet;
        }

        //产生候选集：使用《数据挖掘导论》上P210页的方法
        static List<SortedSet<string>> AprioriGen(List<SortedSet<string>> kFequentSet)
        {
            List<SortedSet<string>> result = new List<SortedSet<string>>();
            for (int i = 0; i < kFequentSet.Count; i++)
            {
                SortedSet<string> aTmpSet = new SortedSet<string>(kFequentSet[i]);
                string aLastElement = kFequentSet[i].Last<string>();
                aTmpSet.Remove(aLastElement);//去掉最后一个元素
                for (int j = i + 1; j < kFequentSet.Count; j++)
                {
                    SortedSet<string> bTmpSet = new SortedSet<string>(kFequentSet[j]);
                    string bLastElement = kFequentSet[j].Last<string>();
                    bTmpSet.Remove(bLastElement);//去掉最后一个元素
                    if (bTmpSet.Count == aTmpSet.Count)
                    {
                        bTmpSet.ExceptWith(aTmpSet);
                        if (bTmpSet.Count == 0 && !aLastElement.Equals(bLastElement))//前k-2个元素相同而最后一个元素不同
                        {
                            result.Add(new SortedSet<string>(kFequentSet[i]));
                            result[result.Count - 1].Add(bLastElement);
                        }
                    }
                }
            }
            return result;
        }
        #endregion

        #region 基于微博发送时间的挖掘
        //按小时统计；参数daysType取值1、2或3，表示只考虑工作日、只考虑双休日或全部考虑；参数dateEnd表示统计从当前时间至多久前的数据
        static List<int> DistributionBasedOnHour(DaysType daysType, DateTime dateEnd)
        {
            List<int> resultList = new List<int>(24);//每个元素对应1小时，分别统计在该小时内发出的微博的频数
            for (int i = 0; i < 24; i++)
            {
                resultList.Add(0);
            }
            foreach (Feed feed in user.FeedList)
            {
                DateTime dateOfFeed = DateTime.ParseExact(feed.Time, "yyyy-MM-dd HH:mm", null);
                if (dateEnd < dateOfFeed)
                {
                    switch (daysType)
                    {
                        case DaysType.Weekdays:
                            if (dateOfFeed.DayOfWeek >= DayOfWeek.Monday && dateOfFeed.DayOfWeek <= DayOfWeek.Friday)
                            {
                                resultList[dateOfFeed.Hour]++;
                            }
                            break;
                        case DaysType.Weekends:
                            if (dateOfFeed.DayOfWeek.Equals(DayOfWeek.Saturday) || dateOfFeed.DayOfWeek.Equals(DayOfWeek.Sunday))
                            {
                                resultList[dateOfFeed.Hour]++;
                            }
                            break;
                        default:
                            resultList[dateOfFeed.Hour]++;
                            break;
                    }
                }
                else
                {
                    break;
                }
            }
            return resultList;
        }

        //按天统计；参数dateEnd表示统计从当前时间至多久前的数据
        static List<int> DistributionBasedOnDay(DateTime dateEnd)
        {
            List<int> resultList = new List<int>(7);
            for (int i = 0; i < 7; i++)
            {
                resultList.Add(0);
            }
            foreach (Feed feed in user.FeedList)
            {
                DateTime dateOfFeed = DateTime.ParseExact(feed.Time, "yyyy-MM-dd HH:mm", null);
                if (dateEnd < dateOfFeed)
                {
                    resultList[(int)dateOfFeed.DayOfWeek]++;
                }
                else
                {
                    break;
                }

            }
            return resultList;
        }
        #endregion

        static void Main(string[] args)
        {
            //ReadInWeiBoData("WeiboWeb_XXX_201307311503.xml");

            //double originalFeedRate = OriginalFeedRate();
            //double attRateAll = AttentionRate(false);
            //double attRateOnlyOrigin = AttentionRate(true);
            //Console.WriteLine("微博总条数：" + user.FeedList.Count);
            //Console.WriteLine("在所有微博中，原创微博占所有微博的 " + originalFeedRate.ToString("P"));
            //Console.WriteLine("考虑所有微博，平均受关注度为：" + attRateAll.ToString("F2"));
            //Console.WriteLine("只考虑原创微博，平均受关注度为：" + attRateOnlyOrigin.ToString("F2"));
            //FindRelationFromReFeed(0.05);

            List<User> userList = new List<User>();
            string documentPath = @"D:\Document\subjects\Computer\Develop\Data\WeiBo\1\";
            string[] pathVector = Directory.GetFiles(documentPath);
            foreach (string path in pathVector)
            {
                StreamReader readerFile = new StreamReader(path, Encoding.UTF8);
                XmlSerializer sr = new XmlSerializer(typeof(User));
                userList.Add((User)sr.Deserialize(readerFile));
            }
            Dictionary<string, Dictionary<string, int>> resultDictionary = new Dictionary<string, Dictionary<string, int>>();
            foreach (User usr in userList)
            {
                string name = "";
                if (!usr.RemarkName.Equals(""))
                {
                    name = usr.RemarkName;
                }
                else
                {
                    name = usr.NickName;
                }
                resultDictionary.Add(name, new Dictionary<string, int>());
                locationList = usr.FeedList.ConvertAll<string>(feed => feed.Location);
                HashSet<string> locationSet = new HashSet<string>();
                HashSet<string> locationSet2 = new HashSet<string>();
                foreach (Feed feed in usr.FeedList)
                {
                    if (!feed.Location.Equals(""))
                    {
                        //locationSet.Add(feed.Location);
                        DateTime dateOfFeed = DateTime.ParseExact(feed.Time, "yyyy-MM-dd HH:mm", null);
                        if (dateOfFeed.Hour >= 21 || dateOfFeed.Hour <= 8)
                        {
                            locationSet.Add(feed.Location);
                        }
                    }
                }
                List<string> popularLocationList = locationSet.ToList<string>();
                popularLocationList.Sort((l1, l2) =>
                {
                    int popular1 = locationList.Count<string>(l => l.Equals(l1));
                    int popular2 = locationList.Count<string>(l => l.Equals(l2));
                    return popular2 - popular1;
                });
                Console.WriteLine(usr.NickName + ":");
                int indexOfPopularLocation = 0;
                foreach (string strLoc in popularLocationList)
                {
                    indexOfPopularLocation++;
                    if (indexOfPopularLocation == 3)
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine(strLoc + " ");
                    }
                }
                //foreach (string strLoc in locationSet2)
                //{
                //    Console.WriteLine(strLoc + " ");
                //}
                Console.WriteLine();
                Console.ReadLine();
            }
        }
    }
}
