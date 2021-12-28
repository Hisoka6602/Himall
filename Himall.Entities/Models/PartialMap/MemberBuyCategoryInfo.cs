using PetaPoco;
using System.ComponentModel;
using System.Configuration;

namespace Himall.Entities
{
    public partial class MemberBuyCategoryInfo
    {

        /// <summary>
        /// 类别名称,需手动填充数据
        /// </summary>
        [ResultColumn]
        public string CategoryName { get; set; }
    }
}
