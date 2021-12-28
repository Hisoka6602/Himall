using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    public class Invoices
    {
        public InvoiceType InvoiceType { get; set; }
        public string InvoiceTitle { get; set; }
        public string InvoiceCode { get; set; }
        public string InvoiceContext { get; set; }
        public string RegisterAddress { get; set; }
        public string RegisterPhone { get; set; }
        public string BankName { get; set; }
        public string BankNo { get; set; }
        public string RealName { get; set; }
        public string CellPhone { get; set; }
        public string Email { get; set; }
        public string RegionID { get; set; }
        public string Address { get; set; }
    }
}
