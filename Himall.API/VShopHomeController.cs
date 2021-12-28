using Himall.Application;
using Himall.Service;
using System.Linq;
using Himall.DTO.QueryModel;

namespace Himall.API
{
    public class VShopHomeController : BaseApiController
    {
        public object GetVShopHome(int pageNo, int pageSize)
        {
            dynamic result = new System.Dynamic.ExpandoObject();
            TopShopModel topVShop = new TopShopModel();
            var service = ServiceProvider.Instance<VShopService>.Create;
            var topShop = service.GetTopShop();
            if (topShop != null)
            {
                var query = new ProductQuery()
                {
                    PageSize = 3,
                    PageNo = 1,
                    ShopId = topShop.ShopId,
                    AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { Entities.ProductInfo.ProductAuditStatus.Audited },
                    SaleStatus = Entities.ProductInfo.ProductSaleStatus.OnSale
                };
                var products = ProductManagerApplication.GetProducts(query).Models;
                var topShopProducts = products.ToArray().Select(item => new HomeProduct()
                {
                    Id = item.Id.ToString(),
                    ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(item.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350),
                    MarketPrice = item.MarketPrice.ToString(),
                    Name = item.ProductName,
                    SalePrice = item.MinSalePrice.ToString(),
                    Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/product/detail/") + item.Id
                });
                topVShop.Success = "true";
                topVShop.ShopName = topShop.Name;
                topVShop.VShopId = topShop.Id.ToString();
                topVShop.ShopId = topShop.ShopId.ToString();
                topVShop.ShopLogo = Core.HimallIO.GetRomoteImagePath(topShop.StrLogo);
                if (!string.IsNullOrEmpty(topShop.Tags))
                {
                    if (topShop.Tags.Contains(";"))
                    {
                        topVShop.Tag1 = topShop.Tags.Split(';')[0];
                        topVShop.Tag2 = topShop.Tags.Split(';')[1];
                    }
                    else
                    {
                        topVShop.Tag1 = topShop.Tags;
                        topVShop.Tag2 = "";
                    }
                }


                topVShop.Products = topShopProducts;//主推店铺的商品
                topVShop.Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/vshop/detail/") + topShop.Id;
                if (CurrentUser != null)
                {
                    var favoriteTShopIds = ServiceProvider.Instance<ShopService>.Create.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();//获取已关注店铺
                    topVShop.IsFavorite = favoriteTShopIds.Contains(topShop.ShopId) ? true : false;
                }
                topVShop.productCount = ShopApplication.GetShopProductCount(topShop.ShopId);
                topVShop.FavoritesCount = ServiceProvider.Instance<ShopService>.Create.GetShopFavoritesCount(topShop.ShopId);//关注人数
            }


            int total = 0;
            var hotShops = ServiceProvider.Instance<VShopService>.Create.GetHotShops(pageNo, pageSize, out total).ToArray();//获取热门微店
            var homeProductService = ServiceProvider.Instance<MobileHomeProductsService>.Create;
            long[] favoriteShopIds = new long[] { };
            if (CurrentUser != null)
            {
                favoriteShopIds = ServiceProvider.Instance<ShopService>.Create.GetFavoriteShopInfos(CurrentUser.Id).Select(item => item.ShopId).ToArray();
            }
            var model = hotShops.Select(item =>
            {
                var queryModel = new ProductQuery()
                {
                    PageSize = 3,
                    PageNo = 1,
                    ShopId = item.ShopId,
                    OrderKey = 4//微店推荐3个商品按商家商品序号排
                };
                queryModel.AuditStatus = new Entities.ProductInfo.ProductAuditStatus[] { Entities.ProductInfo.ProductAuditStatus.Audited };
                queryModel.SaleStatus = Entities.ProductInfo.ProductSaleStatus.OnSale;
                var products = ProductManagerApplication.GetProducts(queryModel).Models;
                string tempTag1 = "";
                string tempTag2 = "";
                if (!string.IsNullOrEmpty(item.Tags))
                {
                    if (item.Tags.Contains(";"))
                    {
                        tempTag1 = item.Tags.Split(';')[0];
                        tempTag2 = item.Tags.Split(';')[1];
                    }
                    else
                        tempTag1 = item.Tags;
                }
                return new
                {
                    VShopId = item.Id.ToString(),
                    ShopName = item.Name,
                    ShopLogo = Core.HimallIO.GetRomoteImagePath(item.StrLogo),
                    Tag1 = tempTag1,
                    Tag2 = tempTag2,
                    Products = products.Select(t => new
                    {
                        Id = t ? .Id.ToString()??string.Empty,
                        Name = t.ProductName,
                        ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(t.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_220),
                        SalePrice = t.MinSalePrice,
                        Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/product/detail/") + t.Id
                    }),
                    IsFavorite = favoriteShopIds.Contains(item.ShopId) ? true : false,
                    ShopId = item.ShopId.ToString(),
                    Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/vshop/detail/") + item.Id,
                    productCount = ShopApplication.GetShopProductCount(item.ShopId),
                    FavoritesCount = ServiceProvider.Instance<ShopService>.Create.GetShopFavoritesCount(item.ShopId)//关注人数
                };
            });
            result.success = true;
            result.total = total;
            result.HotShop = model;
            result.TopVShop = topVShop;
            return result;
        }

    }
}
