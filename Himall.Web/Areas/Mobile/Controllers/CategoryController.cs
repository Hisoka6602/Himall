using Himall.Application;
using Himall.Web.Framework;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class CategoryController : BaseMobileTemplatesController
	{
		// GET: Mobile/Category
		public ActionResult Index()
		{
			var model = CategoryApplication.GetSubCategories();
			return View(model);
		}
	}
}