namespace LivingParisApp.Core.Entities.Models {
    public class CartItem {
        public Dish Dish { get; set; }
        public int Quantity { get; set; }
        public string Name => Dish.Name;
        public decimal TotalPrice => Dish.DishPrice * Quantity;
    }
}