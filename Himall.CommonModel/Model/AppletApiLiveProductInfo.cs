using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel.Model
{
    /// <summary>
    /// 直播商品列表，用于接口返回数据转换
    /// </summary>
    public class AppletApiLiveProductList
    {
        /// <summary>
        /// 错误编号
        /// </summary>
        public int errcode { get; set; }
        /// <summary>
        /// 数量
        /// </summary>
        public int total { get; set; }
        /// <summary>
        /// 商品列表
        /// </summary>
        public List<ListGoodsInfo> goods { get; set; }
    }
    /// <summary>
    /// 小程序商品信息，用于接口
    /// </summary>
    public class AppletApiProductInfo
    {
        public GoodsInfo goodsInfo { get; set; }
    }

    /// <summary>
    /// 小程序更新商品信息，用于接口
    /// </summary>
    public class AppletApiUpdateProductInfo
    {
        public UpdateGoodsInfo goodsInfo { get; set; }
    }
    /// <summary>
    /// 更新商品信息实体
    /// </summary>
    public class ListGoodsInfo : UpdateGoodsInfo
    {
        /// <summary>
        /// 1, 2：表示是为api添加商品，否则是在MP添加商品
        /// </summary>
        public int thirdPartyTag { get; set; }
    }
    /// <summary>
    /// 更新商品信息实体
    /// </summary>
    public class UpdateGoodsInfo : GoodsInfo
    {
        public long goodsId { get; set; }
    }
    public class GoodsInfo
    {
        /// <summary>
        /// 商品图片MediaId
        /// </summary>
        public string coverImgUrl { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 价格内类 1  一口价  2  区间价  3  折扣价
        /// </summary>
        public int priceType { get; set; }
        /// <summary>
        /// 一口价/区间价起始价/折扣价原价 最多保留两位数
        /// </summary>
        public decimal price { get; set; }
        /// <summary>
        /// 区间价结束价/折扣价优惠价 最多保留两位数
        /// </summary>
        public decimal price2 { get; set; }
        /// <summary>
        /// 小程序商品链接
        /// </summary>
        public string url { get; set; }
    }
}
