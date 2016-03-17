//map保存当前加载的地图；currentCity保存到当前城市；friends保存查询点位置集合；
//markers保存地图上表示查询点的marker标记集合；annResultMarker保存表示查询结果的标记；rectangles保存矩形框集合；restaurants保存POI标记集合
//lastInfoWindow保存当前一个气泡窗；annResultMarker保存当前一个POI标记
var map, currentCity;
var friends = [], markers = [], rectangles = [], restaurants = [];
var lastInfoWindow, annResultMarker;

//得到ajax对象
function GetAjaxHttp() {
    var xmlHttp;
    try {
        // Firefox, Chrome
        xmlHttp = new XMLHttpRequest();
    } catch (e) {
        // Internet Explorer
        try {
            xmlHttp = new ActiveXObject("Msxml2.XMLHTTP");
        } catch (e) {
            try {
                xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
            } catch (e) {
                alert("您的浏览器不支持AJAX！");
                return false;
            }
        }
    }
    return xmlHttp;
}

/*通过ajax获取ANN查询结果
  url：接收ajax请求的url
  methodType：post/get
  con：true(异步)|false(同步)
  parameter：传递给服务器的数据
 */
function GetAjaxResult(url, methodType, con, parameter, callBackFunc) {
    var xmlHttp = GetAjaxHttp();
    xmlHttp.open(methodType, url, con);
    var jsonStr = JSON.stringify(parameter);
    xmlHttp.setRequestHeader("Content-Type", "application/x-www-form-urlencoded");
    xmlHttp.send(jsonStr);
    xmlHttp.onreadystatechange = function () {
        if (xmlHttp.readyState == 4) {
            //HTTP响应已经完全接收才调用
            var result = xmlHttp.responseText;
            if (result == "error") {
                alert("Something wrong happened!");
            }
            else {
                if (result == "none") {
                    alert("Sorry, No POI found! ");
                }
                else {
                    var annResult = JSON.parse(result);
                    callBackFunc(annResult);
                }
            }
        }
    };
}

//页面加载初始化
function Initialize(city) {
    //初始化地图
    geoCoder = new google.maps.Geocoder();
    lastInfoWindow = new google.maps.InfoWindow();
    annResultMarker = new google.maps.Marker();
    geoCoder.geocode({
        'address': city
    },
    function (results, state) {
        if (state = google.maps.GeocoderStatus.OK) {
            if (results[0]) {
                var latlng = results[0].geometry.location;
                var myOptions = {
                    zoom: 10,
                    center: latlng,
                    mapTypeId: google.maps.MapTypeId.ROADMAP
                };
                map = new google.maps.Map((document.getElementById("map_canvas")), myOptions);
                //定义在地图上点击事件，点击处即为一个查询点；把该查询点保存在friends数组中，并生成一个marker标记
                google.maps.event.addListener(map, 'click', function (e) {
                    var position = e.latLng;
                    var friend = {};
                    friend.Latitude = position.lat();
                    friend.Longitude = position.lng();
                    friends.push(friend);
                    var marker = new google.maps.Marker({
                        position: position,
                        map: map
                    });
                    markers.push(marker);
                    //定义点击marker标记事件；在friends数组中删除该marker标记所对应的查询点，并在map上删除该marker标记
                    google.maps.event.addListener(marker, 'click', function () {
                        var position = marker.getPosition();
                        var index = -1;
                        for (var i = 0; i < friends.length; i++) {
                            //和C#或Java类似，似乎js中的==对于一个对象来说是判断两个变量是否指向同一对象，所以不能直接写friends[i]==position
                            if (friends[i].lat == position.lat && friends[i].lng == position.lng) {
                                index = i;
                                break;
                            }
                        }
                        //splice(start,deleteCount,val1,val2,...)：从start位置开始删除deleteCount项，并从该位置起插入val1,val2,...
                        friends.splice(index, 1);
                        marker.setMap(null);
                    });
                });
                //显示向Google查询的两点间行车路线代码
                //directionsDisplay.setMap(map);
            }
        }
        else {
            alert("错误代码：" + state);
        }
    });
}

//清除页面所有元素
function ClearAll() {
    for (var i = 0; i < markers.length; i++) {
        markers[i].setMap(null);
    }
    for (var i = 0; i < rectangles.length; i++) {
        rectangles[i].setMap(null);
    }
    for (var i = 0; i < restaurants.length ; i++) {
        restaurants[i].setMap(null);
    }
    friends = [];
    markers = [];
    rectangles = [];
    restaurants = [];
    ClearRestaurantInfo();
    annResultMarker.setMap(null);
    lastInfoWindow.close();
}

//清除餐馆信息区域所有内容
function ClearRestaurantInfo() {
    var parentDiv = document.getElementById("restaurantinfotable");
    while (parentDiv.hasChildNodes()) {
        parentDiv.removeChild(parentDiv.firstChild);
    }
}

//ANN
function GetNearestRestaurant() {
    GetAjaxResult("Handler.ashx", "post", true, friends, ShowRestaurantInfo);
}

//显示餐馆信息
function ShowRestaurantInfo(annResult) {
    //在地图上显示查询结果位置
    var target = new google.maps.LatLng(annResult.Latitude, annResult.Longitude);
    if (annResultMarker != null) {
        annResultMarker.setMap(null);
        ClearRestaurantInfo();
    }
    annResultMarker = new google.maps.Marker({
        position: target,
        map: map,
        icon: "restaurant.jpg"
    });
    //增加点击标记的事件：消除标记
    google.maps.event.addListener(annResultMarker, 'click', function () {
        annResultMarker.setMap(null);
        ClearRestaurantInfo();
    });
    //在标记上显示一个气泡窗以显示POI的基本信息
    var infoWindow = new google.maps.InfoWindow({
        content: '餐馆名称：' + annResult.Name + '<br />地址：' + annResult.Address
    });
    lastInfoWindow.close();
    infoWindow.open(map, annResultMarker);
    lastInfoWindow = infoWindow;
    map.panTo(target);
    //在特定div区域内返回的餐馆信息
    var parentDiv = document.getElementById("restaurantinfotable");
    ClearRestaurantInfo();
    //餐馆名称
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "餐馆名称：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.Name;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //餐馆地址
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "地址：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.Address;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //餐馆电话
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "联系电话：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.Phone;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //人均消费
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "人均消费额：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.AverageCost + "元";
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //口味
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "口味：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.TasteRemark;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //环境
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "环境：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.EnvironmentRemark;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //服务
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "服务：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.ServiceRemark;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
    //星级
    var tr = document.createElement("tr");
    var tdKey = document.createElement("td");
    tdKey.setAttribute("style", "width:100px;vertical-align:top");
    tdKey.innerHTML = "星级：";
    var tdValue = document.createElement("td");
    tdValue.innerHTML = annResult.Rank;
    tr.appendChild(tdKey);
    tr.appendChild(tdValue);
    parentDiv.appendChild(tr);
}

//显示POI集合的R树索引
function GetRTreeMBRs() {
    GetAjaxResult("Handler2.ashx", "post", true, null, ShowRTreeMBRs);
}

function ShowRTreeMBRs(result) {
    //显示所有矩形框
    for (var i = 0; i < result.MbrList.length; i++) {
        var swPoint = new google.maps.LatLng(result.MbrList[i].swLatitude, result.MbrList[i].swLongitude);
        var nePoint = new google.maps.LatLng(result.MbrList[i].neLatitude, result.MbrList[i].neLongitude);
        var rectangle = new google.maps.Rectangle({
            bounds: new google.maps.LatLngBounds(swPoint, nePoint),//矩形框范围，用一个指定了西南角坐标和东北角坐标的LatLngBounds对象来定义
            fillOpacity: 0,//介于 0.0 和 1.0 之间的填充不透明度
            map: map,//要在其上显示矩形的地图
            strokeWeight: 2//笔触宽度（以像素为单位）
        });
        rectangles.push(rectangle);
    }
    //显示POI标记
    for (var i = 0; i < result.POICoordinateList.length; i++) {
        var target = new google.maps.LatLng(result.POICoordinateList[i].Latitude, result.POICoordinateList[i].Longitude);
        var marker = new google.maps.Marker({
            position: target,
            map: map,
            icon: "smallrestaurant.jpg"
        });
        restaurants.push(marker);
    }
}