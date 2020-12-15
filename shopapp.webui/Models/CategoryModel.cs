using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using shopapp.entity;

namespace shopapp.webui.Models
{
    public class CategoryModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage="Kategori adı zorunludur.")]
        [StringLength(100,MinimumLength=5,ErrorMessage="Kategori ismi 5-100 karakter arasında olmalıdır.")]
        public string Name { get; set; }

        [Required(ErrorMessage="Url kısmı zorunludur.")]
        public string Url { get; set; }
        public List<Product> Products { get; set; }
    }
}