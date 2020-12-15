using System.Collections.Generic;

namespace shopapp.entity
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public List<ProductCategory> PruductCategories { get; set; }
    }
}