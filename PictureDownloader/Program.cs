using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PictureDownloader
{
    class Program
    {
        static Dictionary<string, string> picUrls;//保存图片URL和图片文件名的映射关系
        static string filePath = "Download.txt";
        static string targetPath = "MeiZiTu\\";

        //一个图片下载完成后调用的操作
        static void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            WebClient wc = (WebClient)sender;
            wc.Dispose();
            Console.WriteLine("done");
        }

        static void Main(string[] args)
        {
            picUrls = new Dictionary<string, string>();
            StreamReader fReader = new StreamReader(filePath);
            while (!fReader.EndOfStream)
            {
                string[] rawStrs = fReader.ReadLine().Split(' ');
                picUrls.Add(rawStrs[0], rawStrs[1]);
            }
            fReader.Close();
            Console.WriteLine("共须下载" + picUrls.Count + "张图片");
            foreach (var pair in picUrls)
            {
                WebClient wc = new WebClient();
                Console.WriteLine(pair.Key);
                wc.DownloadFileAsync(new Uri(pair.Key), pair.Value);
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
            }
            Console.WriteLine("等待所有图片下载完毕……");
            Console.Read();
        }
    }
}
