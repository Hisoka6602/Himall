﻿using PetaPoco;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class BusinessCategoryApplyDetailInfo
    {
        /// <summary>
        /// 类目路径
        /// </summary>
        [ResultColumn]
        public string CatePath { set; get; }   
    }
}
