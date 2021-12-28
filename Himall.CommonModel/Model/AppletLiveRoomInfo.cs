using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel.Model
{

    /// <summary>
    /// 小程序直播间实体
    /// </summary>
    public class AppletLiveRoomInfo
    {
        /// <summary>
        /// 直播间名字，最短3个汉字，最长17个汉字，1个汉字相当于2个字符
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 背景图，填入mediaID（mediaID获取后，三天内有效）；图片mediaID的获取，请参考以下文档： https://developers.weixin.qq.com/doc/offiaccount/Asset_Management/New_temporary_materials.html；直播间背景图，图片规则：建议像素1080*1920，大小不超过2M
        /// </summary>
        public string coverImg { get; set; }
        /// <summary>
        /// 直播计划开始时间（开播时间需要在当前时间的10分钟后 并且 开始时间不能在 6 个月后）
        /// </summary>
        public long startTime { get; set; }
        /// <summary>
        /// 直播计划结束时间（开播时间和结束时间间隔不得短于30分钟，不得超过24小时）
        /// </summary>
        public long endTime { get; set; }
        /// <summary>
        /// 主播昵称，最短2个汉字，最长15个汉字，1个汉字相当于2个字符
        /// </summary>
        public string anchorName { get; set; }
        /// <summary>
        /// 主播微信号，如果未实名认证，需要先前往“小程序直播”小程序进行实名验证
        /// </summary>
        public string anchorWechat { get; set; }
        /// <summary>
        /// 	分享图，填入mediaID（mediaID获取后，三天内有效）；图片mediaID的获取，请参考以下文档： https://developers.weixin.qq.com/doc/offiaccount/Asset_Management/New_temporary_materials.html；直播间分享图，图片规则：建议像素800*640，大小不超过1M；
        /// </summary>
        public string shareImg { get; set; }
        /// <summary>
        /// 	购物直播频道封面图，填入mediaID（mediaID获取后，三天内有效）；图片mediaID的获取，请参考以下文档： https://developers.weixin.qq.com/doc/offiaccount/Asset_Management/New_temporary_materials.html; 购物直播频道封面图，图片规则：建议像素800*800，大小不超过100KB；
        /// </summary>
        public string feedsImg { get; set; }
        /// <summary>
        /// 直播间类型 【1: 推流，0：手机直播】
        /// </summary>
        public int type { get; set; }
        /// <summary>
        /// 横屏、竖屏 【1：横屏，0：竖屏】（横屏：视频宽高比为16:9、4:3、1.85:1 ；竖屏：视频宽高比为9:16、2:3）
        /// </summary>
        public int screenType { get; set; }
        /// <summary>
        /// 是否关闭点赞 【0：开启，1：关闭】（若关闭，直播开始后不允许开启）
        /// </summary>
        public int closeLike { get; set; }
        /// <summary>
        /// 是否关闭货架 【0：开启，1：关闭】（若关闭，直播开始后不允许开启）
        /// </summary>
        public int closeGoods { get; set; }
        /// <summary>
        /// 是否关闭评论 【0：开启，1：关闭】（若关闭，直播开始后不允许开启）
        /// </summary>
        public int closeComment { get; set; }
    }
}
