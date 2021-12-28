using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 微信群发消息实体（微信需要格式）
    /// </summary>
    public class WXSendMessage
    {
        /// <summary>
        /// OPENID数组
        /// </summary>
        public string[] Touser { get; set; }

        /// <summary>
        /// 模板
        /// </summary>
        public WXSendMessageMpnews Mpnews { get; set; }

        /// <summary>
        /// 用于设定即将发送的图文消息
        /// </summary>
        public string Msgtype { get; set; } = "mpnews";

        /// <summary>
        /// 图文消息被判定为转载时，是否继续群发。 1为继续群发（转载），0为停止群发。 该参数默认为0。
        /// </summary>
        public int Send_ignore_reprint { get; set; } = 0;

        /// <summary>
        /// 视频缩略图的媒体ID
        /// </summary>
        public string Thumb_media_id { get; set; } = "";
    }

    /// <summary>
    /// 模板ID
    /// </summary>
    public class WXSendMessageMpnews
    {
        /// <summary>
        /// 模板ID
        /// </summary>
        public string Media_id { get; set; }
    }

}
