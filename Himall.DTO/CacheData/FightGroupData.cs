using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class FightGroupData
    {
        public long Id { get; set; }
        public long ShopId { get; set; }
        public long ProductId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<FightGroupItemData> Items { get; set; }
        public int LimitQuantity { get; set; }
        public int LimitedNumber { get; set; }
        public decimal LimitedHour { get; set; }
        public FightGroupActiveStatus ActiveStatus
        {
            get
            {
                if (EndTime < DateTime.Now)
                    return FightGroupActiveStatus.Ending;
                else if (StartTime > DateTime.Now)
                    return FightGroupActiveStatus.WillStart;
                else
                    return FightGroupActiveStatus.Ongoing;
            }
        }
    }

    public class FightGroupItemData
    {
        public long ProductId{get; set; }
        public string SkuId { get; set; }
        public decimal ActivePrice { get; set; }
    }
}
