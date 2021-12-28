using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application
{
    public class FavoriteApplication:BaseApplicaion<ProductService>
    {
        public static int GetFavoriteCountByUser(long userid)
        {
            return Service.GetFavoriteCountByUser(userid);
        }

        public static int GetFavoriteCountByProduct(long product) {
            return Service.GetFavoriteCountByProduct(product);
        }
        public static int GetFavoriteShopCount(long userid)
        {
            return Service.GetFavoriteShopCount(userid);
        }

        public static int GetFavoriteShopCountByShop(long shop) {
            return Service.GetFavoriteShopCountByShop(shop);
        }
        public static List<FavoriteShopInfo> GetFavoriteShop(long user, int top)
        {
            return Service.GetFavoriteShop(user, top);
        }
        public static List<FavoriteInfo> GetFavoriteByUser(long user, int top)
        {
            return Service.GetUserAllConcern(user, top);
        }

        /// <summary>
        /// 是否收藏商品
        /// </summary>
        public static bool HasFavoriteProduct(long userId, long productId) =>
            Service.IsFavorite(productId, userId);

        public static bool HasFavoriteShop(long shopId, long userId) =>
            ServiceProvider.Instance<ShopService>.Create.IsFavoriteShop(userId, shopId);
    }
}
