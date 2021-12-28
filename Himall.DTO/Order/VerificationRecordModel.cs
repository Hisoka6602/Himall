using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class VerificationRecordModel: VerificationRecordInfo
    {
        public long Quantity { get; set; }
        public string ImagePath { get; set; }
        public string Specifications { get; set; }

        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string Time { get; set; }


        public string PayDateText { get; set; }
        public string StatusText { get; set; }
        public string Name { get; set; }
        public string VerificationTimeText { get; set; }
    }
}
