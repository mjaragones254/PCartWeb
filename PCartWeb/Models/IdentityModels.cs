using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace PCartWeb.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync2(UserManager<ApplicationUser> manager, string authenticationType)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, authenticationType);
            // Add custom user claims here
            return userIdentity;
        }
    }


    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        public DbSet<EWallet> UserEWallet { get; set; }
        public DbSet<EWalletHistory> EWalletHistories { get; set; }
        public DbSet<COOPMembershipFee> MembershipFees { get; set; }
        public DbSet<CustomerDetailsModel> UserDetails { get; set; }
        public DbSet<CoopAdminDetailsModel> CoopAdminDetails { get; set; }
        public DbSet<CoopApplicationRecords> CoopRecords { get; set; }
        public DbSet<CoopApplicationDocu> CoopRecordsDocu { get; set; }
        public DbSet<CoopMemberDiscount> CoopMemberDiscounts { get; set; }
        public DbSet<DriverDetailsModel> DriverDetails { get; set; }
        public DbSet<CategoryDetailsModel> CategoryDetails { get; set; }
        public DbSet<ProductDetailsModel> ProductDetails { get; set; }
        public DbSet<VoucherDetailsModel> VoucherDetails { get; set; }
        public DbSet<CommissionTable> CommissionDetails { get; set; }
        public DbSet<ExternalImages> PImage { get; set; }
        public DbSet<ProductVariations> PVariation { get; set; }
        public DbSet<CoopDetailsModel> CoopDetails { get; set; }
        public DbSet<WishList> Wishlist { get; set; }
        public DbSet<UserCart> Cart { get; set; }
        public DbSet<ProductCart> ProdCart { get; set; }
        public DbSet<UserCheckout> UCheckout { get; set; }
        public DbSet<ProdCheckout> PCheckout { get; set; }
        public DbSet<UserOrder> UserOrders { get; set; }
        public DbSet<OrderCancel> CancelOrders { get; set; }
        public DbSet<ProdOrder> ProdOrders { get; set; }
        public DbSet<DeliveryStatus> DeliverStatus { get; set; }
        public DbSet<UserVoucherUsed> VoucherUseds { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<CoopLocation> CoopLocations { get; set; }
        public DbSet<CoopDocumentImages> CoopImages { get; set; }
        public DbSet<DiscountModel> DiscountModels { get; set; }
        public DbSet<DiscountedProduct> DiscountedProducts { get; set; }
        public DbSet<PriceTable> Prices { get; set; }
        public DbSet<ProductCost> Cost { get; set; }
        public DbSet<ProductManufacturer> Manufacturer { get; set; }
        public DbSet<CustomerMembership> Memberships { get; set; }
        public DbSet<UserLogs> Logs { get; set; }
        public DbSet<NotificationModel> Notifications { get; set; }
        public DbSet<CoopChat> CoopChats { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<WithdrawRequest> Withdraw { get; set; }
        public DbSet<Complaints> Complaints { get; set; }
        public DbSet<ReturnRefund> ReturnRefunds { get; set; }
        public DbSet<ReturnRefundItem> ReturnRefundItems { get; set; }
        public DbSet<CommissionSale> CommissionSales { get; set; }
        public DbSet<AccountsReceived> AccountsPayable { get; set; }
    }
}