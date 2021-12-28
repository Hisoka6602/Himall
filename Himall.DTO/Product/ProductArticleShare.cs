using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class ProductArticleShare
    {
        /// <summary>
        /// 微信文章的Id（视频号推广使用）
        /// </summary>
        public string ArticleId { get; set; }

        /// <summary>
        /// 微信文章的Url（视频号推广使用）
        /// </summary>
        public string ArticleUrl { get; set; }

        /// <summary>
        /// 图片Url
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// 视频Url（视频号推广使用）
        /// </summary>
        public string VideoUrl { get; set; }

        /// <summary>
        /// 群发成功失败
        /// </summary>
        public bool SendSuccess { get; set; }

        /// <summary>
        /// 群发错误
        /// </summary>
        public string ErrorMsg { get; set; }
    }
}
