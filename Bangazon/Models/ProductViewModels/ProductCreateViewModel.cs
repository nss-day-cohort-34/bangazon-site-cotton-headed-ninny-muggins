using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bangazon.Models.ProductViewModels
{
    public class ProductCreateViewModel
    {

        public Product Product { get; set; }
       
        public List<ProductType> ProductTypes { get; set; }

        public List<SelectListItem> ProductTypeOptions
        {
            get
            {
                return ProductTypes?.Select(pt => new SelectListItem(pt.Label, pt.ProductTypeId.ToString())).ToList();
            }
        }
    }
}
