using System;
using System.Collections.Generic;

namespace Himall.DTO
{
    public class QQMapLocationResult
    {
        /// <summary>
        /// 状态
        /// </summary>
        public int status { get; set; }
        /// <summary>
        /// 信息
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 请求ID
        /// </summary>
        public string request_id { get; set; }
        /// <summary>
        /// 详细结果
        /// </summary>
        public QQMapResult result { get; set; }

    }

    public class QQMapResult
    {
        /// <summary>
        /// 定位信息
        /// </summary>
        public Location location { get; set; }
        /// <summary>
        /// 详细地址
        /// </summary>
        public string address { get; set; }
        /// <summary>
        /// 格式化地址信息
        /// </summary>
        public FormattedAddress formatted_address { get; set; }
        /// <summary>
        /// 地址明细
        /// </summary>
        public address_component address_component { get; set; }
        /// <summary>
        /// 地区编号
        /// </summary>
        public ad_info ad_info { get; set; }
    }
   

    /// <summary>
    /// 地址区号信息
    /// </summary>
    public class ad_info
    {
        public string adcode { get; set; }
    }
    /// <summary>
    /// 格式化地址信息
    /// </summary>
    public class FormattedAddress
    {
        /// <summary>
        /// 
        /// </summary>
        public string recommend { get; set; }
        public string rough { get; set; }
    }
    /// <summary>
    /// 通过接口获取坐标之间的结果实体
    /// </summary>
    public class MapApiDistanceResult
    {
        /// <summary>
        /// 状态码  0 正常   310参数错误  311 key错误    306  请求有护持信息请检查字符串   110 请求来源未授权
        /// </summary>
        public Int32 status { get; set; }
        /// <summary>
        /// 对状态码的描述
        /// </summary>
        public string info { get; set; }
        /// <summary>
        /// 结果
        /// </summary>
        public IList<CaclResultItem> results { get; set; }
    }

    public class MapApiCoordinateResult
    {
        /// <summary>
        /// 状态码  0 正常   310参数错误  311 key错误    306  请求有护持信息请检查字符串   110 请求来源未授权
        /// </summary>
        public Int32 status { get; set; }
        /// <summary>
        /// 对状态码的描述
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 结果
        /// </summary>
        public Coordinates result { get; set; }
    }

    public class CalcResults
    {
        public IList<CaclResultItem> result { get; set; }
    }

    /// <summary>
    /// 计算结果列
    /// </summary>
    public class CaclResultItem
    {
        /// <summary>
        /// 起点坐标，起点坐标序列号（从１开始）
        /// </summary>
        public int origin_id
        {
            get;
            set;
        }
        /// <summary>
        /// 目标点坐标，终点坐标序列号（从１开始）
        /// </summary>
        public int dest_id
        {
            get;
            set;
        }
        /// <summary>
        /// 起点到终点的距离，单位：米
        /// </summary>
        public decimal distance
        {
            get;
            set;
        }
        /// <summary>
        /// 预计行驶时间
        /// </summary>
        public decimal duration
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 坐标
    /// </summary>
    public class Coordinates
    {
        /// <summary>
        /// 纬度
        /// </summary>
        public string lat
        {
            get;
            set;
        }
        /// <summary>
        /// 经度
        /// </summary>
        public string lng
        {
            get;
            set;
        }
        /// <summary>
        /// 坐标
        /// </summary>
        public string LatLng
        {
            get
            {
                return lat + "," + lng;
            }
        }
        public Location location { get; set; }
    }
    public class Location
    {
        /// <summary>
        /// 纬度
        /// </summary>
        public string lat
        {
            get;
            set;
        }
        /// <summary>
        /// 经度
        /// </summary>
        public string lng
        {
            get;
            set;
        }
    }
    public class MapGetAddressByLatLngResult
    {
        /// <summary>
        /// 状态码  0 正常   310参数错误  311 key错误    306  请求有护持信息请检查字符串   110 请求来源未授权
        /// </summary>
        public Int32 status { get; set; }
        /// <summary>
        /// 对状态码的描述
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// 结果
        /// </summary>
        public AddressByLatLng result { get; set; }
    }
    public class AddressByLatLng
    {
        public string address { get; set; }
        public address_component address_component { get; set; }
        public address_reference address_reference { get; set; }
    }
    public class address_component
    {
        public string nation { get; set; }
        public string province { get; set; }
        public string city { get; set; }
        public string district { get; set; }
        public string street { get; set; }
        public string street_number { get; set; }
    }
    public class address_reference
    {
        public town town { get; set; }
        public landmark_l2 landmark_l2 { get; set; }
    }
    public class town
    {
        public string title { get; set; }
    }
    public class landmark_l2
    {
        public string title { get; set; }
    }


    public class NearAddressResult
    {
        //状态 0=正常
        public Int32 status { get; set; }

        //本次搜索结果总数
        public Int32 count { get; set; }
        public IList<POIInfo> data { get; set; }
    }
    public class POIInfo
    {
        public string id { get; set; }
        public string title { get; set; }
        public string address { get; set; }
        public Location location { get; set; }
    }

    public class GaodeGetAddressByLatLngResult
    {
        public Int32 status { get; set; }
        public string info { get; set; }
        public Regeocode regeocode { get; set; }
    }
    public class Regeocode
    {
        public string formatted_address { get; set; }
        public AddressComponent addressComponent { get; set; }
    }

    public class AddressComponent
    {
        public string province { get; set; }

        public string city { get; set; }

        public string district { get; set; }

        public string township { get; set; }

        public Building building { get; set; }

        public Neighborhood neighborhood { get; set; }
    }
    public class Building
    {
        public string name { get; set; }
    }
    public class Neighborhood
    {
        public string name { get; set; }
    }
}
