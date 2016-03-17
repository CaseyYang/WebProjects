using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Serialization;

namespace ExtractAddressFromDianPinRawData
{
    [Serializable]
    public class POI
    {
        /// <summary>
        /// 页面序号
        /// </summary>
        private int page;
        /// <summary>
        /// 页面序号
        /// </summary>
        [XmlAttributeAttribute()]
        public int Page
        {
            get { return page; }
            set { page = value; }
        }
        /// <summary>
        /// 条目序号
        /// </summary>
        private int number;
        /// <summary>
        /// 条目序号
        /// </summary>
        [XmlAttributeAttribute(AttributeName = "No.")]
        public int Number
        {
            get { return number; }
            set { number = value; }
        }
        /// <summary>
        /// 店名
        /// </summary>
        private string name;
        /// <summary>
        /// 店名
        /// </summary>
        [XmlElementAttribute()]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        /// <summary>
        /// 地址
        /// </summary>
        private string address;
        /// <summary>
        /// 地址
        /// </summary>
        [XmlElementAttribute()]
        public string Address
        {
            get { return address; }
            set { address = HttpUtility.HtmlDecode(value).Trim(); }
        }
        /// <summary>
        /// 电话
        /// </summary>
        private string phone;
        /// <summary>
        /// 电话
        /// </summary>
        [XmlElementAttribute()]
        public string Phone
        {
            get { return phone; }
            set { phone = HttpUtility.HtmlDecode(value).Trim(); }
        }
        /// <summary>
        /// 人均消费
        /// </summary>
        private double averageCost;
        /// <summary>
        /// 人均消费
        /// </summary>
        [XmlElementAttribute()]
        public double AverageCost
        {
            get { return averageCost; }
            set { averageCost = value; }
        }
        /// <summary>
        /// 标签
        /// </summary>
        private List<string> tags;
        /// <summary>
        /// 标签
        /// </summary>
        [XmlArray]
        public List<string> Tags
        {
            get { return tags; }
            set { tags = value; }
        }
        /// <summary>
        /// 口味
        /// </summary>
        private int tasteRemark;
        /// <summary>
        /// 口味
        /// </summary>
        [XmlElementAttribute()]
        public int TasteRemark
        {
            get { return tasteRemark; }
            set { tasteRemark = value; }
        }
        /// <summary>
        /// 环境
        /// </summary>
        private int environmentRemark;
        /// <summary>
        /// 环境
        /// </summary>
        [XmlElementAttribute()]
        public int EnvironmentRemark
        {
            get { return environmentRemark; }
            set { environmentRemark = value; }
        }
        /// <summary>
        /// 服务
        /// </summary>
        private int serviceRemark;
        /// <summary>
        /// 服务
        /// </summary>
        [XmlElementAttribute()]
        public int ServiceRemark
        {
            get { return serviceRemark; }
            set { serviceRemark = value; }
        }
        /// <summary>
        /// 星级
        /// </summary>
        private int rank;
        /// <summary>
        /// 星级
        /// </summary>
        [XmlElementAttribute()]
        public int Rank
        {
            get { return rank; }
            set { rank = value; }
        }
        /// <summary>
        /// 点评数量
        /// </summary>
        private int commentCount;
        /// <summary>
        /// 点评数量
        /// </summary>
        [XmlElementAttribute()]
        public int CommentCount
        {
            get { return commentCount; }
            set { commentCount = value; }
        }

        public POI()
        {
            name = address = phone = "";
            averageCost = tasteRemark = environmentRemark = serviceRemark = rank = commentCount = 0;
            tags = new List<string>();
        }
    }
}
