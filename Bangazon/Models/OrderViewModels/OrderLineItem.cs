namespace Bangazon.Models.OrderViewModels {
    public class OrderLineItem {
        public Product Product { get; set; }
        
        // Units, in this context == 'units to be purchased on this order'
        //      not sure where this should be set ----- probably by user when OrderProduct is Created
        public int Units { get; set; }
        public decimal Cost { 
            get {
                return (this.Units * this.Product.Price);
            } 
        }
    }
}