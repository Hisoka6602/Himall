using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.QueryModel
{
    public class AnchorQuery : QueryBase
    {

        public long ShopId { get; set; }
        /// <summary>
        /// 主播名称
        /// </summary>
        public string AnchorName { get; set; }
        /// <summary>
        /// 手机号码
        /// </summary>
        public string Cellphone { get; set; }
        /// <summary>
        /// 微信号
        /// </summary>
        public string WeChat { get; set; }
    }
}
