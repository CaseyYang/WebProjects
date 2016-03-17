<%@ Application Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Xml.Serialization" %>
<%@ Import Namespace="WebSiteTest" %>
<%@ Import Namespace="RTree" %>

<script RunAt="server">

    void Application_Start(object sender, EventArgs e)
    {
        // 在应用程序启动时运行的代码
        ReadInPOI();
        CreateIndexForPOI(7, 3);
    }

    void Application_End(object sender, EventArgs e)
    {
        //  在应用程序关闭时运行的代码
        Application.Clear();
    }

    void Application_Error(object sender, EventArgs e)
    {
        // 在出现未处理的错误时运行的代码

    }

    void Session_Start(object sender, EventArgs e)
    {
        // 在新会话启动时运行的代码

    }

    void Session_End(object sender, EventArgs e)
    {
        // 在会话结束时运行的代码。 
        // 注意: 只有在 Web.config 文件中的 sessionstate 模式设置为
        // InProc 时，才会引发 Session_End 事件。如果会话模式设置为 StateServer
        // 或 SQLServer，则不引发该事件。

    }

    /// <summary>
    /// 读入服务器端的POI文件
    /// </summary>
    void ReadInPOI()
    {
        StreamReader reader = new StreamReader(@"D:\Document\Computer\WebApplicationsSolution\WebSiteTest\App_Data\POIList.xml");
        XmlSerializer sr = new XmlSerializer(typeof(List<POI>));
        List<POI> poiList = (List<POI>)sr.Deserialize(reader);
        reader.Close();
        Application["poiList"] = poiList;
    }

    /// <summary>
    /// 对POI集合建立R树索引
    /// </summary>
    /// <param name="max">R树索引非叶子结点包含最大条目数</param>
    /// <param name="min">R树索引非叶子结点包含最小条目数</param>
    void CreateIndexForPOI(int max, int min)
    {
        RTree<POI> rtree = new RTree<POI>(max, min);
        List<POI> poiList = (List<POI>)Application["poiList"];
        foreach (POI poi in poiList)
        {
            //有些POI没有获得有效的经纬度坐标；对于这些POI就不要加入R树索引了
            if (poi.Longitude > 0)
            {
                rtree.Add(new Rectangle(poi.Longitude, poi.Latitude, poi.Longitude, poi.Latitude, 0, 0), poi);
            }
        }
        Application["POIRTree"] = rtree;
    }
</script>
