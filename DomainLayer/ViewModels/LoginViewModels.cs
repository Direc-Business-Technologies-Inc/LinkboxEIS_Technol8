using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DomainLayer.ViewModels
{
    public class LoginViewModels
    {
        public List<Login> Credentials { get; set; }
        public class Login
        {
            public int userId { get; set; }

            [Required(ErrorMessage = "User Name is required")]
            public string userName { get; set; }

            [Required(ErrorMessage = "Password is required")]
            public string password { get; set; }
            public int attempt { get; set; }
            public bool isActive { get; set; }
            public DateTime lastLoginDate { get; set; }

        }
    }
}