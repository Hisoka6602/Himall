﻿namespace Himall.Web.Areas.Mobile.Models
{
    public class FightGroupOrderOkModel
    {
        /// <summary>
        /// 是否团首订单
        /// </summary>
        public bool isFirst { get; set; }
        /// <summary>
        /// 已参团人数
        /// </summary>
        public int JoinNumber { get; set; }
        /// <summary>
        /// 参团人数限制
        ///</summary>
        public int LimitedNumber { get; set; }
        /// <summary>
        /// 分享图片
        /// </summary>
        public string ShareImage { get; set; }
        /// <summary>
        /// 分享标题
        /// </summary>
        public string ShareTitle { get; set; }
        /// <summary>
        /// 分享链接
        /// </summary>
        public string ShareUrl { get; set; }
        /// <summary>
        /// 分享描述
        /// </summary>
        public string ShareDesc { get; set; }

        public Himall.CommonModel.FightGroupBuildStatus FightGroupStatus { get; set; }
    }
}