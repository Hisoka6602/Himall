using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class ShopCartController : BaseApiController
    {
        /// <summary>
        /// 加入购物车
        /// </summary>
        /// <param name="skuId"></param>
        /// <param name="count"></param>
        /// <param name="memberId"></param>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetUpdateCartItem(string skuId, int count, long shopBranchId, long roomId = 0)
        {
            CheckUserLogin();
            CheckSkuIdIsValid(skuId, shopBranchId);
            //判断库存
            var sku = ProductManagerApplication.GetSKU(skuId);
            if (sku == null)
            {
                throw new HimallException("错误的SKU");
            }
            //if (count > sku.Stock)
            //{
            //    throw new HimallException("库存不足");
            //}
            var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            if (shopBranch == null)
            {
                throw new HimallException("错误的门店id");
            }
            var shopBranchSkuList = ShopBranchApplication.GetSkusByIds(shopBranchId, new List<string> { skuId });
            if (shopBranchSkuList == null || shopBranchSkuList.Count == 0)
            {
                throw new HimallException("门店没有该商品");
            }
            if (shopBranchSkuList[0].Status == ShopBranchSkuStatus.InStock)
            {
                throw new HimallException("此商品已下架");
            }
            var sbsku = shopBranchSkuList.FirstOrDefault();
            if (sbsku.Stock < count)
            {
                throw new HimallException("门店库存不足");
            }
            long memberId = CurrentUser.Id;
            CartApplication.UpdateShopBranchCart(skuId, count, memberId, shopBranchId,roomId);
            return JsonResult<int>();
        }

        /// <summary>
        /// 检验SkuId
        /// </summary>
        /// <param name="skuId"></param>
        /// <param name="shopBranchId"></param>
        private void CheckSkuIdIsValid(string skuId, long shopBranchId)
        {
            long productId = 0;
            long.TryParse(skuId.Split('_')[0], out productId);
            if (productId == 0)
                throw new Himall.Core.InvalidPropertyException("SKUId无效");

            var skuItem = ProductManagerApplication.GetSKU(skuId);
            if (skuItem == null)
                throw new Himall.Core.InvalidPropertyException("SKUId无效");

        }


        public Himall.Entities.ShoppingCartInfo GetCart(long memberId, long shopBranchId)
        {
            Himall.Entities.ShoppingCartInfo shoppingCartInfo;
            if (memberId > 0)//已经登录，系统从服务器读取购物车信息，否则从Cookie获取购物车信息
                shoppingCartInfo = ServiceProvider.Instance<BranchCartService>.Create.GetCart(memberId, shopBranchId);
            else
            {
                shoppingCartInfo = new Himall.Entities.ShoppingCartInfo();

                string cartInfo = WebHelper.GetCookie(CookieKeysCollection.HIMALL_CART_BRANCH);
                if (!string.IsNullOrWhiteSpace(cartInfo))
                {
                    string[] cartItems = cartInfo.Split(',');
                    var cartInfoItems = new List<Himall.Entities.ShoppingCartItem>();
                    int i = 0;
                    foreach (string cartItem in cartItems)
                    {
                        var cartItemParts = cartItem.Split(':');
                        if (shopBranchId == 0 || cartItemParts[2] == shopBranchId.ToString())
                            cartInfoItems[i++] = new Himall.Entities.ShoppingCartItem() { ProductId = long.Parse(cartItemParts[0].Split('_')[0]), SkuId = cartItemParts[0], Quantity = int.Parse(cartItemParts[1]), ShopBranchId = long.Parse(cartItemParts[2]) };
                    }
                    shoppingCartInfo.Items = cartInfoItems;
                }
            }
            return shoppingCartInfo;
        }

        /// <summary>
        /// 获取购物车中的商品
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<CartStoreModel>> GetCart(long shopBranchId)
        {
            //CheckUserLogin();
            long userId = 0;
            //会员折扣
            decimal discount = 1.0M;//默认折扣为1（没有折扣）
            if (CurrentUser != null)
            {
                userId = CurrentUser.Id;
                discount = CurrentUser.MemberDiscount;
            }
            var cart = GetCart(userId, shopBranchId);
            var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            if (shopBranch == null)
                throw new HimallException("门店库存不足");
            var stores = cart.Items.Where(d => d.ShopBranchId > 0).OrderByDescending(d => d.AddTime).Select(d => d.ShopBranchId).GroupBy(d => d);
            decimal prodPrice = 0.0M;//优惠价格
            //var rets = new List<CartStoreModel>();
            var _store = new CartStoreModel();
            _store.ShopBranchId = shopBranch.Id;
            _store.ShopId = shopBranch.ShopId;
            _store.ShopBranchName = shopBranch.ShopBranchName;
            _store.Status = shopBranch.Status.GetHashCode();
            _store.DeliveFee = shopBranch.DeliveFee;
            _store.DeliveTotalFee = shopBranch.DeliveTotalFee;
            _store.FreeMailFee = shopBranch.FreeMailFee;
            var product = cart.Items.Where(d => d.ShopBranchId == shopBranch.Id).OrderBy(s => s.Status).ThenByDescending(o => o.AddTime).ToList();
            _store.Products = new List<CartStoreProduct>();
            foreach (var pitem in product)
            {
                var pro = ProductManagerApplication.GetProduct(pitem.ProductId);
                var shopbranchsku = ShopBranchApplication.GetSkusByIds(_store.ShopBranchId, new List<string> { pitem.SkuId }).FirstOrDefault();
                var shop = ShopApplication.GetShop(pro.ShopId);
                var vshop = VshopApplication.GetVShopByShopId(pro.ShopId);
                DTO.SKU sku = ProductManagerApplication.GetSKU(pitem.SkuId);
                string skuDetails = "";
                if (null != shop && sku != null)
                {
                    prodPrice = sku.SalePrice;
                    if (shop.IsSelf)
                    {
                        //官方自营店才计算会员折扣
                        prodPrice = sku.SalePrice * discount;
                    }
                    prodPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);

                    var typeInfo = TypeApplication.GetProductType(pro.TypeId);
                    skuDetails = "";
                    if (!string.IsNullOrWhiteSpace(sku.Size))
                    {
                        if (!string.IsNullOrWhiteSpace(skuDetails))
                        {
                            skuDetails += "、";
                        }
                        skuDetails += sku.Size;
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Color))
                    {
                        if (!string.IsNullOrWhiteSpace(skuDetails))
                        {
                            skuDetails += "、";
                        }
                        skuDetails += sku.Color;
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Version))
                    {
                        if (!string.IsNullOrWhiteSpace(skuDetails))
                        {
                            skuDetails += "、";
                        }
                        skuDetails += sku.Version;
                    }
                    string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    if (pro != null)
                    {
                        colorAlias = !string.IsNullOrWhiteSpace(pro.ColorAlias) ? pro.ColorAlias : colorAlias;
                        sizeAlias = !string.IsNullOrWhiteSpace(pro.SizeAlias) ? pro.SizeAlias : sizeAlias;
                        versionAlias = !string.IsNullOrWhiteSpace(pro.VersionAlias) ? pro.VersionAlias : versionAlias;
                    }
                    var _product = new CartStoreProduct
                    {
                        ShopBranchId = shopBranchId,
                        CartItemId = pitem.Id,
                        SkuId = pitem.SkuId,
                        Id = pro.Id,
                        ProductName = pro.ProductName,
                        Price = prodPrice,
                        Count = pitem.Quantity,
                        Stock = shopbranchsku == null ? 0 : shopbranchsku.Stock,
                        //阶梯价商品在门店购物车自动下架
                        Status = ProductManagerApplication.GetProductShowStatus(pro, sku, 1, shopBranch, shopbranchsku),//0:正常；1：冻结；2：库存不足
                        SkuDetails = skuDetails,
                        ColorAlias = colorAlias,
                        SizeAlias = sizeAlias,
                        VersionAlias = versionAlias,
                        Size = sku.Size,
                        Color = sku.Color,
                        Version = sku.Version,
                        AddTime = pitem.AddTime,
                        DefaultImage = HimallIO.GetRomoteProductSizeImage(pro.ImagePath, 1, 500)
                    };
                    _store.Products.Add(_product);
                }
            }
            _store.Amount = (_store.Products != null && _store.Products.Count > 0) ? _store.Products.Where(x => x.Status == 0).Sum(s => s.Price * s.Count) : 0;
            _store.TotalCount = (_store.Products != null && _store.Products.Count > 0) ? _store.Products.Where(x => x.Status == 0).Sum(s => s.Count) : 0;
            if (_store.Products.Count > 0)
            {//有商品数据，才返回门店信息
                _store.Products = _store.Products.OrderBy(p => p.Status).ThenByDescending(p => p.AddTime).ToList();
                //rets.Add(_store);
            }
            return JsonResult(_store);
        }

        /// <summary>
        /// 清理门店购物车所有无效商品
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetClearBranchCartProducts(long shopBranchId)
        {
            CheckUserLogin();
            long userId = CurrentUser.Id;
            CartApplication.ClearShopBranchCart(userId, shopBranchId);
            return JsonResult<int>();
        }

        /// <summary>
        /// 清理门店购物车所有无效商品
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetClearBranchCartInvalidProducts(long shopBranchId)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            CartApplication.ClearInvalidShopBranchCart(userId, shopBranchId);
            return JsonResult<int>();
        }
    }
}
