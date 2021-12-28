using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Application
{
    /// <summary>
    /// 结算相关服务应用
    /// </summary>
    public class AccountApplication
    {
        private static AccountService _AccountService= ObjectContainer.Current.Resolve<AccountService>();

        public static QueryPageModel<AccountInfo> GetAccounts(AccountQuery query)
        {
            return _AccountService.GetAccounts(query);
        }

        public static AccountInfo GetAccount(long id)
        {
            return _AccountService.GetAccount(id);
        }
        /// <summary>
        /// 根据ID获取多条结算记录
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<AccountInfo> GetAccounts(IEnumerable<long> ids)
        {
            return _AccountService.GetAccounts(ids);
        }
        /// <summary>
        /// 获取结算订单明细列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<AccountDetailInfo> GetAccountDetails(AccountQuery query)
        {
            return _AccountService.GetAccountDetails(query);
        }
        /// <summary>
        /// 取服务费用
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<AccountMetaModel> GetAccountMeta(AccountQuery query)
        {
            return _AccountService.GetAccountMeta(query);
        }
        /// <summary>
        /// 确认结算
        /// </summary>
        /// <param name="id"></param>
        /// <param name="managerRemark"></param>
        public static void ConfirmAccount(long id, string managerRemark)
        {
            _AccountService.ConfirmAccount(id, managerRemark);
        }
    }
}
