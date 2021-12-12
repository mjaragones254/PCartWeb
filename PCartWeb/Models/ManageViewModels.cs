using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using CompareAttribute = System.ComponentModel.DataAnnotations.CompareAttribute;

namespace PCartWeb.Models
{
    public class IndexViewModel
    {
        public bool HasPassword { get; set; }
        public IList<UserLoginInfo> Logins { get; set; }
        public string PhoneNumber { get; set; }
        public bool TwoFactor { get; set; }
        public bool BrowserRemembered { get; set; }
        public string IsActive { get; set; }
    }

    public class ProductReviewsViewModel
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public bool IsAnonymous { get; set; }
        public int Rate { get; set; }
        public string Description { get; set; }
        public string UserId { get; set; }
    }

    public class WithdrawView
    {
        public int Id { get; set; }
        public HttpPostedFileBase ReceiptFile { get; set; }
        public IList<WithdrawViewModel> ViewModel { get; set; }
    }

    public class WithdrawViewModel
    {
        public int Id { get; set; }
        public string Receipt { get; set; }
        public HttpPostedFileBase ReceiptFile { get; set; }
        [Display(Name = "Fullname")]
        public string Fullname { get; set; }
        [Display(Name = "Contact")]
        public string Contact { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Coop Name")]
        public string CoopName { get; set; }
        public int CoopId { get; set; }
        [Display(Name = "Amount Requested")]
        public decimal Amount { get; set; }
        [Display(Name = "Date Requested")]
        public string DateRequested { get; set; }
        public string RequestStatus { get; set; }
        public string Method { get; set; }
        public decimal ChargeFee { get; set; }
        public string DateFulfilled { get; set; }
    }

    public class ManageLoginsViewModel
    {
        public IList<UserLoginInfo> CurrentLogins { get; set; }
        public IList<AuthenticationDescription> OtherLogins { get; set; }
    }

    public class FactorViewModel
    {
        public string Purpose { get; set; }
    }

    public class SetPasswordViewModel
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

    public class ChangePasswordViewModel
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

    public class AddPhoneNumberViewModel
    {
        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string Number { get; set; }
    }

    public class VerifyPhoneNumberViewModel
    {
        [Required]
        [Display(Name = "Code")]
        public string Code { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }

    public class ConfigureTwoFactorViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
    }

    //Product Manage View Model
    public class AddProdImageViewModel
    {
        [Display(Name = "Upload Picture")]
        public string Product_image { get; set; }
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase ImageFile { get; set; }
    }

    public class AddDocumentImages
    {
        [Display(Name = "Upload Picture")]
        public string Document_image { get; set; }
        [Required(ErrorMessage = "Please select file.")]
        public HttpPostedFileBase ImageFile { get; set; }
    }

    public class AdminAddProductViewModel
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Product Name")]
        public string Product_name { get; set; }
        [Display(Name = "Upload Main Picture")]
        public string Product_image { get; set; }
        public HttpPostedFileBase ImageFile { get; set; }
        [Required]
        [Display(Name = "Product Description")]
        public string Product_desc { get; set; }

        [Display(Name = "Product Manufacturer")]
        public string Product_manufact { get; set; }

        [Display(Name = "Expiry Date (if applicable)")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string ExpiryDate { get; set; }

        [Required(ErrorMessage = "Please enter quantity.")]
        [Display(Name = "Qty")]
        [Range(1, int.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public int Product_qty { get; set; }

        [Display(Name = "Product Cost")]
        [Range(1, float.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public decimal Product_cost { get; set; }

        [Required]
        [Display(Name = "Selling Price")]
        [Range(1, float.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public decimal Product_price { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }
        public SelectList Categorylist { get; set; }
    }

    //Discoount View Model
    public class DiscountViewModel
    {
        public int Id { get; set; }
        public string UserID { get; set; }

        [Display(Name = "Discount Name")]
        public string Name { get; set; }

        [Display(Name = "Discount Percent")]
        public decimal Percent { get; set; }

        [Display(Name = "Discount Date to Implement")]
        public string DateStart { get; set; }

        [Display(Name = "Discount Date End")]
        public string DateEnd { get; set; }
    }

    public class ViewDiscountedProduct
    {
        public int DiscountID { get; set; }

        public int ProductId { get; set; }
    }

    //Add Category Model
    public class AddCategoryViewModel
    {
        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Category Name")]
        public string Cat_name { get; set; }

        [Required]
        [Display(Name = "Category Description")]
        public string Cat_desc { get; set; }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Category Name")]
        public string Cat_name { get; set; }

        [Display(Name = "Category Description")]
        public string Cat_desc { get; set; }
    }

    public class Viewloc
    {
        public DbGeography Geolocation { get; set; }
    }

    public class HomeDisplayModel
    {
        public bool RequestStatus { get; set; }
        public IEnumerable<ReviewDisplay> Reviews { get; set; }
        public ViewListProd Prod { get; set; }
        public IEnumerable<ExternalImages> Images { get; set; }
        public IEnumerable<ViewListProd> ListProds { get; set; }
        public IEnumerable<ProductVariations> Variations { get; set; }
        public CustomerDetailsModel CustomerDetails { get; set; }
        public EWallet CustomerEwallet { get; set; }
        public string Price { get; set; }
        public IList<CategoryDetailsModel> Categories { get; set; }
        public int Qty { get; set; }
        public bool IsSuccess { get; set; }
        public string VarDesc { get; set; }
    }

    public class ReviewDisplay
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Rate { get; set; }
        public string Description { get; set; }
        public string ProdId { get; set; }
        public string Created_at { get; set; }
    }

    public class AddImagesVariationsModel
    {
        public int ProdId { get; set; }
        public int VarId { get; set; }
        public int ImageId { get; set; }
        public IEnumerable<ExternalImages> Images { get; set; }
        public IEnumerable<ViewVariationModel> Variations { get; set; }
        [Required]
        [Display(Name = "Description")]
        public string VarDesc { get; set; }
        [Required]
        [Display(Name = "Variation Price")]
        [Range(1, Double.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public decimal VarPrice { get; set; }
    }

    public class ViewVariationModel
    {
        public int Id { get; set; }
        public string Desc { get; set; }
        public string Price { get; set; }
        public int ProdId { get; set; }
    }

    public class ViewListProd
    {
        public int Id { get; set; }
        public int ProdId { get; set; }
        public decimal DiscountPrice { get; set; }
        [Display(Name = "Delivery Fee")]
        public double Delivery_fee { get; set; }
        public string Product_image { get; set; }
        [Display(Name = "Name")]
        public string Product_name { get; set; }
        [Display(Name = "Description")]
        public string Product_desc { get; set; }
        [Display(Name = "Manufacturer")]
        public string Product_manufact { get; set; }
        [Display(Name = "Quantity")]
        public int GetQty { get; set; }

        [Display(Name = "Qty")]
        public int Product_qty { get; set; }
        [Display(Name = "Cost")]
        public decimal Product_cost { get; set; }
        [Display(Name = "Status")]
        public string Product_status { get; set; }
        [Display(Name = "Price")]
        public decimal Product_price { get; set; }
        [Display(Name = "Category")]
        public string Category { get; set; }
        [Display(Name = "Date created")]
        public string Created_at { get; set; }
        [Display(Name = "Date Updated")]
        public string Updated_at { get; set; }
        public string CustomerId { get; set; }
        public bool Wish { get; set; }
        public string CurrentId { get; set; }
        public string CoopID { get; set; }
    }

    public class ViewListVouch
    {
        public int Id { get; set; }
        [Display(Name = "Voucher Name")]
        public string Name { get; set; }
        [Display(Name = "Discount worth")]
        public decimal Percent_Discount { get; set; }
        [Display(Name = "Minimum Spend")]
        public decimal Min_spend { get; set; }
        [Display(Name = "Date Start")]
        public string DateStart { get; set; }
        [Display(Name = "Expiry Date")]
        public string ExpiryDate { get; set; }
        [Display(Name = "Date Created")]
        public DateTime Created_at { get; set; }
        [Display(Name = "Date Updated")]
        public DateTime Updated_at { get; set; }
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; }
    }

    public class ViewProdReqList
    {
        public int Id { get; set; }
        [Display(Name = "Member's Name")]
        public string CustomerName { get; set; }
        [Display(Name = "Item Name")]
        public string Product_name { get; set; }
        [Display(Name = "Image")]
        public string Image { get; set; }
        [Display(Name = "Description")]
        public string Product_desc { get; set; }
        [Display(Name = "Manufacturer")]
        public string Product_manufact { get; set; }
        [Display(Name = "Qty")]
        public int Product_qty { get; set; }
        [Display(Name = "Cost")]
        public decimal Product_cost { get; set; }
        [Display(Name = "Price")]
        public decimal Product_price { get; set; }
        [Display(Name = "Category")]
        public string Category { get; set; }
        [Display(Name = "Date created")]
        public string Created_at { get; set; }
        [Display(Name = "Date Updated")]
        public string Updated_at { get; set; }
    }

    public class CartViewMdoel
    {
        public bool isChecked { get; set; }
        public string CartId { get; set; }
        public int ProdCartId { get; set; }
        public string ProdId { get; set; }
        public int CoopId { get; set; }
        public string CoopName { get; set; }
        [Display(Name = "Item Name")]
        public string ProdName { get; set; }
        public string Image { get; set; }
        public int Qty { get; set; }
        public string VarDesc { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        [Display(Name = "Date Added")]
        public string Created_at { get; set; }
        public int VarId { get; set; }
    }

    public class ViewEditImagesVariations
    {
        public IEnumerable<ExternalImages> Images { get; set; }
        public IEnumerable<ViewVariationModel> Variations { get; set; }

        [Required]
        public string VarDesc { get; set; }
        [Required]
        [Range(1, Double.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public decimal Price { get; set; }

    }

    public class ViewCoopHomePage
    {
        public CoopDetailsModel Coop { get; set; }
        public IEnumerable<ViewListProd> Products { get; set; }
        public IEnumerable<VoucherDetailsModel> Vouchers { get; set; }
    }

    public class ViewDisplayCart
    {
        public IList<COOPShop2> coopShops { get; set; }
        public IEnumerable<ProductTotalQty> productTotals { get; set; }
        public IList<ProductsInCart> productsInCarts { get; set; }
    }

    public class COOPShop2
    {
        public int CoopID { get; set; }
        public string CoopName { get; set; }
    }

    public class ProductTotalQty
    {
        public int CoopId { get; set; }
        public string ProdId { get; set; }
        public int Qty { get; set; }
    }

    public class ProductsInCart
    {
        public int CoopId { get; set; }
        public int ProdCartId { get; set; }
        public string CartId { get; set; }
        public string ProdId { get; set; }
        public string ProdName { get; set; }
        public string Image { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
        public decimal MemberDiscountedPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class RateItemModel
    {
        public int ProdId { get; set; }
        public string Review { get; set; }
    }
    public class ViewCheckOutPage
    {
        public IList<COOPShop> coopShops { get; set; }
        public IList<UserDetails2> userDetails { get; set; }
        public IList<ProductToCheckout> Products { get; set; }
        public IList<VoucherList> VouchersList { get; set; }
        public IList<FinalCoopShops> FinalCoops { get; set; }
    }

    public class VoucherList
    {
        public int coopID { get; set; }
        public CoopVouchers Vouchers2 { get; set; }
    }


    public class CoopVouchers
    {
        public string Name { get; set; }
        public string DiscountType { get; set; }
        public decimal Percent_Discount { get; set; }
        public string UserType { get; set; }
        public string VoucherCode { get; set; }
        public string VoucherDetails { get; set; }
        public decimal Min_spend { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string VoucherDesc { get; set; }
    }

    public class CoopMembershipDetails
    {
        public CoopAdminDetailsModel CoopAdminDetails { get; set; }
        public CoopDetailsModel CoopDetails { get; set; }
        public decimal MemFee { get; set; }
    }

    public class ViewNotification
    {
        public IList<NotificationModel> Unread { get; set; }
        public IList<NotificationModel> Read { get; set; }
    }
}