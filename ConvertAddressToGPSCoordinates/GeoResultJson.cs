using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ConvertAddressToGPSCoordinates
{
    /// <summary>
    /// 表示从Google Map API返回的数据
    /// </summary>
    [DataContract]
    class GeoResultJson
    {
        [DataMember]
        public List<ClsA> results;
    }

    [DataContract]
    class ClsA
    {
        [DataMember]
        public Geometry geometry;
    }

    [DataContract]
    class Geometry
    {
        [DataMember]
        public Location location;
    }

    [DataContract]
    class Location
    {
        [DataMember]
        public float lat;
        [DataMember]
        public float lng;
    }
}
