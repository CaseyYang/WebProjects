using System;
using System.Collections.Generic;
using System.Net;

namespace SinaWeiBoCrawler
{
    public interface ICrawler
    {
        //表明是移动版爬虫还是网页版爬虫
        String Name
        {
            get;
        }
        //登录用cookie
        String Cookie
        {
            get;
        }
        //登录URL
        String LoginUrl
        {
            get;
        }
        //查询URL
        String QueryUrl
        {
            get;
        }
        //登录微博
        void LoginWeiBo();
        //获取网页
        void GetHtmlFromWeiBo(int index);
        //获取微博信息
        void GetInfoFromHtml(int index,List<Feed> feedList);
        //调用爬取程序
        void RunCrawler(List<Feed> feedList);
    }
}
