﻿using System;

namespace Himall.DTO.QueryModel
{
    public partial class Quotation : QueryBase
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string Title { get; set; }

        public decimal MaxPrice { get; set; }
        public decimal MinPrice { get; set; }

        public int State { get; set; }
    }
}
