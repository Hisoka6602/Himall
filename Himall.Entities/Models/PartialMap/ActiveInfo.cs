﻿using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class ActiveInfo
    {
        /// <summary>
        /// 满减规则(需自行填充)
        /// </summary>
        /// 

        [ResultColumn]
        public List<FullDiscountRuleInfo> Rules { set; get; }

        /// <summary>
        /// 满减商品(需自行填充)
        /// </summary>
        [ResultColumn]
        public List<ActiveProductInfo> Products { set; get; }
    }
}
