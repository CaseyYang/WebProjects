using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using Winista.Text.HtmlParser;
using Winista.Text.HtmlParser.Filters;
using Winista.Text.HtmlParser.Lex;
using Winista.Text.HtmlParser.Tags;
using Winista.Text.HtmlParser.Util;

namespace BeautySelector
{
    class Program
    {
        static string saveOneDayOneBeautyBasePath = "OneDayOneBeauty\\";
        static string saveBeautyFlowBasePath = "BeautyFlow\\";
        static string imgFileNameSetFileName = "ImgFileNameSet.txt";
        static HashSet<string> imgFileNameSet = new HashSet<string>();//以前下载过的照片的链接地址集合，以防重复下载；每次程序运行前从文件读入，每次程序结束前写入ImgFileNameSet.txt
        static Dictionary<string, string> currentImgFileNameSet = new Dictionary<string, string>();//本次应下载照片的链接地址集合
        static HashSet<string> downloadFileNameSet = new HashSet<string>();//本次已下载照片的链接地址集合，配合currentImgFileNameSet能够找出哪些照片下载失败，以便重新下载
        static int totalPicturesCount = 0;
        static int totalDownloadPicturesCount = 0;
        static string oneDayOneBeautyBaseUrl = "http://curator.im/girl_of_the_day/";
        static string BeautyFlowBaseUrl = "http://curator.im/item/";
        //爬取“一天一妹”所用过滤器
        static AndFilter OneDayOneBeautyImgFilter = new AndFilter(new NodeClassFilter(typeof(ImageTag)), new HasAttributeFilter("class", "god"));
        //有些情况下爬取的网页中使用上面的过滤器得不到图片，此时使用下面的过滤器
        static AndFilter OneDayOneBeautyImgFilter2 = new AndFilter(new NodeClassFilter(typeof(ImageTag)), new HasAttributeFilter("data-cfsrc"));
        //爬取“正妹流”所用过滤器
        static NodeClassFilter BeautyNameFilter = new NodeClassFilter(typeof(TitleTag));//得到正妹名字的过滤器
        static AndFilter BeautyFlowImgFilter = new AndFilter(new NodeClassFilter(typeof(ImageTag)), new HasAttributeFilter("itemprop", "contentURL"));//得到正妹图片的过滤器


        static int OneDayOneBeauty(string date)
        {
            try
            {
                string htmlContent = "";
                string url = oneDayOneBeautyBaseUrl + date + "/";
                HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(url);
                httpWebRequest.Method = "GET";
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))
                {
                    StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                    htmlContent = reader.ReadToEnd();
                    //调试代码
                    //StreamWriter fw = new StreamWriter("debug.html");
                    //fw.Write(htmlContent);
                    //fw.Close();
                    //调试完毕
                    httpWebResponse.Close();
                    reader.Close();
                }
                if (!htmlContent.Equals(""))
                {
                    Lexer lexer = new Lexer(htmlContent);
                    Parser parser = new Parser(lexer);
                    parser.AnalyzePage();
                    NodeList divList = parser.ExtractAllNodesThatMatch(OneDayOneBeautyImgFilter);
                    if (divList.Count == 0)
                    {
                        parser.AnalyzePage();
                        divList = parser.ExtractAllNodesThatMatch(OneDayOneBeautyImgFilter2);
                    }
                    int fileIndex = 1;
                    for (int i = 0; i < divList.Count; i++)
                    {
                        ImageTag imgNode = (ImageTag)divList[i];
                        if ((imgNode.Attributes.ContainsKey("SRC") || imgNode.Attributes.ContainsKey("DATA-CFSRC")) && imgNode.Attributes.ContainsKey("ALT"))
                        {
                            string imgUrl = "";
                            if (imgNode.Attributes.ContainsKey("SRC"))
                            {
                                imgUrl = imgNode.GetAttribute("SRC");
                            }
                            else
                            {
                                imgUrl = imgNode.GetAttribute("DATA-CFSRC");
                            }
                            if (imgUrl.Contains("/250x0/"))
                            {
                                imgUrl = imgUrl.Substring(imgUrl.IndexOf("/250x0/") + 7);
                                imgUrl = "http://" + imgUrl;
                            }
                            if (!imgFileNameSet.Contains(imgUrl))
                            {
                                imgFileNameSet.Add(imgUrl);
                                string imgName = imgNode.GetAttribute("ALT");
                                if (!Directory.Exists(saveOneDayOneBeautyBasePath + imgName))
                                {
                                    Directory.CreateDirectory(saveOneDayOneBeautyBasePath + imgName);
                                }
                                WebClient wc = new WebClient();
                                wc.DownloadFileAsync(new Uri(imgUrl), saveOneDayOneBeautyBasePath + imgName + "\\" + imgName + " (" + fileIndex + ").jpg");
                                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                                fileIndex++;
                            }
                        }
                        else
                        {
                            Console.WriteLine("第" + i + "张图片命名错误！");
                        }
                    }
                    //Thread.Sleep(10000);
                    return fileIndex - 1;
                }
                else
                {
                    Console.WriteLine("得到的HTML为空！");
                    return 0;
                }
            }
            catch (WebException e)
            {
                HttpWebResponse httpWebResponse = (HttpWebResponse)e.Response;
                if (httpWebResponse.StatusCode.Equals(HttpStatusCode.NotFound))
                {
                    Console.WriteLine("网页未找到！");
                }
                else
                {
                    Console.WriteLine("访问网页出错！状态码：" + httpWebResponse.StatusCode);
                }
                httpWebResponse.Close();
                return 0;
            }
        }

        static void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WebClient wc = (WebClient)sender;
            downloadFileNameSet.Add(wc.BaseAddress);
            wc.Dispose();
            totalDownloadPicturesCount++;
        }

        static void BeautyFlow(int id)
        {
            HttpWebResponse httpWebResponse = null;
            try
            {
                string htmlContent = "";
                string url = BeautyFlowBaseUrl + id + "/";
                HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(url);
                httpWebRequest.Method = "GET";
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))
                {
                    StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                    htmlContent = reader.ReadToEnd();
                    //调试代码
                    //StreamWriter fw = new StreamWriter("debug.html");
                    //fw.Write(htmlContent);
                    //fw.Close();
                    //调试完毕
                    httpWebResponse.Close();
                    reader.Close();
                }
                if (!htmlContent.Equals(""))
                {
                    int startIndex = htmlContent.IndexOf("/girl/");
                    int endIndex = htmlContent.IndexOf("/", startIndex + 6) + 1;
                    string beautyMorePicturesLink = "http://curator.im" + htmlContent.Substring(startIndex, endIndex - startIndex);
                    string htmlContentTwo = "";
                    httpWebRequest = HttpWebRequest.CreateHttp(beautyMorePicturesLink);
                    httpWebRequest.Method = "GET";
                    httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                        htmlContentTwo = reader.ReadToEnd();
                        //调试代码
                        //StreamWriter fw = new StreamWriter("debug2.html");
                        //fw.Write(htmlContentTwo);
                        //fw.Close();
                        //调试完毕
                        httpWebResponse.Close();
                        reader.Close();
                    }
                    Lexer lexer = new Lexer(htmlContentTwo);
                    Parser parser = new Parser(lexer);
                    parser.AnalyzePage();
                    NodeList divList = parser.ExtractAllNodesThatMatch(BeautyNameFilter);
                    string beautyName = "";
                    if (divList.Count == 1)
                    {
                        beautyName = divList[0].ToPlainTextString();
                        endIndex = beautyName.IndexOf('|') - 1;
                        beautyName = beautyName.Substring(0, endIndex);
                    }
                    else
                    {
                        Console.WriteLine("获取正妹名称出错！ id=" + id);
                        return;
                    }
                    parser.AnalyzePage();
                    divList = parser.ExtractAllNodesThatMatch(BeautyFlowImgFilter);
                    for (int i = 0; i < divList.Count; i++)
                    {
                        ImageTag imgNode = (ImageTag)divList[i];
                        if (imgNode.Attributes.ContainsKey("SRC"))
                        {
                            string imgUrl = imgNode.GetAttribute("SRC");
                            startIndex = imgUrl.IndexOf("/media.curator.im/images/");
                            imgUrl = "http:/" + imgUrl.Substring(startIndex);
                            if (!imgFileNameSet.Contains(imgUrl))//以前没有下载过这张照片
                            {
                                imgFileNameSet.Add(imgUrl);
                                currentImgFileNameSet.Add(imgUrl, saveBeautyFlowBasePath + beautyName + "_" + id + "_" + i + ".jpg");
                                if (!File.Exists(saveBeautyFlowBasePath + beautyName + "_" + id + "_" + i + ".jpg"))
                                {
                                    WebClient wc = new WebClient();
                                    wc.DownloadFileAsync(new Uri(imgUrl), saveBeautyFlowBasePath + beautyName + "_" + id + "_" + i + ".jpg");
                                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                                    totalPicturesCount++;
                                }
                                imgFileNameSet.Add(imgUrl);
                            }
                        }
                        else
                        {
                            Console.WriteLine("获取正妹照片出错！ id=" + id);
                            return;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("得到的HTML为空！");
                    return;
                }
            }
            catch (Exception ex)
            {
                //if (httpWebResponse != null)
                //{
                //    httpWebResponse = (HttpWebResponse)ex.Response;
                //    if (!httpWebResponse.StatusCode.Equals(HttpStatusCode.NotFound))
                //    {
                //        Console.WriteLine("访问网页出错！状态码：" + httpWebResponse.StatusCode);
                //    }
                //    httpWebResponse.Close();
                //}
            }
        }

        //重新下载没有出现在本次已下载照片链接地址集合中的照片
        static void ReDownloadPictures()
        {
            foreach (KeyValuePair<string, string> fileName in currentImgFileNameSet)
            {
                if (!downloadFileNameSet.Contains(fileName.Key))
                {
                    WebClient wc = new WebClient();
                    wc.DownloadFileAsync(new Uri(fileName.Key), fileName.Value);
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                }
            }
        }

        static void OutputImgFileNameSet()
        {
            StreamWriter fw = new StreamWriter("ImgFileNameSet.txt");
            foreach (var obj in imgFileNameSet)
            {
                fw.WriteLine(obj);
            }
            fw.Close();
        }

        static void ReadImgFileNameSetFromFile()
        {
            StreamReader fr = new StreamReader(imgFileNameSetFileName);
            while (!fr.EndOfStream)
            {
                imgFileNameSet.Add(fr.ReadLine());
            }
            fr.Close();
        }

        static void Main(string[] args)
        {
            if (File.Exists(imgFileNameSetFileName))
            {
                ReadImgFileNameSetFromFile();
                Console.WriteLine("读入已有照片列表完毕！");
            }
            else
            {
                Console.WriteLine("未找到已有照片列表！");
            }
            #region 一天一妹：例：http://curator.im/girl_of_the_day/2014-02-25/
            //获取至2014年2月25日
            DateTime today = DateTime.Now;
            bool quitFlag = false;
            bool initFlag = true;
            int initMonth = 2;
            int initDay = 25;
            for (int month = initMonth; month < 13; month++)
            {
                int day;
                if (initFlag)
                {
                    initFlag = false;
                    day = initDay;
                }
                else
                {
                    day = 1;
                }
                for (; day <= DateTime.DaysInMonth(2014, month); day++)
                {
                    DateTime currentDay = new DateTime(2014, month, day);
                    if (currentDay.CompareTo(today) > 0)
                    {
                        quitFlag = true;
                        break;
                    }
                    int resultNum = OneDayOneBeauty(currentDay.ToString("yyyy-MM-dd"));
                    Console.WriteLine(currentDay.ToString("yyyy-MM-dd") + " 找到" + resultNum + "张照片！");
                    totalPicturesCount += resultNum;
                }
                if (quitFlag)
                {
                    break;
                }
            }
            #endregion

            #region 正妹流 例：http://curator.im/item/48/
            //2014年2月25日获取至6910
            //int startPosition = 6911;
            //for (int i = startPosition; i <= 7692; i++)
            //{
            //    BeautyFlow(i);
            //    if ((i - startPosition + 1) % 100 == 0)
            //    {
            //        Console.WriteLine("完成" + (i - startPosition + 1) + "条！");
            //        Thread.Sleep(20000);
            //    }
            //}
            #endregion
            OutputImgFileNameSet();
            Console.WriteLine("共找到" + totalPicturesCount + "张照片！");
            int lastProcess = totalDownloadPicturesCount;
            while (totalDownloadPicturesCount < totalPicturesCount)
            {
                Thread.Sleep(20000);
                Console.WriteLine("已获取" + totalDownloadPicturesCount + "张照片！");
                lastProcess += totalDownloadPicturesCount;
                if (lastProcess == 8 * totalDownloadPicturesCount)
                {
                    Console.WriteLine("有" + (totalPicturesCount - totalDownloadPicturesCount) + "张照片下载失败，重新下载！");
                    ReDownloadPictures();
                    lastProcess = totalDownloadPicturesCount;
                }
            }
            Console.Write("按任意键继续……");
            Console.ReadLine();
        }
    }
}
