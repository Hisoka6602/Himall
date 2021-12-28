using Himall.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Himall.Web.Areas.Admin.Models
{
    public class RoleInfoModel
    {
        [Required(ErrorMessage= "权限组名必填")]
        [StringLength(15,ErrorMessage= "权限组名在15个字符以内")]
        public string RoleName { get; set; }


        public long ID { get;set; }

        //权限列表
      public  IEnumerable<RolePrivilegeInfo> RolePrivilegeInfo { set; get; }
    }
  
}