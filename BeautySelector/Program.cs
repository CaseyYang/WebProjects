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
        //迅雷下载代理对象
        static ThunderAgentLib.AgentClass thunderAgent = new ThunderAgentLib.AgentClass();

        static void GetPicUrlsFromBeautyPersonalPage(ImageTag imgNode, int fileIndex, int type)
        {
            if (imgNode.Attributes.ContainsKey("SRC") || imgNode.Attributes.ContainsKey("DATA-CFSRC"))
            {
                string imgUrl = imgNode.Attributes.ContainsKey("SRC") ? imgNode.GetAttribute("SRC") : imgNode.GetAttribute("DATA-CFSRC");
                //2014年5月16日根据网页结构修改
                //if (imgUrl.Contains("/250x0/"))
                //{
                //    imgUrl = imgUrl.Substring(imgUrl.IndexOf("/250x0/") + 7);
                //    imgUrl = "http://" + imgUrl;
                //}
                //int startIndex = imgUrl.IndexOf("/media.curator.im/images/");
                //imgUrl = "http:/" + imgUrl.Substring(startIndex);
                if (!imgFileNameSet.Contains(imgUrl))
                {
                    string imgName = "";
                    if (imgNode.Attributes.ContainsKey("ALT"))
                    {
                        imgName = imgNode.GetAttribute("ALT");
                        if (type == 2)//type为2是爬取“正妹流”中的妹子的网页的情况
                        {
                            imgName = imgName.Substring(4);
                        }
                    }
                    else
                    {
                        Console.WriteLine("第" + fileIndex + "张图片无法获取alt属性！");
                        return;
                    }
                    imgFileNameSet.Add(imgUrl);
                    //因为要把美女的名字作为文件夹名，所以要排除所有不能用于文件夹的字符
                    int invalideCharIndex = imgName.IndexOfAny(Path.GetInvalidPathChars());
                    while (invalideCharIndex != -1)
                    {
                        imgName = imgName.Remove(invalideCharIndex, 1);
                        invalideCharIndex = imgName.IndexOfAny(Path.GetInvalidPathChars());
                    }
                    //因为要把美女的名字作为文件名，所以要排除所有不能用于文件名的字符
                    invalideCharIndex = imgName.IndexOfAny(Path.GetInvalidFileNameChars());
                    while (invalideCharIndex != -1)
                    {
                        imgName = imgName.Remove(invalideCharIndex, 1);
                        invalideCharIndex = imgName.IndexOfAny(Path.GetInvalidFileNameChars());
                    }
                    string completeImgName = type == 1 ? saveOneDayOneBeautyBasePath + imgName : saveBeautyFlowBasePath + imgName;//和上面类似，用type来区别图片保存路径
                    if (!Directory.Exists(completeImgName))
                    {
                        Directory.CreateDirectory(completeImgName);
                    }
                    currentImgFileNameSet.Add(imgUrl, completeImgName + "\\" + imgName + " (" + fileIndex + ").jpg");
                    thunderAgent.AddTask2(imgUrl, imgName + " (" + fileIndex + ").jpg", "D:\\Download\\" + completeImgName + "\\", "", "", 1, 0, 1);
                    fileIndex++;
                }
            }
            else
            {
                Console.WriteLine("无法获取第" + fileIndex + "张图片！");
                return;
            }
        }

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
                    for (int i = 0; i < divList.Count; i++)
                    {
                        ImageTag imgNode = (ImageTag)divList[i];
                        //2014年5月16日根据网页结构修改
                        GetPicUrlsFromBeautyPersonalPage(imgNode, i, 1);
                    }
                    return divList.Count;
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
                    Console.WriteLine("第一个html读取完成！");
                    int startIndex = htmlContent.IndexOf("/girl/");
                    int endIndex = htmlContent.IndexOf("/", startIndex + 6) + 1;
                    string beautyMorePicturesLink = "http://curator.im" + htmlContent.Substring(startIndex, endIndex - startIndex);
                    //Console.WriteLine(beautyMorePicturesLink);
                    string htmlContentTwo = "";
                    httpWebRequest = HttpWebRequest.CreateHttp(beautyMorePicturesLink);
                    httpWebRequest.Method = "GET";
                    httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024));
                        htmlContentTwo = reader.ReadToEnd();
                        httpWebResponse.Close();
                        reader.Close();
                    }
                    Console.WriteLine("第二个html读取完成！");
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
                        Console.Read();
                        return;
                    }
                    parser.AnalyzePage();
                    divList = parser.ExtractAllNodesThatMatch(BeautyFlowImgFilter);
                    for (int i = 0; i < divList.Count; i++)
                    {
                        ImageTag imgNode = (ImageTag)divList[i];
                        GetPicUrlsFromBeautyPersonalPage(imgNode, i, 2);
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

        static void OutputCurrentImgFileNameSet()
        {
            StreamWriter fw = new StreamWriter("CurrentImgFileNameSet.txt");
            StreamWriter fw2 = new StreamWriter("ImgFileDownload.txt");//此文件用于得到便于迅雷下载的图片URL
            foreach (KeyValuePair<string, string> pair in currentImgFileNameSet)
            {
                fw.WriteLine(pair.Key + " " + pair.Value);
                fw2.WriteLine(pair.Key);
            }
            fw.Close();
            fw2.Close();
        }

        static void ReadImgFileNameSetFromFile()
        {
            StreamReader fr = new StreamReader(imgFileNameSetFileName);
            while (!fr.EndOfStream)
            {
                imgFileNameSet.Add(fr.ReadLine());
            }
            fr.Close();
            Console.WriteLine("已有照片列表中共用" + imgFileNameSet.Count + "条记录！");
        }

        static void Main(string[] args)
        {
            #region 第一步：从网站上读取待下载图片的链接，保存链接到文件；
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
            //获取至2014年3月18日
            //获取至2014年4月9日
            //获取至2014年5月16日
            //DateTime today = DateTime.Now;
            //bool quitFlag = false;
            //bool initFlag = true;
            //int initMonth = 4;
            //int initDay = 9;
            //for (int month = initMonth; month < 13; month++)
            //{
            //    int day;
            //    if (initFlag)
            //    {
            //        initFlag = false;
            //        day = initDay;
            //    }
            //    else
            //    {
            //        day = 1;
            //    }
            //    for (; day <= DateTime.DaysInMonth(2014, month); day++)
            //    {
            //        DateTime currentDay = new DateTime(2014, month, day);
            //        if (currentDay.CompareTo(today) > 0)
            //        {
            //            quitFlag = true;
            //            break;
            //        }
            //        int resultNum = OneDayOneBeauty(currentDay.ToString("yyyy-MM-dd"));
            //        Console.WriteLine(currentDay.ToString("yyyy-MM-dd") + " 找到" + resultNum + "张照片！");
            //        totalPicturesCount += resultNum;
            //    }
            //    if (quitFlag)
            //    {
            //        break;
            //    }
            //}
            #endregion

            #region 正妹流 例：http://curator.im/item/48/
            //2014年2月25日获取至6910
            //2014年3月18日获取至7692
            //2014年4月9日获取至8450
            //2014年5月18日获取至9666
            int startPosition = 8451;
            for (int i = startPosition; i <= 9666; i++)
            {
                BeautyFlow(i);
                Console.WriteLine("完成 " + i);
                //Thread.Sleep(20000);
            }
            #endregion

            #region 输出本次要下载的图片链接到文件ImgFileDownload.txt、CurrentImgFileNameSet.txt和ImgFileNameSet中
            OutputImgFileNameSet();
            thunderAgent.CommitTasks2(1);
            //OutputCurrentImgFileNameSet();
            Console.WriteLine("共找到" + totalPicturesCount + "张照片！");
            Console.Read();
            #endregion

            //int lastProcess = totalDownloadPicturesCount;
            //while (totalDownloadPicturesCount < totalPicturesCount)
            //{
            //    Thread.Sleep(20000);
            //    Console.WriteLine("已获取" + totalDownloadPicturesCount + "张照片！");
            //    lastProcess += totalDownloadPicturesCount;
            //    if (lastProcess == 8 * totalDownloadPicturesCount)
            //    {
            //        Console.WriteLine("有" + (totalPicturesCount - totalDownloadPicturesCount) + "张照片下载失败，重新下载！");
            //        ReDownloadPictures();
            //        lastProcess = totalDownloadPicturesCount;
            //    }
            //}
            //Console.Write("按任意键继续……");
            //Console.ReadLine();
            #endregion

            #region 第二步：从文件ImgFileDownload.txt中复制待下载的图片链接，使用迅雷下载
            #endregion

            #region 第三步：把文件从迅雷下载分别复制到OneDayOneBeauty/BeautyFlow文件夹中，并根据CurrentImgFileNameSet.txt中的键值对修改文件名。在调用迅雷API后，此部分代码可能没用了
            //读入CurrentImgFileNameSet.txt
            //StreamReader fr = new StreamReader("CurrentImgFileNameSet.txt");
            //while (!fr.EndOfStream)
            //{
            //    string rawStr = fr.ReadLine();
            //    string[] l = rawStr.Split(' ');
            //    int index = l[0].LastIndexOf('/');
            //    if (l.Length > 2)
            //    {
            //        for (int i = 2; i < l.Length; i++)
            //        {
            //            l[1] += " " + l[i];
            //        }
            //    }
            //    currentImgFileNameSet.Add(l[0].Substring(index + 1), l[1]);//currentImgFileNameSet集合在此处的作用是保存原始图片名和最终图片文件名之间的映射关系
            //}
            //fr.Close();
            //Console.WriteLine("CurrentImgFileNameSet.txt读入完毕！");
            ////读入迅雷下载的图片文件
            //DirectoryInfo directory = new DirectoryInfo(@"E:\Downloads\图片任务组_20140516_2317");//此处填入迅雷下载图片的文件夹路径
            //foreach (var file in directory.GetFiles())
            //{
            //    if (!currentImgFileNameSet.ContainsKey(file.Name))
            //    {
            //        Console.WriteLine(file.Name);
            //        Console.Read();
            //    }
            //    string fileName = currentImgFileNameSet[file.Name];
            //    //文件路径和文件名不合法的情况在生成CurrentImgFileNameSet.txt时已经检查过，所以不必再检查
            //    //int invalidCount = 0;
            //    //int invalidFileNameChar = fileName.IndexOfAny(Path.GetInvalidFileNameChars());
            //    //while (invalidFileNameChar != -1)
            //    //{
            //    //    invalidCount++;
            //    //    if (invalidCount > 2)
            //    //    {
            //    //        fileName = fileName.Remove(invalidFileNameChar, 1);
            //    //    }
            //    //    invalidFileNameChar = fileName.IndexOfAny(Path.GetInvalidFileNameChars(), invalidFileNameChar + 1);
            //    //}
            //    file.CopyTo(fileName);
            //}
            //Console.WriteLine("文件复制完毕！");
            //Console.WriteLine("按任意键继续……");
            //Console.Read();
            #endregion
        }
    }
}
