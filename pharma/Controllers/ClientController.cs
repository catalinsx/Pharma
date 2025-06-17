using Microsoft.AspNetCore.Mvc;

namespace pharma.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
