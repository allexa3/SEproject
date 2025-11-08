using Microsoft.AspNetCore.Mvc;
using HelloWorldMVC.Data;

namespace HelloWorldMVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var message = _context.Messages.FirstOrDefault();
            return View(model: message?.Text ?? "No message found");
        }
    }
}
