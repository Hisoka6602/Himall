using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class  LiveProductInfo
    {   /// <summary>
        /// 价格类型
        /// </summary>
        public enum PriceTypes
        {
            /// <summary>
            /// 一口价
            /// </summary>
            [Description("一口价")]
            Price = 1,
            /// <summary>
            /// 区间价
            /// </summary>
            [Description("区间价")]
            RangPrice = 2,
            /// <summary>
            /// 折扣价
            /// </summary>
            [Description("折扣价")]
            DiscountPrice = 3,
        }
    }
}
