using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class MemberData
    {
        public long Id { get; set; }
        /// <summary>
        /// 折扣
        /// </summary>
        public decimal MemberDiscount { get; set; }
        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }

        public string Nick { get; set;}
        public string CellPhone { get; set; }

        /// <summary>
        /// 用户冻结
        /// </summary>
        public bool Disabled { get; set; }
    }
}
