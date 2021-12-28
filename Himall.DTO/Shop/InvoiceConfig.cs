using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO
{
    /// <summary>
    /// 发票配置
    /// </summary>
    public class InvoiceConfig
    {
        /// <summary>
        /// 是否可开发票
        /// </summary>
        public bool IsInvoice { get; set; }
        /// <summary>
        /// 普通发票
        /// </summary>
        public bool IsPlainInvoice { get; set; }
        /// <summary>
        /// 电子发票
        /// </summary>
        public bool IsElectronicInvoice { get; set; }
        /// <summary>
        /// 发票
        /// </summary>
        public decimal PlainInvoiceRate { get; set; }
        /// <summary>
        /// 是否可开增值税
        /// </summary>
        public bool IsVatInvoice { get; set; }
        /// <summary>
        /// 开票时间 
        /// </summary>
        public int VatInvoiceDay { get; set; }
        /// <summary>
        /// 增值税发票
        /// </summary>
        public decimal VatInvoiceRate { get; set; }

    }
}
