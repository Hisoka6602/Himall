using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Himall.DTO
{
    /// <summary>
    /// 查询库存同步-公共响应参数
    /// </summary>
    public class StockSyncQueryInfo
    {
        /// <summary>
        /// 状态码:0表示成功,其他表示失败
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// 错误原因
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 库存信息列表
        /// </summary>
        public List<StockChangeList> stock_change_list { get; set; }

        /// <summary>
        /// 获得当前同步记录的条数
        /// </summary>
        public int current_count { get; set; }

    }

    /// <summary>
    /// 查询库存同步-业务响应参数
    /// </summary>
    public class StockChangeList
    {
        /// <summary>
        /// Erp内平台货品表主键id,用于状态回传接口使用
        /// </summary>
        public int rec_id { get; set; }

        /// <summary>
        /// 店铺id
        /// </summary>
        public int shop_id { get; set; }

        /// <summary>
        /// 货品ID
        /// </summary>
        public string goods_id { get; set; }

        /// <summary>
        /// 规格ID
        /// </summary>
        public string spec_id { get; set; }

        /// <summary>
        /// Erp内库存
        /// </summary>
        public int sync_stock { get; set; }

        /// <summary>
        /// 货品编码,代表货品(spu)所有属性的唯一编号
        /// </summary>
        public string goods_no { get; set; }

        /// <summary>
        /// 规格编码,代表单品(sku)所有属性的唯一编码
        /// </summary>
        public string spec_no { get; set; }

        /// <summary>
        /// erp中货品类型,1：单品，2：组合装
        /// </summary>
        public int erp_spec_type { get; set; }

        /// <summary>
        /// erp中规格ID
        /// </summary>
        public int erp_spec_id { get; set; }

        /// <summary>
        /// 库存变化时自增
        /// </summary>
        public int stock_change_count { get; set; }
    }

    /// <summary>
    /// 库存同步回写-公共响应参数
    /// </summary>
    public class StockSyncAckInfo
    {
        /// <summary>
        /// 状态码:0表示成功,其他表示失败
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 库存，使用接口传回的参数即可
        /// </summary>
        public int sync_stock { get; set; }
        /// <summary>
        /// 错误原因
        /// </summary>
        public string message { get; set; }

    }

    /// <summary>
    /// 商品库存变化响应实体
    /// </summary>
    public class GoodsStockChangeResponse : baseResponse
    {
        /// <summary>
        /// 需要同步库存信息的数据节点
        /// </summary>
        public List<StockChangeList> stock_change_list { get; set; }
        /// <summary>
        /// 记录当前获得的记录条数
        /// </summary>
        public int current_count { get; set; }
    }
}
