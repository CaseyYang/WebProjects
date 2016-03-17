<%@ WebHandler Language="C#" Class="Handler" %>

using System;
using System.IO;
using System.Web;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Collections.Generic;
using System.Linq;
using WebSiteTest;
using RTree;

public class Handler : IHttpHandler
{
    /// <summary>
    /// 接收处理浏览器发送的HTTP请求
    /// </summary>
    /// <param name="context">封装HTTP请求的所有HTTP特定的信息</param>
    public void ProcessRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        Stream stream = request.InputStream;
        if (stream.Length != 0)
        {
            List<POI> poiList = (List<POI>)context.Application["poiList"];
            RTree<POI> rtree = (RTree<POI>)context.Application["POIRTree"];
            StreamReader streamReader = new StreamReader(stream);
            string queryPointsStr = streamReader.ReadToEnd();
            List<QueryPoint> queryPointList = queryPointsStr.ToJsonObject<List<QueryPoint>>();
            #region 简单ANN查询（用重心的1NN作为ANN结果）
            //int resultIndex = ANNQueryByPOIList(poiList, queryPointList);
            //if (resultIndex != -1)
            //{
            //    context.Response.Write(poiList[resultIndex].ToJsonString());
            //}
            //else
            //{
            //    context.Response.Write("none");
            //}
            #endregion
            #region 基于R树索引的简单ANN查询（用重心的1NN作为ANN结果）
            //POI resultPOI = ANNQueryByRTree(rtree, queryPointList);
            //if (resultPOI != null)
            //{
            //    context.Response.Write(resultPOI.ToJsonString());
            //}
            //else
            //{
            //    context.Response.Write("none");
            //}
            #endregion
            #region 基于R树索引的标准ANN查询（陈翀版本）
            POI resultPOI = ANNQueryByChenChong(rtree, queryPointList);
            if (resultPOI != null)
            {
                context.Response.Write(resultPOI.ToJsonString());
            }
            else
            {
                context.Response.Write("none");
            }
            #endregion
        }
        else
        {
            context.Response.Write("error");
        }
    }

    /// <summary>
    /// 最简单的ANN查询：遍历POI集合，找到与查询点集合的重心位置最接近的POI
    /// </summary>
    /// <param name="poiList">POI集合</param>
    /// <param name="queryPointList">查询点集合</param>
    /// <returns>查询结果POI在POI集合中的下标</returns>
    public int ANNQueryByPOIList(List<POI> poiList, List<QueryPoint> queryPointList)
    {
        int resultIndex = -1;
        //得到查询点集合的重心位置，记为braycentre
        float averageOfLatitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Latitude);
        float averageOfLongitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Longitude);
        QueryPoint braycentre = new QueryPoint()
        {
            Latitude = averageOfLatitude,
            Longitude = averageOfLongitude
        };
        //找到离braycentre最近的POI，记下其下标
        double currentMinDist = 999999;
        for (int i = 0; i < poiList.Count; i++)
        {
            if (poiList[i].Latitude > 0)
            {
                double distResult = Dist(poiList[i], braycentre);
                if (distResult < currentMinDist)
                {
                    currentMinDist = distResult;
                    resultIndex = i;
                }
            }
        }
        return resultIndex;
    }

    /// <summary>
    /// 基于R树索引的ANN查询：在R树索引中找到与查询点集合的重心位置最接近的POI
    /// </summary>
    /// <param name="rtree">POI集合的R树索引</param>
    /// <param name="queryPointList">查询点集合</param>
    /// <returns>返回查询结果或null</returns>
    private POI ANNQueryByRTree(RTree<POI> rtree, List<QueryPoint> queryPointList)
    {
        //得到查询点集合的重心位置
        float averageOfLatitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Latitude);
        float averageOfLongitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Longitude);
        //R树索引的最近邻查询
        List<POI> candidateList = rtree.Nearest(new Point(averageOfLongitude, averageOfLatitude, 0), 10000);
        //若候选结果不为空，则返回查询结果，否则返回null
        if (candidateList.Count > 0)
        {
            return candidateList[0];
        }
        else
        {
            return null;
        }
    }
    /// <summary>
    /// 基于R树索引的标准ANN查询（陈翀版本）
    /// </summary>
    /// <param name="rtree">POI集合的R树索引</param>
    /// <param name="queryPointList">查询点集合</param>
    /// <returns></returns>
    private POI ANNQueryByChenChong(RTree<POI> rtree, List<QueryPoint> queryPointList)
    {
        /* 具体方法：首先，得到查询点集合的重心m的最近邻（1NN），记录重心和最近邻的距离为下界UB，并记录当前最近邻为候选解；
             * 然后，依次判断重心的kNN，若不满足不等式|Q|*dist(m,kNN)>=UB，则更新下界UB，且记当前kNN为候选解；
             * 当满足上述不等式时，结束迭代，返回当前候选解作为最后ANN查询结果
            */
        //得到查询点集合的重心位置
        float averageOfLatitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Latitude);
        float averageOfLongitude = queryPointList.Average<QueryPoint>(queryPoint => queryPoint.Longitude);
        QueryPoint braycentre = new QueryPoint()
        {
            Latitude = averageOfLatitude,
            Longitude = averageOfLongitude
        };
        //R树索引的最近邻查询
        List<POI> candidateList = rtree.Nearest(new Point(averageOfLongitude, averageOfLatitude, 0), 10000);
        //初始化下界ub
        double ub = 100000;
        int numberOfQueryPoints = queryPointList.Count;
        //若候选结果不为空，则执行后续迭代步骤，否则返回null
        if (candidateList.Count > 0)
        {
            POI queryResult = null;
            //不断取重心点的kNN
            foreach (POI kNN in candidateList)
            {
                //判断是否满足结束条件：若满足，则返回上一候选解；否则更新候选解和下界ub，继续迭代
                double dist = Dist(kNN, braycentre);
                if (numberOfQueryPoints * dist >= ub)
                {
                    break;
                }
                else
                {
                    queryResult = kNN;
                    ub = dist;
                }
            }
            return queryResult;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 辅助函数：计算两点间的距离
    /// </summary>
    /// <param name="poi">POI点</param>
    /// <param name="queryPoint">查询点</param>
    /// <returns>两点之间的欧式距离</returns>

    private double Dist(POI poi, QueryPoint queryPoint)
    {
        return Math.Pow(Math.Abs(poi.Latitude - queryPoint.Latitude), 2) + Math.Pow(Math.Abs(poi.Longitude - queryPoint.Longitude), 2);
    }


    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}