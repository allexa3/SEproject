using Microsoft.AspNetCore.Mvc;
using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using System.Linq;

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

        [HttpPost]
        public IActionResult UpdateMessage(string newMessage)
        {
            if (string.IsNullOrWhiteSpace(newMessage))
            {
                TempData["Error"] = "Message cannot be empty.";
                return RedirectToAction("Index");
            }

            var message = _context.Messages.FirstOrDefault();
            if (message != null)
            {
                message.Text = newMessage;
                _context.SaveChanges();
                TempData["Success"] = "Message updated successfully!";
            }
            else
            {
                // Create new message if none exists
                _context.Messages.Add(new Message { Text = newMessage });
                _context.SaveChanges();
                TempData["Success"] = "Message created successfully!";
            }

            return RedirectToAction("Index");
        }
    }
}
 