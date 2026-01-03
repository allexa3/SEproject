using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HelloWorldMVC.Models;

public sealed class ImageUploadViewModel
{
    [Required(ErrorMessage = "Please choose an image file.")]
    [Display(Name = "Image")]
    public IFormFile? File { get; set; }
}


