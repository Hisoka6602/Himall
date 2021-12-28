using PetaPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Entities
{
    public partial class FloorTablInfo
    {
        /// <summary>
        /// Id == TabId 
        /// </summary>
        [ResultColumn]
        [Obsolete("关联属性移除遗留")]
        public List<FloorTablDetailInfo> FloorTablDetailInfo { get; set; }
    }
}
