using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivingParisApp.Core.Models.Human;

namespace LivingParisApp.Core.Models.OrderInfo {
    public class Order : INotifyPropertyChanged {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int ChefID { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public DateTime OrderDate { get; set; } // Add this to match the database schema
        public decimal OrderTotal { get; set; } // Add this to match the database schema

        private string _status;
        public string Status {
            get => _status;
            set {
                if (_status != value) {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        // Properties for display purposes (populated manually)
        public string ClientName { get; set; }
        public string ChefName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}