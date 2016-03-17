using System.Runtime.Serialization;

namespace WebSiteTest
{
    /// <summary>
    /// 表示一个查询点
    /// </summary>
    [DataContract]
    public class QueryPoint
    {
        /// <summary>
        /// 纬度
        /// </summary>
        [DataMember]
        public float Latitude { get; set; }
        /// <summary>
        /// 经度
        /// </summary>
        [DataMember]
        public float Longitude { get; set; }
        public QueryPoint()
        {
            Latitude = Longitude = -1;
        }
    }
}