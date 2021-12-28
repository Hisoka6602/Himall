using System.Collections.Generic;
using System.Linq;
using Himall.Service;
using Himall.Entities;
using NetRube.Data;

namespace Himall.Service
{
    public class ProductLadderPriceService : ServiceBase
	{
		/// <summary>
		/// 根据商品id获取sku信息
		/// </summary>
		/// <param name="productId"></param>
		/// <returns></returns>
        public List<ProductLadderPriceInfo> GetLadderPricesByProductIds(long productId)
		{
            return DbFactory.Default.Get<ProductLadderPriceInfo>().Where(p => p.ProductId == productId).ToList();
        }

        public List<ProductLadderPriceInfo> GetLadderPricesByProductIds(List<long> products)
        {
            return DbFactory.Default.Get<ProductLadderPriceInfo>().Where(p => p.ProductId.ExIn(products)).ToList();
        }

    }
}
