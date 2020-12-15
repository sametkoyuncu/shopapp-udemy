using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using shopapp.entity;

namespace shopapp.webui.Models
{
    public class ProductModel
    {
        public int Id { get; set; }

        // [Display(Name="Product Name", Prompt="Enter product name")]
        // [Required(ErrorMessage="Name zorunlu bir alan.")]
        // [StringLength(60,MinimumLength=5, ErrorMessage="Ürün ismi 5-60 karakter aralığında olmalıdır.")]
        public string Name { get; set; }

        // [Required(ErrorMessage="Url zorunlu bir alan.")]
        public string Url { get; set; }

        // [Required(ErrorMessage="Price zorunlu bir alan.")]
        // [Range(1,20000,ErrorMessage="Price için 1-20000 arasında bir değer girmelisiniz.")]
        public double? Price { get; set; }

        // [Required(ErrorMessage="Description zorunlu bir alan.")]
        public string Description { get; set; }

        // [Display(Name="Image Url")]
        // [Required(ErrorMessage="Image Url zorunlu bir alan.")]    
        public string ImageUrl { get; set; }
        public bool IsApproved { get; set; }
        public bool IsHome { get; set; } 
        public List<Category> SelectedCategories { get; set; }

    }
}