﻿using System.ComponentModel;

namespace Himall.Entities
{
    public partial class FlashSaleInfo
    {
        /// <summary>
        /// 限时购状态
        /// </summary>
        public enum FlashSaleStatus
        {
            /// <summary>
            /// 等待审核
            /// </summary>
            [Description("待审核")]
            WaitForAuditing = 1,

            /// <summary>
            /// 进行中
            /// </summary>
            [Description("进行中")]
            Ongoing = 2,

            /// <summary>
            /// 未通过(审核失败)
            /// </summary>
            [Description("未通过")]
            AuditFailed = 3,

            /// <summary>
            /// 已结束
            /// </summary>
            [Description("已结束")]
            Ended = 4,

            /// <summary>
            /// 已取消
            /// </summary>
            [Description("已取消")]
            Cancelled = 5,

            [Description("未开始")]
            NotBegin = 6,
        }
    }
}
