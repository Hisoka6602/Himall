using Himall.Service;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class InviteController : Controller
    {
        private MemberInviteService _iMemberInviteService;
        public InviteController(MemberInviteService MemberInviteService)
        {
            _iMemberInviteService = MemberInviteService;
        }
        // GET: Web/Invite
        public ActionResult Index()
        {
            var rule = _iMemberInviteService.GetInviteRule();
            return View(rule);
        }
    }
}