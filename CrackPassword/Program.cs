using System;
using System.IO;
using System.Net;
using System.Text;

namespace CrackPassword
{
    class Program
    {
        static void CrackPasswordForZTHBlog()
        {
            string url = "http://zhu.tianhua.me/wp-login.php?action=postpass";
            string postString = "post_password=aaa&Submit=Submit";
            byte[] postData = Encoding.UTF8.GetBytes(postString);
            string htmlContent = null;

            #region 使用WebClient类
            //WebClient webClient = new WebClient();
            //webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            //byte[] responseData = webClient.UploadData(url, "POST", postData);
            //string srcString = Encoding.UTF8.GetString(responseData);
            //htmlContent = Encoding.UTF8.GetString(responseData);
            //webClient.Dispose();
            #endregion

            #region 使用HttpWebRequest类
            HttpWebRequest request = HttpWebRequest.CreateHttp(url);
            request.Referer = "http://zhu.tianhua.me/archives/8329";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Method = "POST";
            request.ContentLength = postData.Length;
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Console.WriteLine(response.StatusCode);
            StreamReader reader = new StreamReader(new BufferedStream(response.GetResponseStream(), 4 * 200 * 1024));
            htmlContent = reader.ReadToEnd();
            response.Close();
            #endregion

            StreamWriter writer = new StreamWriter("zth.html");
            writer.Write(htmlContent);
            writer.Close();
        }
        static void Main(string[] args)
        {
        }
    }
}
