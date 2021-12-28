using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class DistributorGradeInfo
    {
        /// <summary>
        /// 销售员人数(后补)
        /// </summary>
        [ResultColumn]
        public long MemberCount { get; set; }
    }
}
