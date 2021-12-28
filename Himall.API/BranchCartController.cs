﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Service;
using Himall.Service;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Himall.API
{
    public class BranchCartController : BaseApiController
    {
        private ShopBranchService _iShopBranchService;
        private CartService _iCartService;
        private BranchCartService _iBranchCartService;
        private ProductService _ProductService;
        private ShopService _ShopService;
        private VShopService _VShopService;
        public BranchCartController()
        {
            _iShopBranchService = new ShopBranchService();
            _iCartService = new CartService();
            _ProductService = new ProductService();
            _iBranchCartService = new BranchCartService();
            _ShopService = new ShopService();
            _VShopService = new VShopService();
        }

        /// <summary>
        /// 获取购物车中的商品
        /// </summary>
        /// <returns></returns>
        public Himall.Entities.ShoppingCartInfo GetCart(long memberId, long shopBranchId)
        {
            Himall.Entities.ShoppingCartInfo shoppingCartInfo;
            if (memberId > 0)//已经登录，系统从服务器读取购物车信息，否则从Cookie获取购物车信息
                shoppingCartInfo = _iBranchCartService.GetCart(memberId, shopBranchId);
            else
            {
                shoppingCartInfo = new Himall.Entities.ShoppingCartInfo();

                string cartInfo = WebHelper.GetCookie(CookieKeysCollection.HIMALL_CART_BRANCH);
                if (!string.IsNullOrWhiteSpace(cartInfo))
                {
                    string[] cartItems = cartInfo.Split(',');
                    var cartInfoItems = new List<Himall.Entities.ShoppingCartItem>();
                    foreach (string cartItem in cartItems)
                    {
                        var cartItemParts = cartItem.Split(':');
                        if (shopBranchId == 0 || cartItemParts[2] == shopBranchId.ToString())
                            cartInfoItems.Add(new Himall.Entities.ShoppingCartItem() { ProductId = long.Parse(cartItemParts[0].Split('_')[0]), SkuId = cartItemParts[0], Quantity = int.Parse(cartItemParts[1]), ShopBranchId = long.Parse(cartItemParts[2]) });
                    }
                    shoppingCartInfo.Items = cartInfoItems;
                }
            }
            return shoppingCartInfo;
        }

        /// <summary>
        /// 加入购物车
        /// </summary>
        /// <param name="skuId"></param>
        /// <param name="count"></param>
        /// <param name="memberId"></param>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public object GetUpdateCartItem(string skuId, int count, long shopBranchId)
        {
            CheckSkuIdIsValid(skuId, shopBranchId);
            //判断库存
            var sku = _ProductService.GetSku(skuId);
            if (sku == null)
            {
                throw new HimallException("错误的SKU");
            }
            //if (count > sku.Stock)
            //{
            //    throw new HimallException("库存不足");
            //}
            var shopBranch = _iShopBranchService.GetShopBranchById(shopBranchId);
            if (shopBranch == null)
            {
                throw new HimallException("错误的门店id");
            }
            var shopBranchSkuList = _iShopBranchService.GetSkusByIds(shopBranchId, new List<string> { skuId });
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
                throw new HimallException("库存不足");
            }
            long memberId = 0;
            if (CurrentUser != null)
            {
                memberId = CurrentUser.Id;
            }
            if (memberId > 0)
                _iBranchCartService.UpdateCart(skuId, count, memberId, shopBranchId);
            else
            {
                string cartInfo = WebHelper.GetCookie(CookieKeysCollection.HIMALL_CART_BRANCH);
                if (!string.IsNullOrWhiteSpace(cartInfo))
                {
                    string[] cartItems = cartInfo.Split(',');
                    string newCartInfo = string.Empty;
                    bool exist = false;
                    foreach (string cartItem in cartItems)
                    {
                        var cartItemParts = cartItem.Split(':');
                        if (cartItemParts[0] == skuId && cartItemParts[2] == shopBranchId.ToString())
                        {
                            newCartInfo += "," + skuId + ":" + count + ":" + shopBranchId;
                            exist = true;
                        }
                        else
                            newCartInfo += "," + cartItem;
                    }
                    if (!exist)
                        newCartInfo += "," + skuId + ":" + count + ":" + shopBranchId;

                    if (!string.IsNullOrWhiteSpace(newCartInfo))
                        newCartInfo = newCartInfo.Substring(1);
                    WebHelper.SetCookie(CookieKeysCollection.HIMALL_CART_BRANCH, newCartInfo);

                }
                else
                {
                    WebHelper.SetCookie(CookieKeysCollection.HIMALL_CART_BRANCH, string.Format("{0}:{1}:{2}", skuId, count, shopBranchId));
                }
            }
            return SuccessResult();
        }

        /// <summary>
        /// 检验SkuId
        /// </summary>
        /// <param name="skuId"></param>
        /// <param name="shopBranchId"></param>
        public void CheckSkuIdIsValid(string skuId, long shopBranchId)
        {
            long productId = 0;
            long.TryParse(skuId.Split('_')[0], out productId);
            if (productId == 0)
                throw new Himall.Core.InvalidPropertyException("SKUId无效");

            var skuItem = _ProductService.GetSku(skuId);
            if (skuItem == null)
                throw new Himall.Core.InvalidPropertyException("SKUId无效");

        }

        /// <summary>
        /// 获取底部购物车详情
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public object GetBranchCartProducts(long shopBranchId)
        {
            long userId = 0;
            //会员折扣
            decimal discount = 1.0M;//默认折扣为1（没有折扣）
            if (CurrentUser != null)
            {
                userId = CurrentUser.Id;
                discount = CurrentUser.MemberDiscount;
            }
            var cart = GetCart(userId, shopBranchId);
            //var shopBranch = _iShopBranchService.GetShopBranchById(shopBranchId);
            var shopBranch = ShopBranchApplication.GetShopBranchById(shopBranchId);
            var productService = _ProductService;
            var shopService = _ShopService;
            var vshopService = _VShopService;
            List<long> pids = new List<long>();
            decimal prodPrice = 0.0M;//优惠价格
            var products = cart.Items.Select(item =>
            {
                var product = productService.GetProduct(item.ProductId);
                var shopbranchsku = _iShopBranchService.GetSkusByIds(shopBranchId, new List<string> { item.SkuId }).FirstOrDefault();
                var shop = shopService.GetShop(product.ShopId);
                //Entities.SKUInfo sku = null;
                string skuDetails = "";
                if (null != shop)
                {
                    var vshop = vshopService.GetVShopByShopId(shop.Id);
                    //sku = productService.GetSku(item.SkuId);
                    DTO.SKU sku = ProductManagerApplication.GetSKU(item.SkuId);
                    if (sku == null)
                    {
                        return null;
                    }
                    prodPrice = sku.SalePrice;
                    if (shop.IsSelf)
                    {//官方自营店才计算会员折扣
                        prodPrice = sku.SalePrice * discount;
                    }

                    var typeInfo = TypeApplication.GetProductType(product.TypeId);
                    skuDetails = "";
                    if (!string.IsNullOrWhiteSpace(sku.Size))
                    {
                        skuDetails += sku.Size + "&nbsp;&nbsp;";
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Color))
                    {
                        skuDetails += sku.Color + "&nbsp;&nbsp;";
                    }
                    if (!string.IsNullOrWhiteSpace(sku.Version))
                    {
                        skuDetails += sku.Version + "&nbsp;&nbsp;";
                    }
                    return new
                    {
                        bId = shopBranchId,
                        cartItemId = item.Id,
                        skuId = item.SkuId,
                        id = product.Id,
                        name = product.ProductName,
                        price = decimal.Parse(prodPrice.ToString("F2")),
                        count = item.Quantity,
                        stock = shopbranchsku == null ? 0 : shopbranchsku.Stock,
                        //阶梯价商品在门店购物车自动下架
                        //status = product.IsOpenLadder ? 1 : (shopbranchsku == null ? 1 : (shopbranchsku.Status == ShopBranchSkuStatus.Normal) ? (item.Quantity > shopbranchsku.Stock ? 2 : 0) : 1),//0:正常；1：冻结；2：库存不足
                        status = ProductManagerApplication.GetProductShowStatus(product, sku, 1, shopBranch, shopbranchsku),//0:正常；1：冻结；2：库存不足；3：已下架；
                        skuDetails = skuDetails,
                        AddTime = item.AddTime
                    };
                }
                else
                {
                    return null;
                }
            }).Where(d => d != null).OrderBy(s => s.status).ThenByDescending(o => o.AddTime);

            var cartModel = new
            {
                success = true,
                products = products,
                amount = products.Where(x => x.status == 0).Sum(item => item.price * item.count),
                totalCount = products.Where(x => x.status == 0).Sum(item => item.count),
                DeliveFee = shopBranch.DeliveFee,
                DeliveTotalFee = shopBranch.DeliveTotalFee,
                FreeMailFee = shopBranch.FreeMailFee,
                shopBranchStatus = (int)shopBranch.Status
            };
            return cartModel;
        }
        /// <summary>
        /// 清理门店购物车所有商品
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public object GetClearBranchCartProducts(long shopBranchId)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            var cart = GetCart(userId, shopBranchId);
            foreach (var item in cart.Items)
            {
                RemoveFromCart(item.SkuId, userId, shopBranchId);
            }
            return SuccessResult();
        }

        /// <summary>
        /// 清理门店购物车所有无效商品
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public object GetClearBranchCartInvalidProducts(long shopBranchId)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            var cart = GetCart(userId, shopBranchId);
            foreach (var item in cart.Items)
            {
                var shopbranchsku = _iShopBranchService.GetSkusByIds(shopBranchId, new List<string> { item.SkuId }).FirstOrDefault();
                if (shopbranchsku.Status != ShopBranchSkuStatus.Normal || item.Quantity > shopbranchsku.Stock)
                {
                    RemoveFromCart(item.SkuId, userId, shopBranchId);
                }
            }
            return SuccessResult();
        }
        public void RemoveFromCart(string skuId, long memberId, long shopBranchId)
        {
            if (memberId > 0)
                _iBranchCartService.DeleteCartItem(skuId, memberId, shopBranchId);
            else
            {
                string cartInfo = WebHelper.GetCookie(CookieKeysCollection.HIMALL_CART_BRANCH);
                if (!string.IsNullOrWhiteSpace(cartInfo))
                {
                    string[] cartItems = cartInfo.Split(',');
                    string newCartInfo = string.Empty;
                    foreach (string cartItem in cartItems)
                    {
                        string[] cartItemParts = cartItem.Split(':');
                        string cartItemSkuId = cartItemParts[0];
                        string cartItemshopBranchId = cartItemParts[2];
                        if (cartItemSkuId != skuId && shopBranchId.ToString() != cartItemshopBranchId)
                            newCartInfo += "," + cartItem;
                    }
                    if (!string.IsNullOrWhiteSpace(newCartInfo))
                        newCartInfo = newCartInfo.Substring(1);
                    WebHelper.SetCookie(CookieKeysCollection.HIMALL_CART_BRANCH, newCartInfo);
                }
            }
        }
    }
}
