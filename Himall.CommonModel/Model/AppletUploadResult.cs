using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel.Model
{

    public class AppletUploadResult
    {
        /// <summary>
        /// 媒体文件类型，分别有图片（image）、语音（voice）、视频（video）和缩略图（thumb）
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 媒体文件上传后，获取标识(可复用）媒体文件在微信后台保存时间为3天，即3天后media_id失效。
        /// </summary>
        public string media_id { get; set; }
        /// <summary>
        /// 媒体文件上传时间戳
        /// </summary>
        public long created_at { get; set; }
    }
    /// <summary>
    /// 媒体文件类型
    /// </summary>
    public enum MediaType
    {
        /// <summary>
        /// 图片
        /// </summary>
        image = 1,
        /// <summary>
        /// 语音
        /// </summary>
        voice = 2,
        /// <summary>
        /// 视频
        /// </summary>
        video = 3,
        /// <summary>
        /// 缩略图
        /// </summary>
        thumb = 4,
    }
}
