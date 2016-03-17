using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using RTree;

namespace WebSiteTest
{
    /// <summary>
    /// 表示一个包含POI集合和R树索引中MBR集合的类
    /// </summary>
    /// [DataContract]
    public class RTreeJSON
    {
        /// <summary>
        /// 所有POI的经纬度坐标集合
        /// </summary>
        [DataMember]
        public List<POICoordinate> POICoordinateList;
        /// <summary>
        /// 所有MBR的西南角和东北角经纬度坐标集合
        /// </summary>
        [DataMember]
        public List<RectangleCoordinate> MbrList;
        /// <summary>
        /// 构造函数
        /// </summary>
        public RTreeJSON()
        {
            POICoordinateList = new List<POICoordinate>();
            MbrList = new List<RectangleCoordinate>();
        }
    }

    /// <summary>
    /// 表示一个POI点经纬度坐标值的结构
    /// </summary>
    [DataContract]
    public struct POICoordinate
    {
        /// <summary>
        /// 纬度
        /// </summary>
        [DataMember]
        public float Latitude;
        /// <summary>
        /// 经度
        /// </summary>
        [DataMember]
        public float Longitude;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="latitude">纬度</param>
        /// <param name="longitude">经度</param>
        public POICoordinate(float latitude, float longitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="poi">需要提取经纬度坐标值的POI</param>
        public POICoordinate(POI poi)
        {
            this.Latitude = poi.Latitude;
            this.Longitude = poi.Longitude;
        }
    }

    /// <summary>
    /// 表示一个矩形框西南角和东北角经纬度坐标值的结构
    /// </summary>
    [DataContract]
    public struct RectangleCoordinate
    {
        /// <summary>
        /// 西南角纬度
        /// </summary>
        [DataMember]
        public float swLatitude;
        /// <summary>
        /// 东北角纬度
        /// </summary>
        [DataMember]
        public float neLatitude;
        /// <summary>
        /// 西南角经度
        /// </summary>
        [DataMember]
        public float swLongitude;
        /// <summary>
        /// 东北角经度
        /// </summary>
        [DataMember]
        public float neLongitude;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="swlatitude">矩形框西南角纬度</param>
        /// <param name="nelatitude">矩形框东北角纬度</param>
        /// <param name="swlongitude">矩形框西南角经度</param>
        /// <param name="nelongitude">矩形框东北角经度</param>
        public RectangleCoordinate(float swlatitude, float nelatitude, float swlongitude, float nelongitude)
        {
            this.swLatitude = swlatitude;
            this.neLatitude = nelatitude;
            this.swLongitude = swlongitude;
            this.neLongitude = nelongitude;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="rectangle">需要提取西南角和东北角经纬度坐标值的矩形框</param>
        public RectangleCoordinate(Rectangle rectangle)
        {
            this.swLongitude = rectangle.Min.ToList<float>()[0];
            this.swLatitude = rectangle.Min.ToList<float>()[1];
            this.neLongitude = rectangle.Max.ToList<float>()[0];
            this.neLatitude = rectangle.Max.ToList<float>()[1];
        }
    }
}