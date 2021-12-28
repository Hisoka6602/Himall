using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class CartController : BaseWebController
    {
        CartHelper cartHelper;
        CartService _iCartService;
        ProductService _ProductService;
        MemberService _MemberService;
        OrderService _OrderService;
        ShopService _ShopService;
        private TypeService _iTypeService;

        public CartController(CartService CartService,
            ProductService ProductService,
            MemberService MemberService,
            OrderService OrderService,
            ShopService ShopService, TypeService TypeService
            )
        {
            _iCartService = CartService;
            _ProductService = ProductService;
            _MemberService = MemberService;
            _OrderService = OrderService;
            _ShopService = ShopService;
            _iTypeService = TypeService;
            cartHelper = new CartHelper();
        }
        /*
         *购物车存储说明：
         *游客访问时，点击加入购物车，购物车信息保存至Cookie中，游客点击结算时，Cookie中的购物车信息转移至数据库中并清空Cookie中购物车信息。
         *登录会员点击加入购物车时，购物车信息保存至数据库中。
         *Cookie存储格式： skuId1:count1,skuId2:count2,.....
         */


        // GET: Web/Cart
        public ActionResult AddToCart(string skuId, int count)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            try
            {

                cartHelper.AddToCart(skuId, count, userId);
            }
            catch { }
            return RedirectToAction("AddedToCart", new { skuId = skuId });
        }


        public ActionResult AddedToCart(string skuId)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            try
            {

                string productId = skuId.Split('_')[0];
                ViewBag.ProductId = productId;
                var productService = _ProductService;
                var cart = cartHelper.GetCart(userId);
                Entities.ProductInfo product;
                Entities.SKUInfo sku;
                var products = cart.Items.Select(item =>
                {
                    product = productService.GetProduct(item.ProductId);
                    sku = productService.GetSku(item.SkuId);
                    return new CartItemModel()
                    {
                        skuId = item.SkuId,
                        id = product.Id,
                        imgUrl = product.ImagePath + "/1_50.png",
                        name = product.ProductName,
                        price = sku.SalePrice,
                        count = item.Quantity
                    };
                });

                ViewBag.Current = products.FirstOrDefault(item => item.skuId == skuId);
                ViewBag.Others = products.Where(item => item.skuId != skuId);
                ViewBag.Amount = products.Sum(item => item.price * item.count);
                ViewBag.TotalCount = products.Sum(item => item.count);
                ViewBag.Keyword = SiteSettings.Keyword;
            }
            catch { }
            return View("AddToCart");
        }





        [HttpPost]
        public JsonResult AddProductToCart(string skuId, int count)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            try
            {

                cartHelper.AddToCart(skuId, count, userId);
                return Json(new { success = true });
            }
            catch (HimallException ex) { return Json(new { success = false, msg = ex.Message }); }
        }

        /// <summary>
        /// 验证商品是否可加入购物车
        /// </summary>
        /// <param name="id">商品ID</param>
        /// <returns>success  JSON数据,返回真表示可以加入购物车</returns>
        [HttpPost]
        public JsonResult verificationToCart(long id)
        {
            long buyid = 0;
            bool success = false;

            var iLimitService = ObjectContainer.Current.Resolve<LimitTimeBuyService>();
            var ltmbuy = iLimitService.GetLimitTimeMarketItemByProductId(id);
            if (ltmbuy != null)
            {
                buyid = ltmbuy.Id;
            }
            else
            {
                var sku = _ProductService.GetSKUs(id);
                if (sku.ToList().Count == 1 && sku.FirstOrDefault().Id.Contains("0_0_0"))
                {
                    success = true;
                }
            }

            return Json(new { success = success, id = buyid });
        }

        public ActionResult BatchAddToCart(string skuIds, string counts)
        {
            var skuIdsArr = skuIds.Split(',');
            var countsArr = counts.Split(',').Select(item => int.Parse(item));

            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            for (int i = 0; i < skuIdsArr.Count(); i++)
                cartHelper.AddToCart(skuIdsArr.ElementAt(i), countsArr.ElementAt(i), userId);
            return RedirectToAction("cart");
        }



        public ActionResult Cart()
        {
            //Logo
            ViewBag.Logo = SiteSettingApplication.SiteSettings.Logo;
            ViewBag.Step = 1;

            CartCartModel model = new CartCartModel();

            var memberInfo = base.CurrentUser;

            ViewBag.Member = memberInfo;
            long uid = 0;
            if (CurrentUser != null)
            {
                uid = CurrentUser.Id;
            }

            model.Top3RecommendProducts = _ProductService.GetPlatHotSaleProductByNearShop(10, uid, true).ToList();
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveFromCart(string skuId)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;


            cartHelper.RemoveFromCart(skuId, userId);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult BatchRemoveFromCart(string skuIds)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;

            var skuIdsArr = skuIds.Split(',');

            cartHelper.RemoveFromCart(skuIdsArr, userId);
            return Json(new { success = true });
        }


        [HttpPost]
        public JsonResult UpdateCartItem(string skuId, int count)
        {
            long userId = CurrentUser != null ? CurrentUser.Id : 0;
            var orderService = _OrderService;
            var skuinfo = orderService.GetSkuByID(skuId);
            var product = ProductManagerApplication.GetProduct(skuinfo.ProductId);
            if (product != null)
            {
                if (product.MaxBuyCount > 0 && count > product.MaxBuyCount && !product.IsOpenLadder)
                {
                    return Json(new { success = false, msg = string.Format("每个ID限购{0}件", product.MaxBuyCount), stock = product.MaxBuyCount });
                }
            }

            if (skuinfo.Stock < count)
            { 
                return Json(new { success = false, msg = "库存不足", stock = skuinfo.Stock });
            }
            
            cartHelper.UpdateCartItem(skuId, count, userId);

            #region 购物车修改数量阶梯价变动--张宇枫
            //获取产品详情
            var price = 0m;
            if (product.IsOpenLadder)
            {
                var shop = ShopApplication.GetShop(product.ShopId);

                var groupCartByProduct = cartHelper.GetCart(userId).Items.Where(item => item.ShopBranchId == 0).Select(c =>
                {
                    var cItem = new Himall.Entities.ShoppingCartItem();
                    var skuInfo = _ProductService.GetSku(c.SkuId);
                    if (skuInfo != null)
                        cItem = c;
                    return cItem;
                }).GroupBy(i => i.ProductId).ToList();
                var quantity = groupCartByProduct.Where(i => i.Key == product.Id).ToList().Sum(cartitem => cartitem.Sum(i => i.Quantity));

                decimal discount = 1M;
                if (CurrentUser != null)
                {
                    discount = CurrentUser.MemberDiscount;
                }
                price = ProductManagerApplication.GetProductLadderPrice(product.Id, quantity);
                if (shop.IsSelf)
                    price = price * discount;
            }

            #endregion

            return Json(new { success = true, saleprice = price.ToString("F2"), productid = product.Id, isOpenLadder = product.IsOpenLadder });
        }

        [HttpPost]
        public JsonResult GetSkuByID(string skuId)
        {
            var orderService = _OrderService;
            var skuinfo = orderService.GetSkuByID(skuId);
            var json = new
            {
                Color = skuinfo.Color,
                Size = skuinfo.Size,
                Version = skuinfo.Version
            };
            return Json(json);
        }

        [HttpPost]
        public JsonResult GetCartProducts()
        {
            var memberId = CurrentUser?.Id ?? 0;
            var cartItems = CartApplication.GetCartItems(memberId);
            var products = cartItems.Select(item =>
            {
                var product = item.Product;
                var shop = ShopApplication.GetShop(product.ShopId);
                var sku = item.Sku;
                var pic = Himall.Core.HimallIO.GetProductSizeImage(product.RelativePath, 1, (int)ImageSize.Size_50);
                if (!string.IsNullOrEmpty(sku.ShowPic)) {
                    pic = Himall.Core.HimallIO.GetImagePath(sku.ShowPic);
                }
                return new
                {
                    cartItemId = item.ItemId,
                    skuId = sku.Id,
                    id = product.Id,
                    imgUrl = pic,
                    name = product.ProductName,
                    productstatus = item.IsLimit ? 0 : (sku.Stock <= 0 ? ProductInfo.ProductSaleStatus.InStock.GetHashCode() : product.SaleStatus.GetHashCode()),
                    status = item.ShowStatus,
                    productauditstatus = product.AuditStatus,
                    price = sku.SalePrice,
                    Color = sku.Color,
                    Size = sku.Size,
                    Version = sku.Version,
                    count = item.Quantity,
                    shopId = shop.Id,
                    shopName = shop.ShopName,
                    productcode = !(sku.Version + sku.Color + sku.Size).Equals("") ? sku.Sku : product.ProductCode,
                    ColorAlias = sku.ColorAlias,
                    SizeAlias = sku.SizeAlias,
                    VersionAlias = sku.VersionAlias,
                    AddTime = item.AddTime,
                    minMath = item.Quantity < item.MinMach ? 0 : 1
                };
            }).OrderBy(s => s.status).ThenByDescending(o => o.AddTime).ToList();

            var newproducts = products.Where(p => p.status == 0 && p.productauditstatus != Entities.ProductInfo.ProductAuditStatus.InfractionSaleOff && p.productauditstatus != Entities.ProductInfo.ProductAuditStatus.WaitForAuditing);//获取有效商品
            var cartModel = new
            {
                products,
                amount = newproducts.Sum(item => item.price * item.count),
                totalCount = newproducts.Sum(item => item.count),
            };
            return Json(cartModel);
        }
    }
}