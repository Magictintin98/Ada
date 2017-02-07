﻿using System.ComponentModel.DataAnnotations;

namespace AdaWebApp.ViewModels
{
    public class IdentityLoginViewModel
    {
        [Required(ErrorMessage = "A username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "A password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool CheckMe { get; set; }

    }
}