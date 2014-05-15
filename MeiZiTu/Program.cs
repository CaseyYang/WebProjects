using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MeiZiTu
{
    class Program
    {
        static Dictionary<string, string> shouldDownloadSet = new Dictionary<string, string>();
        static HashSet<string> haveDownloadSet = new HashSet<string>();
        static void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WebClient wc = (WebClient)sender;
            haveDownloadSet.Add(wc.BaseAddress);
            wc.Dispose();
        }

        static void Main(string[] args)
        {
            //2014年3月18日获取至4110
            //2014年4月18日获取至4174
            //2014年5月14日获取至4219
            #region Step 1: 找出所有存在的页面（即返回代码为200的），把生成的程序放在多个文件夹下同时跑，程序运行结束后在文件夹下会得到url.txt，里面保存着所有存在的页面链接；2014年3月18日最新页面为4110
            //StreamWriter fw = new StreamWriter("url.txt");
            //string baseUrl = "http://www.meizitu.com/a/";
            //for (int i = 4175; i < 4220; i++)
            //{
            //    string url = baseUrl + i + ".html";
            //    try
            //    {
            //        HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(url);
            //        httpWebRequest.Method = "GET";
            //        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //        if (httpWebResponse.StatusCode.Equals(HttpStatusCode.OK))
            //        {
            //            fw.WriteLine(url);
            //        }
            //        httpWebResponse.Close();
            //    }
            //    catch (WebException ex)
            //    {
            //        HttpWebResponse response = (HttpWebResponse)ex.Response;
            //        if (response != null)  //排除对象为空的错误
            //        {
            //            Console.WriteLine(response.StatusCode);
            //            response.Close();
            //        }
            //    }
            //    finally
            //    {
            //        Console.WriteLine(i);
            //    }
            //}
            //fw.Close();
            #endregion

            #region Step 2: 根据url.txt爬取页面中的妹子图片
            //string basePath = "MeiZiTu\\";
            //StreamReader fr = new StreamReader("url.txt");
            //List<string> links = new List<string>();
            //while (!fr.EndOfStream)
            //{
            //    links.Add(fr.ReadLine());
            //}
            //fr.Close();
            //foreach (string link in links)
            //{
            //    try
            //    {
            //        HttpWebRequest httpWebRequest = HttpWebRequest.CreateHttp(link);
            //        httpWebRequest.Method = "GET";
            //        HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            //        StreamReader reader = new StreamReader(new BufferedStream(httpWebResponse.GetResponseStream(), 4 * 200 * 1024), Encoding.GetEncoding("gb2312"));
            //        string htmlContent = reader.ReadToEnd();
            //        httpWebResponse.Close();
            //        reader.Close();
            //        int startIndex = 0;
            //        startIndex = htmlContent.IndexOf("<title>");
            //        int endIndex = htmlContent.IndexOf(" | 妹子图");
            //        string title = htmlContent.Substring(startIndex + 7, endIndex - startIndex - 7);
            //        List<string> picLinks = new List<string>();
            //        do
            //        {
            //            startIndex = htmlContent.IndexOf("src=\"http://www.meizitu.com/wp-content/uploads/", startIndex);
            //            if (startIndex != -1)
            //            {
            //                endIndex = htmlContent.IndexOf(".jpg", startIndex);
            //                startIndex += 5;
            //                string picLink = htmlContent.Substring(startIndex, endIndex + 4 - startIndex);
            //                if (picLink.IndexOf("limg.jpg") == -1 && picLink.IndexOf("hezuo") == -1)
            //                {
            //                    picLinks.Add(picLink);
            //                }
            //            }
            //            else
            //            {
            //                break;
            //            }
            //        } while (true);
            //        int picLinkIndex = 0;
            //        foreach (string picLink in picLinks)
            //        {
            //            string fileName = basePath + title + "_" + picLinkIndex + ".jpg";
            //            if (!shouldDownloadSet.ContainsKey(picLink))
            //            {
            //                shouldDownloadSet.Add(picLink, fileName);
            //                //WebClient wc = new WebClient();
            //                //wc.DownloadFileAsync(new Uri(picLink), fileName);
            //                //wc.DownloadFileCompleted += wc_DownloadFileCompleted;
            //            }
            //            picLinkIndex++;
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(link + "出错！");
            //    }
            //}
            //Console.WriteLine("共找到" + shouldDownloadSet.Count + "张照片！");
            //StreamWriter fw = new StreamWriter("Download.txt");
            //foreach (KeyValuePair<string, string> fileName in shouldDownloadSet)
            //{
            //    fw.WriteLine(fileName.Key + " " + fileName.Value);
            //}
            //fw.Close();
            //此部分代码可能没有必要了
            //StreamWriter fw2 = new StreamWriter("Download2.txt");
            //foreach (KeyValuePair<string, string> fileName in shouldDownloadSet)
            //{
            //    fw2.WriteLine(fileName.Key);
            //}
            //fw2.Close();
            #endregion

            #region Step 3: 根据Download.txt和保存图片的文件夹，重新下载文件大小为0的图片
            //string[] fileNameList = Directory.GetFiles("MeiZiTu/");
            //List<string> needReDownloadFileList = new List<string>();
            //foreach (string fileName in fileNameList)
            //{
            //    FileInfo file = new FileInfo(fileName);
            //    if (file.Length == 0)
            //    {
            //        needReDownloadFileList.Add(file.Name);
            //    }
            //}
            //StreamReader fr = new StreamReader("Download.txt");
            //while (!fr.EndOfStream)
            //{
            //    string rawStr = fr.ReadLine();
            //    int splitIndex = rawStr.IndexOf(".jpg MeiZiTu");
            //    string key = rawStr.Substring(0, splitIndex + 4);
            //    string value = rawStr.Substring(splitIndex + 13);
            //    if (!shouldDownloadSet.ContainsKey(value))
            //    {
            //        shouldDownloadSet.Add(value, key);
            //    }
            //}
            //foreach (string fileName in needReDownloadFileList)
            //{
            //    string url = shouldDownloadSet[fileName];
            //    WebClient wc = new WebClient();
            //    wc.DownloadFileAsync(new Uri(url), fileName);
            //    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
            //}
            //Console.Read();
            #endregion
        }
    }
}
