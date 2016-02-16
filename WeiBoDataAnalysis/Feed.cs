
using System;
using System.Web;
using System.Xml.Serialization;
namespace WeiBoDataAnalysis
{
    [Serializable]
    public class Feed
    {
        private int page;
        [XmlAttributeAttribute()]
        public int Page
        {
            get { return page; }
            set { page = value; }
        }
        private int number;
        [XmlAttributeAttribute(AttributeName = "No.")]
        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        private string author;//微博作者
        [XmlElementAttribute()]
        public string Author
        {
            get { return author; }
            set { author = value; }
        }
        private string remarkName;//备注名
        [XmlElementAttribute()]
        public string RemarkName
        {
            get { return remarkName; }
            set { remarkName = value; }
        }
        private string content;//微博内容
        [XmlElementAttribute()]
        public string Content
        {
            get { return content; }
            set { content = HttpUtility.HtmlDecode(value); }
        }
        private bool reFeedOrNot;//是否为转发微博
        [XmlElementAttribute()]
        public bool ReFeedOrNot
        {
            get { return reFeedOrNot; }
            set { reFeedOrNot = value; }
        }
        private string originalAuthor;//微博原作者
        [XmlElementAttribute()]
        public string OriginalAuthor
        {
            get { return originalAuthor; }
            set { originalAuthor = value; }
        }
        private string reFeedFrom;//转发微博的来源
        [XmlElementAttribute()]
        public string ReFeedFrom
        {
            get { return reFeedFrom; }
            set { reFeedFrom = value; }
        }
        private string reFeedReason;//转发微博理由
        [XmlElementAttribute()]
        public string ReFeedReason
        {
            get { return reFeedReason; }
            set { reFeedReason = HttpUtility.HtmlDecode(value); }
        }
        private int likeCount;//赞数
        [XmlElementAttribute()]
        public int LikeCount
        {
            get { return likeCount; }
            set { likeCount = value; }
        }
        private int reFeedCount;//转发数
        [XmlElementAttribute()]
        public int ReFeedCount
        {
            get { return reFeedCount; }
            set { reFeedCount = value; }
        }
        private int commentCount;//评论数
        [XmlElementAttribute()]
        public int CommentCount
        {
            get { return commentCount; }
            set { commentCount = value; }
        }
        private string time;//微博发布时间
        [XmlElementAttribute()]
        public string Time
        {
            get { return time; }
            set { time = value; }
        }
        private string device;//发送设备
        [XmlElementAttribute()]
        public string Device
        {
            get { return device; }
            set { device = value; }
        }
        private string location;//发送地点
        [XmlElementAttribute()]
        public string Location
        {
            get { return location; }
            set { location = value; }
        }


        public Feed()
        {
            author = "";
            remarkName = "";
            content = "";
            time = "";
            reFeedOrNot = false;
            originalAuthor = "";
            reFeedFrom = "";
            reFeedReason = "";
            device = "";
            location = "";
            reFeedCount = likeCount = commentCount = 0;

        }
        public Feed(string author, string remarkName, string content, string time, bool isReFeed, string originalAuthor, string source, string reason, string device, string location, int reFeedCount, int commentCount, int likeCount)
        {
            this.author = author;
            this.remarkName = remarkName;
            this.content = content;
            this.time = time;
            this.reFeedOrNot = isReFeed;
            this.originalAuthor = originalAuthor;
            this.reFeedFrom = source;
            this.reFeedReason = reason;
            this.device = device;
            this.location = location;
            this.reFeedCount = reFeedCount;
            this.commentCount = commentCount;
            this.likeCount = likeCount;
        }
    }
}
