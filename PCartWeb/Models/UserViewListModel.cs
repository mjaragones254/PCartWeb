using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PCartWeb.Models
{
    public class MemberViewListModel
    {
        public int Id { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public decimal Ewallet { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public string Bdate { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        [Display(Name = "Date Created")]
        public DateTime Created_at { get; set; }
        [Display(Name = "Date Updated")]
        public DateTime Updated_at { get; set; }
        public string AccountId { get; set; }
        public string CoopId { get; set; }
    }

    public class AccountPayablesRepModel
    {
        public IList<CommissionSale> CommissionSale { get; set; }
        public string DueDate { get; set; }
        public string Status { get; set; }
        public string Receipt { get; set; }
        public HttpPostedFileBase ReceiptFile { get; set; }
        public decimal TotalTobePay { get; set; }
        public string Mode { get; set; }
        public bool IsSucess { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Contact { get; set; }
    }

    public class CompalintsModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Fullname { get; set; }
        public string Category { get; set; }
        public string Reason { get; set; }
        public string Description { get; set; }
        public string PostFile { get; set; }
        public HttpPostedFileBase File { get; set; }
        public string Status { get; set; }
        public string DateCreated { get; set; }
        public string DateUpdated { get; set; }
    }

    public class ViewApplicantsModel
    {
        public int Id { get; set; }
        [Display(Name = "Fullname")]
        public string Fullname { get; set; }
        [Display(Name = "Membership Form")]
        public string Filepath { get; set; }
    }

    public class CoopViewModel
    {
        public int Id { get; set; }
        public int Userid { get; set; }
        [Display(Name = "Profile")]
        public string Image { get; set; }

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

        [Required]
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

        [Display(Name = "Civil Status")]
        public string CStatus { get; set; }
        [Display(Name = "Date Registered")]
        public string Created_at { get; set; }

        [Display(Name = "Date Updated")]
        public string Updated_at { get; set; }
        public string AccntId { get; set; }
        public string Approval { get; set; }
    }

    public class SuperAdmin
    {
        public decimal Ewallet { get; set; }
        public string Cooperatives { get; set; }
        public string Commission { get; set; }

    }

    public class HomeViewModel
    {
        public IEnumerable<ViewListProd> ListProds { get; set; }
        public IEnumerable<CategoryDetailsModel> Categories { get; set; }
    }

    public class CoopFormModel
    {
        public decimal MembershipFee { get; set; }
        public decimal MembersDiscount { get; set; }
        public string MembershipForm { get; set; }
        public HttpPostedFileBase DocFile { get; set; }
    }
    public class UserViewModel
    {
        public CoopFormModel Forms { get; set; }
        public int Id { get; set; }
        public string AccountId { get; set; }
        [Display(Name = "Firstname")]
        public string Firstname { get; set; }
        [Display(Name = "Profile Picture")]
        public string Image { get; set; }
        [Display(Name = "Lastname")]
        public string Lastname { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        [Display(Name = "Birth Date")]
        public string Bdate { get; set; }
        public string Gender { get; set; }
        public string Role { get; set; }
        [Display(Name = "Date Created")]
        public string Created_at { get; set; }
        [Display(Name = "Date Updated")]
        public string Updated_at { get; set; }
        public string Email { get; set; }
        public string IsActive { get; set; }
        public decimal MembershipFee { get; set; }
        public string MembershipForm { get; set; }
        public HttpPostedFileBase DocFile { get; set; }
        public decimal Ewallet { get; set; }
        public int ProductNum { get; set; }
        public int MembersNum { get; set; }
        public decimal Commission { get; set; }
        public decimal MemberDiscount { get; set; }
    }

    public class DriverViewList
    {
        public int Id { get; set; }
        public string AccountId { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Created_at { get; set; }
        public string Updated_at { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }

    public class ViewUserDetails
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Address { get; set; }
        public string Contact { get; set; }
        public string Created { get; set; }
        public string Updated { get; set; }
    }

    public class OrderList
    {
        public int Id { get; set; }
        [Display(Name = "Order No")]
        public string OrderNo { get; set; }
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }
        [Display(Name = "Delivery Fee")]
        public string Delivery_fee { get; set; }
        public string ModeOfPay { get; set; }
    }
    public class ProdOrder2
    {
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string UOrderId { get; set; }
        public string ProdImage { get; set; }
        public string ProdName { get; set; }
        public decimal Price { get; set; }
        public decimal MemberDiscountedPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string ProdId { get; set; }
        public int Qty { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class UserVoucherUsed2
    {
        public string UserId { get; set; }
        public int CoopId { get; set; }
        public string UserOrderId { get; set; }
        public string VoucherCode { get; set; }
        public string DiscountType { get; set; }
        public decimal Discount { get; set; }
        public string VoucherUsed { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class ViewCustomerOrder
    {
        public HttpPostedFileBase ImageFile { get; set; }
        public UserOrder UserOrders { get; set; }
        public OrderCancel CancelOrder { get; set; }
        public DeliveryStatus2 DeliveryDetails { get; set; }
        public Location CustomerAddress { get; set; }
        public IEnumerable<ProdOrder2> ProdOrders { get; set; }
        public decimal Fees { get; set; }
        public UserVoucherUsed2 VoucherUsed { get; set; }
        public string CoopAddress { get; set; }
        public string RefundStatus { get; set; }
        public bool IsRated { get; set; }
        public CustomerDetailsModel CustomerDetails { get; set; }
        public string CustLat { get; set; }
        public string CustLong { get; set; }
        public string CoopLat { get; set; }
        public string CoopLong { get; set; }
        public string Delivery_Fee { get; set; }
        public string Address { get; set; }
        public DeliveryListDisplay Delivery { get; set; }
    }

    public class DeliveryListDisplay
    {
        public string OrderNo { get; set; }
        public string Customer { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerId { get; set; }
        public string Contact { get; set; }
        public string PickupDate { get; set; }
        public DateTime DateDelivered { get; set; }
        public string Mode { get; set; }
    }
    public class DeliveryStatus2
    {
        public int Id { get; set; }
        public int UserOrderId { get; set; }
        public string DriverId { get; set; }
        public string Name { get; set; }
        public string ContactNo { get; set; }
        public string PickUpDate { get; set; }
        public string PickUpSuccessDate { get; set; }
        public string ExpectedDeldate { get; set; }
        public DateTime? DateDelivered { get; set; }
        public string Status { get; set; }
        public string ReturnedReason { get; set; }
    }

    public class DriverDetails2
    {
        public string UserId { get; set; }
        public string Image { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string ContactNo { get; set; }
        public int ForDelivery { get; set; }
    }

    public class ArrangePickUpDetails
    {
        public int UserOrderId { get; set; }
        [Required]
        [Display(Name = "Pick-Up Date")]
        [DataType(DataType.Date)]
        public string PickUpDate { get; set; }
        public IList<DriverDetails2> Drivers { get; set; }
    }

    public class ViewChat
    {
        public string SenderName { get; set; }
        public string ReceiversName { get; set; }
        public string ReceiversId { get; set; }
        public string MessageToSend { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }

    public class ViewInbox
    {
        public int InboxId { get; set; }
        public string ReceiversName { get; set; }
        public string ReceiversId { get; set; }
        public string SenderName { get; set; }
        public string LatestMessage { get; set; }
        public DateTime DateSent { get; set; }
        public bool IsRead { get; set; }
    }

    public class ViewCommissionTable
    {
        [Display(Name = "Rate")]
        [Required]
        public decimal Rate { get; set; }
        public List<CommissionTable> ViewCommisions { get; set; }
    }

    public class ViewSalesReport
    {
        public IList<SalesReport> SalesReports { get; set; }
        public IList<AllCoop> Coops { get; set; }
        public IList<ViewBySale> ViewBySales { get; set; }
        [Display(Name = "Coop Search: ")]
        public string CoopSearch { get; set; }
        public string CoopSearchError { get; set; }
        public string ViewBy { get; set; }
    }

    public class ViewBySale
    {
        public int CoopId { get; set; }
        public string CoopName { get; set; }
        public string Date { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class AllCoop
    {
        public int CoopId { get; set; }
        public string CoopName { get; set; }
    }

    public class SalesReport
    {
        public int Id { get; set; }
        [Display(Name = "Order No")]
        public string OrderNo { get; set; }
        public string CoopName { get; set; }
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        [Display(Name = "Total Amount (minus commission fee)")]
        public decimal TotalAmount { get; set; }
        [Display(Name = "Commission Fee")]
        public decimal CommisionFee { get; set; }
        [Display(Name = "Delivery Fee")]
        public string Delivery_fee { get; set; }
    }

    public class ReturnView
    {
        public IList<CoopProdOrder> CustomerOrder { get; set; }
        public int UOrderId { get; set; }
    }

    public class ViewReturnRefundList
    {
        [Display(Name = "Request ID")]
        public int ReturnId { get; set; }
        [Display(Name = "Order ID")]
        public int UOrderId { get; set; }
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; }
        [Display(Name = "Contact No.")]
        public string ContactNo { get; set; }
        [Display(Name = "Type")]
        public string Type { get; set; }
        [Display(Name = "Refund Amount")]
        public decimal RefundAmount { get; set; }
    }

    public class ReturnRefundDetails
    {
        public ReturnRefund ReturnRefunds { get; set; }
        public IList<ProdOrder2> RefundItem { get; set; }
    }
}