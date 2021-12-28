using PetaPoco;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Himall.Entities.LiveProductInfo;

namespace Himall.Entities
{
   public partial class LiveProductLibraryInfo
    {
        
        /// <summary>
        /// 直播商品审核状态
        /// </summary>
        public enum LiveProductAuditStatus
        {

            [Description("未提交")]
            NoSubmit = -1,
            /// <summary>
            /// 未审核
            /// </summary>
            [Description("待审核")]
            NoAudit = 0,
            /// <summary>
            /// 审核中
            /// </summary>
            [Description("审核中")]
            Auditing = 1,
            /// <summary>
            /// 审核通过
            /// </summary>
            [Description("审核通过")]
            Audited = 2,
            /// <summary>
            /// 审核驳回
            /// </summary>
            [Description("审核驳回")]
            AuditFailed = 3,
        }

    }
}
