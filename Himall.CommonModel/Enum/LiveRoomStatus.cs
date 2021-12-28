using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel
{

    public enum LiveRoomStatus
    {
        [Description("未提交审核")]
        NoSubmit = 0,
        [Description("审核中")]
        Audting = 1,
        /// <summary>
        /// 直播中
        /// </summary>
        [Description("直播中")]
        Living = 101,
        /// <summary>
        /// 未开始
        /// </summary>
        [Description("未开始")]
        NotStart = 102,
        /// <summary>
        /// 已结束
        /// </summary>
        [Description("已结束")]
        End = 103,
        /// <summary>
        /// 禁播
        /// </summary>
        [Description("禁播")]
        forbid = 104,
        /// <summary>
        /// 暂停中
        /// </summary>
        [Description("暂停中")]
        Pause = 105,
        /// <summary>
        /// 异常
        /// </summary>
        [Description("异常")]
        Exception = 106,
        /// <summary>
        /// 已过期
        /// </summary>
        [Description("已过期")]
        Expire = 107
    }
}
