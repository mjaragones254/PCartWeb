using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCartWeb.Models
{
    public class EWallet
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string COOP_ID { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class EWalletHistory
    {
        public int ID { get; set; }
        public int EWallet_ID { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class COOPMembershipFee
    {
        public int ID { get; set; }
        public string COOP_ID { get; set; }
        public string COOP_AdminID { get; set; }
        public decimal MemFee { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class CustomerDetailsModel
    {
        public int Id { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        public string Image { get; set; }
        [Required]
        public string Contact { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Bdate { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string Role { get; set; }
        [Required]
        [Display(Name = "Date Created")]
        public DateTime Created_at { get; set; }
        [Required]
        [Display(Name = "Date Updated")]
        public DateTime Updated_at { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string AccountId { get; set; }
        public string CoopId { get; set; }
        public string CoopAdminId { get; set; }
        public string IsActive { get; set; }
        public string MemberLock { get; set; }
        public string DateAccLocked { get; set; }
        public string DateAccRetrieved { get; set; }
    }

    public class UserRequestReturn
    {
        public int Id { get; set; }
    }

    public class ImageProof
    {
        public int Id { get; set; }
        public string ImageFile { get; set; }
        [NotMapped]
        public HttpPostedFileBase MyFile { get; set; }
        public string CoopId { get; set; }
    }

    public class CustomerMembership
    {
        public int Id { get; set; }
        public string Formpath { get; set; }
        [NotMapped]
        public HttpPostedFileBase DocFile { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string UserId { get; set; }
        public string RequestStatus { get; set; }
        public virtual CoopDetailsModel Coop { get; set; }
        public string Coop_code { get; set; }
        public string Date_requested { get; set; }
        public string Date_joined { get; set; }
    }

    public class CoopMemberDiscount
    {
        public int ID { get; set; }
        [Required]
        [Display(Name = "COOP Member Discount")]
        public decimal MemberDiscount { get; set; }
        public int COOP_ID { get; set; }
        public string COOP_AdminID { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class CoopApplicationRecords
    {
        public int Id { get; set; }
        [Required]
        public string CoopName { get; set; }
        [Required]
        [Display(Name = "Coop Address")]
        public string Address { get; set; }
        [Display(Name = "Contact")]
        public string Contact { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        public string Image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        [Display(Name = "Phone Contact")]
        [DataType(DataType.PhoneNumber)]
        public string AdminContact { get; set; }
        [Required]
        public string AdminAddress { get; set; }
        [Required]
        public string Bdate { get; set; }
        public string Approval { get; set; }
        public string Email { get; set; }
        [Required]
        public DateTime Created_at { get; set; }
        public DateTime DateAnswered { get; set; }
    }
    //unsa dw error niya?
    public class CoopApplicationDocu
    {
        public int Id { get; set; }
        public virtual CoopApplicationRecords CoopRecord { get; set; }
        [Required]
        public int COOP_RecordID { get; set; }
        [Required]
        public string Document_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
    }

    public class CoopDetailsModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Coop Name")]
        public string CoopName { get; set; }
        [Required]
        [Display(Name = "Coop Address")]
        public string Address { get; set; }
        [Display(Name = "Contact")]
        public string Contact { get; set; }
        public string Approval { get; set; }
        public string IsLocked { get; set; }
        public string MembershipForm { get; set; }
        [Display(Name = "Date Created")]
        public DateTime Coop_Created { get; set; }
        [Display(Name = "Date Updated")]
        public DateTime Coop_Updated { get; set; }
        public string DateAccLocked { get; set; }
        public string DateAccRetrieved { get; set; }
    }

    public class CoopAdminDetailsModel
    {
        public int ID { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        public string Image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string Status { get; set; }
        [Required]
        [Display(Name = "Phone Contact")]
        [DataType(DataType.PhoneNumber)]
        public string Contact { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Bdate { get; set; }
        [Required]
        public DateTime Created_at { get; set; }
        [Required]
        public DateTime Updated_at { get; set; }
        public virtual ApplicationUser User { get; set; }
        [Required]
        public string UserId { get; set; }
        public virtual CoopDetailsModel Coop { get; set; }
        public int Coop_code { get; set; }
        public string Approval { get; set; }
        public string Email { get; set; }
        public string IsLocked { get; set; }
        public string IsResign { get; set; }
        public string DateResigned { get; set; }
        public string PreviousEmail { get; set; }
    }

    public class DriverDetailsModel
    {
        public int Id { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        public string Image { get; set; }
        [Required]
        public string Driver_License { get; set; }
        [Required]
        public string Contact { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string Bdate { get; set; }
        [Required]
        public string CStatus { get; set; }
        public string PlateNum { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
        public virtual ApplicationUser User { get; set; }
        [Required]
        public string UserId { get; set; }
        public virtual CoopDetailsModel Coop { get; set; }
        public string CoopId { get; set; }
        public string IsActive { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOnDuty { get; set; }
    }

    public class CoopDocumentImages
    {
        public int Id { get; set; }
        [Display(Name = "Upload Image")]
        public string Document_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string Userid { get; set; }
    }

    public class CategoryDetailsModel
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
    }

    public class ExternalImages
    {
        public int Id { get; set; }
        [Display(Name = "Upload Image")]
        public string Product_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public string ProductId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string Userid { get; set; }
    }

    public class ProductVariations
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public int ProdId { get; set; }
        public bool IsAvailable = true;
        public string Created_at { get; set; }
    }

    public class ProductDetailsModel
    {
        public int Id { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z''-'\s]{1,40}$", ErrorMessage = "Special characters are not allowed.")]
        [Display(Name = "Name")]
        public string Product_Name { get; set; }
        [Required]
        [Display(Name = "Main Picture")]
        public string Product_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }

        [Required]
        [Display(Name = "Description")]
        public string Product_desc { get; set; }

        [Required]
        [Display(Name = "Qty")]
        [Range(1, int.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public int Product_qty { get; set; }

        public int Product_sold { get; set; }

        public decimal DiscountedPrice { get; set; }

        public string Product_status { get; set; }

        public string ExpiryDate { get; set; }

        public virtual CategoryDetailsModel Category { get; set; }

        [Display(Name = "Customer's Name")]
        public int Category_Id { get; set; }

        [NotMapped]
        public SelectList Categorylist { get; set; }

        [Display(Name = "Category")]
        public string Categoryname { get; set; }

        public virtual ApplicationUser Coop { get; set; }

        public string CoopAdminId { get; set; }
        public int CoopId { get; set; }

        public virtual ApplicationUser Customer { get; set; }

        public string CustomerId { get; set; }

        [Display(Name = "Date Created")]
        public DateTime Prod_Created_at { get; set; }

        [Display(Name = "Date Updated")]
        public DateTime Prod_Updated_at { get; set; }

        public DateTime Date_ApprovalStatus { get; set; }
    }

    public class PriceTable
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public int ProdId { get; set; }
        public int VarId { get; set; }
        public string Created_at { get; set; }
    }

    public class ProductCost
    {
        public int Id { get; set; }

        public decimal Cost { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public int ProdId { get; set; }
        public string Created_at { get; set; }
    }

    public class ProductManufacturer
    {
        public int Id { get; set; }

        public string Manufacturer { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public int ProdId { get; set; }
        public string Created_at { get; set; }
    }

    public class EditProductModel
    {
        public ProductDetailsModel ProductDetailsModel { get; set; }
        public PriceTable PriceTable { get; set; }
        public ProductCost ProductCost { get; set; }
        public ProductManufacturer ProductManufacturer { get; set; }
        public IEnumerable<ProductVariations> Variations { get; set; }
    }

    public class ProductVariationModel
    {
        public int Id { get; set; }
        public string Desc { get; set; }
        public decimal Price { get; set; }
    }

    public class ProductDisplayModel
    {
        public int Id { get; set; }

        public string Product_Name { get; set; }

        public string Product_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }

        public string Product_desc { get; set; }

        public int Product_qty { get; set; }

        public int Product_sold { get; set; }
        public decimal Product_price { get; set; }

        public decimal DiscountedPrice { get; set; }

        public string Product_status { get; set; }

        public string ExpiryDate { get; set; }

        public int Category_Id { get; set; }

        [NotMapped]
        public SelectList Categorylist { get; set; }

        public string Categoryname { get; set; }


        public string CoopId { get; set; }

        public string CustomerId { get; set; }

        public DateTime Prod_Created_at { get; set; }

        public DateTime Prod_Updated_at { get; set; }

        public DateTime Date_ApprovalStatus { get; set; }
    }



    public class ProductDiscount
    {
        [Required]
        public DiscountModel Discount { get; set; }
        public IEnumerable<ProductDetailsModel> Product { get; set; }
        public IEnumerable<PriceTable> Price { get; set; }
        public IEnumerable<ProductCost> Cost { get; set; }
        public IEnumerable<ProductManufacturer> Manufacturer { get; set; }
    }

    public class EditProductDiscount
    {
        public DiscountModel Discount { get; set; }
        public IEnumerable<ProductDetailsModel2> Product { get; set; }
        public IEnumerable<PriceTable> Price { get; set; }
        public IEnumerable<ProductCost> Cost { get; set; }
        public IEnumerable<ProductManufacturer> Manufacturer { get; set; }
    }

    public class ProductDetailsModel2
    {
        public bool isChecked { get; set; }
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Product_Name { get; set; }
        [Required]
        [Display(Name = "Main Picture")]
        public string Product_image { get; set; }
        [NotMapped]
        public HttpPostedFileBase ImageFile { get; set; }

        [Required]
        [Display(Name = "Qty")]
        [Range(1, int.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public int Product_qty { get; set; }
        public decimal DiscountedPrice { get; set; }

        public int Product_sold { get; set; }

        public string ExpiryDate { get; set; }
    }

    public class DiscountModel
    {
        public int Id { get; set; }

        [Required]
        public string UserID { get; set; }
        public int CoopID { get; set; }

        [Required]
        [Display(Name = "Discount Name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Discount Percent")]
        [Range(1, float.MaxValue, ErrorMessage = "Enter numbers starting from 1 and above")]
        public decimal Percent { get; set; }

        [Required]
        [Display(Name = "Discount Date Start")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string DateStart { get; set; }

        [Required]
        [Display(Name = "Discount Date End")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string DateEnd { get; set; }
    }

    public class DiscountedProduct
    {
        public int Id { get; set; }

        [Required]
        public int DiscountID { get; set; }

        [Required]
        public int ProductId { get; set; }
    }

    public class VoucherDetailsModel
    {
        public int Id { get; set; }
        public string VoucherCode { get; set; }
        [Required]
        [Display(Name = "Voucher Name")]
        public string Name { get; set; }
        [Required]
        [Display(Name = "Discount worth")]
        public decimal Percent_Discount { get; set; }
        [Required]
        [Display(Name = "Minimum Spend")]
        public decimal Min_spend { get; set; }
        [Display(Name = "Date Start")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string DateStart { get; set; }
        [Display(Name = "Expiry Date")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:dd/MM/yyyy}")]
        public string ExpiryDate { get; set; }
        [Required]
        [Display(Name = "Date Created")]
        public DateTime Created_at { get; set; }
        [Required]
        [Display(Name = "Date Updated")]
        public DateTime Updated_at { get; set; }
        [Display(Name = "Discount Type")]
        public string DiscountType { get; set; }
        [NotMapped]
        public SelectList DiscType { get; set; }
        [Display(Name = "For User")]
        public string UserType { get; set; }
        [NotMapped]
        public SelectList UserTypeList { get; set; }
        public virtual ApplicationUser Coop { get; set; }
        public string CoopAdminId { get; set; }
        public int CoopId { get; set; }
        public virtual ApplicationUser Customer { get; set; }
        public string CustomerId { get; set; }
    }

    public class CommissionTable
    {
        public int Id { get; set; }
        [Display(Name = "Rate")]
        [Required]
        public decimal Rate { get; set; }
        public DateTime Updated_at { get; set; }
    }

    public class WishList
    {
        public int Id { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public string ProductId { get; set; }
        public bool Favorite { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string UserId { get; set; }
        public DateTime Created_at { get; set; }
    }

    public class UserCart
    {
        public int Id { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string UserId { get; set; }
    }

    public class ProductCart
    {
        public int Id { get; set; }
        public int Qty { get; set; }
        public int VarId { get; set; }
        public string Variation { get; set; }
        public virtual UserCart Cart { get; set; }
        public string CartId { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public string ProductId { get; set; }
        public DateTime Created_at { get; set; }

    }

    public class CoopView
    {
        public int Id { get; set; }
        [Display(Name = "Coop Name")]
        public string CoopName { get; set; }
        [Display(Name = "Coop Address")]
        public string Address { get; set; }
        [Display(Name = "Contact")]
        public string Contact { get; set; }
        public string Approval { get; set; }
        public string MembershipForm { get; set; }
        [Display(Name = "Date Created")]
        public DateTime Coop_Created { get; set; }
        [Display(Name = "Date Updated")]
        public DateTime Coop_Updated { get; set; }
    }

    public class COOPShop
    {
        public string CoopAdminId { get; set; }
        public string CoopID { get; set; }
        public string CoopName { get; set; }
        public double? Delivery { get; set; }
        public decimal TotalEach { get; set; }
        public decimal? discountedTotalPrice { get; set; }
        [Display(Name = "Voucher Code")]
        public string voucherUsed { get; set; }
        public string VoucherCode { get; set; }
        public string VoucherError { get; set; }
    }

    public class FinalCoopShops
    {
        public string CoopID { get; set; }
        public string CoopName { get; set; }
        public double? Delivery { get; set; }
        public decimal TotalEach { get; set; }
        public decimal? discountedTotalPrice { get; set; }
        [Display(Name = "Voucher Code")]
        public string voucherUsed { get; set; }
        public string VoucherCode { get; set; }
    }

    public class CoopPlaceOrder
    {
        public string CoopID { get; set; }
        public double? Delivery { get; set; }
        public string MOP { get; set; }
    }

    public class CoopProdOrder
    {
        public string CoopID2 { get; set; }
        public double? Delivery_fee { get; set; }
        public string ProdCartId { get; set; }
        public string CartId { get; set; }
        public string ProdId { get; set; }
        public string ProdName { get; set; }
        public string Image { get; set; }
        public string Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
        public string Created_at { get; set; }
        public string Review { get; set; }
        [Range(1, 5, ErrorMessage = "Enter numbers starting from 1 and above")]
        public int Rate { get; set; }
        public bool IsRated { get; set; }
        public string UOrderId { get; set; }
        public string ProdOrderId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDetails2
    {
        public string Userid { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class ProductToCheckout
    {
        public string Userid { get; set; }
        public string CoopID2 { get; set; }
        public double? Delivery_fee { get; set; }
        public string ProdCartId { get; set; }
        public string CartId { get; set; }
        public string ProdId { get; set; }
        public string ProdName { get; set; }
        public string Image { get; set; }
        public string Qty { get; set; }
        public decimal Price { get; set; }
        public decimal MemberDiscountedPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal Subtotal { get; set; }
        public string Created_at { get; set; }
    }

    public class UserCheckout
    {
        public int Id { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string UserId { get; set; }
        public string CoopId { get; set; }
    }

    public class ProdCheckout
    {
        public int Id { get; set; }
        public int Qty { get; set; }
        public virtual UserCheckout Checkout { get; set; }
        public string OrderId { get; set; }
        public virtual ProductDetailsModel Product { get; set; }
        public string ProductId { get; set; }
        public DateTime Created_at { get; set; }
        public decimal PartialTotal { get; set; }
    }

    public class UserOrder
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string OrderCreated_at { get; set; }
        public string OStatus { get; set; }
        public string ModeOfPay { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal CommissionFee { get; set; }
        public double? Delivery_fee { get; set; }
    }

    public class OrderCancel
    {
        public int ID { get; set; }
        public int UserOrder_ID { get; set; }
        public string CancelledBy { get; set; }
        public string Reason { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class DeliveryStatus
    {
        public int Id { get; set; }
        public int UserOrderId { get; set; }
        public string DriverId { get; set; }
        public string PickUpDate { get; set; }
        public DateTime? PickUpSuccessDate { get; set; }
        public DateTime? ExpectedDeldate { get; set; }
        public DateTime? DateDelivered { get; set; }
        public string Status { get; set; }
        public string ReturnedReason { get; set; }
        public string Proof { get; set; }
    }

    public class ProdOrder
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string UOrderId { get; set; }
        public string ProdName { get; set; }
        public string Variation { get; set; }
        public decimal Price { get; set; }
        public decimal MemberDiscountedPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string ProdId { get; set; }
        public int Qty { get; set; }
        public decimal SubTotal { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public DateTime Created_at { get; set; }
        public DbGeography Geolocation { get; set; }
        public virtual ApplicationUser User { get; set; }
        public string UserId { get; set; }
    }

    public class CoopLocation
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public DateTime Created_at { get; set; }
        public DbGeography Geolocation { get; set; }
        public virtual CoopDetailsModel Coop { get; set; }
        public string CoopId { get; set; }
    }

    public class UserLogs
    {
        public int Id { get; set; }
        public string Role { get; set; }
        public string Logs { get; set; }
        public string Date { get; set; }
    }

    public class UserVoucherUsed
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string UserOrderId { get; set; }
        public string VoucherCode { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class NotificationModel
    {
        public int Id { get; set; }
        public string ToRole { get; set; }
        public string ToCOOP_ID { get; set; }
        public string ToUser { get; set; }
        public string NotifFrom { get; set; }
        public string NotifHeader { get; set; }
        public string NotifMessage { get; set; }
        public string NavigateURL { get; set; }
        public bool IsRead { get; set; }
        public DateTime DateReceived { get; set; }
    }

    public class CoopChat
    {
        public int Id { get; set; }
        public string CoopId { get; set; }
        public string UserId { get; set; }
    }

    public class ChatMessage
    {
        public int Id { get; set; }
        public int CoopChatId { get; set; }
        public string From { get; set; }
        public string MessageBody { get; set; }
        public DateTime DateSent { get; set; }
        public bool IsRead { get; set; }
    }

    public class UserHubModels
    {
        public string UserName { get; set; }
        public HashSet<string> ConnectionIds { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }
        public string Desc { get; set; }
        public int Rate { get; set; }
        public int ProdId { get; set; }
        public int ProdOrderId { get; set; }
        public string UserId { get; set; }
        public string Created_at { get; set; }
        public bool IsAnonymous { get; set; }
    }
    public class WithdrawRequest
    {
        public int Id { get; set; }
        public int CoopId { get; set; }
        public string Fullname { get; set; }
        public string Contact { get; set; }
        public string Method { get; set; }
        public string Email { get; set; }
        public decimal Amount { get; set; }
        public decimal ChargeFee { get; set; }
        public string Receipt { get; set; }
        [NotMapped]
        public HttpPostedFileBase ReceiptFile { get; set; }
        public string RequestStatus { get; set; }
        public string DateReqeuested { get; set; }
        public string DateFulfilled { get; set; }
    }
    public class Complaints
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Category { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public string PostFile { get; set; }
        public string Status { get; set; }
        public string DateCreated { get; set; }
        public string DateUpdated { get; set; }
    }

    public class ReturnRefund
    {
        public int Id { get; set; }
        public int UserOrderId { get; set; }
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime? DateRefunded { get; set; }
        public bool IsAccepted { get; set; }
        public DateTime? DateAccepted { get; set; }
        public DateTime Created_At { get; set; }
    }

    public class ReturnRefundItem
    {
        public int Id { get; set; }
        public int ReturnId { get; set; }
        public int ProdOrderId { get; set; }
    }

    public class CommissionSale
    {
        public int Id { get; set; }
        public int CoopCode { get; set; }
        public string CoopAdminId { get; set; }
        public decimal CommissionFee { get; set; }
        public int UserOrderID { get; set; }
        public string Status { get; set; }
        public DateTime Created_at { get; set; }
        public DateTime Updated_at { get; set; }
    }

    public class AccountsReceived
    {
        public int Id { get; set; }
        public decimal TotalAmount { get; set; }
        public int CoopId { get; set; }
        public string AccountId { get; set; }
        public string Receipt { get; set; }
        public string ModeOfPayment { get; set; }
        public DateTime Created_at { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Contact { get; set; }
    }
}