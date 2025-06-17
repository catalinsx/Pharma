using Microsoft.AspNetCore.Mvc;

namespace pharma.Controllers
{
    public class MedicamentController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
