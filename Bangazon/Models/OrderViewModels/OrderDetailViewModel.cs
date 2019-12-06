using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Bangazon.Models.OrderViewModels
{
    public class OrderDetailViewModel
    {
        public Order Order { get; set; }
        public List<OrderProduct> OrderProducts { get; set; }
        public IEnumerable<OrderLineItem> LineItems { get; set; }
        public List<PaymentType> PaymentTypes { get; set; }
        public List<SelectListItem> PaymentTypeOptions
        {
            get
            {
                return PaymentTypes?.Select(pt => new SelectListItem(pt.Description + " " + pt.AccountNumber, pt.PaymentTypeId.ToString())).ToList();
            }
        }
    }
}