﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class WXCardCodeLogInfo
    {
        /// <summary>
        /// 审核状态
        /// </summary>
        public enum CodeStatusEnum
        {
            /// <summary>
            /// 正常
            /// </summary>
            Normal = 1,
            /// <summary>
            /// 待领取
            /// </summary>
            WaitReceive = 0,
            /// <summary>
            /// 已失效
            /// </summary>
            HasFailed = -1,
            /// <summary>
            /// 已消费
            /// </summary>
            HasConsume = 2,
            /// <summary>
            /// 已删除
            /// </summary>
            HasDelete = 3,
        }
    }
}
