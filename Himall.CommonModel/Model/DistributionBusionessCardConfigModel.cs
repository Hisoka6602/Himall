using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Himall.CommonModel
{
    public class DistributionBusionessCardConfigModel
    {
        public List<BussionCardPosition> PosList { get; set; }

        public string DefaultHead { get; set; }

        public string DefaultQRCode { get; set; }

        public string MyUserName { get; set; }

        public string ShopName { get; set; }
        
        public string BgImg { get; set; }

        public int MyUserNameSize { get; set; }

        public int ShopNameSize { get; set; }

        public string MyUserNameColor { get; set; }

        public string ShopNameColor { get; set; }

        public string NickNameColor { get; set; }

        public string StoreNameColor { get; set; }

        public string WriteDate { get; set; }
    }

    public class BussionCardPosition {
        public decimal Left { get; set; }

        public decimal Top { get; set; }

        public decimal Width { get; set; }
    }
}