using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class ShopBonusData
    {
        public long Id { get; set; }
        public long ShopId { get; set; }
        public string ShopName { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public DateTime BonusDateStart { get; set; }
        public DateTime BonusDateEnd { get; set; }
        public string ShareTitle { get; set; }
        public string ShareDetail { get; set; }
        public string ShareImg { get; set; }
        public string CardTitle { get; set; }
        public string CardColor { get; set; }
        public string CardSubtitle { get; set; }
        public decimal RandomAmountStart { get; set; }
        public decimal RandomAmountEnd { get; set; }
        public decimal GrantPrice { get; set; }
        public int Count { get; set; }
    }
}
