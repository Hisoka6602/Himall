using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    public class LiveAjaxModel
    {
        public int status { get; set; }
        public List<LivesContent> list { get; set; }
        public string page { get; set; }
    }

    public class LivesContent { 
        /// <summary>
        /// 主播图片
        /// </summary>
        public string AnchorImg { get; set; }

        /// <summary>
        /// 主播名称
        /// </summary>
        public string AnchorName { get; set; }

        /// <summary>
        /// 封面图
        /// </summary>
        public string CoverImg { get; set; }

        public string Link { get; set; }

        /// <summary>
        /// 直播间名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 直播间商品数量
        /// </summary>
        public long ProductCount { get; set; }


        /// <summary>
        /// 商品集合
        /// </summary>
        public List<LiveProductContent> ProductList { get; set; }


        /// <summary>
        /// 直播房号
        /// </summary>
        public long RoomId { set; get; }

        /// <summary>
        /// 直播开始时间
        /// </summary>
        public DateTime StartTime { set; get; }



        public string StartTimeDesc { set; get; }

        public string StartTimeStr { set; get; }

        /// <summary>
        /// 直播间状态
        /// </summary>
        public string StatusDesc { set; get; }
        public LiveRoomStatus Status { get; set; }


    }

    public class LiveProductContent {
        public string Image { set; get; }

        public string Name { set; get; }

        public decimal Price { set; get; }

        public long ProductId { set; get; }

        public long RoomId { set; get; }

        public decimal SaleAmount { set; get; }

        public long SaleCount { set; get; }

        public long SaleMember { set; get; }

        public string Url { set; get; }

    }
}