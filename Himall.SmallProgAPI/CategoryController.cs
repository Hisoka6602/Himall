using Himall.Application;
using Himall.Service;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class CategoryController : BaseApiController
    {
        public JsonResult<Result<dynamic>> GetAllCategories()
        {
            var categories = CategoryApplication.GetMainCategory();
            var model = categories.Where(p => p.IsShow).OrderBy(c => c.DisplaySequence).Select(c => new
            {
                cid = c.Id,
                name = c.Name,
                subs = CategoryApplication.GetCategoryByParentId(c.Id).Select(a => new
                {
                    cid = a.Id,
                    name = a.Name
                })
            }).ToList();
            var result = SuccessResult<dynamic>(data: model);
            return Json(result);
        }
    }
}
