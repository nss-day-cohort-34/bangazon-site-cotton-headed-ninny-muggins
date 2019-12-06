using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Bangazon.Models.ReportViewModels
{
    public class UserOrderCount
    {
        public ApplicationUser User { get; set; }
        [Display(Name = "Open Orders")]
        public int OpenOrderNumber { get; set; }
    }
}