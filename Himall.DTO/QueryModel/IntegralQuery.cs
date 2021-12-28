﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.QueryModel
{
    public class IntegralQuery : QueryBase
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string UserName { get; set; }

    }

    public class IntegralRecordQuery : QueryBase
    {
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string UserName { get; set; }

        public long? UserId { set; get; }

        public Himall.Entities.MemberIntegralInfo.IntegralType? IntegralType { set; get; }

    }

}
