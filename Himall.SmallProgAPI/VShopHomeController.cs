using Himall.Application;
using Himall.Service;
using System.Linq;
using Himall.DTO.QueryModel;
using Himall.SmallProgAPI.Model;
using Himall.Entities;
using System.Collections.Generic;
using static Himall.Entities.CustomerServiceInfo;

namespace Himall.SmallProgAPI
{
    public class VShopHomeController : BaseApiController
    {
        public object GetVShopHome(int pageNo, int pageSize,long? cid)
        {
            dynamic result = new System.Dynamic.ExpandoObject();
            TopShopModel topVShop = new TopShopModel();
            var service = ServiceProvider.Instance<VShopService>.Create;
            var settings = SiteSettingApplication.SiteSettings;

            Entities.VShopInfo topShop = null;

            if (!settings.StartVShop)
            {
                var selfShop = ServiceProvider.Instance<ShopService>.Create.GetSelfShop();
                topShop = service.GetVShopByShopId(selfShop.Id);
            }
            else
            {
                topShop = service.GetTopShop();
            }

            string MeiQiaEnt_Id = "";
            topVShop.Success = "false";
            if (topShop != null)
            {
                var query = new ProductQuery()
                {
                    PageSize = 4,
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

                List<CustomerServiceInfo> customerServices = ServiceProvider.Instance<CustomerCustomerService>.Create.GetCustomerService(topShop.ShopId);
                if (customerServices != null && customerServices.Count > 0)
                {
                    var meiqiaServiceInfo = customerServices.Where(c => c.ServerStatus == Entities.CustomerServiceInfo.ServiceStatusType.Open && c.Tool == ServiceTool.MeiQia).FirstOrDefault();
                    if (meiqiaServiceInfo != null)
                    {
                        MeiQiaEnt_Id = meiqiaServiceInfo.AccountCode;
                    }
                }
            }

            if (!settings.StartVShop)
            {
                result.success = true;
                result.total = 1;
                result.HotShop = null;
                result.TopVShop = topVShop;
                return result;
            }

            int total = 0;
            long shopcategoryId=0;
            if (cid.HasValue)
                shopcategoryId = cid.Value;
            var hotShops = ServiceProvider.Instance<VShopService>.Create.GetHotShops(pageNo, pageSize, out total, shopcategoryId).ToArray();//获取热门微店
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
                    PageSize = 4,
                    PageNo = 1,
                    ShopId = item.ShopId,
                    OrderKey = 4//微店推荐4个商品按商家商品序号排
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
                        Id = t?.Id.ToString() ?? string.Empty,
                        Name = t.ProductName,
                        ImageUrl = Core.HimallIO.GetRomoteProductSizeImage(t.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_220),
                        SalePrice = t.MinSalePrice,
                        Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/product/detail/") + t.Id
                    }),
                    IsFavorite = favoriteShopIds.Contains(item.ShopId) ? true : false,
                    ShopId = item.ShopId.ToString(),
                    Url = Core.HimallIO.GetRomoteImagePath("/m-IOS/vshop/detail/") + item.Id,
                    productCount = ShopApplication.GetShopProductCount(item.ShopId),
                    MeiQiaEnt_Id = MeiQiaEnt_Id,
                    FavoritesCount = ServiceProvider.Instance<ShopService>.Create.GetShopFavoritesCount(item.ShopId)//关注人数
                };
            });
            result.success = true;
            result.total = total;
            result.HotShop = model;
            result.TopVShop = topVShop;
            return result;
        }

        /// <summary>
        /// 微店首页栏目
        /// </summary>
        /// <param name="vshopid">店铺id</param>
        /// <returns></returns>
        public List<MobileFootMenuInfo> GetFootMenus(long shopid)
        {
            //string pathurl = SiteSettingApplication.GetCurDomainUrl().Replace("http://", "https://");
            var result = WXSmallProgramApplication.GetMobileFootMenuInfos(MenuInfo.MenuType.SmallProg, shopid);
            //result.ForEach(item => item.MenuIcon = pathurl + item.MenuIcon);
            return result;
        }

    }
}
