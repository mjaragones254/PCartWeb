using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace PCartWeb.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    public class UserInfoViewModel
    {
        public string Email { get; set; }

        public bool HasRegistered { get; set; }

        public string LoginProvider { get; set; }
    }
    public class CoopAdminViewModel
    {
        public CoopFormModel Forms { get; set; }
        public string Coop_code { get; set; }
        public decimal MembershipFee { get; set; }
        public string MembershipForm { get; set; }
        public HttpPostedFileBase DocFile { get; set; }
        [Display(Name = "Profile")]
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }
        [Display(Name = "Coop Name")]
        public string CoopName { get; set; }
        [Display(Name = "Coop Address")]
        public string CoopAddress { get; set; }
        [Display(Name = "Coop Contact")]
        public string CoopContact { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Firstname")]
        public string Firstname { get; set; }

        [Display(Name = "Lastname")]
        public string Lastname { get; set; }

        [Display(Name = "Phone Contact")]
        public string Contact { get; set; }

        [Display(Name = "Home Address")]
        public string Address { get; set; }

        [Display(Name = "Birthdate")]
        public string Bdate { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [Display(Name = "Marital Status")]
        public string CStatus { get; set; }
    }
    public class ManageInfoViewModel
    {
        public string LocalLoginProvider { get; set; }

        public string Email { get; set; }

        public IEnumerable<UserLoginInfoViewModel> Logins { get; set; }

        public IEnumerable<ExternalLoginViewModel> ExternalLoginProviders { get; set; }
    }
    public class SetPasswordBindingModel
    {
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class RegisterExternalBindingModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
    public class RegisterBindingModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class RemoveLoginBindingModel
    {
        [Required]
        [Display(Name = "Login provider")]
        public string LoginProvider { get; set; }

        [Required]
        [Display(Name = "Provider key")]
        public string ProviderKey { get; set; }
    }

    public class AddExternalLoginBindingModel
    {
        [Required]
        [Display(Name = "External access token")]
        public string ExternalAccessToken { get; set; }
    }
    public class ChangePasswordBindingModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
    public class ExternalLoginViewModel
    {
        public string Name { get; set; }

        public string Url { get; set; }

        public string State { get; set; }
    }
    public class UserLoginInfoViewModel
    {
        public string LoginProvider { get; set; }

        public string ProviderKey { get; set; }
    }
    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "Remember this browser?")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Firstname")]
        public string Firstname { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Lastname")]
        public string Lastname { get; set; }
        [Display(Name = "Upload Profile")]
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }

        [Required]
        [Display(Name = "Contact No.")]
        [DataType(DataType.PhoneNumber)]
        [MaxLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [MinLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Contact No. must be numeric.")]
        public string Contact { get; set; }

        [Required]
        [Display(Name = "Home Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Birthdate")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string Bdate { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; }
        public System.Web.Mvc.SelectList GenderList { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Longitude { get; set; }
        public string Latitude { get; set; }
        [Display(Name = "Active Status")]
        public string IsActive { get; set; }
        public System.Web.Mvc.SelectList SelectStatus { get; set; }
    }

    public class RegisterUserAdminViewModel
    {
        public string MembershipForm { get; set; }
        public HttpPostedFileBase DocFile { get; set; }
        [Display(Name = "Upload Profile")]
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Firstname")]
        public string Firstname { get; set; }

        [Required]
        [Display(Name = "Lastname")]
        public string Lastname { get; set; }

        [Required]
        [Display(Name = "Phone Contact")]
        [DataType(DataType.PhoneNumber)]
        public string Contact { get; set; }

        [Required]
        [Display(Name = "Home Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Birthdate")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string Bdate { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }
        public System.Web.Mvc.SelectList GenderList { get; set; }

        [Display(Name = "Marital Status")]
        public string CStatus { get; set; }
        public System.Web.Mvc.SelectList CStatusList { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public bool RememberMe { get; set; }
    }

    public class RegisterCoopAdminViewmodel
    {
        public string MembershipForm { get; set; }
        public HttpPostedFileBase DocFile { get; set; }
        [Display(Name = "Upload Profile")]
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }
        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Co-op Name")]
        public string CoopName { get; set; }
        [Required]
        [Display(Name = "Co-op Address")]
        public string CoopAddress { get; set; }
        [Required]
        [Display(Name = "Co-op Contact No.")]
        [DataType(DataType.PhoneNumber)]
        [MaxLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [MinLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Contact No. must be numeric.")]
        public string CoopContact { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Firstname")]
        public string Firstname { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Lastname")]
        public string Lastname { get; set; }

        [Required]
        [Display(Name = "Contact No.")]
        [DataType(DataType.PhoneNumber)]
        [MaxLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [MinLength(11, ErrorMessage = "Invalid contact no. Kindly enter a valid one.")]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Contact No. must be numeric.")]
        public string Contact { get; set; }

        [Required]
        [Display(Name = "Home Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Birthdate")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string Bdate { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }
        public System.Web.Mvc.SelectList GenderList { get; set; }

        [Display(Name = "Marital Status")]
        public string CStatus { get; set; }
        public System.Web.Mvc.SelectList CStatusList { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Longitude { get; set; }
        public string Latitude { get; set; }
        public string Longitude1 { get; set; }
        public string Latitude1 { get; set; }
        public bool RememberMe { get; set; }
    }

    public class RegisterDriverViewModel
    {
        [Display(Name = "Upload Profile")]
        public string Image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Firstname")]
        public string Firstname { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Lastname")]
        public string Lastname { get; set; }

        [Display(Name = "Driver's License")]
        public string Driver_License { get; set; }
        public HttpPostedFileBase DriverFile { get; set; }

        [Required]
        [Display(Name = "Plate Number")]
        public string PlateNum { get; set; }

        [Required]
        [Display(Name = "Phone Contact")]
        [DataType(DataType.PhoneNumber)]
        public string Contact { get; set; }

        [Required]
        [Display(Name = "Home Address")]
        public string Address { get; set; }

        [Required]
        [Display(Name = "Birthdate")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string Bdate { get; set; }

        [Required]
        [Display(Name = "Gender")]
        public string Gender { get; set; }
        public System.Web.Mvc.SelectList GenderList { get; set; }
        [Required]
        [Display(Name = "Civil Status")]
        public string CStatus { get; set; }
        public System.Web.Mvc.SelectList CStatuslist { get; set; }

        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Longitude { get; set; }
        public string Latitude { get; set; }
        [Display(Name = "Active Status")]
        public string ActiveStatus { get; set; }
        public System.Web.Mvc.SelectList SelectStatus { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}
