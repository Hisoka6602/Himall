using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.CommonModel.Model
{
   public class AppletLiveProductImportInfo
    {
        /// <summary>
        /// 导入的商品ID列表（goodsId）
        /// </summary>
        public List<long> ids { get; set; }
        /// <summary>
        /// 房间号
        /// </summary>
        public long roomId { get; set; }
    }
}
