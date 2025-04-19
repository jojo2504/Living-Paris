using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LivingParisApp.Core.Models.Human {
    public class User : INotifyPropertyChanged {
        private int _userID;
        private string _lastName;
        private string _firstName;
        private string _street;
        private int _streetNumber;
        private string _postcode;
        private string _city;
        private string _phoneNumber;
        private string _mail;
        private string _closestMetro;
        private string _password;
        private int _isClient;
        private int _isChef;

        [Key]
        public int UserID {
            get => _userID;
            set {
                if (_userID != value) {
                    _userID = value;
                    OnPropertyChanged(nameof(UserID));
                }
            }
        }

        [Required]
        [StringLength(255)]
        public string LastName {
            get => _lastName;
            set {
                if (_lastName != value) {
                    _lastName = value;
                    OnPropertyChanged(nameof(LastName));
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        [Required]
        [StringLength(255)]
        public string FirstName {
            get => _firstName;
            set {
                if (_firstName != value) {
                    _firstName = value;
                    OnPropertyChanged(nameof(FirstName));
                    OnPropertyChanged(nameof(FullName));
                }
            }
        }

        [Required]
        [StringLength(255)]
        public string Street {
            get => _street;
            set {
                if (_street != value) {
                    _street = value;
                    OnPropertyChanged(nameof(Street));
                }
            }
        }

        [Required]
        public int StreetNumber {
            get => _streetNumber;
            set {
                if (_streetNumber != value) {
                    _streetNumber = value;
                    OnPropertyChanged(nameof(StreetNumber));
                }
            }
        }

        [Required]
        [StringLength(5)]
        public string Postcode {
            get => _postcode;
            set {
                if (_postcode != value) {
                    _postcode = value;
                    OnPropertyChanged(nameof(Postcode));
                }
            }
        }

        [Required]
        [StringLength(255)]
        public string City {
            get => _city;
            set {
                if (_city != value) {
                    _city = value;
                    OnPropertyChanged(nameof(City));
                }
            }
        }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PhoneNumber {
            get => _phoneNumber;
            set {
                if (_phoneNumber != value) {
                    _phoneNumber = value;
                    OnPropertyChanged(nameof(PhoneNumber));
                }
            }
        }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Mail {
            get => _mail;
            set {
                if (_mail != value) {
                    _mail = value;
                    OnPropertyChanged(nameof(Mail));
                }
            }
        }

        [StringLength(255)]
        public string ClosestMetro {
            get => _closestMetro;
            set {
                if (_closestMetro != value) {
                    _closestMetro = value;
                    OnPropertyChanged(nameof(ClosestMetro));
                }
            }
        }

        [Required]
        [StringLength(50)]
        public string Password {
            get => _password;
            set {
                if (_password != value) {
                    _password = value;
                    OnPropertyChanged(nameof(Password));
                }
            }
        }

        [Required]
        public int IsClient {
            get => _isClient;
            set {
                if (_isClient != value) {
                    _isClient = value;
                    OnPropertyChanged(nameof(IsClient));
                    OnPropertyChanged(nameof(Roles));
                }
            }
        }

        [Required]
        public int IsChef {
            get => _isChef;
            set {
                if (_isChef != value) {
                    _isChef = value;
                    OnPropertyChanged(nameof(IsChef));
                    OnPropertyChanged(nameof(Roles));
                }
            }
        }

        // Computed property for FullName
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Computed property for Roles
        public string Roles {
            get {
                var roles = new List<string>();
                if (IsClient == 1) roles.Add("Client");
                if (IsChef == 1) roles.Add("Chef");
                return string.Join(", ", roles);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}