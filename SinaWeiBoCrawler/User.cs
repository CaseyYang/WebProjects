using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SinaWeiBoCrawler
{
    [Serializable]
    public class User
    {
        [XmlElementAttribute()]
        public string NickName
        {
            get;
            set;
        }
        [XmlElementAttribute()]
        public string RemarkName
        {
            get;
            set;
        }
        [XmlElementAttribute()]
        public string LinkURL
        {
            get;
            set;
        }
        [XmlElementAttribute()]
        public string SelfIntroduction
        {
            get;
            set;
        }
        [XmlElementAttribute()]
        public string Profile
        {
            get;
            set;
        }
        [XmlElementAttribute()]
        public string Tags
        {
            get;
            set;
        }
        [XmlArray]
        public List<Feed> FeedList
        {
            get;
            set;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public User()
        {
            NickName = "";
            RemarkName = "";
            LinkURL = "";
            SelfIntroduction = "";
            Profile = "";
            Tags = "";
            FeedList = new List<Feed>();
        }
    }
}
