using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Himall.Web.Areas.Admin.Models
{
    public class AdvanceModel
    {
        /// <summary>
        /// 是否开启首页弹窗
        /// </summary>
        public bool IsEnable { get; set; }

        /// <summary>
        /// 链接
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// 广告图片
        /// </summary>
        public string Img { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }


        /// <summary>
        /// 是否重复播放
        /// </summary>
        public bool IsReplay { get; set; }
    }
}