using System.Collections.Generic;

namespace Himall.CommonModel
{
    public interface ISellerManager:IManager
    {
        long VShopId { get; set; }
        List<SellerPrivilege> SellerPrivileges { set; get; }

        /// <summary>
        /// 是否主账号
        /// </summary>
        bool IsMainAccount
        {
            get;
        }
    }
}
