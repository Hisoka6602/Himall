using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Application;
using Himall.Web.Framework;

namespace Himall.API
{
    [BaseShopLoginedActionFilter]
    public abstract class BaseShopLoginedApiController : BaseShopApiController
    {

    }
}
