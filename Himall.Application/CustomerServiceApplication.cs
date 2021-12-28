using Himall.Core;
using Himall.Service;
using Himall.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.DTO;
using Himall.CommonModel;

namespace Himall.Application
{
    public class CustomerServiceApplication
    {
		private static CustomerCustomerService _customerService = ObjectContainer.Current.Resolve<CustomerCustomerService>();

		/// <summary>
		/// 获取平台的客服信息
		/// </summary>
		/// <param name="isOpen">是否开启</param>
		/// <param name="isMobile">是否适用于移动端</param>
		public static List<CustomerService> GetPlatformCustomerService(bool isOpen = false, bool isMobile = false, bool isHiChat = false, MemberInfo member = null,PlatformType plattype= PlatformType.PC)
		{
			var customerlist = _customerService.GetPlatformCustomerService(isOpen, isMobile).Map<List<CustomerService>>();

			AddHiChat(customerlist, isHiChat, 0, member,null, plattype);
			return customerlist;
		}

		/// <summary>
		/// 获取门店可用售前客服
		/// </summary>
		/// <param name="shopId"></param>
		/// <returns></returns>
		public static List<CustomerService> GetPreSaleByShopId(long shopId,bool isHiChat=false, MemberInfo member = null, ProductInfo product = null)
		{
			var customerlist= _customerService.GetPreSaleByShopId(shopId).Map<List<CustomerService>>();
			AddHiChat(customerlist, isHiChat, shopId, member, product);
			return customerlist;
		}

		/// <summary>
		/// 获取商家自己客服
		/// </summary>
		/// <param name="list"></param>
		/// <param name="shopId"></param>
		private static void AddHiChat(List<CustomerService> list,bool isHiChat=false, long shopId = 0, MemberInfo member = null,ProductInfo product=null, PlatformType plattype = PlatformType.PC) {
			if (!isHiChat)
				return;

			var shoplist = new List<long>() { shopId };
			var newshop = new ShopInfo();
			if (shopId <= 0)
			{
				newshop = ShopApplication.GetSelfShop();
			}
			else {
				newshop = ShopApplication.GetShop(shopId);
			}
			var shopname = shopId <= 0 ? ShopApplication.GetSelfShop().ShopName : ShopApplication.GetShopNames(shoplist).First().Value;//获取自营店
			if (!string.IsNullOrWhiteSpace(shopname))
			{
				if (list == null)
					list = new List<CustomerService>();

				var custerdata = ShopOpenApiApplication.Get(newshop.Id);//获取对应的商家appkey；

				var chaturl = string.Format("/CustomerServices/HiChat?appkey={0}&link={1}&shopName={2}"
					, custerdata.AppKey, Core.Helper.WebHelper.UrlEncode(Core.Helper.WebHelper.GetUrl()), shopname);

				if (member != null)
					chaturl += string.Format("&photo={0}&nick={1}&userId={2}", HimallIO.GetRomoteImagePath(member.PhotoUrl), member.ShowNick, member.Id);

				if (product != null && product.Id>0)
				{
					var picimg = HimallIO.GetRomoteProductSizeImage(product.RelativePath, 1, (int)ImageSize.Size_350);
					chaturl += string.Format("&name={0}&price={1}&image={2}",
						Core.Helper.WebHelper.UrlEncode(product.ProductName), product.MinSalePrice.ToString("F2"), Core.Helper.WebHelper.UrlEncode(picimg));
				}

				list.Insert(0, new CustomerService()
				{
					AccountCode = plattype==PlatformType.Android ? custerdata.AppKey: chaturl,
					Tool = CustomerServiceInfo.ServiceTool.HiChat,
					TerminalType = CustomerServiceInfo.ServiceTerminalType.All,
					ShopId = newshop.Id,
					Name = shopname,
					ServerStatus = newshop.IsOpenHiChat ? CustomerServiceInfo.ServiceStatusType.Open : CustomerServiceInfo.ServiceStatusType.Close
				});
			}
		}

        /// <summary>
        /// 获取门店可用售后客服(美洽客服不分售后、售前)
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<CustomerService> GetAfterSaleByShopId(long shopId)
		{
			var customerlist = _customerService.GetAfterSaleByShopId(shopId).Map<List<CustomerService>>();
			AddHiChat(customerlist, true, shopId, null, null);
			return customerlist;
		}


        /// <summary>
        /// 获取移动端客服且包含美洽(如有美洽，美洽存放第一个位置)
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<CustomerService> GetMobileCustomerServiceAndMQ(long shopId,bool isHiChat=false, MemberInfo member = null,ProductInfo product=null,PlatformType palteform=PlatformType.PC)
        {
			var customerServices = _customerService.GetMobileCustomerService(shopId).Map<List<CustomerService>>();
            var meiqia = _customerService.GetPreSaleByShopId(shopId).Map<List<CustomerService>>().FirstOrDefault(p => p.Tool == Entities.CustomerServiceInfo.ServiceTool.MeiQia);
            if (meiqia != null)
            {
                if (customerServices == null)
                    customerServices = new List<CustomerService>();
                customerServices.Insert(0, meiqia);
            }

			AddHiChat(customerServices, isHiChat, shopId, member, product, palteform);
			return customerServices;
        }

        /// <summary>
        /// 更新平台客服信息
        /// </summary>
        /// <param name="models"></param>
        public static void UpdatePlatformService(IEnumerable<CustomerService> models)
		{
			var css = models.Map<List<CustomerServiceInfo>>();
			_customerService.Save(css);
		}

		/// <summary>
		/// 添加客服
		/// </summary>
		/// <param name="model">客服信息</param>
		public static long AddPlateCustomerService(CustomerService model)
		{
			var cs = model.Map<CustomerServiceInfo>();
			_customerService.Create(cs);
			model.Id = cs.Id;
			return cs.Id;
		}
    }
}
