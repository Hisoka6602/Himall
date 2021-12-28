using System.Collections.Generic;

namespace Himall.CommonModel
{
    public interface IPaltManager:IManager
    {
        List<AdminPrivilege> AdminPrivileges { set; get; }
    }
}
