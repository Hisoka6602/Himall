using Himall.CommonModel;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.QueryModel
{
    public class ApplyWithDrawQuery : QueryBase
    {
        public ApplyWithDrawInfo.ApplyWithDrawStatus? withDrawStatus { get; set; }

        public long? MemberId { get; set; }

        public long? WithDrawNo { get; set; }
        public UserWithdrawType? ApplyType { get; set; }
    }
}
