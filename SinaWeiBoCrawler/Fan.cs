using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace SinaWeiBoCrawler
{
    [Serializable]
    public class Fan
    {
        private string name;
        /// <summary>
        /// 粉丝名称
        /// </summary>
        [XmlElementAttribute()]
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }
        private string userID;
        /// <summary>
        /// 用户ID
        /// </summary>
        [XmlAttributeAttribute(AttributeName = "ID")]
        public string UserID
        {
            get
            {
                return userID;
            }
            set
            {
                userID = value;
            }
        }
        private string linkURL;
        /// <summary>
        /// 粉丝微博链接
        /// </summary>
        [XmlElementAttribute()]
        public string LinkURL
        {
            get
            {
                return linkURL;
            }
            set
            {
                linkURL = value;
            }
        }
        private string gender;
        /// <summary>
        /// 粉丝性别
        /// </summary>
        [XmlElementAttribute()]
        public string Gender
        {
            get
            {
                return gender;
            }
            set
            {
                gender = value;
            }
        }
        private int followCount;
        /// <summary>
        /// 关注数
        /// </summary>
        [XmlElementAttribute()]
        public int FollowCount
        {
            get
            {
                return followCount;
            }
            set
            {
                followCount = value;
            }
        }
        private int fansCount;
        /// <summary>
        /// 粉丝数
        /// </summary>
        [XmlElementAttribute()]
        public int FansCount
        {
            get
            {
                return fansCount;
            }
            set
            {
                fansCount = value;
            }
        }
        private int feedsCount;
        /// <summary>
        /// 微博数
        /// </summary>
        [XmlElementAttribute()]
        public int FeedsCount
        {
            get
            {
                return feedsCount;
            }
            set
            {
                feedsCount = value;
            }
        }
        private string introduction;
        /// <summary>
        /// 简介
        /// </summary>
        [XmlElementAttribute()]
        public string Introduction
        {
            get
            {
                return introduction;
            }
            set
            {
                introduction = HttpUtility.HtmlDecode(value);
            }
        }
        private string location;
        /// <summary>
        /// 地点
        /// </summary>
        [XmlElementAttribute()]
        public string Location
        {
            get
            {
                return location;
            }
            set
            {
                location = HttpUtility.HtmlDecode(value);
            }
        }
        private string followMethod;
        /// <summary>
        /// 添加关注的方式
        /// </summary>
        [XmlElementAttribute()]
        public string FollowMethod
        {
            get
            {
                return followMethod;
            }
            set
            {
                followMethod = value;
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Fan()
        {
            this.Name = "";
            this.UserID = "";
            this.Gender = "";
            this.LinkURL = "";
            this.FollowCount = 0;
            this.FansCount = 0;
            this.FeedsCount = 0;
            this.Introduction = "";
            this.Location = "";
            this.FollowMethod = "";
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">微博用户名</param>
        /// <param name="gender">用户性别</param>
        /// <param name="uid">用户ID</param>
        /// <param name="url">微博链接</param>
        /// <param name="followerCount">关注数</param>
        /// <param name="fansCount">粉丝数</param>
        /// <param name="feedsCount">微博数</param>
        /// <param name="introduction">简介</param>
        /// <param name="location">地点</param>
        /// <param name="method">关注方式</param>
        public Fan(string name, string gender, string uid, string url, int followerCount, int fansCount, int feedsCount, string introduction, string location, string method)
        {
            this.Name = name;
            this.UserID = uid;
            this.Gender = gender;
            this.LinkURL = url;
            this.FollowCount = followerCount;
            this.FansCount = fansCount;
            this.FeedsCount = feedsCount;
            this.Introduction = introduction;
            this.Location = location;
            this.FollowMethod = method;
        }
    }
}
