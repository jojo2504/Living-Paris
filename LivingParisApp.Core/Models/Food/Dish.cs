using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivingParisApp.Core.Models.Human;
using LivingParisApp.Core.Models.OrderInfo;

namespace LivingParisApp.Core.Models.Food {
    public class Dish : INotifyPropertyChanged {
        private string _name;
        private string _type;
        private decimal _dishPrice;
        private string _diet;
        private string _origin;

        [Key]
        public int DishID { get; set; }

        [Required]
        public int ChefID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name {
            get => _name;
            set {
                if (_name != value) {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [Required]
        [StringLength(255)]
        public string Type {
            get => _type;
            set {
                if (_type != value) {
                    _type = value;
                    OnPropertyChanged(nameof(Type));
                }
            }
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DishPrice {
            get => _dishPrice;
            set {
                if (_dishPrice != value) {
                    _dishPrice = value;
                    OnPropertyChanged(nameof(DishPrice));
                }
            }
        }

        [Required]
        public DateTime FabricationDate { get; set; }

        [Required]
        public DateTime PeremptionDate { get; set; }

        [StringLength(255)]
        public string Diet {
            get => _diet;
            set {
                if (_diet != value) {
                    _diet = value;
                    OnPropertyChanged(nameof(Diet));
                }
            }
        }

        [StringLength(255)]
        public string Origin {
            get => _origin;
            set {
                if (_origin != value) {
                    _origin = value;
                    OnPropertyChanged(nameof(Origin));
                }
            }
        }

        public virtual string ChefName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}