using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Web;
using System.Xml.Serialization;

namespace ConvertAddressToGPSCoordinates
{
    /// <summary>
    /// 表示一个兴趣点
    /// </summary>
    [Serializable]
    [DataContract]
    public class POI
    {
        /// <summary>
        /// 页面序号
        /// </summary>
        [XmlAttributeAttribute()]
        public int Page { get; set; }
        /// <summary>
        /// 条目序号
        /// </summary>
        [XmlAttributeAttribute(AttributeName = "No.")]
        public int Number { get; set; }
        /// <summary>
        /// 店名
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public string Name { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        private string address;
        /// <summary>
        /// 地址
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public string Address
        {
            get { return address; }
            set { address = HttpUtility.HtmlDecode(value).Trim(); }
        }
        /// <summary>
        /// 经度
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public float Longitude { get; set; }
        /// <summary>
        /// 纬度
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public float Latitude { get; set; }
        /// <summary>
        /// 电话
        /// </summary>
        private string phone;
        /// <summary>
        /// 电话
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public string Phone
        {
            get { return phone; }
            set { phone = HttpUtility.HtmlDecode(value).Trim(); }
        }
        /// <summary>
        /// 人均消费
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public float AverageCost { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        [XmlArray]
        public List<string> Tags { get; set; }
        /// <summary>
        /// 口味
        /// </summary>
        [XmlElementAttribute()]
        public int TasteRemark { get; set; }
        /// <summary>
        /// 环境
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public int EnvironmentRemark { get; set; }
        /// <summary>
        /// 服务
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public int ServiceRemark { get; set; }
        /// <summary>
        /// 星级
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public int Rank { get; set; }
        /// <summary>
        /// 点评数量
        /// </summary>
        [XmlElementAttribute()]
        [DataMember()]
        public int CommentCount { get; set; }

        public POI()
        {
            Name = address = phone = "";
            AverageCost = TasteRemark = EnvironmentRemark = ServiceRemark = Rank = CommentCount = 0;
            Longitude = Latitude = -1;
            Tags = new List<string>();
        }
    }
}
