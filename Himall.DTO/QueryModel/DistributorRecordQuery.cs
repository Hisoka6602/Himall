﻿using Himall.DTO.QueryModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class DistributorRecordQuery:QueryBase
    {
        public long MemberId { get; set; }
    }
}
