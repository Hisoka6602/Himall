using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Himall.DTO
{
    /// <summary>
    /// 旺店通原始订单类(查询订单管理)
    /// </summary>
    public class WanDianTradeInfo
    {
        public string trade_id { get; set; }
        public string trade_no { get; set; }
        public int platform_id { get; set; }
        public string shop_no { get; set; }
        public string shop_name { get; set; }
        public string shop_remark { get; set; }
        public int warehouse_type { get; set; }
        public string warehouse_no { get; set; }
        public string src_tids { get; set; }
        public int trade_status { get; set; }
        public int consign_status { get; set; }
        public int trade_type { get; set; }
        public int delivery_term { get; set; }
        public int freeze_reason { get; set; }
        public int refund_status { get; set; }
        public int fenxiao_type { get; set; }
        public string fenxiao_nick { get; set; }
        public DateTime trade_time { get; set; }
        public DateTime pay_time { get; set; }
        public string customer_name { get; set; }
        public string customer_no { get; set; }
        public string pay_account { get; set; }
        public string buyer_nick { get; set; }
        public string receiver_name { get; set; }
        public int receiver_province { get; set; }
        public int receiver_city { get; set; }
        public int receiver_district { get; set; }
        public string receiver_address { get; set; }
        public string receiver_mobile { get; set; }
        public string receiver_telno { get; set; }
        public string receiver_zip { get; set; }
        public string receiver_area { get; set; }
        public string receiver_ring { get; set; }
        public string receiver_dtb { get; set; }
        public string to_deliver_time { get; set; }
        public int bad_reason { get; set; }
        public int logistics_id { get; set; }
        public string logistics_name { get; set; }
        public string logistics_code { get; set; }
        public int logistics_type { get; set; }
        public string logistics_no { get; set; }
        public string buyer_message { get; set; }
        public string cs_remark { get; set; }
        public int remark_flag { get; set; }
        public string print_remark { get; set; }
        public int goods_type_count { get; set; }
        public decimal goods_count { get; set; }
        public decimal goods_amount { get; set; }
        public decimal post_amount { get; set; }
        public decimal other_amount { get; set; }
        public decimal discount { get; set; }
        public decimal receivable { get; set; }
        public decimal dap_amount { get; set; }
        public decimal cod_amount { get; set; }
        public decimal ext_cod_fee { get; set; }
        public decimal goods_cost { get; set; }
        public decimal post_cost { get; set; }
        public decimal paid { get; set; }
        public decimal weight { get; set; }
        public decimal profit { get; set; }
        public decimal tax { get; set; }
        public decimal tax_rate { get; set; }
        public decimal commission { get; set; }
        public int invoice_type { get; set; }
        public string invoice_title { get; set; }
        public string invoice_content { get; set; }
        public int salesman_id { get; set; }
        public int checker_id { get; set; }
        public string fullname { get; set; }
        public string checker_name { get; set; }
        public int fchecker_id { get; set; }
        public int checkouter_id { get; set; }
        public string stockout_no { get; set; }
        public string flag_name { get; set; }
        public string trade_from { get; set; }
        public string single_spec_no { get; set; }
        public decimal raw_goods_count { get; set; }
        public int raw_goods_type_count { get; set; }
        public string currency { get; set; }
        public int split_package_num { get; set; }
        public int invoice_id { get; set; }
        public int version_id { get; set; }
        public DateTime modified { get; set; }
        public DateTime created { get; set; }
        public int id_card_type { get; set; }
        public string id_card { get; set; }
        public List<WanDianGoodsInfo> goods_list { get; set; }
    }

    public class WanDianGoodsInfo
    {
        public int rec_id { get; set; }
        public int trade_id { get; set; }
        public int spec_id { get; set; }
        public int platform_id { get; set; }
        public string src_oid { get; set; }
        public string platform_goods_id { get; set; }
        public string platform_spec_id { get; set; }
        public int suite_id { get; set; }
        public int flag { get; set; }
        public string src_tid { get; set; }
        public int gift_type { get; set; }
        public int refund_status { get; set; }
        public int guarantee_mode { get; set; }
        public int delivery_term { get; set; }
        public string bind_oid { get; set; }
        public decimal num { get; set; }
        public decimal price { get; set; }
        public decimal actual_num { get; set; }
        public decimal refund_num { get; set; }
        public decimal order_price { get; set; }
        public decimal share_price { get; set; }
        public decimal adjust { get; set; }
        public decimal discount { get; set; }
        public decimal share_amount { get; set; }
        public decimal share_post { get; set; }
        public decimal paid { get; set; }
        public string goods_name { get; set; }
        public string prop2 { get; set; }
        public int goods_id { get; set; }
        public string goods_no { get; set; }
        public string spec_name { get; set; }
        public string spec_no { get; set; }
        public string spec_code { get; set; }
        public string suite_no { get; set; }
        public string suite_name { get; set; }
        public decimal suite_num { get; set; }
        public decimal suite_amount { get; set; }
        public decimal suite_discount { get; set; }
        public string api_goods_name { get; set; }
        public string api_spec_name { get; set; }
        public decimal weight { get; set; }
        public decimal commission { get; set; }
        public int goods_type { get; set; }
        public int large_type { get; set; }
        public int invoice_type { get; set; }
        public string invoice_content { get; set; }
        public int from_mask { get; set; }
        public int cid { get; set; }
        public string remark { get; set; }
        public DateTime modified { get; set; }
        public DateTime created { get; set; }
        public decimal tax_rate { get; set; }
        public int base_unit_id { get; set; }
        public string unit_name { get; set; }
        public string pay_id { get; set; }
        public int pay_status { get; set; }
        public DateTime pay_time { get; set; }
    }

    public class WdtGoodsArchives
    {

        /// <summary>
        /// 货品ID
        /// </summary>
        public long goods_id { get; set; }


        /// <summary>
        /// 货品编号
        /// </summary>
        public string goods_no { get; set; }

        /// <summary>
        /// 货品类别
        /// 货品类别 1销售商品 2原材料 3包装 4周转材料5虚拟商品6固定资产 0其它
        /// </summary>
        public int goods_type { get; set; }


        /// <summary>
        /// 货品名称
        /// </summary>
        public string goods_name { get; set; }


        /// <summary>
        /// 市场价
        /// </summary>
        public decimal market_price { get; set; }

        /// <summary>
        /// 货品规格集合
        /// </summary>
        public List<WdtGoods> spec_list { get; set; }

    }



    /*接口地址 https://open.wangdian.cn/open/apidoc/doc?path=api_goodsspec_push.php */
    public class WdtGoods
    {
        /// <summary>
        /// 货品ID
        /// </summary>
        public string goods_id { get; set; }


        public string spec_id { get; set; }
        public string goods_no { get; set; }

        /// <summary>
        /// 商家编码
        /// </summary>
        public string spec_no { get; set; }
        public int status { get; set; }
        public string goods_name { get; set; }

        /// <summary>
        /// 规格码
        /// </summary>
        public string spec_code { get; set; }
        public string spec_name { get; set; }
        public string pic_url { get; set; }
        public decimal price { get; set; }
        public decimal stock_num { get; set; }
        public string cid { get; set; }
    }

    public class QueryWDTOrderResponse
    {
        public int code { get; set; }
        public string message { get; set; }
   
        public List<LogisticsSyncTrades> trades { get; set; }
    }
    public class baseResponse
    {
        public int code { get; set; }
        public string message { get; set; }
    }

    public class errorPush
    {
        public string tid { get; set; }
        public string error { get; set; }
    }

    public class queryWDT
    {
        public int? status { get; set; }
        public DateTime start_time { get; set; }
        public DateTime end_time { get; set; }
        public int? page_size { get; set; }
        public int? page_no { get; set; }
        public string src_tid { get; set; }
        public int? img_url { get; set; }
        public string trade_no { get; set; }
        public string shop_no { get; set; }
        public string warehouse_no { get; set; }
        public int? goodstax { get; set; }
        public int? has_logistics_no { get; set; }
        public int? src { get; set; }
        public string logistics_no { get; set; }

        public int? platform_id { get; set; }

        public string src_oid { get; set; }

    }
    

    
    public enum pushState
    {
        Fail = -1,  //推送失败
        Success = 0,  //推送成功
        PartSuccess = 1,  //部分推送成功
        Unnecessary = 2,  //不需要推送
        Error = 3  //推送异常
    }
}
