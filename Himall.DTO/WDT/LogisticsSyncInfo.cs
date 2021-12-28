using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Himall.DTO
{
    /// <summary>
    /// 查询物流同步-公共响应参数
    /// </summary>
    public class LogisticsSyncQueryInfo
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
        /// 待同步物流订单信息列表
        /// </summary>
        public List<LogisticsSyncTrades> trades { get; set; }

    }
    /// <summary>
    /// 发货信息同步响应结果
    /// </summary>
    public class LogisticsSyncResponse : baseResponse
    {
        /// <summary>
        /// 需要同步库存信息的数据节点
        /// </summary>
        public List<LogisticsSyncTrades> trades { get; set; }
        /// <summary>
        /// 记录当前获得的记录条数
        /// </summary>
        public int current_count { get; set; }
    }
    /// <summary>
    /// 查询物流同步-业务响应参数
    /// </summary>
    public class LogisticsSyncTrades
    {
        /// <summary>
        /// 主键,用于logistics_sync_ack回写状态
        /// </summary>
        public int rec_id { get; set; }

        /// <summary>
        /// 代表店铺所有属性的唯一编码，用于店铺区分，ERP内支持自定义（ERP店铺界面设置）
        /// </summary>
        public string shop_no { get; set; }

        /// <summary>
        /// 原始订单编号，商城或平台订单号
        /// </summary>
        public string tid { get; set; }

        /// <summary>
        /// 物流或者快递面单对应的编号
        /// </summary>
        public string logistics_no { get; set; }

        /// <summary>
        /// 物流方式
        /// </summary>
        public int logistics_type { get; set; }

        /// <summary>
        /// 发货条件 1款到发货 2货到付款(包含部分货到付款) 3分期付款
        /// </summary>
        public int delivery_term { get; set; }

        /// <summary>
        /// 发货时间
        /// </summary>
        public DateTime consign_time { get; set; }

        /// <summary>
        /// 是否拆分发货,1:拆单发货,0:不进行拆单发货
        /// </summary>
        public int is_part_sync { get; set; }

        /// <summary>
        /// 原始子订单
        /// </summary>
        public string oids { get; set; }

        /// <summary>
        /// 平台ID
        /// </summary>
        public int platform_id { get; set; }

        /// <summary>
        /// 订单ID
        /// </summary>
        public int trade_id { get; set; }

        /// <summary>
        /// erp物流编号
        /// </summary>
        public string logistics_code_erp { get; set; }

        /// <summary>
        /// erp物流公司名称
        /// </summary>
        public string logistics_name_erp { get; set; }

        /// <summary>
        /// 物流公司名称
        /// </summary>
        public string logistics_name { get; set; }
    }

    /// <summary>
    /// 物流同步回写-公共响应参数
    /// </summary>
    public class LogisticsSyncAckInfo
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
        /// 处理失败的错误列表,
        /// 当code为0且有错误信息时才非空.
        /// 只列出同一批内有错误的记录.如果code=0,
        /// errors为空说明全部成功
        /// </summary>
        public List<LogisticsSyncErrors> errors { get; set; }
    }

    /// <summary>
    /// 物流同步回写-业务响应参数
    /// </summary>
    public class LogisticsSyncErrors
    {
        /// <summary>
        /// 回写的记录id
        /// </summary>
        public int rec_id { get; set; }

        /// <summary>
        /// 回写状态: 0成功 1失败
        /// </summary>
        public int status { get; set; } = 1;

        /// <summary>
        /// 错误信息的描述
        /// </summary>
        public string error { get; set; }
    }

    /// <summary>
    /// 物流同步回写-业务响应参数
    /// </summary>
    public class LogisticsListInfo
    {
        /// <summary>
        /// 回写的记录id
        /// </summary>
        public int rec_id { get; set; }

        /// <summary>
        /// 回写状态: 0成功 1失败
        /// </summary>
        public int status { get; set; } = 1;

        /// <summary>
        /// 错误信息的描述
        /// </summary>
        public string message { get; set; }
    }
}
