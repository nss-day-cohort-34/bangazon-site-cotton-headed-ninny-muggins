using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Bangazon.Models.ReportViewModels
{
    public class ProductTypeCount
    {
        [Display(Name = "Product Type")]
        public ProductType ProductType { get; set; }
        [Display(Name = "Your Incomplete Orders")]
        public int IncompleteOrderCount { get; set; }
    }
}