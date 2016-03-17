<%@ WebHandler Language="C#" Class="Handler2" %>

using System;
using System.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using WebSiteTest;
using RTree;

public class Handler2 : IHttpHandler
{

    public void ProcessRequest(HttpContext context)
    {
        //获取POI集合和R树索引中的所有MBR集合
        List<POI> poiList = (List<POI>)context.Application["poiList"];
        RTree<POI> rtree = (RTree<POI>)context.Application["POIRTree"];
        List<Rectangle> mbrs = rtree.GetMBRs();
        //定义一个要返回的JSON对象的实例
        RTreeJSON result = new RTreeJSON();
        //填充JSON对象中的POI集合数组
        foreach (POI poi in poiList)
        {
            result.POICoordinateList.Add(new POICoordinate(poi));
        }
        //填充JSON对象中的MBR集合数组        
        foreach (Rectangle rectangle in mbrs)
        {
            result.MbrList.Add(new RectangleCoordinate(rectangle));
        }
        System.IO.StreamWriter writer = new System.IO.StreamWriter(@"d:\result.json");
        writer.Write(result.ToJsonString<RTreeJSON>());
        writer.Close();
        context.Response.Write(result.ToJsonString<RTreeJSON>());
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

}