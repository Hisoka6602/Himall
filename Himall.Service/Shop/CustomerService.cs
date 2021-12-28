using Himall.Core;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    public class CustomerCustomerService : ServiceBase
    {
        public void Create(CustomerServiceInfo model)
        {
            Verify(model);
            model.Name = model.Name.Trim();//去除首尾空白
            DbFactory.Default.Add(model);
            CacheManager.ClearCustomerService(model.ShopId);
        }

        public List<CustomerServiceInfo> GetCustomerService(long shopId) =>
            CacheManager.GetCustomerService(shopId, () => DbFactory.Default.Get<CustomerServiceInfo>().Where(item => item.ShopId == shopId).ToList());


        public void UpdateCustomerService(CustomerServiceInfo customerService)
        {
            //检查
            var ori = CheckPropertyWhenUpdate(customerService);

            //更新信息
            ori.Name = customerService.Name;
            ori.Type = customerService.Type;
            ori.Tool = customerService.Tool;
            ori.AccountCode = customerService.AccountCode;
            ori.TerminalType = customerService.TerminalType;
            ori.ServerStatus = customerService.ServerStatus;

            //保存更改

            DbFactory.Default.Update(ori);
            CacheManager.ClearCustomerService(ori.ShopId);
        }

        public void Remove(long shopId, params long[] ids)
        {
            DbFactory.Default.Del<CustomerServiceInfo>(item => item.ShopId == shopId && item.Id.ExIn(ids));
            CacheManager.ClearCustomerService(shopId);
        }

        public void RemoveMobile(long shopId)
        {
            DbFactory.Default.Del<CustomerServiceInfo>(item => item.ShopId == shopId && item.TerminalType == CustomerServiceInfo.ServiceTerminalType.Mobile);
            CacheManager.ClearCustomerService(shopId);
        }


        /// <summary>
        /// 添加时检查属性
        /// </summary>
        /// <param name="customerService"></param>
		private void Verify(CustomerServiceInfo customerService)
        {
            if (string.IsNullOrWhiteSpace(customerService.Name))
                throw new InvalidPropertyException("客服名称不能为空");
            if (string.IsNullOrWhiteSpace(customerService.AccountCode))
                throw new InvalidPropertyException("沟通工具账号不能为空");
        }

        /// <summary>
        /// 更新时检查属性
        /// </summary>
        /// <param name="customerService"></param>
        /// <returns>返回原始客服信息</returns>
        CustomerServiceInfo CheckPropertyWhenUpdate(CustomerServiceInfo customerService)
        {
            if (customerService.ShopId == 0)
                throw new InvalidPropertyException("店铺id必须大于0");

            return CheckPlatformCustomerServiceWhenUpdate(customerService);
        }

        /// <summary>
        /// 更新时检查属性
        /// </summary>
        /// <param name="customerService"></param>
        /// <returns>返回原始客服信息</returns>
        CustomerServiceInfo CheckPlatformCustomerServiceWhenUpdate(CustomerServiceInfo customerService)
        {
            if (string.IsNullOrWhiteSpace(customerService.Name))
                throw new InvalidPropertyException("客服名称不能为空");
            if (string.IsNullOrWhiteSpace(customerService.AccountCode))
                throw new InvalidPropertyException("沟通工具账号不能为空");

            var ori = DbFactory.Default.Get<CustomerServiceInfo>().Where(item => item.Id == customerService.Id && item.ShopId == customerService.ShopId).FirstOrDefault();//查找指定店铺下指定id的客服
            if (ori == null)
                throw new InvalidPropertyException("不存在id为" + customerService.Id + "的客服信息");
            return ori;
        }

        public CustomerServiceInfo GetCustomerService(long shopId, long id) =>
            GetCustomerService(shopId).FirstOrDefault(p => p.Id == id);

        public CustomerServiceInfo GetCustomerServiceForMobile(long shopId) =>
             GetCustomerService(shopId).FirstOrDefault(r => r.TerminalType == CustomerServiceInfo.ServiceTerminalType.Mobile);

        public List<CustomerServiceInfo> GetMobileCustomerService(long shopId) =>
             GetCustomerService(shopId).Where(r => r.TerminalType == CustomerServiceInfo.ServiceTerminalType.Mobile).ToList();

        /// <summary>
        /// 获取门店可用售前客服
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public List<CustomerServiceInfo> GetPreSaleByShopId(long shopId)
        {
            return GetCustomerService(shopId).Where(p => p.ServerStatus == CustomerServiceInfo.ServiceStatusType.Open && p.Type == CustomerServiceInfo.ServiceType.PreSale).ToList();
        }

        /// <summary>
        /// 获取门店可用售后客服(美洽客服不分售后、售前)
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public List<CustomerServiceInfo> GetAfterSaleByShopId(long shopId)
        {
            return GetCustomerService(shopId).Where(p => p.ServerStatus == CustomerServiceInfo.ServiceStatusType.Open && (p.Type == CustomerServiceInfo.ServiceType.AfterSale || p.Tool == CustomerServiceInfo.ServiceTool.MeiQia) && p.ShopId == shopId).ToList();
        }

        public List<CustomerServiceInfo> GetPlatformCustomerService(bool isOpen = false, bool isMobile = false)
        {
            var result = GetCustomerService(0);
            if (isOpen)
                result = result.Where(r => r.ServerStatus == CustomerServiceInfo.ServiceStatusType.Open).ToList();
            if (isMobile)
                result = result.Where(r => r.TerminalType == CustomerServiceInfo.ServiceTerminalType.Mobile).ToList();
            return result.ToList();
        }

        public void Save(IEnumerable<CustomerServiceInfo> models)
        {
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in models)
                {
                    var ori = CheckPlatformCustomerServiceWhenUpdate(item);

                    ori.AccountCode = item.AccountCode;
                    ori.Name = item.Name;
                    ori.ServerStatus = item.ServerStatus;
                    ori.TerminalType = item.TerminalType;
                    ori.Tool = item.Tool;
                    DbFactory.Default.Update(ori);
                }
            });
            foreach (var shop in models.Select(p => p.ShopId).Distinct())
                CacheManager.ClearCustomerService(shop);
        }
    }
}
