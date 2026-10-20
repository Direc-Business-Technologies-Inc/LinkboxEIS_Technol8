using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DomainLayer.ViewModels
{
    public class Authorizations
    {
        public List<AuthorizationViewModel> AuthorizationsList { get; set; }
        public AuthorizationViewModel CreateAuthorization { get; set; }
        public class AuthorizationViewModel
        {
            public int AuthId { get; set; }
            [Required(ErrorMessage ="Please Enter Authorization Description")]
            public string Description { get; set; }
            public bool IsActive { get; set; }
        }

        public List<Modules> ModulesList { get; set; }
        public class Modules
        {
            public int ModId { get; set; }
            public string ModName { get; set; }
            public string ModType { get; set; }
            public string ModCode { get; set; }
        }

        public List<AuthorizationModules> AuthorizationModulesList { get; set; }
        public class AuthorizationModules
        {
            public int AMId { get; set; }
            public int ModId { get; set; }
            public int AuthId { get; set; }
            public string AuthName { get; set; }
            public string ModuleName { get; set; }
            public string ModuleCode { get; set; }
            public string ModType { get; set; }
            public bool IsActive { get; set; }

        }

    }

    public class UserDetailsViewModel
    {
        public List<AuthorizationViewModel> Auths { get; set; }
        public class AuthorizationViewModel
        {
            public int AuthId { get; set; }
            public string Description { get; set; }
        }
        public bool IsAdmin { get; set; }
        public List<UserViewModel> CreateUser { get; set; }
        public UserViewModel UpdateUser { get; set; }
        public class UserViewModel
        {
            public int UserId { get; set; }
            [Required(ErrorMessage = "Please Enter User Name")]
            //[RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Special characters and white spaces are not allowed")]
            public string UserName { get; set; }
            [Required(ErrorMessage = "Please Enter First Name")]
            public string FirstName { get; set; }
            [Required(ErrorMessage = "Please Enter Last Name")]
            public string LastName { get; set; }
            public string MiddleName { get; set; }
            [Required(ErrorMessage = "Please Enter Password")]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).+$",
                 ErrorMessage = "Password must contain uppercase, lowercase, a number, and a special character.")]
            public string Password { get; set; }
            public string UserAPIKey { get; set; }

            [Required(ErrorMessage = "Please confirm your password.")]
            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }
            public string LastPassword { get; set; }
            public bool IsActive { get;set; }
            [Required(ErrorMessage = "Please Select Authorization")]
            public int AuthorizationId { get; set; }
            public string AuthorizationName { get; set; }
        }
        public class UpdateUserViewModel
        {
            public int UserId { get; set; }

            [Required(ErrorMessage = "Please Enter User Name")]
            [RegularExpression(@"^[a-zA-Z0-9]+$", ErrorMessage = "Special characters and white spaces are not allowed")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Please Enter First Name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "Please Enter Last Name")]
            public string LastName { get; set; }

            public string MiddleName { get; set; }

            // Optional on update — user isn't always changing their password
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
            [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).+$",
                ErrorMessage = "Password must contain uppercase, lowercase, a number, and a special character.")]
            public string Password { get; set; }

            [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
            public string ConfirmPassword { get; set; }

            public string UserAPIKey { get; set; }
            public string LastPassword { get; set; }
            public bool IsActive { get; set; }

            [Required(ErrorMessage = "Please Select Authorization")]
            public int AuthorizationId { get; set; }

            public string AuthorizationName { get; set; }
        }
        public class isAdmin
        {
            public bool _isAdmin { get; set; }

        }
        public List<PasswordHistoryViewModel> PasswordHistoryList { get; set; }

        public PasswordHistoryViewModel PasswordHistory { get; set; }

        public class PasswordHistoryViewModel
        {
            public string Id { get; set; }
            public string UserId { get; set; }
            public string PasswordHash { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public List<Modules> ModulesList { get; set; }
        public class Modules
        {
            public int ModId { get; set; }
            public string ModName { get; set; }
            public string ModType { get; set; }
            public string ModCode { get; set; }
        }

        public List<UserModules> UserModuleList { get; set; }
        public class UserModules
        {
            public int UMId { get; set; }
            public int ModId { get; set; }
            public string ModuleCode { get; set; }
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string ModuleName { get; set; }
            public string ModType { get; set; }
            public bool IsActive { get; set; }

        }

    }

    public class UserAuthorizationsViewModel
    {
        public List<UserAuthorizations> UserAuthorizationList { get; set; }
        public class UserAuthorizations
        {
            public int Id { get; set; }

            public int AuthId { get; set; }
            public string Description { get; set; }
            //public bool authIsActive { get; set; }

            public int UserId { get; set; }
            public string UserName { get; set; }
            //public bool userIsActive { get; set; }

            public bool IsActive { get; set; }
        }

        public List<UserViewModel> Users { get; set; }
        public class UserViewModel
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
        }

        public List<AuthorizationViewModel> Auths { get; set; }
        public class AuthorizationViewModel
        {
            public int AuthId { get; set; }
            public string Description { get; set; }
        }
    }
}