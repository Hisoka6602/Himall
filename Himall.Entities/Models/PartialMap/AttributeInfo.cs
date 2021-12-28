using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class AttributeInfo
    {
        [ResultColumn]
        public string AttrValue { get; set; }
   
        [ResultColumn]
        [Obsolete("关联属性移除遗留")]
        public List<AttributeValueInfo> AttributeValueInfo { get; set; }

    }
}
