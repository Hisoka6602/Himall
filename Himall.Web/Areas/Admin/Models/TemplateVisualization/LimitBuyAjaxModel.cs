using System.Collections.Generic;

namespace Himall.Web.Areas.Admin.Models
{
    public class LimitBuyAjaxModel
    {
        public int status { get; set; }
        public List<LimitBuyContent> list { get; set; }
        public string page { get; set; }
    }


    public class LimitBuyContent
    {
        public long item_id { get; set; }
        public string title { get; set; }
        public string create_time { get; set; }
        public string link { get; set; }
        public string pc_link { get; set; }
        public string pic { get; set; }
        public string shopName { get; set; }
        public string price { get; set; }
        public string startTime { get; set; }
        public string endTime { get; set; }

        public long beginSec { get; set; }

        public long endSec { get; set; }

        /// <summary>
        /// 售出数量
        /// </summary>
        public int number { set; get; }

        public string saleprice { set; get; }

        /// <summary>
        /// 商品卖点
        /// </summary>
        public string sellingPoint { set; get; }

        /// <summary>
        /// 剩余活动库存
        /// </summary>
        public int stock { get; set; }

        /// <summary>
        /// 商品编号
        /// </summary>
        public long pid { get; set; }
    }
}