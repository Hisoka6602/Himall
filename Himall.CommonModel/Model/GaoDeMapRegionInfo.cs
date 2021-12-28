using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{

    public class GaoDeMapRegionInfo
    {
        /// <summary>
        /// 状态
        /// </summary>
        public int status { get; set; }
        /// <summary>
        /// 信息
        /// </summary>
        public string info { get; set; }
        /// <summary>
        /// 信息码
        /// </summary>
        public int infocode { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// 地区信息
        /// </summary>
        public List<DistrictInfo> districts { get; set; }
    }

    /// <summary>
    /// 地区数据
    /// </summary>
    public class DistrictInfo
    {

        public string citycode { get; set; }
        /// <summary>
        /// 区域编码  街道没有独有的adcode，均继承父类（区县）的adcode
        /// </summary>
        public int adcode { get; set; }
        /// <summary>
        /// 地区名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 区域中心点 经纬度
        /// </summary>
        public string center { get; set; }
        /// <summary>
        /// 级别
        /// country:国家 province:省份（直辖市会在province和city显示） city:市（直辖市会在province和city显示）  district:区县   street:街道
        /// </summary>
        public string level { get; set; }
        /// <summary>
        /// 下级行政区
        /// </summary>
        public List<DistrictInfo> districts { get; set; }
    }
    /// <summary>
    /// 建议结果列表
    /// </summary>
    public class SuggestionInfo
    {
        /// <summary>
        /// 查询关键字   建议关键字列表
        /// </summary>
        public List<string> keyworkds { get; set; }
        /// <summary>
        /// 查询城市    建议城市列表
        /// </summary>
        public List<string> cities { get; set; }


    }



    public class RegionSyncStatus
    {
        /// <summary>
        /// 总数
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// 已同步数量
        /// </summary>
        public int SynchronizedCount { get; set; }
        /// <summary>
        /// 完成率
        /// </summary>
        public decimal CompletionRate { get { return TotalCount > 0 ? (Decimal.Parse((SynchronizedCount * 1.0M / TotalCount * 1.0M).ToString("f2")) * 100M) : 100.00M; } }
        /// <summary>
        /// 是否正在同步中
        /// </summary>
        public bool IsSynchroning { get { return TotalCount > 0 && SynchronizedCount != TotalCount; } }

    }
}
