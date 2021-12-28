using Himall.Core;
using Himall.Service;
using Himall.Entities;
using Himall.CommonModel;

namespace Himall.Application
{
    public class ShopOpenApiApplication
    {
        static ShopOpenApiService _iShopOpenApiService = ObjectContainer.Current.Resolve<ShopOpenApiService>();

        /// <summary>
        /// 获取店铺的OpenApi配置
        /// </summary>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public static ShopOpenApiSettingInfo Get(string appkey)
        {
            return _iShopOpenApiService.Get(appkey);
        }

        public static ShopOpenApiSettingInfo Get(long shopId)
        {
            var shop = Cache.Get<ShopOpenApiSettingInfo>(CacheKeyCollection.Cache_ShopApiSite(shopId));
            if (shop != null) return shop;

            var data = _iShopOpenApiService.Get(shopId);
            if (data == null)
            {
                data = _iShopOpenApiService.MakeOpenApi(shopId);
                _iShopOpenApiService.Add(data);
            }
            Cache.Insert(CacheKeyCollection.Cache_ShopApiSite(shopId), data, 600);

            return data;
        }
    }
}
