using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 会员资产
    /// </summary>
    public class MemberAssets
    {
        public string PayPassword { get; set; }

        public string PayPasswordSalt { get; set; }

        public decimal Balance { get; set; }

        public int Integral { get; set; }

    }
}
