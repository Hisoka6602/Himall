using Himall.Core.Extends;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Live
{
    public class AnchorModel : AnchorInfo
    {
       
        public string UserName { get; set; }

        public string Nick { get; set; }

        public string RealName { get; set; }

        public string ShowName { get { return RealName.IsEmptyString() ? (Nick.IsEmptyString() ? UserName : Nick) : RealName; } }

        public string ShopName { get; set; }


    }
}
