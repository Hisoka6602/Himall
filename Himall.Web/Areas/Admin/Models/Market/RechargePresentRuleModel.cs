using Himall.Core;
using Himall.DTO;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Himall.Web.Areas.Admin.Models
{
    public class RechargePresentRuleModel
    {
        public bool IsEnable { get; set; }
        public List<RechargePresentRule> Rules { get; set; }

        public string RulesJson { get; set; }

        /// <summary>
        /// 验证有效性
        /// </summary>
        public void CheckValidation()
        {
            if (IsEnable)
            {
                foreach(var item in Rules)
                {
                    if (Rules.Where(d=>d.ChargeAmount== item.ChargeAmount).Count() > 1)
                    {
                        throw new HimallException("有重复的充值赠送规则");
                    }
                }
            }
        }
    }
}