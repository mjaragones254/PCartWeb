
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PCartWeb.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using PCartWeb.Hubs;

namespace PCartWeb.Controllers
{
    [Authorize(Roles = "Coop Admin")]
    public class CoopadminController : Controller
    {
        // GET: Coopadmin

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        public ActionResult Index()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            List<UserViewModel> userview = new List<UserViewModel>();
            List<ProductDetailsModel> product = new List<ProductDetailsModel>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manufacturer = new List<ProductManufacturer>();
            List<int> productID = new List<int>();

            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
            var productDetails = db.ProductDetails.Where(x => x.CoopId == coop.Id && x.Product_status == "Approved" && x.Product_qty != 0).ToList();
            var member = db.UserDetails.Where(x => x.CoopId == user).ToList();
            var commission = db.CommissionDetails.FirstOrDefault();
            var discountCheck = db.DiscountModels.Where(x => x.CoopID == coop.Id).ToList();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coop.Id && x.OStatus == "Complete").ToList();

            foreach (var order in userOrder)
            {
                var deliveryStatus = db.DeliverStatus.Where(x => x.UserOrderId == order.Id).FirstOrDefault();
                var dateDelivered = Convert.ToDateTime(deliveryStatus.DateDelivered);

                if (dateDelivered.AddDays(7) <= DateTime.Now)
                {
                    var coopEwallet = db.UserEWallet.Where(x => x.COOP_ID == order.CoopId.ToString() && x.Status == "Active").FirstOrDefault();
                    coopEwallet.Balance += ((order.TotalPrice + Convert.ToDecimal(order.Delivery_fee)) - order.CommissionFee);
                    db.Entry(coopEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = coopEwallet.ID;
                    ewalletHistory.Amount = ((order.TotalPrice + Convert.ToDecimal(order.Delivery_fee)) - order.CommissionFee);
                    ewalletHistory.Action = "Order Payment";
                    ewalletHistory.Description = "Payment received from Order No. " + order.Id + ".";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();

                    var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                    var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                    adminEwallet.Balance -= ((order.TotalPrice + Convert.ToDecimal(order.Delivery_fee)) - order.CommissionFee);
                    db.Entry(adminEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var coopDetails = db.CoopDetails.Where(x => x.Id.ToString() == coopEwallet.COOP_ID).FirstOrDefault();
                    ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = adminEwallet.ID;
                    ewalletHistory.Amount = ((order.TotalPrice + Convert.ToDecimal(order.Delivery_fee)) - order.CommissionFee);
                    ewalletHistory.Action = "Payment Send";
                    ewalletHistory.Description = "Payment from Order No. " + order.Id + " is sent to " + coopDetails.CoopName + "(" + coopDetails.Id + ").";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();

                    order.OStatus = "Transfered";
                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in productDetails)
                    {
                        var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                        var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id && x.ProductId == item.Id).FirstOrDefault();

                        if (discountProdCheck != null)
                        {
                            decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                            decimal prodPrice = getprice.Price - discountPrice;

                            product.Add(new ProductDetailsModel
                            {
                                Product_image = item.Product_image,
                                Id = item.Id,
                                Product_desc = item.Product_desc,
                                Product_Name = item.Product_Name,
                                DiscountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                Product_qty = item.Product_qty,
                                Categoryname = item.Categoryname,
                                Category_Id = item.Category_Id
                            });

                            price.Add(new PriceTable
                            {
                                ProdId = item.Id,
                                Price = getprice.Price
                            });

                            manufacturer.Add(new ProductManufacturer
                            {
                                ProdId = item.Id,
                                Manufacturer = getmanu.Manufacturer
                            });

                            productID.Add(item.Id);
                        }
                    }
                }
            }

            foreach (var item in productDetails)
            {
                if (!productID.Contains(item.Id))
                {
                    var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    product.Add(new ProductDetailsModel
                    {
                        Product_image = item.Product_image,
                        Id = item.Id,
                        Product_desc = item.Product_desc,
                        Product_Name = item.Product_Name,
                        Product_qty = item.Product_qty,
                        Categoryname = item.Categoryname,
                        Category_Id = item.Category_Id
                    });

                    price.Add(new PriceTable
                    {
                        ProdId = item.Id,
                        Price = getprice.Price
                    });

                    manufacturer.Add(new ProductManufacturer
                    {
                        ProdId = item.Id,
                        Manufacturer = getmanu.Manufacturer
                    });
                }
            }

            productID.Clear();

            if (commission == null)
            {
                var ewallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
                userview.Add(new UserViewModel
                {
                    Firstname = coop.CoopName,
                    Ewallet = ewallet.Balance,
                    MembersNum = member.Count(),
                    ProductNum = productDetails.Count(),
                    Commission = 0
                });
            }
            else
            {
                var ewallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
                userview.Add(new UserViewModel
                {
                    Firstname = coopAdmin.Firstname,
                    Lastname = coopAdmin.Lastname,
                    Ewallet = ewallet.Balance,
                    MembersNum = member.Count(),
                    ProductNum = productDetails.Count(),
                    Commission = commission.Rate,
                    Email = coopAdmin.Email,
                    Contact = coopAdmin.Contact
                });
            }

            dynamic mymodel = new ExpandoObject();
            mymodel.Userview = userview;
            mymodel.Productview = product;
            mymodel.Price = price;
            Session.Abandon();
            return View(mymodel);
        }

        [HttpPost]
        public ActionResult Index(string paypal, string remittance)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            // ----- To Display back the details ----- 

            List<UserViewModel> userview = new List<UserViewModel>();
            List<ProductDetailsModel> product = new List<ProductDetailsModel>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manufacturer = new List<ProductManufacturer>();
            List<int> productID = new List<int>();

            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
            var productDetails = db.ProductDetails.Where(x => x.CoopId == coop.Id && x.Product_status != "Deleted" && x.Product_qty != 0).ToList();
            var member = db.UserDetails.Where(x => x.CoopId == user).ToList();
            var commission = db.CommissionDetails.FirstOrDefault();
            var discountCheck = db.DiscountModels.Where(x => x.CoopID == coop.Id).ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in productDetails)
                    {
                        var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                        var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id && x.ProductId == item.Id).FirstOrDefault();

                        if (discountProdCheck != null)
                        {
                            decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                            decimal prodPrice = getprice.Price - discountPrice;

                            product.Add(new ProductDetailsModel
                            {
                                Product_image = item.Product_image,
                                Id = item.Id,
                                Product_desc = item.Product_desc,
                                Product_Name = item.Product_Name,
                                DiscountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                Product_qty = item.Product_qty,
                                Categoryname = item.Categoryname,
                                Category_Id = item.Category_Id
                            });

                            price.Add(new PriceTable
                            {
                                ProdId = item.Id,
                                Price = getprice.Price
                            });

                            manufacturer.Add(new ProductManufacturer
                            {
                                ProdId = item.Id,
                                Manufacturer = getmanu.Manufacturer
                            });

                            productID.Add(item.Id);
                        }
                    }
                }
            }

            foreach (var item in productDetails)
            {
                if (!productID.Contains(item.Id))
                {
                    var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    product.Add(new ProductDetailsModel
                    {
                        Product_image = item.Product_image,
                        Id = item.Id,
                        Product_desc = item.Product_desc,
                        Product_Name = item.Product_Name,
                        Product_qty = item.Product_qty,
                        Categoryname = item.Categoryname,
                        Category_Id = item.Category_Id
                    });

                    price.Add(new PriceTable
                    {
                        ProdId = item.Id,
                        Price = getprice.Price
                    });

                    manufacturer.Add(new ProductManufacturer
                    {
                        ProdId = item.Id,
                        Manufacturer = getmanu.Manufacturer
                    });
                }
            }

            productID.Clear();

            if (commission == null)
            {
                userview.Add(new UserViewModel
                {
                    Firstname = coop.CoopName,
                    Ewallet = ewallet.Balance,
                    MembersNum = member.Count(),
                    ProductNum = productDetails.Count(),
                    Commission = 0
                });
            }
            else
            {
                userview.Add(new UserViewModel
                {
                    Firstname = coopAdmin.Firstname,
                    Lastname = coopAdmin.Lastname,
                    Ewallet = ewallet.Balance,
                    MembersNum = member.Count(),
                    ProductNum = productDetails.Count(),
                    Commission = commission.Rate
                });
            }
            dynamic mymodel = new ExpandoObject();
            mymodel.Userview = userview;
            mymodel.Productview = product;
            mymodel.Price = price;

            //----- End of data display ------

            //------- Withdraw functions-------

            var email = Request["email"];
            var amount = Request["amount"];
            var fullname = Request["name"];
            var contact = Request["contact"];
            decimal myamount = decimal.Parse(amount);
            var select = Request["select"];

            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var checkcoop = db.CoopDetails.Where(x => x.Id == getcoop.Coop_code).FirstOrDefault();

            if (checkcoop != null)
            {
                if (myamount > ewallet.Balance)
                {
                    ViewBag.ErrorMessage = "Please check your balance.";
                    return View(mymodel);
                }
                else if (myamount < 1000)
                {
                    ViewBag.ErrorMessage = "The minimum to withdraw your balance is 1000 only.";
                    return View(mymodel);
                }
                else
                {
                    //Calculating total amount of Pending requests of this COOP
                    var checkwithdraws = db.Withdraw.Where(x => x.CoopId == coop.Id && x.RequestStatus == "Pending").ToList();
                    decimal alltotal = 0;
                    foreach (var mywithdraw in checkwithdraws)
                    {
                        alltotal += mywithdraw.Amount;
                    }

                    alltotal += myamount;

                    if (alltotal > ewallet.Balance)
                    {
                        ViewBag.ErrorMessage = "Please check your balance.";
                        return View(mymodel);
                    }

                    if (remittance != null) // If the coop chooses thru remittance
                    {
                        decimal charge = CalculateCharge(myamount, select); //Calculate Charges
                        decimal totalamount = myamount - charge;

                        var withdraw = new WithdrawRequest
                        {
                            Amount = totalamount,
                            CoopId = checkcoop.Id,
                            Fullname = fullname,
                            Method = select,
                            Contact = contact,
                            DateReqeuested = DateTime.Now.ToString(),
                            RequestStatus = "Pending",
                            ChargeFee = charge
                        };
                        db.Withdraw.Add(withdraw);
                        db.SaveChanges();
                    }
                    else if (paypal != null) // If the coop chooses thru paypal
                    {
                        double charge = (double.Parse(myamount.ToString()) * 0.044) + 15;
                        myamount -= decimal.Parse(charge.ToString());
                        var withdraw = new WithdrawRequest
                        {
                            Fullname = fullname,
                            Email = email,
                            CoopId = checkcoop.Id,
                            Method = "Paypal",
                            DateReqeuested = DateTime.Now.ToString(),
                            Amount = myamount,
                            RequestStatus = "Pending",
                            ChargeFee = decimal.Parse(charge.ToString())
                        };
                        db.Withdraw.Add(withdraw);
                        db.SaveChanges();
                    }

                }
            }

            return View(mymodel);
        }

        public decimal CalculateCharge(decimal amount, string select)
        {
            decimal total = 0;

            if (select == "Palawan Express")
            {
                if (amount == 1000)
                    total = 15;
                else if (amount > 1000 || amount <= 1500)
                    total = 20;
                else if (amount > 1500 || amount <= 2000)
                    total = 30;
                else if (amount > 2000 || amount <= 2500)
                    total = 40;
                else if (amount > 2500 || amount <= 3000)
                    total = 50;
                else if (amount > 3000 || amount <= 3500)
                    total = 60;
                else if (amount > 3500 || amount <= 4000)
                    total = 70;
                else if (amount > 4000 || amount <= 5000)
                    total = 90;
                else if (amount > 5000 || amount <= 7000)
                    total = 115;
                else if (amount > 7000 || amount <= 9500)
                    total = 125;
                else if (amount > 9500 || amount <= 10000)
                    total = 140;
                else if (amount > 10000 || amount <= 14000)
                    total = 210;
                else if (amount > 14000 || amount <= 15000)
                    total = 220;
                else if (amount > 15000 || amount <= 20000)
                    total = 250;
                else if (amount > 20000 || amount <= 30000)
                    total = 290;
                else if (amount > 30000 || amount <= 40000)
                    total = 320;
                else if (amount > 40000 || amount <= 50000)
                    total = 345;
                else
                    total = 0;
            }
            else if (select == "Cebuana Lhuillier")
            {
                if (amount == 1000)
                    total = 15;
                else if (amount > 1000 || amount <= 1500)
                    total = 20;
                else if (amount > 1500 || amount <= 2000)
                    total = 30;
                else if (amount > 2000 || amount <= 2500)
                    total = 40;
                else if (amount > 2500 || amount <= 3000)
                    total = 50;
                else if (amount > 3000 || amount <= 3500)
                    total = 60;
                else if (amount > 3500 || amount <= 4000)
                    total = 70;
                else if (amount > 4000 || amount <= 5000)
                    total = 90;
                else if (amount > 5000 || amount <= 6000)
                    total = 114;
                else if (amount > 6000 || amount <= 7000)
                    total = 118;
                else if (amount > 7000 || amount <= 8000)
                    total = 125;
                else if (amount > 8000 || amount <= 9500)
                    total = 125;
                else if (amount > 9500 || amount <= 10000)
                    total = 140;
                else if (amount > 10000 || amount <= 14000)
                    total = 250;
                else if (amount > 14000 || amount <= 15000)
                    total = 300;
                else if (amount > 15000 || amount <= 20000)
                    total = 480;
                else if (amount > 20000 || amount <= 30000)
                    total = 720;
                else if (amount > 30000 || amount <= 40000)
                    total = 960;
                else if (amount > 40000 || amount <= 50000)
                    total = 1200;
                else
                    total = 0;
            }
            else if (select == "M Lhuillier")
            {
                if (amount == 1000)
                    total = 15;
                else if (amount > 1000 || amount <= 1300)
                    total = 20;
                else if (amount > 1300 || amount <= 1500)
                    total = 20;
                else if (amount > 1500 || amount <= 1800)
                    total = 30;
                else if (amount > 1800 || amount <= 2000)
                    total = 30;
                else if (amount > 2000 || amount <= 2300)
                    total = 40;
                else if (amount > 2300 || amount <= 2500)
                    total = 40;
                else if (amount > 2500 || amount <= 2800)
                    total = 50;
                else if (amount > 2800 || amount <= 3000)
                    total = 50;
                else if (amount > 3000 || amount <= 3500)
                    total = 60;
                else if (amount > 3500 || amount <= 4000)
                    total = 70;
                else if (amount > 4000 || amount <= 4500)
                    total = 90;
                else if (amount > 4500 || amount <= 5000)
                    total = 90;
                else if (amount > 5000 || amount <= 6000)
                    total = 115;
                else if (amount > 6000 || amount <= 6500)
                    total = 115;
                else if (amount > 6500 || amount <= 7000)
                    total = 115;
                else if (amount > 7000 || amount <= 8000)
                    total = 125;
                else if (amount > 8000 || amount <= 9500)
                    total = 125;
                else if (amount > 9500 || amount <= 10000)
                    total = 140;
                else if (amount > 10000 || amount <= 14000)
                    total = 210;
                else if (amount > 14000 || amount <= 15000)
                    total = 220;
                else if (amount > 15000 || amount <= 20000)
                    total = 250;
                else if (amount > 20000 || amount <= 30000)
                    total = 290;
                else if (amount > 30000 || amount <= 40000)
                    total = 320;
                else if (amount > 40000 || amount <= 50000)
                    total = 345;
                else
                    total = 0;
            }
            return total;
        }

        public ActionResult CommissionFeeReport()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var commissions = db.CommissionSales.Where(x => x.CoopAdminId == user && x.Status == "Pending").ToList();
            return View(commissions);
        }

        public ActionResult OrderHistory()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getuserorders = db.UserOrders.Where(x => x.CoopId == getcoop.Coop_code && x.OStatus == "Complete" || x.CoopId == getcoop.Coop_code && x.OStatus == "Cancelled").ToList();
            return View(getuserorders);
        }

        public ActionResult WithdrawRequestReports()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var checkcoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            List<WithdrawRequest> getcompletetrans = new List<WithdrawRequest>();
            if(checkcoop!=null)
            {
                getcompletetrans = db.Withdraw.Where(x => x.CoopId == checkcoop.Coop_code && x.RequestStatus != "Pending").OrderBy(p => p.RequestStatus).ToList();
            }
            return View(getcompletetrans);
        }
        private bool ValidateFile(HttpPostedFileBase file)
        {
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedFileTypes = { ".gif", ".png", ".jpeg", ".jpg", ".docx", ".pdf" };
            if ((file.ContentLength > 0 && file.ContentLength < 2097152) && allowedFileTypes.Contains(fileExtension))
            {
                return true;
            }
            return false;
        }

        //Product Module
        public ActionResult ViewProdRequest()
        {
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == user2).FirstOrDefault();
            var prices = db.Prices.ToList();
            var product = (from prod in db.ProductDetails
                           join categ in db.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           join customer in db.UserDetails
                           on prod.CustomerId equals customer.AccountId
                           where prod.Product_status == "Request" &&
                           customer.CoopId == getcoop.Coop_code.ToString()
                           select new ViewProdReqList
                           {
                               Id = prod.Id,
                               CustomerName = customer.Firstname + " " + customer.Lastname,
                               Product_name = prod.Product_Name,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               Image = prod.Product_image
                           }).ToList();

            foreach (var item in product)
            {
                var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_price = getprice.Price;
                var getcost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_cost = getcost.Cost;
                var getmanufacturer = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_manufact = getmanufacturer.Manufacturer;
            }

            return View(product);
        }

        public ActionResult RequestDetails(int? id)
        {
            var db = new ApplicationDbContext();
            ViewProdReqList product = new ViewProdReqList();

            var get = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var getid = get.CustomerId;
            product = (from prod in db.ProductDetails
                       where prod.CustomerId == getid
                       where prod.Id == id
                       join categ in db.CategoryDetails
                       on prod.Category_Id equals categ.Id
                       join user in db.UserDetails
                       on prod.CustomerId equals getid
                       select new ViewProdReqList
                       {
                           Id = prod.Id,
                           Product_name = prod.Product_Name,
                           Product_desc = prod.Product_desc,
                           Image = prod.Product_image,
                           Product_qty = prod.Product_qty,
                           Category = categ.Name,
                           Created_at = prod.Prod_Created_at.ToString(),
                           Updated_at = prod.Prod_Updated_at.ToString(),
                           CustomerName = user.Firstname + " " + user.Lastname
                       }).FirstOrDefault();

            var getCost = db.Cost.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_cost = getCost.Cost;
            var getmanu = db.Manufacturer.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_manufact = getmanu.Manufacturer;
            var getprice = db.Prices.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_price = getprice.Price;
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        public ActionResult ApproveProdReq(int id)
        {
            var db = new ApplicationDbContext();
            var product = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == product.CoopId).FirstOrDefault();

            if (product != null)
            {
                product.Product_status = "Approved";
                product.Date_ApprovalStatus = DateTime.Now;
                db.Entry(product).State = EntityState.Modified;
                db.SaveChanges();
            }

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = product.CustomerId,
                NotifFrom = coopDetails.CoopName,
                NotifHeader = "Product Request Approved",
                NotifMessage = "Your product request has been approve. Kindly go to the branch to give the product to the COOP.",
                NavigateURL = "",
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToUser);

            return RedirectToAction("ViewProdRequest");
        }

        public ActionResult RejectProdReq(int id)
        {
            var db = new ApplicationDbContext();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == prod.CoopId).FirstOrDefault();

            if (prod != null)
            {
                prod.Product_status = "Rejected";
                prod.Date_ApprovalStatus = DateTime.Now;
                db.Entry(prod).State = EntityState.Modified;
                db.SaveChanges();
            }

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = prod.CustomerId,
                NotifFrom = coopDetails.CoopName,
                NotifHeader = "Product Request Rejected",
                NotifMessage = "Your product request has been reject. Kindly coordinate with the coop to know why.",
                NavigateURL = "",
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToUser);

            return RedirectToAction("ViewProdRequest");
        }

        public ActionResult ViewDriverList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var driver = db.DriverDetails.Where(p => p.CoopId == coopAdminDetails.Coop_code.ToString()).ToList();

            return View(driver);
        }

        public ActionResult ViewMemberList()
        {
            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user2).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == coopAdminDetails.Coop_code).FirstOrDefault();

            var person = (from u in db.UserDetails
                          join user in db.Users
                          on u.AccountId equals user.Id
                          where u.Role == "Member" &&
                          u.CoopId == coopDetails.Id.ToString()
                          select new UserViewModel
                          {
                              Firstname = u.Firstname,
                              Lastname = u.Lastname,
                              Id = u.Id,
                              IsActive = u.MemberLock,
                              AccountId = u.AccountId,
                              Email = user.Email,
                              Created_at = u.Created_at.ToString(),
                              Updated_at = u.Updated_at.ToString(),
                              Role = u.Role
                          }).ToList();

            return View(person);
        }

        public ActionResult MembershipApplication(int? numMessage)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var checkForm = db.CoopDetails.Where(x => x.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            //var checkForm = db.MembershipForms.Where(x => x.COOP_ID == coopAdminDetails.Coop_code.ToString()).FirstOrDefault();
            var memFee = db.MembershipFees.Where(x => x.COOP_ID == coopAdminDetails.Coop_code.ToString()).OrderByDescending(x => x.ID).FirstOrDefault();
            var memDiscount = db.CoopMemberDiscounts.Where(x => x.COOP_ID == coopAdminDetails.Coop_code).OrderByDescending(x => x.ID).FirstOrDefault();
            string form = "";
            decimal memberDiscout = 0;
            decimal memberFee = 0;
            var model = new CoopFormModel();

            if (numMessage == 1)
            {
                ViewBag.ErrorMessage = "Membership form cannot be null.";
            }
            else if (numMessage == 2)
            {
                ViewBag.Message = "Membership form is added successfully.";
            }
            else if (numMessage == 3)
            {
                ViewBag.ErrorMessage = "Membership fee cannot be 0.";
            }
            else if (numMessage == 4)
            {
                ViewBag.Message = "Membership fee is added successfully.";
            }
            else if (numMessage == 5)
            {
                ViewBag.ErrorMessage = "Member's discount cannot be 0.";
            }
            else if (numMessage == 6)
            {
                ViewBag.Message = "Member's discount is added successfully.";
            }
            else if(numMessage == 7)
            {
                ViewBag.ErrorMessage = "Please create member's discount.";
            }

            if (checkForm.MembershipForm == null)
            {
                form = string.Empty;
            }
            else
            {
                form = checkForm.MembershipForm;
            }

            if (memFee == null)
            {
                memberFee = 0;
            }
            else
            {
                memberFee = memFee.MemFee;
            }

            if (memDiscount == null)
            {
                memberDiscout = 0;
            }
            else
            {
                memberDiscout = memDiscount.MemberDiscount;
            }

            model.MembershipForm = form;
            model.MembersDiscount = memberDiscout;
            model.MembershipFee = memberFee;

            return View(model);
        }

        [HttpPost]
        public ActionResult UploadMembershipForm(CoopFormModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            if (model.DocFile == null)
            {
                if(ValidateFile(model.DocFile) == false)
                {
                    return RedirectToAction("MembershipApplication", new { numMessage = 1 });
                }
            }
            else
            {
                var checkoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                var coop = db.CoopDetails.Where(x => x.Id == checkoop.Coop_code).FirstOrDefault();
                string name = Path.GetFileNameWithoutExtension(model.DocFile.FileName); //getting file name without extension  
                string extension = Path.GetExtension(model.DocFile.FileName); //getting extension of the file
                coop.MembershipForm = "~/Documents/" + name + extension;
                var myfile = name + extension;
                var path = Path.Combine(Server.MapPath("../Documents/"), myfile);
                model.DocFile.SaveAs(path);
                db.Entry(coop).State = EntityState.Modified;
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "Non-Member",
                    ToUser = "",
                    NotifFrom = coop.CoopName,
                    NotifHeader = coop.CoopName + "is now accepting new members. Apply Now!",
                    NotifMessage = "Click here and apply now!",
                    NavigateURL = "CoopDetailView/" + coop.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToRole);
            }

            return RedirectToAction("MembershipApplication", new { numMessage = 2 });
        }

        [HttpPost]
        public ActionResult MembershipFee(CoopFormModel model)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            if (model.MembershipFee == 0 || model.MembershipFee.ToString() == null)
            {
                return RedirectToAction("MembershipApplication", new { numMessage = 3 });
            }
            else
            {
                var checkCoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                var coop = db.CoopDetails.Where(x => x.Id == checkCoop.Coop_code).FirstOrDefault();
                var memFee = new COOPMembershipFee { MemFee = model.MembershipFee, COOP_AdminID = user, COOP_ID = coop.Id.ToString(), Created_At = DateTime.Now };
                db.MembershipFees.Add(memFee);
                db.SaveChanges();
            }

            ViewBag.MemFee = "Membership fee is added successfully .";

            return RedirectToAction("MembershipApplication", new { numMessage = 4 });
        }

        [HttpPost]
        public ActionResult MembersDiscount(CoopFormModel model)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            if (model.MembersDiscount == 0 || model.MembersDiscount.ToString() == null)
            {
                return RedirectToAction("MembershipApplication", new { numMessage = 5 });
            }
            else
            {
                var checkCoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                var coop = db.CoopDetails.Where(x => x.Id == checkCoop.Coop_code).FirstOrDefault();
                var memDiscount = new CoopMemberDiscount { MemberDiscount = model.MembersDiscount, COOP_AdminID = user, COOP_ID = coop.Id, Created_At = DateTime.Now };
                db.CoopMemberDiscounts.Add(memDiscount);
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "Member",
                    ToUser = "",
                    ToCOOP_ID = coop.Id.ToString(),
                    NotifFrom = coop.CoopName,
                    NotifHeader = coop.CoopName + "membership discount is " + model.MembersDiscount + "%. Order Now!",
                    NotifMessage = "",
                    NavigateURL = "Index",
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToRole);
            }

            return RedirectToAction("MembershipApplication", new { numMessage = 6 });
        }

        public FileResult Download(string fileName)
        {
            string myfile = fileName.Replace("~/Documents/", "");
            string fullPath = Path.Combine(Server.MapPath("../Documents"), myfile);
            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult ViewListProduct()
        {
            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();
            var user = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            List<ViewListProd> viewListProds = new List<ViewListProd>();
            List<PriceTable> price = new List<PriceTable>();
            List<int> productID = new List<int>();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var discountCheck = db.DiscountModels.Where(x => x.UserID == user).ToList();

            var product = (from prod in db.ProductDetails
                           join categ in db.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.CoopId == coopAdmin.Coop_code && prod.Product_status == "Approved"
                           && prod.Product_status != "Deleted"
                           select new
                           {
                               Id = prod.Id,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString()
                           }).ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in product)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id && x.ProductId == item.Id).FirstOrDefault();

                        if (discountProdCheck != null)
                        {
                            var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                            var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                            var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                            decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                            decimal prodPrice = getprice.Price - discountPrice;
                            viewListProds.Add(new ViewListProd
                            {
                                Id = item.Id,
                                DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                Product_name = item.Product_name,
                                Product_desc = item.Product_desc,
                                Product_price = getprice.Price - discountPrice,
                                Product_manufact = getmanu.Manufacturer,
                                Product_qty = item.Product_qty,
                                Product_cost = getCost.Cost,
                                Category = item.Category,
                                Created_at = item.Created_at,
                                Updated_at = item.Updated_at
                            });

                            productID.Add(item.Id);
                        }
                    }
                }
            }

            foreach (var item in product)
            {
                if (!productID.Contains(item.Id))
                {
                    var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    viewListProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Product_name = item.Product_name,
                        Product_desc = item.Product_desc,
                        Product_price = getprice.Price,
                        Product_manufact = getmanu.Manufacturer,
                        Product_qty = item.Product_qty,
                        Product_cost = getCost.Cost,
                        Category = item.Category,
                        Created_at = item.Created_at,
                        Updated_at = item.Updated_at
                    });
                }
            }

            productID.Clear();

            return View(viewListProds);
        }

        public ActionResult ProdDetails(int? id)
        {
            var db = new ApplicationDbContext();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var cost = db.Cost.Where(x => x.ProdId == id).FirstOrDefault();
            var manu = db.Manufacturer.Where(x => x.ProdId == id).FirstOrDefault();
            var price = db.Prices.Where(x => x.ProdId == id).FirstOrDefault();
            var model = new EditProductModel();

            model.ProductDetailsModel = new ProductDetailsModel();
            model.ProductDetailsModel = prod;
            model.ProductCost = new ProductCost();
            model.ProductCost = cost;
            model.ProductManufacturer = new ProductManufacturer();
            model.ProductManufacturer = manu;
            model.PriceTable = new PriceTable();
            model.PriceTable = price;

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (prod == null)
            {
                return HttpNotFound();
            }

            return View(model);
        }

        // To be Emerged Connect Join
        public ActionResult ViewOrderList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && x.OStatus == "To Pay").ToList();
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult AcceptCOD(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
            userOrder.OStatus = "COD Accepted";
            db.Entry(userOrder).State = EntityState.Modified;
            db.SaveChanges();

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = userOrder.UserId,
                ToCOOP_ID = "",
                NotifFrom = coopAdmin.UserId,
                NotifHeader = "COD Accepted",
                NotifMessage = "COD as payment method is accepted in Order No. " + userOrder.Id,
                NavigateURL = "OrderDetails/" + userOrder.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToRole);

            var mess = "You have accepted the COD request of order " + id + ".";
            return RedirectToAction("OrderDetails", new { id = id, driver = 1, message = mess });
        }

        public ActionResult CancelOrder(int? id)
        {
            var reason = Request.Form["Reason"];

            if(reason == "" || reason == null)
            {
                var mess = "Kindly choose a reason for the cancellation.";

                return RedirectToAction("OrderDetails", new { id = id, driver = 1, message = mess });
            }

            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var useroder = db.UserOrders.Where(x => x.Id == id).FirstOrDefault();

            if (useroder != null)
            {
                if (useroder.ModeOfPay == "E-Wallet" || useroder.ModeOfPay == "Paypal")
                {
                    var userEwallet = db.UserEWallet.Where(x => x.UserID == useroder.UserId).FirstOrDefault();
                    userEwallet.Balance += useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(userEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = userEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund";
                    ewalletHistory.Description = "Order No. " + useroder.Id + "was cancelled. Payment successfully refunded.";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();

                    var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                    var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                    adminEwallet.Balance -= useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(adminEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = adminEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund Payment";
                    ewalletHistory.Description = "Orcer Cancelled. Payment refunded from Order No. " + useroder.Id + ".";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();
                }

                useroder.OStatus = "Cancelled";
                db.Entry(useroder).State = EntityState.Modified;
                db.SaveChanges();

                var cancel = new OrderCancel();
                cancel.UserOrder_ID = useroder.Id;
                cancel.CancelledBy = coopAdminDetails.UserId;
                cancel.Reason = reason;
                cancel.Created_At = DateTime.Now;
                db.CancelOrders.Add(cancel);
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = useroder.UserId,
                    ToCOOP_ID = "",
                    NotifFrom = user,
                    NotifHeader = "Order No. " + useroder.Id + " is cancelled.",
                    NotifMessage = "Order No. " + useroder.Id + "was cancelled by " + coopDetails.CoopName,
                    NavigateURL = "OrderDetails/" + useroder.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToRole);
            }

            return RedirectToAction("OrderDetails", new { id = id.ToString(), drivers = 1, message = "Order is cancelled." });
        }

        public ActionResult ArrangePickUpList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && (x.OStatus == "Paid" || x.OStatus == "COD Accepted")).ToList();
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult OrderDetails(string id, int? drivers, string message)
        {
            var db = new ApplicationDbContext();
            var uid = Convert.ToInt32(id);
            var user = User.Identity.GetUserId();
            var userOrder = db.UserOrders.Where(u => u.Id == uid).FirstOrDefault();
            var customerDeatils = db.UserDetails.Where(x => x.AccountId == userOrder.UserId).FirstOrDefault();
            var customerAddress = db.Locations.Where(x => x.UserId == userOrder.UserId).FirstOrDefault(); 
            var cancel = db.CancelOrders.Where(x => x.UserOrder_ID == userOrder.Id).FirstOrDefault();
            var prodOrder = db.ProdOrders.Where(p => p.UOrderId == id).ToList();
            var voucherUsed = db.VoucherUseds.Where(v => v.UserOrderId == id).FirstOrDefault();
            var deliveryDetails = db.DeliverStatus.Where(o => o.UserOrderId == uid).FirstOrDefault();
            var comRate = db.CommissionDetails.OrderByDescending(c => c.Id).FirstOrDefault();

            if (message != null)
            {
                ViewBag.Message = message;
            }
            else if (drivers == 0)
            {
                ViewBag.ErrorMessage = "You currently do not have a delivery driver. Kindly add one in delivry driver management.";
            }

            if (comRate == null)
            {
                return RedirectToAction("ViewOrderList");
            }

            var model = new ViewCustomerOrder();
            UserOrder order = new UserOrder();
            List<ProdOrder2> product = new List<ProdOrder2>();
            UserVoucherUsed2 voucher = new UserVoucherUsed2();
            VoucherDetailsModel voucherDetails = null;
            DeliveryStatus2 deliver = new DeliveryStatus2();
            decimal fee = 0;

            if (cancel != null)
            {
                OrderCancel orderCancel = new OrderCancel();
                orderCancel.UserOrder_ID = userOrder.Id;

                if (cancel.CancelledBy == user)
                {
                    var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                    var coop = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
                    orderCancel.CancelledBy = coop.CoopName + " (Seller)";
                }
                else
                {
                    var userDetails = db.UserDetails.Where(x => x.AccountId == cancel.CancelledBy).FirstOrDefault();
                    orderCancel.CancelledBy = userDetails.Firstname + " " + userDetails.Lastname + " (Customer)";
                }

                orderCancel.Reason = cancel.Reason;
                orderCancel.Created_At = cancel.Created_At;
                model.CancelOrder = orderCancel;
            }

            if (deliveryDetails != null)
            {
                var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
                if(deliveryDetails.PickUpSuccessDate == date || deliveryDetails.ExpectedDeldate == date || deliveryDetails.DateDelivered == date)
                {
                    deliver.PickUpSuccessDate = "";
                    deliver.ExpectedDeldate = "";
                    deliver.DateDelivered = null;
                }
                else
                {
                    deliver.PickUpSuccessDate = deliveryDetails.PickUpSuccessDate.ToString();
                    deliver.ExpectedDeldate = deliveryDetails.ExpectedDeldate.ToString();
                    deliver.DateDelivered = deliveryDetails.DateDelivered;
                }

                var driver = db.DriverDetails.Where(d => d.UserId == deliveryDetails.DriverId).FirstOrDefault();
                deliver.UserOrderId = deliveryDetails.UserOrderId;
                deliver.DriverId = deliveryDetails.DriverId;
                deliver.Name = driver.Firstname + " " + driver.Lastname;
                deliver.ContactNo = driver.Contact;
                deliver.PickUpDate = deliveryDetails.PickUpDate;
                deliver.Status = deliveryDetails.Status;
                
            }
            else
            {
                deliver = null;
            }

            if (voucherUsed != null)
            {
                voucherDetails = db.VoucherDetails.Where(vd => vd.VoucherCode == voucherUsed.VoucherCode).FirstOrDefault();
            }

            fee = comRate.Rate / 100;

            order.Id = userOrder.Id;
            order.UserId = userOrder.UserId;
            order.CoopId = userOrder.CoopId;
            order.OrderCreated_at = userOrder.OrderCreated_at;
            order.OStatus = userOrder.OStatus;
            order.ModeOfPay = userOrder.ModeOfPay;
            order.TotalPrice = userOrder.TotalPrice;
            order.CommissionFee = userOrder.CommissionFee;
            order.Delivery_fee = userOrder.Delivery_fee;

            if(prodOrder !=null)
            {
                foreach (var prod in prodOrder)
                {
                    var prod2 = db.ProductDetails.Where(p => p.Id.ToString() == prod.ProdId).FirstOrDefault();
                    product.Add(new ProdOrder2
                    {
                        UserId = prod.UserId,
                        CoopId = prod.CoopId,
                        UOrderId = prod.UOrderId,
                        ProdImage = prod2.Product_image,
                        ProdName = prod.ProdName,
                        Price = prod.Price,
                        ProdId = prod.ProdId,
                        MemberDiscountedPrice = prod.MemberDiscountedPrice,
                        DiscountedPrice = prod.DiscountedPrice,
                        Qty = prod.Qty,
                        SubTotal = prod.SubTotal
                    });
                }

                model.ProdOrders = product;
            }

            if (voucherUsed != null)
            {
                if (voucherDetails.DiscountType == "Percent")
                {
                    voucher.VoucherUsed = voucherDetails.Name + " " + voucherDetails.Percent_Discount + "% OFF";
                }
                else
                {
                    voucher.VoucherUsed = voucherDetails.Name + " PHP " + voucherDetails.Percent_Discount + " OFF";
                }

                voucher.UserId = voucherUsed.UserId;
                voucher.CoopId = voucherUsed.CoopId;
                voucher.UserOrderId = voucherUsed.UserOrderId;
                voucher.DiscountType = voucherDetails.DiscountType;
                voucher.Discount = Convert.ToDecimal(voucherDetails.Percent_Discount);
                voucher.VoucherCode = voucherUsed.VoucherCode;
                voucher.DateCreated = voucherUsed.DateCreated;

                model.VoucherUsed = voucher;
            }

            model.CustomerDetails = customerDeatils;
            model.CustomerAddress = customerAddress;
            model.Fees = decimal.Round(fee, 2, MidpointRounding.AwayFromZero);
            model.UserOrders = order;
            model.DeliveryDetails = deliver;

            return View(model);
        }

        [HttpPost]
        public ActionResult OrderDetails(string submit)
        {
            var reason = Request["Reason"];
            var useorder = Request["UserOrderId"];
            var id = Convert.ToInt32(useorder);

            if (reason == "" || reason == null)
            {
                var mess = "Kindly choose a reason for the cancellation.";
                return RedirectToAction("OrderDetails", new { id = id, message = mess });
            }

            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var useroder = db.UserOrders.Where(x => x.Id == id).FirstOrDefault();

            if (useroder != null)
            {
                if (useroder.ModeOfPay == "E-Wallet" || useroder.ModeOfPay == "Paypal")
                {
                    var userEwallet = db.UserEWallet.Where(x => x.UserID == useroder.UserId).FirstOrDefault();
                    userEwallet.Balance += useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(userEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = userEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund";
                    ewalletHistory.Description = "Order No. " + useroder.Id + "was cancelled. Payment successfully refunded.";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();

                    var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                    var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                    adminEwallet.Balance -= useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(adminEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = adminEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund Payment";
                    ewalletHistory.Description = "Orcer Cancelled. Payment refunded from Order No. " + useroder.Id + ".";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();
                }

                useroder.OStatus = "Cancelled";
                db.Entry(useroder).State = EntityState.Modified;
                db.SaveChanges();

                var cancel = new OrderCancel();
                cancel.UserOrder_ID = useroder.Id;
                cancel.CancelledBy = coopAdminDetails.UserId;
                cancel.Reason = reason;
                cancel.Created_At = DateTime.Now;
                db.CancelOrders.Add(cancel);
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = useroder.UserId,
                    ToCOOP_ID = "",
                    NotifFrom = user,
                    NotifHeader = "Order No. " + useroder.Id + " is cancelled.",
                    NotifMessage = "Order No. " + useroder.Id + "was cancelled by " + coopDetails.CoopName,
                    NavigateURL = "OrderDetails/" + useroder.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToRole);
            }

            return RedirectToAction("OrderDetails", new { id = id.ToString(), drivers = 1, message = "Order is cancelled." });
        }

        public ActionResult DeleteItemImage(int id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            string pname = Session["pname"].ToString();
            string desc = Session["desc"].ToString();
            string manu = Session["manu"].ToString();
            decimal cost = (decimal)Session["cost"];
            int qty = (int)Session["qty"];
            decimal price = (decimal)Session["price"];
            string categ = Session["categ"].ToString();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.Product_Name == pname && p.Product_desc == desc
            && p.Product_qty == qty && p.Categoryname == categ && p.CoopId == coopAdmin.Coop_code).FirstOrDefault();

            if (id > 0)
            {
                var byid = db.PImage.Where(x => x.Id == id).FirstOrDefault();

                if (byid != null)
                {
                    db.Entry(byid).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("ViewImageList", new { id = prod.Id });
        }

        int prodid = 0;
        [HttpGet]
        public ActionResult ViewImageList(int id)
        {
            if (Session["pname"].ToString() == "")
            {
                return RedirectToAction("ViewListProduct");
            }
            Session["ImageId"] = id;
            AddImagesVariationsModel model = new AddImagesVariationsModel();
            List<ExternalImages> images = new List<ExternalImages>();
            List<ViewVariationModel> viewVariations = new List<ViewVariationModel>();
            var db = new ApplicationDbContext();
            var imagelist = db.PImage.Where(p => p.ProductId == id.ToString()).ToList();
            var vars = db.PVariation.Where(x => x.ProdId == id).ToList();
            if (imagelist == null)
            {
                return RedirectToAction("ViewListProduct");
            }
            if (vars != null)
            {
                foreach (var varia in vars)
                {
                    var price = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                    viewVariations.Add(new ViewVariationModel
                    {
                        Id = varia.Id,
                        Desc = varia.Description,
                        Price = price.Price.ToString(),
                        ProdId = varia.ProdId
                    });
                }
                model.Variations = viewVariations;
            }

            model.Images = imagelist;
            return View(model);
        }

        [HttpPost]
        public ActionResult ViewImageList(AddImagesVariationsModel model)
        {
            Int32 id = (int)Session["ImageId"];
            List<ExternalImages> images = new List<ExternalImages>();
            List<ViewVariationModel> viewVariations = new List<ViewVariationModel>();
            var db = new ApplicationDbContext();
            var imagelist = db.PImage.Where(p => p.ProductId == id.ToString()).ToList();

            var getprice = db.Prices.Where(x => x.ProdId == id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();

            if (imagelist == null)
            {
                return RedirectToAction("ViewListProduct");
            }

            model.Images = imagelist;
            var getvariation = db.PVariation.Where(x => x.ProdId == id && x.Description == model.VarDesc).FirstOrDefault();
            if (ModelState.IsValid)
            {
                if (getprice.Price >= model.VarPrice)
                {
                    var vars2 = db.PVariation.Where(x => x.ProdId == id).ToList();
                    if (vars2 != null)
                    {
                        foreach (var varia in vars2)
                        {
                            var price = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                            viewVariations.Add(new ViewVariationModel
                            {
                                Id = varia.Id,
                                Desc = varia.Description,
                                Price = price.Price.ToString(),
                                ProdId = varia.ProdId
                            });
                        }
                        model.Variations = viewVariations;
                    }
                    model.VarDesc = "";
                    model.VarPrice = 0;
                    model.Images = imagelist;
                    return View(model);
                }

                if (getvariation != null)
                {
                    var vars2 = db.PVariation.Where(x => x.ProdId == id).ToList();
                    if (vars2 != null)
                    {
                        foreach (var varia in vars2)
                        {
                            var price = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                            viewVariations.Add(new ViewVariationModel
                            {
                                Id = varia.Id,
                                Desc = varia.Description,
                                Price = price.Price.ToString(),
                                ProdId = varia.ProdId
                            });
                        }
                        model.Variations = viewVariations;
                    }
                    model.VarDesc = "";
                    model.VarPrice = 0;
                    model.Images = imagelist;
                    ViewBag.errorMessage = "This variation is already existed in this product.";
                    return View(model);
                }
                ProductVariations variation = new ProductVariations();
                PriceTable prices = new PriceTable();
                variation.Description = model.VarDesc;
                variation.ProdId = id;
                string date = DateTime.Now.ToString();
                variation.IsAvailable = true;
                variation.Created_at = date;
                db.PVariation.Add(variation);
                db.SaveChanges();

                var getvar = db.PVariation.Where(x => x.Created_at == date && x.Description == model.VarDesc && x.ProdId == id).OrderByDescending(p => p.Id).FirstOrDefault();

                prices.Price = model.VarPrice;
                prices.ProdId = id;
                prices.Created_at = DateTime.Now.ToString();
                prices.VarId = getvar.Id;

                db.Prices.Add(prices);
                db.SaveChanges();
            }
            var vars = db.PVariation.Where(x => x.ProdId == id).ToList();
            if (vars != null)
            {
                foreach (var varia in vars)
                {
                    var price = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                    viewVariations.Add(new ViewVariationModel
                    {
                        Id = varia.Id,
                        Desc = varia.Description,
                        Price = price.Price.ToString(),
                        ProdId = varia.ProdId
                    });
                }
                model.Variations = viewVariations;
            }
            model.VarDesc = string.Empty;
            model.VarPrice = 0;
            model.Images = imagelist;
            return View(model);
        }

        public ActionResult DeleteVariation(int? id, int? prodid)
        {
            var db = new ApplicationDbContext();
            var getvar = db.PVariation.Where(x => x.Id == id).FirstOrDefault();
            if (getvar != null)
            {
                db.Entry(getvar).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("ViewImageList", new { id = prodid });
        }

        public ActionResult DeleteEditVariation(int? id, int? prodid)
        {
            var db = new ApplicationDbContext();
            var getvar = db.PVariation.Where(x => x.Id == id).FirstOrDefault();
            if (getvar != null)
            {
                db.Entry(getvar).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("EditProductList");
        }

        public ActionResult ProductVar()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            string pname = Session["pname"].ToString();
            string desc = Session["desc"].ToString();
            string manu = Session["manu"].ToString();
            decimal cost = (decimal)Session["cost"];
            int qty = (int)Session["qty"];
            decimal price = (decimal)Session["price"];
            string categ = Session["categ"].ToString();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.Product_Name == pname && p.Product_desc == desc
            && p.Product_qty == qty && p.Categoryname == categ && p.CoopId == coopAdmin.Coop_code).FirstOrDefault();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewVariation()
        {
            var data = new List<object>();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AddProductImage()
        {
            if (Session["pname"].ToString() == "")
            {
                return RedirectToAction("ViewListProduct");
            }

            return View();
        }

        [HttpPost]
        public ActionResult AddProductImage(AddProdImageViewModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", "jpeg"
                };

            var user = User.Identity.GetUserId();
            string pname = Session["pname"].ToString();
            string desc = Session["desc"].ToString();
            string manu = Session["manu"].ToString();
            decimal cost = (decimal)Session["cost"];
            int qty = (int)Session["qty"];
            decimal price = (decimal)Session["price"];
            string categ = Session["categ"].ToString();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.Product_Name == pname && p.Product_desc == desc
            && p.Product_qty == qty && p.Categoryname == categ && p.CoopId == coopAdmin.Coop_code).FirstOrDefault();

            if (model.ImageFile == null)
            {
                return RedirectToAction("ViewImageList", new { id = prod.Id });
            }
            if (model.ImageFile != null)
            {
                if(ValidateFile(model.ImageFile) != true)
                {
                    ViewBag.message = "Please choose one Image File";
                    return View(model);
                }
            }
            string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
            string extension = Path.GetExtension(model.ImageFile.FileName);
            var imagefile = new ExternalImages();

            if (ValidateFile(model.ImageFile)==true)
            {
                imagefile.Product_image = name + extension;
                imagefile.ProductId = prod.Id.ToString();
                prodid = prod.Id;
                imagefile.Userid = user;
                var myfile = name + extension;
                var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                db.PImage.Add(imagefile);
                db.SaveChanges();
                model.ImageFile.SaveAs(path);
                Session["prodid"] = prodid;
                return RedirectToAction("ViewImageList", new { id = prod.Id });
            }
            else
            {
                ViewBag.ErrorMessage = "Kindly upload a valid image file.";
            }

            return View(model);
        }

        public ActionResult EditProduct(int id)
        {
            var db = new ApplicationDbContext();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var cost = db.Cost.Where(x => x.ProdId == id).FirstOrDefault();
            var manu = db.Manufacturer.Where(x => x.ProdId == id).FirstOrDefault();
            var price = db.Prices.Where(x => x.ProdId == id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
            var model = new EditProductModel();
            var variations = db.PVariation.Where(x => x.ProdId == id).ToList();
            List<ViewVariationModel> variationModels = new List<ViewVariationModel>();
            prod.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name");
            if (prod != null)
            {
                Session["key"] = id;
                TempData.Keep();
                if (variations != null)
                {
                    foreach (var varia in variations)
                    {
                        var getvarprice = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                        variationModels.Add(new ViewVariationModel { Desc = varia.Description, Price = getvarprice.Price.ToString(), Id = varia.Id });
                    }
                }
                model.ProductDetailsModel = new ProductDetailsModel();
                model.ProductDetailsModel = prod;
                model.ProductCost = new ProductCost();
                model.ProductCost = cost;
                model.ProductManufacturer = new ProductManufacturer();
                model.ProductManufacturer = manu;
                model.PriceTable = new PriceTable();
                model.PriceTable = price;
                return View(model);
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult EditProduct(EditProductModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            model.ProductDetailsModel.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name", model.ProductDetailsModel.Category_Id);
            Int32 prodid = (int)Session["key"];
            var categname = db.CategoryDetails.Where(x => x.Id == model.ProductDetailsModel.Category_Id).FirstOrDefault();
            var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", "jpeg"
                };
            string name = "";
            string extension = "";
            var prod = db.ProductDetails.Where(x => x.Id == prodid).FirstOrDefault();
            var price = new PriceTable();
            var cost = new ProductCost();
            var manu = new ProductManufacturer();

            if (prod != null)
            {
                if (model.ProductDetailsModel.ImageFile == null)
                {
                    prod.Product_image = prod.Product_image;
                }
                else
                {
                    name = Path.GetFileNameWithoutExtension(model.ProductDetailsModel.ImageFile.FileName); //getting file name without extension  
                    extension = Path.GetExtension(model.ProductDetailsModel.ImageFile.FileName);
                    prod.Product_image = name + extension;
                }

                if (String.IsNullOrEmpty(categname.Name))
                    prod.Categoryname = prod.Categoryname;
                else
                {
                    prod.Categoryname = categname.Name;
                    prod.Category_Id = model.ProductDetailsModel.Category_Id;
                }

                if (model.ProductDetailsModel.ExpiryDate == null)
                {
                    prod.ExpiryDate = prod.ExpiryDate;
                }
                else
                {
                    prod.ExpiryDate = model.ProductDetailsModel.ExpiryDate;
                }

                if (String.IsNullOrEmpty(model.ProductDetailsModel.Product_Name))
                    prod.Product_Name = prod.Product_Name;
                else
                    prod.Product_Name = model.ProductDetailsModel.Product_Name;

                if (String.IsNullOrEmpty(model.ProductDetailsModel.Product_desc))
                    prod.Product_desc = prod.Product_desc;
                else
                    prod.Product_desc = model.ProductDetailsModel.Product_desc;

                manu.Manufacturer = model.ProductManufacturer.Manufacturer;
                price.Price = model.PriceTable.Price;
                cost.Cost = model.ProductCost.Cost;

                var db2 = new ApplicationDbContext();
                var db3 = new ApplicationDbContext();
                var db4 = new ApplicationDbContext();
                prod.Prod_Updated_at = DateTime.Now;
                price.ProdId = prodid;
                price.Created_at = DateTime.Now.ToString();
                cost.ProdId = prodid;
                cost.Created_at = DateTime.Now.ToString();
                manu.ProdId = prodid;
                manu.Created_at = DateTime.Now.ToString();
                db.Entry(prod).State = EntityState.Modified;
                db2.Prices.Add(price);
                db3.Cost.Add(cost);
                db4.Manufacturer.Add(manu);
                db2.SaveChanges();
                db.SaveChanges();
            }

            return RedirectToAction("EditProductList");
        }

        //Please Continue here
        public ActionResult EditProductList()
        {
            Int32 id = (int)Session["key"];
            var db = new ApplicationDbContext();
            ViewEditImagesVariations model = new ViewEditImagesVariations();
            List<ViewVariationModel> variationModels = new List<ViewVariationModel>();
            var getprod = db.PImage.Where(x => x.ProductId == id.ToString()).ToList();
            var getvariations = db.PVariation.Where(x => x.ProdId == id).ToList();
            if (getvariations != null)
            {
                foreach (var variation in getvariations)
                {
                    var getprice = db.Prices.Where(x => x.VarId == variation.Id).FirstOrDefault();
                    variationModels.Add(new ViewVariationModel { Id = variation.Id, Desc = variation.Description, Price = getprice.Price.ToString() });
                }
                model.Variations = variationModels;
            }

            model.Images = getprod;

            return View(model);
        }

        [HttpPost]
        public ActionResult EditProductList(ViewEditImagesVariations model)
        {
            Int32 id = (int)Session["key"];
            var db = new ApplicationDbContext();
            List<ViewVariationModel> variationModels = new List<ViewVariationModel>();
            var getprod = db.PImage.Where(x => x.ProductId == id.ToString()).ToList();
            var getprice2 = db.Prices.Where(x => x.ProdId == id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
            if (ModelState.IsValid)
            {
                if (getprice2.Price >= model.Price)
                {
                    var getvariations2 = db.PVariation.Where(x => x.ProdId == id).ToList();
                    if (getvariations2 != null)
                    {
                        foreach (var varia in getvariations2)
                        {
                            var getprice = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                            variationModels.Add(new ViewVariationModel { Id = varia.Id, Desc = varia.Description, Price = getprice.Price.ToString() });
                        }
                        model.Variations = variationModels;

                    }
                    model.Images = getprod;
                    return View(model);
                }
                var getvariation3 = db.PVariation.Where(x => x.ProdId == id && x.Description == model.VarDesc).FirstOrDefault();
                if (getvariation3 != null)
                {
                    var vars2 = db.PVariation.Where(x => x.ProdId == id).ToList();
                    if (vars2 != null)
                    {
                        foreach (var varia in vars2)
                        {
                            var price = db.Prices.Where(x => x.VarId == varia.Id).FirstOrDefault();
                            variationModels.Add(new ViewVariationModel
                            {
                                Id = varia.Id,
                                Desc = varia.Description,
                                Price = price.Price.ToString(),
                                ProdId = varia.ProdId
                            });
                        }
                        model.Variations = variationModels;
                    }
                    model.VarDesc = "";
                    model.Price = 0;
                    model.Images = getprod;
                    ViewBag.errorMessage = "This variation is already existed in this product.";
                    return View(model);
                }

                ProductVariations variation = new ProductVariations();
                PriceTable prices = new PriceTable();
                variation.Description = model.VarDesc;
                variation.ProdId = id;
                string date = DateTime.Now.ToString();
                variation.IsAvailable = true;
                variation.Created_at = date;
                db.PVariation.Add(variation);
                db.SaveChanges();

                var getvar = db.PVariation.Where(x => x.Created_at == date && x.Description == model.VarDesc && x.ProdId == id).OrderByDescending(p => p.Id).FirstOrDefault();

                prices.Price = model.Price;
                prices.ProdId = id;
                prices.Created_at = DateTime.Now.ToString();
                prices.VarId = getvar.Id;

                db.Prices.Add(prices);
                db.SaveChanges();
            }

            var getvariations = db.PVariation.Where(x => x.ProdId == id).ToList();
            if (getvariations != null)
            {
                foreach (var variation in getvariations)
                {
                    var getprice = db.Prices.Where(x => x.VarId == variation.Id).FirstOrDefault();
                    variationModels.Add(new ViewVariationModel { Id = variation.Id, Desc = variation.Description, Price = getprice.Price.ToString() });
                }
                model.Variations = variationModels;
            }

            model.Images = getprod;

            return View(model);
        }

        public ActionResult EditProductImage()
        {
            return View();
        }

        [HttpPost]
        public ActionResult EditProductImage(ExternalImages model, HttpPostedFileBase file)
        {
            Int32 id = (int)Session["key"];
            var db = new ApplicationDbContext();
            if (model.ImageFile == null)
            {
                return RedirectToAction("EditProductList");
            }
            var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", "jpeg"
                };
            string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
            string extension = Path.GetExtension(model.ImageFile.FileName);
            var imagefile = new ExternalImages();
            if (ValidateFile(model.ImageFile)==true)
            {
                imagefile.Product_image = name + extension;
                imagefile.ProductId = id.ToString();
                prodid = id;
                var myfile = name + extension;
                var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                db.PImage.Add(imagefile);
                db.SaveChanges();
                model.ImageFile.SaveAs(path);
                return RedirectToAction("EditProductList");
            }
            else
            {
                ViewBag.ErrorMessage = "Please choose only Image file";
            }
            return View();
        }

        public ActionResult DeleteEditImage(int id)
        {
            var db = new ApplicationDbContext();
            var prod = db.PImage.Where(x => x.Id == id).FirstOrDefault();
            if (id > 0)
            {
                db.Entry(prod).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("EditProductList");
        }
        //End of new

        public ActionResult AddProduct()
        {
            int redirect = 0;
            ModelState.Clear();
            var db = new ApplicationDbContext();
            var model = new AdminAddProductViewModel();
            var categ = db.CategoryDetails.FirstOrDefault();

            if (categ == null)
            {
                redirect = 1;
                return RedirectToAction("AddCategory", new { redirect = redirect });
            }

            model.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name");

            return View(model);
        }

        [HttpPost]
        public ActionResult AddProduct(AdminAddProductViewModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            model.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name", model.CategoryId);
            bool check = CheckItemExist(model.Product_name, model.Product_desc, model.Product_manufact);

            if (check == false)
            {
                ViewData["ErrorMessage"] = "This item is already exist! Check the list and update (optional).";
                return View(model);
            }
            else
            {
                if (model.ImageFile != null)
                {
                    if(ValidateFile(model.ImageFile) != true)
                    {
                        ViewBag.ErrorMessage = "Kindly upload a valid image file.";
                        return View(model);
                    }
                    
                }
                var categname = db.CategoryDetails.Where(x => x.Id == model.CategoryId).FirstOrDefault();
                var user = User.Identity.GetUserId(); //Get the Current loggedin userid for foreign key
                var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };

                string name = ""; 
                string extension = "";
                var myfile = "";
                var path = "";
                if(model.ImageFile != null)
                {
                    name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    extension = Path.GetExtension(model.ImageFile.FileName);
                    model.Product_image = name + extension;
                    myfile = name + extension;
                    path = Path.Combine(Server.MapPath("../Images/"), myfile);
                    model.ImageFile.SaveAs(path);
                }
                else
                {
                    model.Product_image = "products.png";
                }
                var details = new ProductDetailsModel();
                details.Product_image = model.Product_image;
                Session["pname"] = model.Product_name;
                Session["desc"] = model.Product_desc;
                Session["manu"] = model.Product_manufact;
                Session["cost"] = model.Product_cost;
                Session["qty"] = model.Product_qty;
                Session["price"] = model.Product_price;
                Session["categ"] = categname.Name;

                if (ModelState.IsValid)
                {
                    details.Category_Id = model.CategoryId;
                    details.Categoryname = categname.Name;
                    details.Product_Name = model.Product_name;
                    details.Product_desc = model.Product_desc;
                    details.Product_qty = model.Product_qty;

                    if (model.ExpiryDate == null)
                    {
                        details.ExpiryDate = null;
                    }
                    else
                    {
                        details.ExpiryDate = model.ExpiryDate;
                    }

                    details.Product_status = "Approved";
                    details.Product_sold = 0;
                    details.DiscountedPrice = 0;
                    details.CoopAdminId = user;
                    details.CoopId = coopAdmin.Coop_code;
                    details.Prod_Created_at = DateTime.Now;
                    details.Prod_Updated_at = DateTime.Now;
                    details.Date_ApprovalStatus = DateTime.Now;
                    db.ProductDetails.Add(details);
                    db.SaveChanges();

                    var db2 = new ApplicationDbContext();
                    var getprodid = db.ProductDetails.OrderByDescending(p => p.Id).FirstOrDefault();
                    var price = new PriceTable();
                    price.ProdId = getprodid.Id;
                    price.Price = model.Product_price;
                    price.Created_at = DateTime.Now.ToString();
                    db2.Prices.Add(price);
                    db2.SaveChanges();

                    var db3 = new ApplicationDbContext();
                    var cost = new ProductCost();
                    cost.ProdId = getprodid.Id;
                    cost.Cost = model.Product_cost;
                    cost.Created_at = DateTime.Now.ToString();
                    db3.Cost.Add(cost);
                    db3.SaveChanges();

                    var db4 = new ApplicationDbContext();
                    var manu = new ProductManufacturer();
                    manu.ProdId = getprodid.Id;
                    manu.Manufacturer = model.Product_manufact;
                    manu.Created_at = DateTime.Now.ToString();
                    db4.Manufacturer.Add(manu);
                    db4.SaveChanges();
                    ModelState.Clear();

                    return RedirectToAction("AddProductImage");
                }
            }

            return View(model);
        }

        public bool CheckItemExist(string name, string desc, string manu)
        {
            using (var db = new ApplicationDbContext())
            {
                var user = User.Identity.GetUserId();
                var getcoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
                var check = db.ProductDetails.Where(p => p.Product_Name == name && p.Product_desc == desc && p.CoopId == getcoop.Coop_code && p.Product_status != "Deleted").FirstOrDefault();

                if (check != null)
                {
                    return false;
                }
            }

            return true;
        }

        public ActionResult DeleteProduct(int id)
        {
            var db = new ApplicationDbContext();

            if (id > 0)
            {
                var byid = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();

                if (byid != null)
                {
                    byid.Product_status = "Deleted";
                    db.Entry(byid).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("ViewListProduct");
        }

        public ActionResult ViewListCategory()
        {
            var db = new ApplicationDbContext();

            var category = (from cat in db.CategoryDetails
                            select new CategoryViewModel
                            {
                                Id = cat.Id,
                                Cat_name = cat.Name,
                                Cat_desc = cat.Description
                            }).ToList();

            return View(category);
        }

        [AllowAnonymous]
        public ActionResult AddCat()
        {
            return View();
        }

        [HttpPost]
        public ActionResult AddCat(AddCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                int flag = 0;
                var db = new ApplicationDbContext();
                var category = (from cat in db.CategoryDetails
                                select new CategoryViewModel
                                {
                                    Id = cat.Id,
                                    Cat_name = cat.Name,
                                    Cat_desc = cat.Description
                                }).ToList();

                foreach (var item in category)
                {
                    if (item.Cat_name.Equals(model.Cat_name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ViewBag.message = "Category already exist!";
                        flag = 1;
                        break;
                    }
                }

                if (flag == 1)
                {
                    return View(model);
                }
                else
                {
                    var details = new CategoryDetailsModel { Name = model.Cat_name, Description = model.Cat_desc, Created_at = DateTime.Now, Updated_at = DateTime.Now };
                    db.CategoryDetails.Add(details);
                    db.SaveChanges();
                    ModelState.Clear();
                    ViewBag.message = "Successfully Created";

                    return RedirectToAction("ViewListCategory");
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AddCategory(AddCategoryViewModel model, int redirect)
        {
            if (ModelState.IsValid)
            {
                int flag = 0;
                var db = new ApplicationDbContext();
                var category = (from cat in db.CategoryDetails
                                select new CategoryViewModel
                                {
                                    Id = cat.Id,
                                    Cat_name = cat.Name,
                                    Cat_desc = cat.Description
                                }).ToList();

                foreach (var item in category)
                {
                    if (item.Cat_name.Equals(model.Cat_name, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ViewBag.message = "Category already exist!";
                        flag = 1;
                        break;
                    }
                }

                if (flag == 1)
                {
                    return View();
                }
                else
                {
                    var details = new CategoryDetailsModel { Name = model.Cat_name, Description = model.Cat_desc, Created_at = DateTime.Now, Updated_at = DateTime.Now };
                    db.CategoryDetails.Add(details);
                    db.SaveChanges();
                    ModelState.Clear();
                    ViewBag.message = "Successfully Created";

                    if (redirect == 1)
                    {
                        redirect = 0;
                        return RedirectToAction("AddProduct");
                    }
                    else
                    {
                        return View();
                    }
                }
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult EditCategory(int id)
        {
            var db = new ApplicationDbContext();
            var cat = db.CategoryDetails.Where(x => x.Id == id).FirstOrDefault();

            if (cat != null)
            {
                TempData["Key"] = id;
                TempData.Keep();
                return View(cat);
            }

            return View();
        }

        [HttpPost]
        public ActionResult EditCategory(CategoryDetailsModel model)
        {
            Int32 id = (int)TempData["Key"];

            var db = new ApplicationDbContext();
            var cat = db.CategoryDetails.Where(x => x.Id == id).FirstOrDefault();

            if (cat != null)
            {
                if (model.Name == null)
                {
                    cat.Name = cat.Name;
                }
                else
                {
                    cat.Name = model.Name;
                }

                if (model.Description == null)
                {
                    cat.Description = cat.Description;
                }
                else
                {
                    cat.Description = model.Description;
                }

                cat.Updated_at = DateTime.Now;
                db.Entry(cat).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ViewListCategory");
            }

            return View(model);
        }

        public ActionResult DeleteCategory(int id)
        {
            var db = new ApplicationDbContext();
            if (id > 0)
            {
                var byid = db.CategoryDetails.Where(x => x.Id == id).FirstOrDefault();
                var checkprod = db.ProductDetails.Where(x => x.Category_Id == byid.Id).ToList();
                if (checkprod.Count > 0)
                {
                    return RedirectToAction("ViewListCategory");
                }
                if (byid != null)
                {
                    db.Entry(byid).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("ViewListCategory");
        }

        public ActionResult ProductDiscounts()
        {
            var user = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.CoopId == coopAdmin.Coop_code).ToList();
            var model = new ProductDiscount();
            var currDate = DateTime.Now;
            List<ProductCost> cost = new List<ProductCost>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manu = new List<ProductManufacturer>();

            foreach (var item in prod)
            {
                var discountCheck = db.DiscountModels.Where(x => x.CoopID == coopAdmin.Coop_code).ToList();
                int flag = 0;

                foreach (var disCheck in discountCheck)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    var startDate = Convert.ToDateTime(disCheck.DateStart, culture);
                    var endStart = Convert.ToDateTime(disCheck.DateEnd, culture);
                    if (startDate <= currDate && endStart > currDate)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (disProdCheck.ProductId == item.Id)
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                }

                if (flag == 0)
                {
                    var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getPrice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    cost.Add(new ProductCost
                    {
                        ProdId = item.Id,
                        Cost = getCost.Cost
                    });

                    price.Add(new PriceTable
                    {
                        ProdId = item.Id,
                        Price = getPrice.Price
                    });

                    manu.Add(new ProductManufacturer
                    {
                        ProdId = item.Id,
                        Manufacturer = getmanu.Manufacturer
                    });
                }
            }

            model.Discount = new DiscountModel();
            model.Product = prod;
            model.Cost = cost;
            model.Price = price;
            model.Manufacturer = manu;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProductDiscounts(ProductDiscount model)
        {
            var user = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.CoopId == coopAdmin.Coop_code).ToList();
            List<ProductCost> cost = new List<ProductCost>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manu = new List<ProductManufacturer>();

            foreach (var item in prod)
            {
                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getPrice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                cost.Add(new ProductCost
                {
                    ProdId = item.Id,
                    Cost = getCost.Cost
                });

                price.Add(new PriceTable
                {
                    ProdId = item.Id,
                    Price = getPrice.Price
                });

                manu.Add(new ProductManufacturer
                {
                    ProdId = item.Id,
                    Manufacturer = getmanu.Manufacturer
                });
            }

            model.Product = prod;
            model.Cost = cost;
            model.Price = price;
            model.Manufacturer = manu;

            string selected = Request.Form["isCheck"].ToString();
            string[] selectedList = selected.Split(',');
            var date1 = model.Discount.DateStart;
            var date2 = model.Discount.DateEnd;

            var discount = new DiscountModel
            {
                UserID = user,
                CoopID = coopAdmin.Coop_code,
                Name = model.Discount.Name,
                Percent = model.Discount.Percent,
                DateStart = date1,
                DateEnd = date2
            };

            db.DiscountModels.Add(discount);
            db.SaveChanges();

            foreach (string item in selectedList)
            {
                var discountID = db.DiscountModels.Where(x => x.UserID == user).OrderByDescending(x => x.Id).FirstOrDefault();
                var discountItems = new DiscountedProduct { DiscountID = discountID.Id, ProductId = Convert.ToInt32(item) };
                db.DiscountedProducts.Add(discountItems);
                db.SaveChanges();
            }

            var notif = new NotificationModel
            {
                ToRole = "Non-Member",
                ToUser = "",
                NotifFrom = user,
                NotifHeader = "Products from " + coopDetails.CoopName + "is currently on discount. Order now!",
                NotifMessage = "Click here go shop!",
                NavigateURL = "ViewCoop/?coopID=" + coopDetails.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToRole);

            notif = new NotificationModel
            {
                ToRole = "Member",
                ToUser = "",
                ToCOOP_ID = coopDetails.Id.ToString(),
                NotifFrom = user,
                NotifHeader = "Products from " + coopDetails.CoopName + "is currently on discount. Order now!",
                NotifMessage = "Click here go shop!",
                NavigateURL = "ViewCoop/?coopID=" + coopDetails.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            objNotifHub.SendNotification(notif.ToRole);

            ViewBag.Message = "Discount/s has been applied!";
            return View(model);
        }

        public ActionResult EditDiscount(int id)
        {
            var db = new ApplicationDbContext();
            var discount = db.DiscountModels.Where(x => x.Id == id).FirstOrDefault();

            if (discount != null)
            {
                TempData["Key"] = id;
                TempData.Keep();
            }

            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.CoopId == coopAdmin.Coop_code).ToList();
            var disProd = db.DiscountedProducts.Where(p => p.DiscountID == id).ToList();
            var model = new EditProductDiscount();
            var currDate = DateTime.Now;
            List<ProductDetailsModel2> prod2 = new List<ProductDetailsModel2>();
            List<ProductCost> cost = new List<ProductCost>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manu = new List<ProductManufacturer>();

            foreach (var item in prod)
            {
                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getPrice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var discountCheck = db.DiscountModels.Where(x => x.CoopID == coopAdmin.Coop_code).ToList();
                int flag = 0;

                foreach (var disCheck in discountCheck)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    var startDate = Convert.ToDateTime(disCheck.DateStart, culture);
                    var endStart = Convert.ToDateTime(disCheck.DateEnd, culture);
                    if (startDate <= currDate && endStart > currDate && disCheck.Id != id)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (disProdCheck.ProductId == item.Id)
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                }

                if (flag == 0)
                {
                    cost.Add(new ProductCost
                    {
                        ProdId = item.Id,
                        Cost = getCost.Cost
                    });

                    price.Add(new PriceTable
                    {
                        ProdId = item.Id,
                        Price = getPrice.Price
                    });

                    manu.Add(new ProductManufacturer
                    {
                        ProdId = item.Id,
                        Manufacturer = getmanu.Manufacturer
                    });

                    var disProd2 = db.DiscountedProducts.Where(p => p.DiscountID == id && p.ProductId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    if (disProd2 != null)
                    {
                        prod2.Add(new ProductDetailsModel2
                        {
                            Id = item.Id,
                            isChecked = true,
                            Product_Name = item.Product_Name,
                            Product_image = item.Product_image,
                            Product_qty = item.Product_qty,
                            Product_sold = item.Product_sold,
                            ExpiryDate = item.ExpiryDate,
                        });
                    }
                    else
                    {
                        prod2.Add(new ProductDetailsModel2
                        {
                            Id = item.Id,
                            isChecked = false,
                            Product_Name = item.Product_Name,
                            Product_image = item.Product_image,
                            Product_qty = item.Product_qty,
                            Product_sold = item.Product_sold,
                            ExpiryDate = item.ExpiryDate,
                        });
                    }
                }
            }

            model.Discount = discount;
            model.Product = prod2;
            model.Cost = cost;
            model.Price = price;
            model.Manufacturer = manu;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDiscount(EditProductDiscount model)
        {
            Int32 id = (int)TempData["Key"];
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.CoopId == coopAdmin.Coop_code).ToList();
            var disProd = db.DiscountedProducts.Where(p => p.DiscountID == id).ToList();
            var currDate = DateTime.Now;
            List<ProductDetailsModel2> prod2 = new List<ProductDetailsModel2>();
            List<ProductCost> cost = new List<ProductCost>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manu = new List<ProductManufacturer>();

            foreach (var item in prod)
            {
                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getPrice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var discountCheck1 = db.DiscountModels.Where(x => x.CoopID == coopAdmin.Coop_code).ToList();
                int flag = 0;

                foreach (var disCheck in discountCheck1)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    var startDate = Convert.ToDateTime(disCheck.DateStart, culture);
                    var endStart = Convert.ToDateTime(disCheck.DateEnd, culture);
                    if (startDate <= currDate && endStart > currDate && disCheck.Id != id)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (disProdCheck.ProductId == item.Id)
                            {
                                flag = 1;
                                break;
                            }
                        }
                    }
                }

                if (flag == 0)
                {
                    cost.Add(new ProductCost
                    {
                        ProdId = item.Id,
                        Cost = getCost.Cost
                    });

                    price.Add(new PriceTable
                    {
                        ProdId = item.Id,
                        Price = getPrice.Price
                    });

                    manu.Add(new ProductManufacturer
                    {
                        ProdId = item.Id,
                        Manufacturer = getmanu.Manufacturer
                    });

                    var disProd2 = db.DiscountedProducts.Where(p => p.DiscountID == id && p.ProductId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    if (disProd2 != null)
                    {
                        prod2.Add(new ProductDetailsModel2
                        {
                            Id = item.Id,
                            isChecked = true,
                            Product_Name = item.Product_Name,
                            Product_image = item.Product_image,
                            Product_qty = item.Product_qty,
                            Product_sold = item.Product_sold,
                            ExpiryDate = item.ExpiryDate,
                        });
                    }
                    else
                    {
                        prod2.Add(new ProductDetailsModel2
                        {
                            Id = item.Id,
                            isChecked = false,
                            Product_Name = item.Product_Name,
                            Product_image = item.Product_image,
                            Product_qty = item.Product_qty,
                            Product_sold = item.Product_sold,
                            ExpiryDate = item.ExpiryDate,
                        });
                    }
                }
            }

            model.Product = prod2;
            model.Cost = cost;
            model.Price = price;
            model.Manufacturer = manu;

            string selected = Request.Form["isCheck"].ToString();
            string[] selectedList = selected.Split(',');
            var date1 = model.Discount.DateStart;
            var date2 = model.Discount.DateEnd;
            var curDate = currDate.Date;
            var discountCheck = db.DiscountModels.Where(x => x.Id == id).FirstOrDefault();

            if (discountCheck != null)
            {
                discountCheck.Name = model.Discount.Name;
                discountCheck.Percent = model.Discount.Percent;
                discountCheck.DateStart = model.Discount.DateStart;
                discountCheck.DateEnd = model.Discount.DateEnd;
                db.Entry(discountCheck).State = EntityState.Modified;
                db.SaveChanges();

                var discountProd = db.DiscountedProducts.Where(p => p.DiscountID == id).ToList();
                foreach (var item2 in discountProd)
                {
                    db.Entry(item2).State = EntityState.Deleted;
                    db.SaveChanges();
                }

                foreach (string item3 in selectedList)
                {
                    var discountItems = new DiscountedProduct { DiscountID = id, ProductId = Convert.ToInt32(item3) };
                    db.DiscountedProducts.Add(discountItems);
                    db.SaveChanges();
                }

                ModelState.Clear();
            }

            ViewBag.message = "Edited Successfully";
            return View(model);
        }

        public ActionResult DiscountDetails(int id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var prod = db.ProductDetails.Where(p => p.CoopId == coopAdmin.Coop_code).ToList();
            var discount = db.DiscountModels.Where(p => p.Id == id).FirstOrDefault();
            var model = new EditProductDiscount();
            List<ProductDetailsModel2> prod2 = new List<ProductDetailsModel2>();
            List<ProductCost> cost = new List<ProductCost>();
            List<PriceTable> price = new List<PriceTable>();
            List<ProductManufacturer> manu = new List<ProductManufacturer>();

            foreach (var item in prod)
            {
                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                var getPrice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                cost.Add(new ProductCost
                {
                    ProdId = item.Id,
                    Cost = getCost.Cost
                });

                price.Add(new PriceTable
                {
                    ProdId = item.Id,
                    Price = getPrice.Price
                });

                manu.Add(new ProductManufacturer
                {
                    ProdId = item.Id,
                    Manufacturer = getmanu.Manufacturer
                });

                var disProd2 = db.DiscountedProducts.Where(p => p.DiscountID == id && p.ProductId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                if (disProd2 != null)
                {
                    decimal discPrice = getPrice.Price - (getPrice.Price * (discount.Percent / 100));
                    prod2.Add(new ProductDetailsModel2
                    {
                        Id = item.Id,
                        isChecked = true,
                        Product_Name = item.Product_Name,
                        Product_image = item.Product_image,
                        Product_qty = item.Product_qty,
                        DiscountedPrice = decimal.Round(discPrice, 2, MidpointRounding.AwayFromZero),
                        Product_sold = item.Product_sold,
                        ExpiryDate = item.ExpiryDate,
                    });
                }
            }

            model.Discount = discount;
            model.Product = prod2;
            model.Cost = cost;
            model.Price = price;
            model.Manufacturer = manu;

            return View(model);
        }

        public ActionResult DeleteDiscount(int id)
        {
            var db = new ApplicationDbContext();
            if (id > 0)
            {
                var byid = db.DiscountModels.Where(x => x.Id == id).FirstOrDefault();
                if (byid != null)
                {
                    db.Entry(byid).State = EntityState.Deleted;
                    db.SaveChanges();

                    var discountProd = db.DiscountedProducts.Where(p => p.DiscountID == id).ToList();
                    foreach (var item2 in discountProd)
                    {
                        db.Entry(item2).State = EntityState.Deleted;
                        db.SaveChanges();
                    }
                }
            }

            return RedirectToAction("ViewListDiscount");
        }

        public ActionResult ViewListDiscount()
        {
            var user = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var discounts = (from disc in db.DiscountModels
                             where disc.CoopID == coopAdmin.Coop_code
                             select new DiscountViewModel
                             {
                                 Id = disc.Id,
                                 Name = disc.Name,
                                 Percent = disc.Percent,
                                 DateStart = disc.DateStart,
                                 DateEnd = disc.DateEnd
                             }).OrderByDescending(x => x.Id).ToList();

            return View(discounts);
        }

        //End of Product Module
        //Start of User Modules for Drivers and Members
        [AllowAnonymous]
        public ActionResult CreateDriver()
        {
            var model = new RegisterDriverViewModel();
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            model.CStatuslist = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            return View(model);
        }

        string userid = "";
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateDriver(RegisterDriverViewModel model, HttpPostedFileBase file, HttpPostedFileBase file2)
        {
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            model.CStatuslist = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            int gen = 0, stat = 0, license = 0;
            if (model.DriverFile == null)
            {
                license = 1;
            }
            if (model.Gender == null)
            {
                gen = 1;
            }
            if (model.CStatus == null)
            {
                stat = 1;
            }
            if (gen == 1 || stat == 1 || license == 1)
            {
                ViewBag.GenderError = "Please select your gender.";
                ViewBag.StatusError = "Please select your marital status.";
                ViewBag.License = "Please upload your driver's license";
                model.Latitude = string.Empty;
                model.Longitude = string.Empty;
                ModelState.Clear();
                return View(model);
            }
            if (ModelState.IsValid)
            {
                if (model.ImageFile != null)
                {
                    if(ValidateFile(model.ImageFile) != true)
                    {
                        ViewBag.message = "Please choose one Image File";
                        return View(model);
                    }
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                string pass = model.Lastname + model.Bdate.ToString();
                var result = await UserManager.CreateAsync(user, pass);
                var user2 = await UserManager.FindByIdAsync(User.Identity.GetUserId()); //Get the Current loggedin userid for foreign key
                string id = user2.Id;
                userid = id;
                var db = new ApplicationDbContext();
                var coopUser = User.Identity.GetUserId();
                var coopId = db.CoopAdminDetails.Where(x => x.UserId == coopUser).FirstOrDefault();
                var db2 = new ApplicationDbContext();
                if (result.Succeeded)
                {
                    string name = Path.GetFileNameWithoutExtension(model.DriverFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.DriverFile.FileName);
                    if (model.ImageFile == null)
                    {
                        var details = new DriverDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Driver_License = model.Driver_License, Contact = model.Contact, Address = model.Address, Gender = model.Gender, Bdate = model.Bdate.ToString(), PlateNum = model.PlateNum, Created_at = DateTime.Now, Updated_at = DateTime.Now, UserId = user.Id, CoopId = coopId.Coop_code.ToString() };
                        model.Driver_License = name + extension;
                        model.Image = "defaultprofile.jpg";
                        details.IsOnDuty = true;
                        details.IsAvailable = true;
                        details.IsActive = "Active";
                        details.CStatus = model.CStatus;
                        details.Driver_License = model.Driver_License;
                        details.PlateNum = model.PlateNum;
                        var myfile = name + extension;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db.Locations.Add(location);
                        db.DriverDetails.Add(details);
                        db.SaveChanges();
                        model.DriverFile.SaveAs(path);
                        await this.UserManager.AddToRoleAsync(user.Id, "Driver");
                        ModelState.Clear();
                        UpdateConfirm(user.Id);
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();
                        return RedirectToAction("ViewDriverList");
                    }
                    var allowedExtensions1 = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                    string name1 = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension1 = Path.GetExtension(model.ImageFile.FileName);
                    if ( ValidateFile(model.ImageFile)==true) //check what type of extension  
                    {
                        model.Image = name1 + extension1;
                        var myfile = name1 + extension1;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        var details = new DriverDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Driver_License = model.Driver_License, Contact = model.Contact, Address = model.Address, Gender = model.Gender, Bdate = model.Bdate.ToString(), PlateNum = model.PlateNum, Created_at = DateTime.Now, Updated_at = DateTime.Now, UserId = user.Id, CoopId = coopId.Coop_code.ToString() };
                        details.IsOnDuty = true;
                        model.Driver_License = name + extension1;
                        details.Driver_License = model.Driver_License;
                        details.IsAvailable = true;
                        details.IsActive = "Active";
                        details.CStatus = model.CStatus;
                        var myfile1 = name + extension;
                        var path1 = Path.Combine(Server.MapPath("../Images/"), myfile1);
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db2.Locations.Add(location);
                        db.DriverDetails.Add(details);
                        db.SaveChanges();
                        db2.SaveChanges();
                        await this.UserManager.AddToRoleAsync(user.Id, "Driver");
                        model.ImageFile.SaveAs(path);
                        ModelState.Clear();
                        model.DriverFile.SaveAs(path1);
                        UpdateConfirm(user.Id);
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();
                        return RedirectToAction("ViewDriverList");
                    }
                    else
                    {
                        ViewBag.message = "Please choose only Image file";
                    }
                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        [HttpGet]
        public ActionResult EditDriver(Int32 id)
        {
            var viewmodel = new RegisterDriverViewModel();
            var db = new ApplicationDbContext();
            var getuser = db.DriverDetails.Where(x => x.Id == id).FirstOrDefault();
            if (getuser != null)
            {
                viewmodel.Address = getuser.Address;
                viewmodel.Contact = getuser.Contact;
                viewmodel.CStatus = getuser.CStatus;
                viewmodel.Gender = getuser.Gender;
                viewmodel.PlateNum = getuser.PlateNum;
                viewmodel.Driver_License = getuser.Driver_License;
                viewmodel.Firstname = getuser.Firstname;
                viewmodel.Lastname = getuser.Lastname;
                viewmodel.ActiveStatus = getuser.IsActive;
                viewmodel.Bdate = getuser.Bdate;
                viewmodel.Driver_License = getuser.Driver_License;
                viewmodel.Image = getuser.Image;
                TempData["id"] = id;
            }
            viewmodel.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            viewmodel.CStatuslist = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            viewmodel.SelectStatus = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Active", Value = "Active"},
                new SelectListItem {Selected = false, Text = "Inactive", Value = "Inactive"}
            }, "Value", "Text", 1);
            return View(viewmodel);
        }

        [HttpPost]
        public ActionResult EditDriver(RegisterDriverViewModel model, HttpPostedFileBase file, HttpPostedFileBase file2)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            Int32 id = (int)TempData["id"];
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            model.CStatuslist = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            model.SelectStatus = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Active", Value = "Active"},
                new SelectListItem {Selected = false, Text = "Inactive", Value = "Inactive"}
            }, "Value", "Text", 1);
            int gen = 0, stat = 0;
            var getuser = db.DriverDetails.Where(x => x.Id == id).FirstOrDefault();
            var location = db.Locations.Where(x => x.UserId == getuser.UserId).FirstOrDefault();
            model.Image = getuser.Image;
            model.Driver_License = getuser.Driver_License;

            if (model.Gender == null)
            {
                gen = 1;
            }
            if (model.CStatus == null)
            {
                stat = 1;
            }
            if (gen == 1 || stat == 1)
            {
                ViewBag.GenderError = "Please select your gender.";
                ViewBag.StatusError = "Please select your marital status.";
                ViewBag.License = "Please upload your driver's license";
                model.Latitude = string.Empty;
                model.Longitude = string.Empty;
                model.Address = string.Empty;
                model.Image = getuser.Image;
                model.Driver_License = getuser.Driver_License;
                ModelState.Clear();
                return View(model);
            }
            if (model.ImageFile != null)
            {
                if(ValidateFile(model.ImageFile) != true)
                {
                    ViewBag.message = "Please choose one Image File";
                    return View(model);
                }
                
            }
            if (getuser != null)
            {
                if (model.DriverFile == null)
                    model.Driver_License = getuser.Driver_License;
                else
                {
                    string name = Path.GetFileNameWithoutExtension(model.DriverFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.DriverFile.FileName);
                    model.Driver_License = name + extension;
                    var myfile = name + extension;
                    var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                    model.DriverFile.SaveAs(path);
                }
                if (model.ImageFile == null)
                {
                    getuser.IsActive = model.ActiveStatus;
                    getuser.Driver_License = model.Driver_License;
                    if (model.Address == null)
                        getuser.Address = getuser.Address;
                    else
                        getuser.Address = model.Address;
                    if (model.Bdate == null)
                        getuser.Bdate = getuser.Bdate;
                    else
                        getuser.Bdate = model.Bdate.ToString();
                    if (model.Contact == null)
                        getuser.Contact = getuser.Contact;
                    else
                        getuser.Contact = model.Contact;
                    if (model.PlateNum == null)
                        getuser.PlateNum = getuser.PlateNum;
                    else
                        getuser.PlateNum = model.PlateNum;
                    if (model.Lastname != null)
                        getuser.Lastname = model.Lastname;
                    else
                        getuser.Lastname = getuser.Lastname;
                    if (model.Firstname != null)
                        getuser.Firstname = model.Firstname;
                    else
                        getuser.Firstname = getuser.Firstname;
                    getuser.Gender = model.Gender;
                    getuser.CStatus = model.CStatus;
                    getuser.Updated_at = DateTime.Now;

                    if (model.Longitude == null)
                    {
                        model.Longitude = location.Geolocation.Longitude.ToString();
                    }
                    if (model.Latitude == null)
                    {
                        model.Latitude = location.Geolocation.Latitude.ToString();
                    }
                    location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                    location.Address = model.Address;
                    var logs = new UserLogs();
                    var driverAcc = db.Users.Where(da => da.Id == getuser.UserId).FirstOrDefault();
                    var userrole = driverAcc.Roles.Where(x => x.UserId == driverAcc.Id).FirstOrDefault();
                    var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                    logs.Logs = driverAcc.Email + " account updated";
                    logs.Role = getuserrole.Name;
                    logs.Date = DateTime.Now.ToString();
                    db2.Logs.Add(logs);
                    db2.SaveChanges();
                    db.Entry(location).State = EntityState.Modified;
                    db.SaveChanges();
                    db.Entry(getuser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("ViewDriverList");
                }
                var allowedExtensions1 = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                string name1 = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                string extension1 = Path.GetExtension(model.ImageFile.FileName);
                if (ValidateFile(model.ImageFile) == true)
                {
                    model.Image = name1 + extension1;
                    getuser.Image = model.Image;
                    getuser.IsActive = model.ActiveStatus;
                    getuser.Driver_License = model.Driver_License;
                    if (model.Address == null)
                    {
                        getuser.Address = getuser.Address;
                    }
                    else
                    {
                        getuser.Address = model.Address;
                    }
                    string date = model.Bdate.ToString();
                    if (date == "1/1/0001 12:00:00 AM")
                    {
                        getuser.Bdate = getuser.Bdate;
                    }
                    else
                    {
                        getuser.Bdate = model.Bdate.ToString();
                    }
                    if (model.Contact == null)
                    {
                        getuser.Contact = getuser.Contact;
                    }
                    else
                    {
                        getuser.Contact = model.Contact;
                    }
                    if (model.PlateNum == null)
                    {
                        getuser.PlateNum = getuser.PlateNum;
                    }
                    else
                    {
                        getuser.PlateNum = model.PlateNum;
                    }
                    if (model.Lastname != null)
                    {
                        getuser.Lastname = model.Lastname;
                    }
                    else
                    {
                        getuser.Lastname = getuser.Lastname;
                    }
                    if (model.Firstname != null)
                    {
                        getuser.Firstname = model.Firstname;
                    }
                    else
                    {
                        getuser.Firstname = getuser.Firstname;
                    }
                    getuser.Gender = model.Gender;
                    getuser.CStatus = model.CStatus;
                    getuser.Updated_at = DateTime.Now;

                    var myfile1 = name1 + extension1;
                    var path1 = Path.Combine(Server.MapPath("../Images/"), myfile1);
                    if (model.Longitude == null)
                    {
                        model.Longitude = location.Geolocation.Longitude.ToString();
                    }
                    if (model.Latitude == null)
                    {
                        model.Latitude = location.Geolocation.Latitude.ToString();
                    }
                    location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                    location.Address = model.Address;
                    var logs = new UserLogs();
                    var user = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
                    var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                    var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                    logs.Logs = model.Email + " account created";
                    logs.Role = getuserrole.Name;
                    logs.Date = DateTime.Now.ToString();
                    db2.Logs.Add(logs);
                    db2.SaveChanges();
                    db.Entry(location).State = EntityState.Modified;
                    db.Entry(getuser).State = EntityState.Modified;
                    model.ImageFile.SaveAs(path1);
                    db.SaveChanges();
                    return RedirectToAction("ViewDriverList");
                }
            }
            return View(model);
        }

        [AllowAnonymous]
        public ActionResult CreateMember()
        {
            var db = new ApplicationDbContext();
            var model = new RegisterViewModel();
            var user = User.Identity.GetUserId();
            var checkcoop = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            var checkmemberdisc = db.CoopMemberDiscounts.Where(x => x.COOP_ID == checkcoop.Coop_code).ToList();
            if(checkmemberdisc.Count == 0)
            {
                ViewBag.Errormessage = "Please create member's discount first. Thank you.";
            }
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateMember(RegisterViewModel model, HttpPostedFileBase file)
        {
            int gen = 0;
            var db2 = new ApplicationDbContext();
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            var user3 = User.Identity.GetUserId();
            var checkcoop = db2.CoopAdminDetails.Where(x => x.UserId == user3).FirstOrDefault();
            var checkmemberdisc = db2.CoopMemberDiscounts.Where(x => x.COOP_ID == checkcoop.Coop_code).ToList();
            if (checkmemberdisc.Count == 0)
            {
                return RedirectToAction("MembershipApplication", new { numMessage = 5 });
            }
            if (model.Gender == null)
            {
                gen = 1;
            }
            if (gen == 1)
            {
                ViewBag.GenderError = "Please select your gender.";
                return View(model);
            }
            if (ModelState.IsValid)
            {
                if (model.ImageFile != null)
                {
                    if(ValidateFile(model.ImageFile) != true)
                    {
                        ViewBag.message = "Please choose one Image File";
                        return View(model);
                    }
                }
                var db4 = new ApplicationDbContext();
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                string pass = model.Lastname + model.Bdate.ToString();
                var result = await UserManager.CreateAsync(user, pass);
                var user2 = await UserManager.FindByIdAsync(User.Identity.GetUserId()); //Get the Current loggedin userid for foreign key
                string id = user2.Id;
                userid = id;
                var coopAdmin = db4.CoopAdminDetails.Where(x => x.UserId == userid).FirstOrDefault();
                
                if (result.Succeeded)
                {
                    var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                    if (model.ImageFile == null)
                    {
                        var db = new ApplicationDbContext();
                        var details = new CustomerDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Contact = model.Contact, Gender = model.Gender, Role = "Member", AccountId = user.Id, CoopAdminId = id, CoopId = coopAdmin.Coop_code.ToString() };
                        details.Image = "defaultprofile.jpg";
                        details.IsActive = "Active";
                        details.MemberLock = "Active";
                        var cart = new UserCart { UserId = user.Id };
                        var checkout = new UserCheckout { UserId = user.Id };
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db.Locations.Add(location);
                        db.UCheckout.Add(checkout);
                        db.Cart.Add(cart);
                        db.UserDetails.Add(details);
                        db.SaveChanges();

                        var ewallet = new EWallet();
                        ewallet.UserID = user.Id;
                        ewallet.Balance = 0;
                        ewallet.Created_At = DateTime.Now;
                        ewallet.Status = "Active";
                        db.UserEWallet.Add(ewallet);
                        db.SaveChanges();

                        await this.UserManager.AddToRoleAsync(user.Id, "Member"); // mao diay ni djuls hahahaha para create sa kato role ba try nako balik huh
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();


                        ModelState.Clear();
                        UpdateConfirm(user.Id);
                        return RedirectToAction("ViewMemberList");
                    }
                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    if (ValidateFile(model.ImageFile)==true) //check what type of extension  
                    {
                        model.Image = name + extension;
                        var myfile = name + extension;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        var db = new ApplicationDbContext();
                        var details = new CustomerDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Contact = model.Contact, Gender = model.Gender, Role = "Member", AccountId = user.Id, CoopAdminId = id, CoopId = coopAdmin.Coop_code.ToString() };
                        details.IsActive = "Active";
                        details.MemberLock = "Active";
                        var cart = new UserCart { UserId = user.Id };
                        var checkout = new UserCheckout { UserId = user.Id };
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db.Locations.Add(location);
                        db.UCheckout.Add(checkout);
                        db.Cart.Add(cart);
                        db.UserDetails.Add(details);
                        db.SaveChanges();
                        model.ImageFile.SaveAs(path);
                        await this.UserManager.AddToRoleAsync(user.Id, "Member");

                        var ewallet = new EWallet();
                        ewallet.UserID = user.Id;
                        ewallet.Balance = 0;
                        ewallet.Created_At = DateTime.Now;
                        ewallet.Status = "Active";
                        db.UserEWallet.Add(ewallet);
                        db.SaveChanges();

                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();
                        ModelState.Clear();
                        UpdateConfirm(user.Id);
                        return RedirectToAction("ViewMemberList");
                    }
                    else
                    {
                        ViewBag.message = "Please choose only Image file";
                    }


                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //Confirming email of Members' and Drivers' created account
        private void UpdateConfirm(string Id)
        {
            var db = new ApplicationDbContext();
            var user = db.Users.Where(x => x.Id == Id).FirstOrDefault();
            if (user != null)
            {
                user.EmailConfirmed = true;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        public async Task<ActionResult> DetailsOfUser(int? id)
        {
            var db = new ApplicationDbContext();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CustomerDetailsModel viewuser = await db.UserDetails.FindAsync(id);
            if (viewuser == null)
            {
                return HttpNotFound();
            }
            return View(viewuser);
        }

        public ActionResult EditUser(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user = db.UserDetails.Where(x => x.Id == id).FirstOrDefault();
            var viewmodel = new RegisterViewModel();
            viewmodel.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            viewmodel.SelectStatus = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Active", Value = "Active"},
                new SelectListItem {Selected = false, Text = "Inactive", Value = "Inactive"}
            }, "Value", "Text", 1);
            if (user != null)
            {
                viewmodel.Firstname = user.Firstname;
                viewmodel.Lastname = user.Lastname;
                viewmodel.Image = user.Image;
                viewmodel.Gender = user.Gender;
                viewmodel.Bdate = user.Bdate;
                viewmodel.Contact = user.Contact;
                viewmodel.Address = user.Address;
                viewmodel.IsActive = user.IsActive;
                TempData["Id"] = id;
                TempData.Keep();
                return View(viewmodel);
            }
            return View();
        }

        [HttpPost]
        public ActionResult EditUser(RegisterViewModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            Int32 userid = (int)TempData["Id"];
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            model.SelectStatus = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Active", Value = "Active"},
                new SelectListItem {Selected = false, Text = "Inactive", Value = "Inactive"}
            }, "Value", "Text", 1);
            var getuser = db.UserDetails.Where(x => x.Id == userid).FirstOrDefault();
            var location = db.Locations.Where(x => x.UserId == getuser.AccountId).FirstOrDefault();
            var db2 = new ApplicationDbContext();
            if (getuser != null)
            {
                if (model.ImageFile != null)
                {
                    if(ValidateFile(model.ImageFile) != true)
                    {
                        ViewBag.message = "Please choose one Image File";
                        return View(model);
                    }
                    
                }
                if (model.ImageFile == null)
                {
                    getuser.Image = getuser.Image;
                }
                else
                {
                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    model.Image = name + extension;
                    var myfile = name + extension;
                    var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                }
                if (model.Address == null)
                {
                    getuser.Address = getuser.Address;
                }
                else
                {
                    getuser.Address = model.Address;
                }
                string date = model.Bdate.ToString();
                if (date == "1/1/0001 12:00:00 AM")
                {
                    getuser.Bdate = getuser.Bdate;
                }
                else
                {
                    getuser.Bdate = model.Bdate.ToString();
                }
                if (model.Contact == null)
                {
                    getuser.Contact = getuser.Contact;
                }
                else
                {
                    getuser.Contact = model.Contact;
                }
                if (model.Lastname != null)
                {
                    getuser.Lastname = model.Lastname;
                }
                else
                {
                    getuser.Lastname = getuser.Lastname;
                }
                if (model.Firstname != null)
                {
                    getuser.Firstname = model.Firstname;
                }
                else
                {
                    getuser.Firstname = getuser.Firstname;
                }
                if (model.Longitude == null)
                {
                    model.Longitude = location.Geolocation.Longitude.ToString();
                }
                if (model.Latitude == null)
                {
                    model.Latitude = location.Geolocation.Latitude.ToString();
                }
                location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                location.Address = model.Address;
                getuser.Gender = model.Gender;
                getuser.Updated_at = DateTime.Now;
                getuser.IsActive = model.IsActive;
                db.Entry(location).State = EntityState.Modified;
                db.Entry(getuser).State = EntityState.Modified;
                db.SaveChanges();
                var logs = new UserLogs();
                var user = db.Users.Where(x => x.Id == getuser.AccountId).FirstOrDefault();
                var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = model.Email + " account updated";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                return RedirectToAction("ViewMemberList");
            }
            return View(model);
        }

        //Voucher Module
        [HttpGet]
        public ActionResult CreateVoucher()
        {
            var model = new VoucherDetailsModel();
            model.DiscType = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Percent", Value = "Percent"},
                new SelectListItem { Selected = false, Text = "Peso", Value = "Peso"}
            }, "Value", "Text");
            model.UserTypeList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Member", Value = "Member"},
                new SelectListItem { Selected = false, Text = "Non-Member", Value = "Non-Member"},
                new SelectListItem { Selected = false, Text = "Both", Value = "Both"},
            }, "Value", "Text");

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateVoucher(VoucherDetailsModel model)
        {
            model.DiscType = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Percent", Value = "Percent"},
                new SelectListItem { Selected = false, Text = "Peso", Value = "Peso"},
            }, "Value", "Text");
            model.UserTypeList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Member", Value = "Member"},
                new SelectListItem { Selected = false, Text = "Non-Member", Value = "Non-Member"},
                new SelectListItem { Selected = false, Text = "Both", Value = "Both"},
            }, "Value", "Text");

            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();

            if (ModelState.IsValid)
            {
                var details = new VoucherDetailsModel();
                details.VoucherCode = GetCode();
                details.DiscountType = model.DiscountType;
                details.Name = model.Name;
                details.Min_spend = model.Min_spend;
                details.DateStart = model.DateStart;
                details.ExpiryDate = model.ExpiryDate;
                details.UserType = model.UserType;
                details.Created_at = DateTime.Now;
                details.Updated_at = DateTime.Now;
                details.Percent_Discount = model.Percent_Discount;
                details.CoopAdminId = user;
                details.CoopId = coopAdmin.Coop_code;
                db.VoucherDetails.Add(details);
                db.SaveChanges();
                ModelState.Clear();

                if (model.UserType == "Non-Member")
                {
                    var notif = new NotificationModel
                    {
                        ToRole = "Non-Member",
                        ToUser = "",
                        NotifFrom = user,
                        NotifHeader = "A voucher is waiting for you at " + coop.CoopName + ". Order now before it runs out!",
                        NotifMessage = "Click here and apply now!",
                        NavigateURL = "ViewCoop/?coopID=/" + coop.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();
                    NotificationHub objNotifHub = new NotificationHub();
                    objNotifHub.SendNotification(notif.ToRole);
                }
                else if (model.UserType == "Member")
                {
                    var notif = new NotificationModel
                    {
                        ToRole = "Member",
                        ToUser = "",
                        ToCOOP_ID = coop.Id.ToString(),
                        NotifFrom = user,
                        NotifHeader = "A voucher is waiting for you at " + coop.CoopName + ". Order now before it runs out!",
                        NotifMessage = "Click here and apply now!",
                        NavigateURL = "ViewCoop/?coopID=/" + coop.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();
                    NotificationHub objNotifHub = new NotificationHub();
                    objNotifHub.SendNotification(notif.ToRole);
                }
                else
                {
                    var notif = new NotificationModel
                    {
                        ToRole = "Non-Member",
                        ToUser = "",
                        NotifFrom = user,
                        NotifHeader = "A voucher is waiting for you at " + coop.CoopName + ". Order now before it runs out!",
                        NotifMessage = "Click here and apply now!",
                        NavigateURL = "ViewCoop/?coopID=" + coop.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();
                    NotificationHub objNotifHub = new NotificationHub();
                    objNotifHub.SendNotification(notif.ToRole);

                    notif = new NotificationModel
                    {
                        ToRole = "Member",
                        ToUser = "",
                        ToCOOP_ID = coop.Id.ToString(),
                        NotifFrom = user,
                        NotifHeader = "A voucher is waiting for you at " + coop.CoopName + ". Order now before it runs out!",
                        NotifMessage = "Click here and apply now!",
                        NavigateURL = "ViewCoop/?coopID=" + coop.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();
                    objNotifHub.SendNotification(notif.ToRole);
                }

                ViewBag.Message = "Voucher has been created successfully!";
            }

            return View(model);
        }

        public string GetCode()
        {
            string finalString = GenerateCode(8);
            return finalString;
        }

        public string GenerateCode(int length)
        {
            using (var crypto = new RNGCryptoServiceProvider())
            {
                var bits = (length * 6);
                var byte_size = ((bits + 7) / 8);
                var bytesarray = new byte[byte_size];
                crypto.GetBytes(bytesarray);
                return Convert.ToBase64String(bytesarray);
            }
        }

        public ActionResult ViewVoucherList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var voucher = (from vouch in db.VoucherDetails
                           where vouch.CoopId == coopAdmin.Coop_code
                           select new ViewListVouch
                           {
                               Id = vouch.Id,
                               Name = vouch.Name,
                               Percent_Discount = vouch.Percent_Discount,
                               Min_spend = vouch.Min_spend,
                               DateStart = vouch.DateStart,
                               ExpiryDate = vouch.ExpiryDate,
                               Created_at = vouch.Created_at,
                               Updated_at = vouch.Updated_at,
                               DiscountType = vouch.DiscountType
                           }).OrderByDescending(x => x.Id).ToList();

            return View(voucher);
        }

        [HttpGet]
        public ActionResult EditVoucher(int id)
        {
            var db = new ApplicationDbContext();
            var vouch = db.VoucherDetails.Where(x => x.Id == id).FirstOrDefault();

            vouch.DiscType = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Percent", Value = "Percent"},
                new SelectListItem { Selected = false, Text = "Peso", Value = "Peso"},
            }, "Value", "Text");
            vouch.UserTypeList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Member", Value = "Member"},
                new SelectListItem { Selected = false, Text = "Non-Member", Value = "Non-Member"},
                new SelectListItem { Selected = false, Text = "Both", Value = "Both"},
            }, "Value", "Text");

            if (vouch != null)
            {
                TempData["Key"] = id;
                TempData.Keep();
                return View(vouch);
            }

            return View();
        }

        [HttpPost]
        public ActionResult EditVoucher(VoucherDetailsModel model)
        {
            model.DiscType = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Percent", Value = "Percent"},
                new SelectListItem { Selected = false, Text = "Peso", Value = "Peso"},
            }, "Value", "Text");
            model.UserTypeList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem { Selected = false, Text = "Member", Value = "Member"},
                new SelectListItem { Selected = false, Text = "Non-Member", Value = "Non-Member"},
                new SelectListItem { Selected = false, Text = "Both", Value = "Both"},
            }, "Value", "Text");

            Int32 id = (int)TempData["Key"];

            var db = new ApplicationDbContext();
            var vouch = db.VoucherDetails.Where(x => x.Id == id).FirstOrDefault();

            if (vouch != null)
            {
                vouch.Name = model.Name;
                vouch.Percent_Discount = model.Percent_Discount;
                vouch.DiscountType = model.DiscountType;
                vouch.UserType = model.UserType;
                vouch.Min_spend = model.Min_spend;
                vouch.DateStart = model.DateStart;
                vouch.ExpiryDate = model.ExpiryDate;
                vouch.Updated_at = DateTime.Now;
                db.Entry(vouch).State = EntityState.Modified;
                db.SaveChanges();
                ViewBag.Message = "Voucher has been edited successfully!";
            }

            return View(model);
        }

        public ActionResult DeleteVoucher(int id)
        {
            var db = new ApplicationDbContext();
            if (id > 0)
            {
                var byid = db.VoucherDetails.Where(x => x.Id == id).FirstOrDefault();
                if (byid != null)
                {
                    db.Entry(byid).State = EntityState.Deleted;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("ViewVoucherList");
        }

        public ActionResult VoucherDetails(string id)
        {
            var db = new ApplicationDbContext();
            var voucher = db.VoucherDetails.Where(v => v.Id.ToString() == id).FirstOrDefault();

            return View(voucher);
        }

        public ActionResult ViewProfile()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var viewprof = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            return View(viewprof);
        }

        public ActionResult EditMyProfile(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var viewprof = db.CoopAdminDetails.Where(x => x.ID == id).FirstOrDefault();
            var location = db.Locations.Where(x => x.UserId == user).FirstOrDefault();
            var myprofile = new RegisterCoopAdminViewmodel();
            myprofile.Image = viewprof.Image;
            myprofile.Address = viewprof.Address;
            myprofile.Bdate = viewprof.Bdate;
            myprofile.Contact = viewprof.Contact;
            myprofile.CStatus = viewprof.Status;
            myprofile.Firstname = viewprof.Firstname;
            myprofile.Lastname = viewprof.Lastname;
            myprofile.Latitude = location.Geolocation.Latitude.ToString();
            myprofile.Longitude = location.Geolocation.Longitude.ToString();
            myprofile.Gender = viewprof.Gender;
            myprofile.CStatusList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            myprofile.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            return View(myprofile);
        }
        [HttpPost]
        public ActionResult EditMyProfile(RegisterCoopAdminViewmodel model, HttpPostedFileBase file)
        {
            var db1 = new ApplicationDbContext();
            var db3 = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            model.CStatusList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
            }, "Value", "Text", 1);
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            var getuser = db1.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var location = db3.Locations.Where(x => x.UserId == user).FirstOrDefault();
            string[] genders = { "Male", "Female", "LGBTQ", "Rather Not Say" };
            string[] statuslist = { "Single", "Married", "Divorced", "Separated", "Widowed" };
            int flag = 0;
            if(!genders.Contains(model.Gender))
            {
                flag = 1;
                ViewBag.ErrorGender = "Please select a proper value.";
            }
            if(!statuslist.Contains(model.CStatus))
            {
                flag = 1;
                ViewBag.ErrorStatus = "Please select a proper value.";
            }
            if(flag != 0)
            {
                model.Image = getuser.Image;
                model.Address = getuser.Address;
                model.Bdate = getuser.Bdate;
                model.Contact = getuser.Contact;
                model.CStatus = getuser.Status;
                model.Firstname = getuser.Firstname;
                model.Lastname = getuser.Lastname;
                model.Latitude = location.Geolocation.Latitude.ToString();
                model.Longitude = location.Geolocation.Longitude.ToString();
                model.Gender = getuser.Gender;
                return View(model);
            }
            
            if (model.ImageFile == null)
            {
                getuser.Image = getuser.Image;
            }
            else
            {
                string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                string extension = Path.GetExtension(model.ImageFile.FileName);
                model.Image = name + extension;
                var myfile = name + extension;
                var path = Path.Combine(Server.MapPath("../Images/"), myfile);
            }
            if (model.Address == null)
            {
                getuser.Address = getuser.Address;
            }
            else
            {
                getuser.Address = model.Address;
            }
            if (model.Bdate == null)
            {
                getuser.Bdate = getuser.Bdate;
            }
            else
            {
                getuser.Bdate = model.Bdate;
            }
            if (model.Contact == null)
            {
                getuser.Contact = getuser.Contact;
            }
            else
            {
                getuser.Contact = model.Contact;
            }
            if (model.Lastname != null)
            {
                getuser.Lastname = model.Lastname;
            }
            else
            {
                getuser.Lastname = getuser.Lastname;
            }
            if (model.Firstname != null)
            {
                getuser.Firstname = model.Firstname;
            }
            else
            {
                getuser.Firstname = getuser.Firstname;
            }
            if (model.Longitude == null)
            {
                model.Longitude = location.Geolocation.Longitude.ToString();
            }
            if (model.Latitude == null)
            {
                model.Latitude = location.Geolocation.Latitude.ToString();
            }
            getuser.Gender = model.Gender;
            getuser.Updated_at = DateTime.Now;

            db1.Entry(getuser).State = EntityState.Modified;
            db1.SaveChanges();

            location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
            location.Address = model.Address;
            db3.Entry(location).State = EntityState.Modified;
            db3.SaveChanges();
            
            var db2 = new ApplicationDbContext();
            var logs = new UserLogs();
            var user2 = db2.Users.Where(x => x.Id == getuser.UserId).FirstOrDefault();
            var userrole = user2.Roles.Where(x => x.UserId == user2.Id).FirstOrDefault();
            var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
            logs.Logs = model.Email + " account updated";
            logs.Role = getuserrole.Name;
            logs.Date = DateTime.Now.ToString();
            db2.Logs.Add(logs);
            db2.SaveChanges();
            return RedirectToAction("Index", "Manage");
        }

        public ActionResult Resign()
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();

            coopAdminDetails.IsResign = "True";
            coopAdminDetails.DateResigned = DateTime.Now.ToString();
            db.Entry(coopAdminDetails).State = EntityState.Modified;
            db.SaveChanges();

            ewallet.Status = "On-Hold";
            db.Entry(ewallet).State = EntityState.Modified;
            db.SaveChanges();

            var ewalletHistory = new EWalletHistory();
            ewalletHistory.EWallet_ID = ewallet.ID;
            ewalletHistory.Amount = ewallet.Balance;
            ewalletHistory.Action = "Account On-Hold";
            ewalletHistory.Description = "COOP Admin " + coopAdminDetails.Firstname + " " + coopAdminDetails.Lastname + " has resigned. E-Wallet will be On-Hold.";
            ewalletHistory.Created_At = DateTime.Now;
            db.EWalletHistories.Add(ewalletHistory);
            db.SaveChanges();

            var logs = new UserLogs();
            var user2 = db2.Users.Where(x => x.Id == user).FirstOrDefault();
            var userrole = user2.Roles.Where(x => x.UserId == user).FirstOrDefault();
            var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();

            logs.Logs = user2.Email + " coop admin resigned";
            logs.Role = getuserrole.Name;
            logs.Date = DateTime.Now.ToString();
            db2.Logs.Add(logs);
            db2.SaveChanges();
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);

            var superAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var notif = new NotificationModel
            {
                ToRole = "Admin",
                ToUser = superAdmin.Id,
                NotifFrom = coopAdminDetails.Coop_code.ToString(),
                NotifHeader = "COOP Admin resigned.",
                NotifMessage = "COOP Admin of COOP " + coopDetails.CoopName + " has resigned.",
                NavigateURL = "ListOfCoop",
                IsRead = false,
                DateReceived = DateTime.Now
            };
            db2.Notifications.Add(notif);
            db2.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToUser);
            return RedirectToAction("Login", "Account");
        }

        public ActionResult ArrangePickUp(string id)
        {
            var db = new ApplicationDbContext();
            List<DriverDetails2> drivers = new List<DriverDetails2>();
            var model = new ArrangePickUpDetails();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            int userOrderId = Convert.ToInt32(id);
            var userOrder = db.UserOrders.Where(u => u.Id == userOrderId).FirstOrDefault();
            var customer = db.Locations.Where(l => l.UserId == userOrder.UserId).FirstOrDefault();
            var deliveryDrivers = db.DriverDetails.Where(d => d.CoopId == coopAdmin.Coop_code.ToString() && d.IsAvailable == true && d.IsOnDuty == true).ToList();
            var getcooploc = (from coop in db.CoopDetails
                              join co in db.CoopAdminDetails
                              on coop.Id equals co.Coop_code
                              join loc in db.CoopLocations
                              on coop.Id.ToString() equals loc.CoopId
                              where co.UserId == user
                              select new
                              {
                                  CoopId = co.UserId,
                                  Longitude = loc.Geolocation.Longitude,
                                  Latitude = loc.Geolocation.Latitude
                              }).FirstOrDefault();

            if (deliveryDrivers.Count == 0)
            {
                return RedirectToAction("OrderDetails", new { id = id, driver = 0 });
            }

            foreach (var rider in deliveryDrivers)
            {
                var deliverStatus = db.DeliverStatus.Where(ds => ds.DriverId == rider.UserId && ds.Status == "To Pick-Up" || ds.Status == "To be Delivered").ToList();
                var forDel = deliverStatus.Count();

                drivers.Add(new DriverDetails2
                {
                    UserId = rider.UserId,
                    Image = rider.Image,
                    Firstname = rider.Firstname,
                    Lastname = rider.Lastname,
                    ContactNo = rider.Contact,
                    ForDelivery = forDel
                });
            }

            Session["uorderid"] = id;
            model.UserOrderId = userOrderId;
            model.Drivers = drivers;
            return View(model);
        }

        [HttpPost]
        public ActionResult ArrangePickUp(ArrangePickUpDetails model)
        {
            var db = new ApplicationDbContext();

            if (Request["driverId"] == null)
            {
                string id2 = Session["uorderid"].ToString();
                return RedirectToAction("ArrangePickUp", new { id = id2 });
            }

            string selected = Request.Form["driverId"].ToString();
            List<DriverDetails2> drivers = new List<DriverDetails2>();
            string id = Session["uorderid"].ToString();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            int userOrderId = Convert.ToInt32(id);
            var userOrder = db.UserOrders.Where(u => u.Id == userOrderId).FirstOrDefault();
            var customer = db.Locations.Where(l => l.UserId == userOrder.UserId).FirstOrDefault();
            var deliveryDrivers = db.DriverDetails.Where(d => d.CoopId == coopAdmin.Coop_code.ToString() && d.IsAvailable == true && d.IsOnDuty == true).ToList();
            var getcooploc = (from coop in db.CoopDetails
                              join co in db.CoopAdminDetails
                              on coop.Id equals co.Coop_code
                              join loc in db.CoopLocations
                              on coop.Id.ToString() equals loc.CoopId
                              where co.UserId == user
                              select new
                              {
                                  CoopId = co.UserId,
                                  Longitude = loc.Geolocation.Longitude,
                                  Latitude = loc.Geolocation.Latitude
                              }).FirstOrDefault();

            if (deliveryDrivers.Count == 0)
            {
                return RedirectToAction("OrderDetails", new { id = id });
            }

            foreach (var rider in deliveryDrivers)
            {
                var deliverStatus = db.DeliverStatus.Where(ds => ds.DriverId == rider.UserId && ds.Status == "To Pick-Up" || ds.Status == "To be Delivered").ToList();
                var forDel = deliverStatus.Count();

                drivers.Add(new DriverDetails2
                {
                    UserId = rider.UserId,
                    Image = rider.Image,
                    Firstname = rider.Firstname,
                    Lastname = rider.Lastname,
                    ContactNo = rider.Contact,
                    ForDelivery = forDel
                });
            }

            model.UserOrderId = userOrderId;
            model.Drivers = drivers;
            var date = model.PickUpDate.ToString();
            if (date == "1/1/0001 12:00:00 AM")
            {
                return View(model);
            }
            var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var riderBook = new DeliveryStatus { DriverId = selected, PickUpDate = model.PickUpDate, ExpectedDeldate = DateTime.Now.AddDays(3), UserOrderId = model.UserOrderId, Status = "To Pick-Up" };
            db.DeliverStatus.Add(riderBook);
            db.SaveChanges();

            userOrder.OStatus = "Ready for pick-up.";
            db.Entry(userOrder).State = EntityState.Modified;
            db.SaveChanges();

            var userDetails = db.UserDetails.Where(u => u.AccountId == userOrder.UserId).FirstOrDefault();
            var notif = new NotificationModel
            {
                ToRole = userDetails.Role,
                ToUser = userDetails.AccountId,
                NotifFrom = user,
                NotifHeader = "Your order is ready for pick-up.",
                NotifMessage = "Order " + model.UserOrderId + "from " + coopDetails.CoopName + " is ready for pickup",
                NavigateURL = "OrderDetails/" + model.UserOrderId,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToUser);

            notif = new NotificationModel
            {
                ToRole = "",
                ToUser = selected,
                NotifFrom = user,
                NotifHeader = "An order is scheduled for pick-up.",
                NotifMessage = "Order " + model.UserOrderId + "from " + coopDetails.CoopName + " has been assign to you for pick-up",
                NavigateURL = "DeliveryDetails/" + model.UserOrderId,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            objNotifHub.SendNotification(notif.ToUser);

            return RedirectToAction("OrderDetails", new { id = model.UserOrderId });
        }

        public ActionResult ViewNotification()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var model = new ViewNotification();
            var coopAdmin = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            var allCoopAdmin = db.CoopAdminDetails.Where(ca => ca.Coop_code == coopAdmin.Coop_code).ToList();
            List<NotificationModel> unreadNotif = new List<NotificationModel>();
            List<NotificationModel> readNotif = new List<NotificationModel>();
            var unreadNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Coop Admin") && n.IsRead == false).ToList();

            if (unreadNotification != null)
            {
                foreach (var notif in unreadNotification)
                {
                    unreadNotif.Add(new NotificationModel
                    {
                        Id = notif.Id,
                        NavigateURL = notif.NavigateURL,
                        ToUser = user,
                        ToRole = notif.ToRole,
                        NotifFrom = notif.NotifFrom,
                        NotifHeader = notif.NotifHeader,
                        NotifMessage = notif.NotifMessage,
                        IsRead = notif.IsRead,
                        DateReceived = notif.DateReceived
                    });
                }

                foreach (var admin in allCoopAdmin)
                {
                    unreadNotification = db.Notifications.Where(n => n.ToUser == admin.UserId && n.IsRead == false).ToList();

                    foreach (var notif in unreadNotification)
                    {
                        unreadNotif.Add(new NotificationModel
                        {
                            Id = notif.Id,
                            NavigateURL = notif.NavigateURL,
                            ToUser = user,
                            ToRole = notif.ToRole,
                            NotifFrom = notif.NotifFrom,
                            NotifHeader = notif.NotifHeader,
                            NotifMessage = notif.NotifMessage,
                            IsRead = notif.IsRead,
                            DateReceived = notif.DateReceived
                        });
                    }
                }
            }
            else
            {
                unreadNotif = null;
            }

            var readNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Coop Admin") && n.IsRead == true).ToList();

            if (readNotification != null)
            {
                foreach (var notif in readNotification)
                {
                    readNotif.Add(new NotificationModel
                    {
                        Id = notif.Id,
                        NavigateURL = notif.NavigateURL,
                        ToUser = user,
                        ToRole = notif.ToRole,
                        NotifFrom = notif.NotifFrom,
                        NotifHeader = notif.NotifHeader,
                        NotifMessage = notif.NotifMessage,
                        IsRead = notif.IsRead,
                        DateReceived = notif.DateReceived
                    });
                }

                foreach (var admin in allCoopAdmin)
                {
                    readNotification = db.Notifications.Where(n => n.ToUser == admin.UserId && n.IsRead == true).ToList();

                    foreach (var notif in unreadNotification)
                    {
                        readNotif.Add(new NotificationModel
                        {
                            Id = notif.Id,
                            NavigateURL = notif.NavigateURL,
                            ToUser = user,
                            ToRole = notif.ToRole,
                            NotifFrom = notif.NotifFrom,
                            NotifHeader = notif.NotifHeader,
                            NotifMessage = notif.NotifMessage,
                            IsRead = notif.IsRead,
                            DateReceived = notif.DateReceived
                        });
                    }
                }
            }
            else
            {
                readNotif = null;
            }

            model.Unread = unreadNotif;
            model.Read = readNotif;

            return View(model);
        }

        public ActionResult NotifRead()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            var notifId = Request["id"];
            var notif = db.Notifications.Where(n => n.Id.ToString() == notifId).FirstOrDefault();

            notif.IsRead = true;
            db.Entry(notif).State = EntityState.Modified;
            db.SaveChanges();

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult InboxChat()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(u => u.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.CoopId == coopDetails.Id.ToString()).ToList();
            List<ViewInbox> inbox = new List<ViewInbox>();
            var model = new ViewChat();

            if (coopChat != null)
            {
                foreach (var chat in coopChat)
                {
                    var chatMessage = db.ChatMessages.Where(cm => cm.CoopChatId == chat.Id).OrderByDescending(cm => cm.Id).FirstOrDefault();
                    var customerDetails = db.UserDetails.Where(cd => cd.AccountId == chat.UserId).FirstOrDefault();

                    inbox.Add(new ViewInbox
                    {
                        InboxId = chat.Id,
                        ReceiversName = customerDetails.Firstname + " " + customerDetails.Lastname,
                        ReceiversId = customerDetails.AccountId,
                        SenderName = coopDetails.CoopName,
                        LatestMessage = chatMessage.MessageBody,
                        DateSent = chatMessage.DateSent,
                        IsRead = chatMessage.IsRead,
                    });
                }
            }
            else
            {
                inbox = null;
            }

            return View(inbox);
        }

        public ActionResult CoopChat(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(u => u.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.UserId == id && cc.CoopId == coopDetails.Id.ToString()).FirstOrDefault();
            List<ChatMessage> messages = new List<ChatMessage>();
            var model = new ViewChat();

            if (coopChat != null)
            {
                var customerDetails = db.UserDetails.Where(u => u.AccountId == coopChat.UserId).FirstOrDefault();
                model.ReceiversName = customerDetails.Firstname + " " + customerDetails.Lastname;
                model.ReceiversId = coopChat.UserId;

                var chatMessages = db.ChatMessages.Where(cm => cm.CoopChatId == coopChat.Id).ToList();

                if (chatMessages != null)
                {
                    foreach (var message in chatMessages)
                    {
                        var from = "";

                        if (message.From == customerDetails.AccountId)
                        {
                            from = customerDetails.Firstname + " " + customerDetails.Lastname;
                        }
                        else
                        {
                            from = coopDetails.CoopName;
                        }

                        messages.Add(new ChatMessage
                        {
                            CoopChatId = message.CoopChatId,
                            From = from,
                            MessageBody = message.MessageBody,
                            DateSent = message.DateSent,
                            IsRead = message.IsRead
                        });
                    }
                }
                else
                {
                    messages = null;
                }
            }
            else
            {
                coopChat = null;
            }

            model.SenderName = coopDetails.CoopName;
            model.Messages = messages;

            return View(model);
        }

        [HttpPost]
        public ActionResult CoopChat(ViewChat model)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(u => u.UserId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == coopAdminDetails.Coop_code).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.CoopId == coopDetails.Id.ToString() && cc.UserId == model.ReceiversId).FirstOrDefault();
            List<ChatMessage> messages = new List<ChatMessage>();

            if (coopChat != null)
            {
                var chatMessage = new ChatMessage
                {
                    CoopChatId = coopChat.Id,
                    From = coopDetails.Id.ToString(),
                    MessageBody = model.MessageToSend,
                    DateSent = DateTime.Now,
                    IsRead = false,
                };

                db.ChatMessages.Add(chatMessage);
                db.SaveChanges();
            }
            else
            {
                coopChat = new CoopChat
                {
                    CoopId = coopDetails.Id.ToString(),
                    UserId = model.ReceiversId
                };

                db.CoopChats.Add(coopChat);
                db.SaveChanges();

                coopChat = db.CoopChats.Where(cc => cc.UserId == model.ReceiversId && cc.CoopId == coopDetails.Id.ToString()).FirstOrDefault();

                var chatMessage = new ChatMessage
                {
                    CoopChatId = coopChat.Id,
                    From = coopDetails.Id.ToString(),
                    MessageBody = model.MessageToSend,
                    DateSent = DateTime.Now,
                    IsRead = false,
                };

                db.ChatMessages.Add(chatMessage);
                db.SaveChanges();
            }

            if (coopChat != null)
            {
                var customerDetails = db.UserDetails.Where(u => u.AccountId == coopChat.UserId).FirstOrDefault();
                model.ReceiversName = customerDetails.Firstname + " " + customerDetails.Lastname;
                model.ReceiversId = coopChat.UserId;

                var chatMessages = db.ChatMessages.Where(cm => cm.CoopChatId == coopChat.Id).ToList();

                if (chatMessages != null)
                {
                    foreach (var message in chatMessages)
                    {
                        var from = "";

                        if (message.From == customerDetails.AccountId)
                        {
                            from = customerDetails.Firstname + " " + customerDetails.Lastname;
                        }
                        else
                        {
                            from = coopDetails.CoopName;
                        }

                        messages.Add(new ChatMessage
                        {
                            CoopChatId = message.CoopChatId,
                            From = from,
                            MessageBody = message.MessageBody,
                            DateSent = message.DateSent,
                            IsRead = message.IsRead
                        });
                    }
                }
                else
                {
                    messages = null;
                }
            }
            else
            {
                coopChat = null;
            }

            model.SenderName = coopDetails.CoopName;
            model.Messages = messages;
            model.MessageToSend = "";

            return View(model);
        }

        public ActionResult ToShipPage()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && x.OStatus == "Ready for pick-up.").ToList();
            decimal total = 0;
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                total = 0;
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();
                var prodorder = db.ProdOrders.Where(x => x.CoopId == coopAdmin.Coop_code && x.UOrderId == order.Id.ToString()).ToList();

                foreach (var item in prodorder)
                {
                    total += item.SubTotal;
                }

                var voucher = db.VoucherUseds.Where(v => v.UserOrderId == order.Id.ToString()).FirstOrDefault();

                if (voucher != null)
                {
                    var voucherDetails = db.VoucherDetails.Where(vd => vd.VoucherCode == voucher.VoucherCode).FirstOrDefault();

                    if (voucherDetails.DiscountType == "Percent")
                    {
                        var discount = total * (Convert.ToDecimal(voucherDetails.Percent_Discount) / 100);
                        total = total - discount;
                    }
                    else
                    {
                        total = total - (Convert.ToDecimal(voucherDetails.Percent_Discount));
                    }
                }

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = total,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult ToReceivePage()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && x.OStatus == "To Be Delivered").ToList();
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult CompletedPage()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && (x.OStatus == "Complete" || x.OStatus == "Transferred")).ToList();
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult CancelledPage()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var userOrder = db.UserOrders.Where(x => x.CoopId == coopAdmin.Coop_code && x.OStatus == "Cancelled").ToList();
            List<OrderList> orderlist = new List<OrderList>();

            foreach (var order in userOrder)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                orderlist.Add(new OrderList
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            return View(orderlist);
        }

        public ActionResult MemberApplicants()
        {
            var db = new ApplicationDbContext();
            List<ViewApplicantsModel> model = new List<ViewApplicantsModel>();
            var user = User.Identity.GetUserId();
            var coopdetail = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == coopdetail.Coop_code).FirstOrDefault();

            var showapplicants = db.Memberships.Where(x => x.Coop_code == coop.Id.ToString() && x.RequestStatus == "Pending").ToList();
            foreach (var applicant in showapplicants)
            {
                var getinfo = db.UserDetails.Where(x => x.AccountId == applicant.UserId).FirstOrDefault();
                model.Add(new ViewApplicantsModel { Filepath = applicant.Formpath, Fullname = getinfo.Firstname + " " + getinfo.Lastname, Id = applicant.Id });
            }

            return View(model);
        }

        public ActionResult ApproveApplicant(int? id)
        {
            var db = new ApplicationDbContext();
            var form = db.Memberships.Where(x => x.Id == id).FirstOrDefault();
            if (form != null)
            {
                form.RequestStatus = "To be payed";
                db.Entry(form).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("MemberApplicants");
        }

        public List<UserOrder> GetUserOrders()
        {
            var db = new ApplicationDbContext();
            List<UserOrder> order = new List<UserOrder>();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            var userOrders = db.UserOrders.Where(u => u.CoopId == coopAdminDetails.Coop_code).ToList();

            order = userOrders;

            return (order);
        }

        public List<ProdOrder> GetProdOrders()
        {
            var db = new ApplicationDbContext();
            List<ProdOrder> product = new List<ProdOrder>();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            var prodOrders = db.ProdOrders.Where(p => p.CoopId == coopAdminDetails.Coop_code).ToList();

            product = prodOrders;

            return (product);
        }

        public List<UserVoucherUsed> GetVoucherUsed()
        {
            var db = new ApplicationDbContext();
            List<UserVoucherUsed> voucher = new List<UserVoucherUsed>();
            var user = User.Identity.GetUserId();
            var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.UserId == user).FirstOrDefault();
            var voucherUsed = db.VoucherUseds.Where(v => v.CoopId == coopAdminDetails.Coop_code).ToList();

            voucher = voucherUsed;

            return (voucher);
        }

        public ActionResult ViewCoopSales()
        {
            var db = new ApplicationDbContext();
            var model = new ViewSalesReport();

            List<SalesReport> orderlist = new List<SalesReport>();
            List<UserOrder> userOrders = GetUserOrders();
            List<ProdOrder> prodOrders = GetProdOrders();
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed();

            foreach (var order in userOrders)
            {
                var customer = db.UserDetails.Where(c => c.AccountId == order.UserId).FirstOrDefault();
                var prodorder = db.ProdOrders.Where(x => x.UOrderId == order.Id.ToString()).ToList();
                var coopDetails = db.CoopDetails.Where(c => c.Id == order.CoopId).FirstOrDefault();

                orderlist.Add(new SalesReport
                {
                    OrderNo = order.Id.ToString(),
                    TotalAmount = order.TotalPrice - order.CommissionFee,
                    CommisionFee = order.CommissionFee,
                    CoopName = coopDetails.CoopName,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    Contact = customer.Contact,
                    Address = customer.Address,
                    Delivery_fee = order.Delivery_fee.ToString(),
                });
            }

            model.SalesReports = orderlist;

            return View(model);
        }

        [HttpPost]
        public ActionResult ViewCoopSales(ViewSalesReport model)
        {
            var db = new ApplicationDbContext();
            List<SalesReport> orderlist = new List<SalesReport>();
            List<ViewBySale> viewBySale = new List<ViewBySale>();
            List<UserOrder> userOrders = GetUserOrders();
            List<ProdOrder> prodOrders = GetProdOrders();
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed();
            List<string> date = new List<string>();
            decimal total = 0;
            var coop_id = 0;

            if (model.ViewBy != null && model.ViewBy == "Yearly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    DateTime getdate = Convert.ToDateTime(order.OrderCreated_at, CultureInfo.CurrentCulture);
                    var year = getdate.Year;

                    total = order.TotalPrice;

                    if (!date.Contains(year.ToString()))
                    {
                        date.Add(year.ToString());
                        var coopDetails = db.CoopDetails.Where(c => c.Id == order.CoopId).FirstOrDefault();

                        viewBySale.Add(new ViewBySale
                        {
                            CoopId = order.CoopId,
                            CoopName = coopDetails.CoopName,
                            Date = year.ToString(),
                            TotalPrice = total,
                        });
                    }
                    else
                    {
                        foreach (var view in viewBySale)
                        {
                            if (view.Date == year.ToString() && view.CoopId == coop_id)
                            {
                                view.TotalPrice += total;
                            }
                        }
                    }
                }

                total = 0;
                date.Clear();
                orderlist = null;
            }
            else if (model.ViewBy != null && model.ViewBy == "Monthly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    DateTime getdate = Convert.ToDateTime(order.OrderCreated_at, CultureInfo.CurrentCulture);
                    var month = getdate.Month;
                    var year = getdate.Year;
                    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                    total = order.TotalPrice;
                    coop_id = order.CoopId;

                    if (!date.Contains(monthName + " " + year.ToString()))
                    {
                        date.Add(monthName + " " + year.ToString());

                        var coopDetails = db.CoopDetails.Where(c => c.Id == order.CoopId).FirstOrDefault();
                        viewBySale.Add(new ViewBySale
                        {
                            CoopId = order.CoopId,
                            CoopName = coopDetails.CoopName,
                            Date = monthName + " " + year.ToString(),
                            TotalPrice = total,
                        });
                    }
                    else
                    {
                        foreach (var view in viewBySale)
                        {
                            if (view.Date == monthName + " " + year.ToString() && view.CoopId == coop_id)
                            {
                                view.TotalPrice += total;
                            }
                        }
                    }
                }

                total = 0;
                date.Clear();
                orderlist = null;
            }
            else if (model.ViewBy != null && model.ViewBy == "Weekly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    DateTime getdate = Convert.ToDateTime(order.OrderCreated_at, CultureInfo.CurrentCulture);
                    //DateTime getdate = DateTime.Parse(order.OrderCreated_at, culture);
                    var week = GetWeekNumberOfMonth(getdate);
                    var month = getdate.Month;
                    var year = getdate.Year;
                    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                    total = order.TotalPrice;
                    coop_id = order.CoopId;

                    if (!date.Contains("Week " + week + " of " + monthName + " " + year.ToString()))
                    {
                        date.Add("Week " + week + " of " + monthName + " " + year.ToString());

                        var coopDetails = db.CoopDetails.Where(c => c.Id == order.CoopId).FirstOrDefault();
                        viewBySale.Add(new ViewBySale
                        {
                            CoopId = order.CoopId,
                            CoopName = coopDetails.CoopName,
                            Date = "Week " + week + " of " + monthName + " " + year.ToString(),
                            TotalPrice = total,
                        });
                    }
                    else
                    {
                        foreach (var view in viewBySale)
                        {
                            if (view.Date == "Week " + week + " of " + monthName + " " + year.ToString() && view.CoopId == coop_id)
                            {
                                view.TotalPrice += total;
                            }
                        }
                    }
                }

                total = 0;
                date.Clear();
                orderlist = null;
            }
            else
            {
                foreach (var order in userOrders)
                {
                    var customer = db.UserDetails.Where(c => c.AccountId == order.UserId).FirstOrDefault();
                    var prodorder = db.ProdOrders.Where(x => x.UOrderId == order.Id.ToString()).ToList();
                    var coopDetails = db.CoopDetails.Where(c => c.Id == order.CoopId).FirstOrDefault();

                    orderlist.Add(new SalesReport
                    {
                        OrderNo = order.Id.ToString(),
                        TotalAmount = order.TotalPrice - order.CommissionFee,
                        CommisionFee = order.CommissionFee,
                        CoopName = coopDetails.CoopName,
                        CustomerName = customer.Firstname + " " + customer.Lastname,
                        Contact = customer.Contact,
                        Address = customer.Address,
                        Delivery_fee = order.Delivery_fee.ToString(),
                    });
                }

                viewBySale = null;
            }

            model.SalesReports = orderlist;
            model.ViewBySales = viewBySale;
            return View(model);
        }

        static int GetWeekNumberOfMonth(DateTime date)
        {
            date = date.Date;
            DateTime firstMonthDay = new DateTime(date.Year, date.Month, 1);
            DateTime firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);

            if (firstMonthMonday > date)
            {
                firstMonthDay = firstMonthDay.AddMonths(-1);
                firstMonthMonday = firstMonthDay.AddDays((DayOfWeek.Monday + 7 - firstMonthDay.DayOfWeek) % 7);
            }

            return (date - firstMonthMonday).Days / 7 + 1;
        }

        public ActionResult TransactionReport()
        {
            var db = new ApplicationDbContext();
            List<WithdrawViewModel> model = new List<WithdrawViewModel>();
            var user = User.Identity.GetUserId();
            var coopde1 = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var transactions = db.Withdraw.Where(x => x.RequestStatus == "Completed" && x.CoopId == coopde1.Coop_code).ToList();
            foreach (var transaction in transactions)
            {
                var coopde = db.CoopDetails.Where(x => x.Id == transaction.CoopId).FirstOrDefault();
                model.Add(new WithdrawViewModel
                {
                    Amount = transaction.Amount,
                    Contact = transaction.Contact,
                    Fullname = transaction.Fullname,
                    Method = transaction.Method,
                    Receipt = transaction.Receipt,
                    ChargeFee = transaction.ChargeFee,
                    DateRequested = transaction.DateReqeuested,
                    DateFulfilled = transaction.DateFulfilled,
                    Email = transaction.Email
                });
            }
            return View(model);
        }

        public ActionResult PendingTransactions()
        {
            var db = new ApplicationDbContext();
            List<WithdrawViewModel> model = new List<WithdrawViewModel>();
            var user = User.Identity.GetUserId();
            var coopde1 = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var transactions = db.Withdraw.Where(x => x.RequestStatus == "Pending" && x.CoopId == coopde1.Coop_code).ToList();
            foreach (var transaction in transactions)
            {
                var coopde = db.CoopDetails.Where(x => x.Id == transaction.CoopId).FirstOrDefault();
                model.Add(new WithdrawViewModel
                {
                    Amount = transaction.Amount,
                    Contact = transaction.Contact,
                    Fullname = transaction.Fullname,
                    Method = transaction.Method,
                    Receipt = transaction.Receipt,
                    ChargeFee = transaction.ChargeFee,
                    DateRequested = transaction.DateReqeuested,
                    DateFulfilled = transaction.DateFulfilled,
                    Email = transaction.Email
                });
            }
            return View(model);
        }

        public ActionResult AccountPayables()
        {
            var db = new ApplicationDbContext();
            var model = new AccountPayablesRepModel();
            List<CommissionSale> commissionSales = new List<CommissionSale>();
            
            var user = User.Identity.GetUserId();
            var getuser = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getcoop = db.CoopDetails.Where(x => x.Id == getuser.Coop_code).FirstOrDefault();
            var myuser = db.Users.Where(x => x.Id == user).FirstOrDefault();
            var coop = new CoopDetailsModel();
            DateTime date = DateTime.Now;
            var dateMonth = date.Month;
            var year = date.Year;
            DateTime dueDate = new DateTime(year, dateMonth, 5);
            if (getuser != null)
            {
                coop = db.CoopDetails.Where(x => x.Id == getuser.Coop_code).FirstOrDefault();
                model.Fullname = getuser.Firstname + " " + getuser.Lastname;
                model.Contact = getuser.Contact;
                model.Email = myuser.Email;
            }
            var commissions = db.CommissionSales.Where(x => x.Status == "Pending" && x.CoopCode == coop.Id).ToList();
            model.DueDate = "Every 5th day of the month.";

            if (date == dueDate)
            {
                model.Status = "Kindly pay your due today. If not paid, account will be locked.";
            }

            //if(date > dueDate)
            //{
            //    getcoop.IsLocked = "Locked";
            //    getcoop.DateAccLocked = DateTime.Now.ToString();
            //    var logs = new UserLogs();
            //    var userrole = myuser.Roles.Where(x => x.UserId == user).FirstOrDefault();
            //    var getuserrole = db.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
            //    logs.Logs = myuser.Email + " COOP locked";
            //    logs.Role = getuserrole.Name;
            //    logs.Date = DateTime.Now.ToString();
            //    db.Logs.Add(logs);
            //    db.SaveChanges();
            //    db.Entry(getcoop).State = EntityState.Modified;
            //    db.SaveChanges();
            //}

            if (commissions != null)
            {
                model.CommissionSale = commissions;
                foreach(var comm in commissions)
                {
                    model.TotalTobePay += comm.CommissionFee;
                }
            }

            return View(model);
        }
        [HttpPost]
        public ActionResult AccountPayables(AccountPayablesRepModel model2, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getuser = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var coop = new CoopDetailsModel();
            if (getuser != null)
            {
                coop = db.CoopDetails.Where(x => x.Id == getuser.Coop_code).FirstOrDefault();
            }
            if (model2!=null)
            {
                if(model2.ReceiptFile!=null)
                {
                    if( model2.Mode !=null)
                    {

                        if(ValidateFile(model2.ReceiptFile) == true)
                        {
                            if (model2.Mode == "Paypal" && model2.TotalTobePay > 0 && model2.Email !=null)
                            {
                                string name = Path.GetFileNameWithoutExtension(model2.ReceiptFile.FileName); //getting file name without extension  
                                string extension = Path.GetExtension(model2.ReceiptFile.FileName);
                                var path = Path.Combine(Server.MapPath("../Receipts/"), name + extension);
                                model2.ReceiptFile.SaveAs(path);
                                Session["money"] = model2.TotalTobePay;
                                Session["filename"] = name;
                                Session["extension"] = extension;
                                Session["coopid"] = coop.Id.ToString();
                                Session["accountid"] = user;
                                Session["mode"] = model2.Mode;
                                Session["email"] = model2.Email;
                                return RedirectToAction("Paymentwithpaypal", "Paypalpayable");
                            }
                            else if ((model2.Mode == "MLhuillier" || model2.Mode == "Cebuana" || model2.Mode == "Palawan") && model2.TotalTobePay > 0 && model2.Fullname != null && model2.Contact != null)
                            {
                                
                                string name = Path.GetFileNameWithoutExtension(model2.ReceiptFile.FileName); //getting file name without extension  
                                string extension = Path.GetExtension(model2.ReceiptFile.FileName);
                                var path = Path.Combine(Server.MapPath("../Receipts/"), name + extension);
                                model2.ReceiptFile.SaveAs(path);
                                var accountspay = new AccountsReceived();
                                accountspay.Contact = model2.Contact;
                                accountspay.Email = model2.Email;
                                accountspay.Fullname = model2.Fullname;
                                accountspay.AccountId = user;
                                accountspay.CoopId = coop.Id;
                                accountspay.Receipt = name + extension;
                                accountspay.TotalAmount = model2.TotalTobePay;
                                accountspay.Created_at = DateTime.Now;
                                accountspay.ModeOfPayment = model2.Mode;
                                db.AccountsPayable.Add(accountspay);
                                db.SaveChanges();

                                var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                                var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                                adminEwallet.Balance += model2.TotalTobePay;
                                db.Entry(adminEwallet).State = EntityState.Modified;
                                db.SaveChanges();

                                var ewalletHistory = new EWalletHistory();
                                ewalletHistory = new EWalletHistory();
                                ewalletHistory.EWallet_ID = adminEwallet.ID;
                                ewalletHistory.Amount = model2.TotalTobePay;
                                ewalletHistory.Action = "Accont Payable";
                                ewalletHistory.Description = "Commission fee payments received from " + coop.CoopName + ".";
                                ewalletHistory.Created_At = DateTime.Now;
                                db.EWalletHistories.Add(ewalletHistory);
                                db.SaveChanges();

                                var pendings = db.CommissionSales.Where(x => x.CoopCode == coop.Id && x.Status == "Pending").ToList();
                                if (pendings != null)
                                {
                                    foreach (var pending in pendings)
                                    {
                                        pending.Status = "Received";
                                        pending.Updated_at = DateTime.Now;
                                        db.Entry(pending).State = EntityState.Modified;
                                        db.SaveChanges();
                                    }
                                }
                                model2.IsSucess = true;
                            }
                            else
                            {
                                ViewBag.ErrorMessage = "Something went wrong.";
                            }
                        }
                        
                    }
                }
            }
            List<CommissionSale> commissionSales = new List<CommissionSale>();
            var model = new AccountPayablesRepModel();
            
            
            var commissions = db.CommissionSales.Where(x => x.Status == "Pending" && x.CoopCode == coop.Id).ToList();
            if (commissions != null)
            {
                model.CommissionSale = commissions;
            }
            return View(model);
        }

        public ActionResult ViewReturnRefund()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.CoopId == coopDetails.Coop_code).OrderByDescending(x => x.Status).ToList();
            List<ViewReturnRefundList> returnRefundList = new List<ViewReturnRefundList>();

            foreach (var order in returnRefunds)
            {
                var customer = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();

                returnRefundList.Add(new ViewReturnRefundList
                {
                    ReturnId = order.Id,
                    UOrderId = order.UserOrderId,
                    CustomerName = customer.Firstname + " " + customer.Lastname,
                    ContactNo = customer.Contact,
                    Type = order.Type,
                    RefundAmount = order.RefundAmount
                });
            }

            return View(returnRefundList);
        }

        public ActionResult ReturnRefundDetails(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var coopDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.Id.ToString() == id).FirstOrDefault();
            var returnRefundItems = db.ReturnRefundItems.Where(rr => rr.ReturnId.ToString() == id).ToList();
            var model = new ReturnRefundDetails();
            List<ProdOrder2> returnProd = new List<ProdOrder2>();
            List<ProdOrder2> customerOrder = new List<ProdOrder2>();
            ReturnRefund details = new ReturnRefund();

            details.Id = returnRefunds.Id;
            details.CoopId = returnRefunds.CoopId;
            details.Created_At = returnRefunds.Created_At;
            details.Reason = returnRefunds.Reason;
            details.RefundAmount = returnRefunds.RefundAmount;
            details.Type = returnRefunds.Type;
            details.Status = returnRefunds.Status;
            details.DateAccepted = returnRefunds.DateAccepted;
            details.UserOrderId = returnRefunds.UserOrderId;

            foreach (var item in returnRefundItems)
            {
                var prod = db.ProdOrders.Where(p => p.Id == item.ProdOrderId).FirstOrDefault();
                var prod2 = db.ProductDetails.Where(p => p.Id.ToString() == prod.ProdId).FirstOrDefault();

                returnProd.Add(new ProdOrder2
                {
                    UserId = prod.UserId,
                    CoopId = prod.CoopId,
                    UOrderId = prod.UOrderId,
                    ProdImage = prod2.Product_image,
                    ProdName = prod.ProdName,
                    Price = prod.Price,
                    ProdId = prod.ProdId,
                    MemberDiscountedPrice = prod.MemberDiscountedPrice,
                    DiscountedPrice = prod.DiscountedPrice,
                    Qty = prod.Qty,
                    SubTotal = prod.SubTotal
                });
            }

            model.RefundItem = returnProd;
            model.ReturnRefunds = details;

            return View(model);
        }

        public ActionResult AcceptReturn(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.Id.ToString() == id).FirstOrDefault();
            var returnItems = db.ReturnRefundItems.Where(x => x.ReturnId == returnRefunds.Id).ToList();
            var getuserorder = db.UserOrders.Where(x => x.Id == returnRefunds.UserOrderId).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == getuserorder.CoopId).FirstOrDefault();
            var ewallet = db.UserEWallet.Where(x => x.UserID == getuserorder.UserId && x.Status == "Active").FirstOrDefault();
            returnRefunds.Status = "Accepted";
            returnRefunds.IsAccepted = true;
            returnRefunds.DateAccepted = DateTime.Now;
            db.Entry(returnRefunds).State = EntityState.Modified;
            db.SaveChanges();

            getuserorder.TotalPrice -= returnRefunds.RefundAmount;
            db.Entry(getuserorder).State = EntityState.Modified;
            db.SaveChanges();

            if (getuserorder.ModeOfPay == "E-Wallet" || getuserorder.ModeOfPay == "Paypal")
            {
                var getuser = db.UserDetails.Where(x => x.AccountId == getuserorder.UserId).FirstOrDefault();
                ewallet.Balance += returnRefunds.RefundAmount;
                db.Entry(getuser).State = EntityState.Modified;
                db.SaveChanges();

                var ewalletHistory = new EWalletHistory();
                ewalletHistory.EWallet_ID = ewallet.ID;
                ewalletHistory.Amount = returnRefunds.RefundAmount;
                ewalletHistory.Action = "Refund";
                ewalletHistory.Description = "Order No. " + getuserorder.Id + "was returned/refunded. Payment successfully refunded.";
                ewalletHistory.Created_At = DateTime.Now;
                db.EWalletHistories.Add(ewalletHistory);
                db.SaveChanges();

                var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                adminEwallet.Balance -= returnRefunds.RefundAmount;
                db.Entry(adminEwallet).State = EntityState.Modified;
                db.SaveChanges();

                ewalletHistory = new EWalletHistory();
                ewalletHistory.EWallet_ID = adminEwallet.ID;
                ewalletHistory.Amount = returnRefunds.RefundAmount;
                ewalletHistory.Action = "Refund Payment";
                ewalletHistory.Description = "Order returned/refunded. Payment refunded from Order No. " + getuserorder.Id + ".";
                ewalletHistory.Created_At = DateTime.Now;
                db.EWalletHistories.Add(ewalletHistory);
                db.SaveChanges();
            }

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = getuserorder.UserId,
                NotifFrom = user,
                NotifHeader = coopDetails.CoopName + " accepted your return/refund request.",
                NotifMessage = "Click here to learn more.",
                NavigateURL = "CoopDetailView/",
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToRole);

            return RedirectToAction("ViewReturnRefund");
        }

        public ActionResult RejectReturn(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.Id.ToString() == id).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == returnRefunds.CoopId).FirstOrDefault();
            returnRefunds.Status = "Rejected";
            returnRefunds.IsAccepted = false;
            returnRefunds.DateAccepted = DateTime.Now;
            db.Entry(returnRefunds).State = EntityState.Modified;
            db.SaveChanges();

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = returnRefunds.UserId,
                NotifFrom = user,
                NotifHeader = coopDetails.CoopName + " rejected your return/refund request.",
                NotifMessage = "Click here to learn more.",
                NavigateURL = "CoopDetailView/",
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToRole);


            return RedirectToAction("ViewReturnRefund");
        }

        public ActionResult ViewEwalletHistory()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user).FirstOrDefault();
            var ewalletHistory = db.EWalletHistories.Where(x => x.EWallet_ID == ewallet.ID).OrderByDescending(x => x.Created_At).ToList();

            return View(ewalletHistory);
        }

        public ActionResult EwalletHistoryDetails(int id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user).FirstOrDefault();
            var ewalletHistory = db.EWalletHistories.Where(x => x.EWallet_ID == ewallet.ID && x.ID == id).FirstOrDefault();

            return View(ewalletHistory);
        }
    }
}