using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    /// <summary>
    /// 评论统计
    /// </summary>
    public class CommentSummaryData
    {
        /// <summary>
        /// 全部
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 平均分
        /// </summary>
        public decimal Average { get; set; }
        /// <summary>
        /// 好评
        /// </summary>
        public int Positive { get; set; }
        /// <summary>
        /// 中评
        /// </summary>
        public int Neutral { get; set; }
        /// <summary>
        /// 负面
        /// </summary>
        public int Negative { get; set; }
    }
}
