using System;
using PayPal.Api;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.Web;
using System.Web.Mvc;
using PCartWeb.Models;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;
using System.Data.Entity.Spatial;
using System.Globalization;

namespace PCartWeb.Controllers
{
    public class PaypalController : Controller
    {
        // GET: Paypal
        ApplicationDbContext db = new ApplicationDbContext();
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        public ActionResult PaymentWithPayPal(string money, string user_id, List<string> vouchcode, string Cancel = null)
        {
            Session["TotalAmount"] = money;
            //getting the apiContext  
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {
                //A resource representing a Payer that funds a payment Payment Method as paypal  
                //Payer Id will be returned when payment proceeds or click to pay  
                //A resource representing a Payer that funds a payment Payment Method as paypal
                //Payer Id will be returned when payment proceeds or click to pay
                string payerId = Request.Params["PayerId"];
                if (string.IsNullOrEmpty(payerId))
                {
                    //this section will be executed first because PayerID doesn't exist
                    //it is returned by the create function call of the payment class
                    // Creating a payment
                    // baseURL is the url on which paypal sendsback the data.
                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority +
                    "/Paypal/PaymentWithPayPal?";
                    //here we are generating guid for storing the paymentID received in session
                    //which will be used in the payment execution
                    var guid = Convert.ToString((new Random()).Next(100000));
                    //CreatePayment function gives us the payment approval url
                    //on which payer is redirected for paypal account payment
                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);
                    //get links returned from paypal in response to Create function call
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            //saving the payapalredirect URL to which user will be redirected for payment
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    // saving the paymentID in the key guid
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This function exectues after receving all parameters for the payment  
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    //If executed payment failed then we will show payment failure message to user  
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }
                }
            }
            catch (Exception)
            {
                return View("FailureView");
            }
            //on successful payment, show success page to user.  

            var itemSelected = Session["Ids"].ToString();
            string[] itemsSelected = itemSelected.Split(',');
            List<string> vouchers = vouchcode;
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            var dbase = new ApplicationDbContext();
            List<string> arrid = new List<string>();
            double? dist = 0;
            int final = 0;
            double? deliver_fee = 0;
            List<COOPShop> coop2 = new List<COOPShop>();
            List<UserDetails2> user2 = new List<UserDetails2>();
            List<ProductToCheckout> prodDetails = new List<ProductToCheckout>();
            List<string> IDCoop = new List<string>();
            //decimal finaltot = decimal.Parse(money);
            var userOrderId = "";
            var comRate = db.CommissionDetails.OrderByDescending(cr => cr.Id).FirstOrDefault();
            decimal subTotal = 0;
            var date = "";
            IDCoop.Clear();
            foreach (string item in itemsSelected)
            {
                List<string> coopID = new List<string>();
                var prodcart = db.ProdCart.Where(x => x.Id.ToString() == item && x.CartId == cart.Id.ToString()).FirstOrDefault();
                var prode = db.ProductDetails.Where(x => x.Id.ToString() == prodcart.ProductId).FirstOrDefault();
                PriceTable getprice = new PriceTable();
                if (prodcart.VarId == 0 || prodcart.VarId.ToString() == null)
                {
                    getprice = db.Prices.Where(x => x.ProdId.ToString() == prodcart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                }
                else
                {
                    getprice = db.Prices.Where(x => x.ProdId.ToString() == prodcart.ProductId && x.VarId == prodcart.VarId).FirstOrDefault();
                    if (getprice == null)
                    {
                        getprice = db.Prices.Where(x => x.ProdId.ToString() == prodcart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                    }
                }
                decimal memPrice = 0;
                decimal itemPrice = 0;

                if (prode.CoopId.ToString() == userRole.CoopId)
                {
                    if (userRole.Role == "Member")
                    {
                        var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                        if (memDiscount != null)
                        {
                            decimal memDisc = memDiscount.MemberDiscount;
                            decimal totDisc = getprice.Price * (memDisc / 100);
                            memPrice = getprice.Price - totDisc;
                            memPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }

                var discountCheck = db.DiscountModels.ToList();

                foreach (var disCheck in discountCheck)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                    var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                    if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id && x.ProductId.ToString() == prodcart.ProductId).FirstOrDefault();

                        if (discountProdCheck != null)
                        {
                            if (prode.CoopId.ToString() == userRole.CoopId)
                            {
                                if (userRole.Role == "Member")
                                {
                                    var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                    if (memDiscount != null)
                                    {
                                        decimal memDisc = memDiscount.MemberDiscount + Convert.ToDecimal(disCheck.Percent);
                                        decimal totDisc = getprice.Price * (memDisc / 100);
                                        memPrice = getprice.Price - totDisc;
                                        itemPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                                    }
                                    else
                                    {
                                        decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                        decimal prodPrice = getprice.Price - discountPrice;
                                        itemPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                    }
                                }
                            }
                            else
                            {
                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                decimal prodPrice = getprice.Price - discountPrice;
                                itemPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                            }
                        }
                    }
                }

                var coopAdminId = prode.CoopAdminId;
                var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId.ToString() == coopAdminId).FirstOrDefault();
                var coopde2 = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
                string source = prode.Product_image;
                var replace = source.Replace("~", "..");
                var userloc = (from u in dbase.UserDetails
                               join loc in dbase.Locations
                               on u.AccountId equals loc.UserId
                               where u.AccountId == user
                               select new
                               {
                                   Longitude = loc.Geolocation.Longitude,
                                   Latitude = loc.Geolocation.Latitude
                               }).FirstOrDefault();

                var getloc = DbGeography.FromText("POINT(" + userloc.Longitude + " " + userloc.Latitude + ")");

                var cooploc = (from prod in dbase.ProductDetails
                               join coopd in dbase.CoopDetails
                               on prod.CoopId equals coopd.Id
                               join coop in dbase.CoopLocations
                               on coopd.Id.ToString() equals coop.CoopId
                               where prod.Id == prode.Id
                               select new
                               {
                                   CoopId = coop.CoopId,
                                   Cooplocation = coop.Geolocation.Distance(getloc)
                               }).FirstOrDefault();

                if (!IDCoop.Contains(cooploc.CoopId.ToString()))
                {
                    IDCoop.Add(coopde2.Id.ToString());
                    deliver_fee = 0;
                    if (cooploc.Cooplocation >= 0 && cooploc.Cooplocation <= 1000)
                    {
                        deliver_fee = deliver_fee + 55;
                    }
                    else
                    {
                        final = (int)Math.Round(cooploc.Cooplocation.Value);
                        dist = (int)Math.Round(0.001 * (float)(final));
                        deliver_fee = 55 + (10 * (dist - 1));
                    }

                    var user_order = new UserOrder();
                    user_order.CoopId = coopde2.Id;
                    user_order.ModeOfPay = "Paypal";
                    user_order.OStatus = "Paid";
                    user_order.UserId = user;
                    user_order.Delivery_fee = deliver_fee.Value;
                    user_order.OrderCreated_at = DateTime.Now.ToString();
                    date = user_order.OrderCreated_at;
                    db.UserOrders.Add(user_order);
                    db.SaveChanges();

                    var checkorder = db.UserOrders.Where(x => x.UserId == user && x.Delivery_fee == deliver_fee && x.CoopId == coopde2.Id
                        && x.OStatus == "Paid" && x.ModeOfPay == "Paypal" && x.OrderCreated_at == date).FirstOrDefault();

                    var notif = new NotificationModel
                    {
                        ToRole = "",
                        ToUser = coopde2.Id.ToString(),
                        NotifFrom = user,
                        NotifHeader = userRole.Firstname + " " + userRole.Lastname + " ordered.",
                        NotifMessage = "View to see what he/she ordered and ready to prepare it!",
                        NavigateURL = "OrderDetails/" + checkorder.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();

                    notif = new NotificationModel
                    {
                        ToRole = "",
                        ToUser = user,
                        NotifFrom = user,
                        NotifHeader = "Order from COOP " + coopde2.CoopName,
                        NotifMessage = "Your order from COOP " + coopde2.CoopName + " is being process.",
                        NavigateURL = "OrderDetails/" + checkorder.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();

                    coop2.Add(new COOPShop { CoopID = coopde2.Id.ToString(), Delivery = deliver_fee.Value, CoopName = coopde2.CoopName });
                }

                decimal totaleach = 0;
                foreach (var coop3 in coop2)
                {
                    if (coop3.CoopID == coopde2.Id.ToString())
                    {
                        if (itemPrice != 0)
                        {
                            totaleach = Convert.ToDecimal(prodcart.Qty.ToString()) * itemPrice;
                        }
                        else if (memPrice != 0)
                        {
                            totaleach = Convert.ToDecimal(prodcart.Qty.ToString()) * memPrice;
                        }
                        else
                        {
                            totaleach = Convert.ToDecimal(prodcart.Qty.ToString()) * getprice.Price;
                        }

                        coop3.TotalEach = coop3.TotalEach + totaleach;
                        coop3.voucherUsed = null;
                        coop3.discountedTotalPrice = null;

                        var checkorder = db.UserOrders.Where(x => x.UserId == user && x.Delivery_fee == deliver_fee && x.CoopId == coopde2.Id
                            && x.OStatus == "Paid" && x.ModeOfPay == "Paypal" && x.OrderCreated_at == date).FirstOrDefault();
                        checkorder.TotalPrice = coop3.TotalEach;
                        checkorder.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                        db.Entry(checkorder).State = EntityState.Modified;
                        db.SaveChanges();

                        CommissionSale commissionSale = new CommissionSale();
                        commissionSale.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                        commissionSale.CoopCode = coopde2.Id;
                        commissionSale.CoopAdminId = coopAdmin.UserId;
                        commissionSale.Status = "Received";

                        commissionSale.Created_at = DateTime.Now;
                        commissionSale.Updated_at = DateTime.Now;
                        commissionSale.UserOrderID = checkorder.Id;
                        db.CommissionSales.Add(commissionSale);
                        db.SaveChanges();
                    }
                }

                if (itemPrice != 0)
                {
                    var checkorder = db.UserOrders.Where(x => x.UserId == user && x.Delivery_fee == deliver_fee && x.CoopId == coopde2.Id
                        && x.OStatus == "Paid" && x.ModeOfPay == "Paypal" && x.OrderCreated_at == date).FirstOrDefault();
                    userOrderId = checkorder.Id.ToString();
                    var prod_order = new ProdOrder();
                    prod_order.UOrderId = checkorder.Id.ToString();
                    prod_order.UserId = user;
                    prod_order.CoopId = coopde2.Id;
                    prod_order.Price = getprice.Price;
                    prod_order.Qty = prodcart.Qty;
                    prod_order.ProdName = prode.Product_Name;
                    prod_order.MemberDiscountedPrice = 0;
                    prod_order.DiscountedPrice = decimal.Round(itemPrice, 2, MidpointRounding.AwayFromZero);
                    prod_order.SubTotal = prodcart.Qty * itemPrice;
                    prod_order.ProdId = prode.Id.ToString();
                    prod_order.Created_At = DateTime.Now;
                    db.ProdOrders.Add(prod_order);
                    db.SaveChanges();
                    UpdateProduct(prode.Id.ToString(), prod_order.Qty);
                }
                else
                {
                    var checkorder = db.UserOrders.Where(x => x.UserId == user && x.Delivery_fee == deliver_fee && x.CoopId == coopde2.Id
                        && x.OStatus == "Paid" && x.ModeOfPay == "Paypal" && x.OrderCreated_at == date).FirstOrDefault();
                    userOrderId = checkorder.Id.ToString();
                    var prod_order = new ProdOrder();
                    prod_order.UOrderId = checkorder.Id.ToString();
                    prod_order.UserId = user;
                    prod_order.CoopId = coopde2.Id;
                    prod_order.Price = getprice.Price;
                    prod_order.Qty = prodcart.Qty;
                    prod_order.ProdName = prode.Product_Name;
                    prod_order.MemberDiscountedPrice = memPrice;
                    prod_order.DiscountedPrice = 0;
                    prod_order.SubTotal = prodcart.Qty * getprice.Price;
                    prod_order.ProdId = prode.Id.ToString();
                    prod_order.Created_At = DateTime.Now;
                    db.ProdOrders.Add(prod_order);
                    db.SaveChanges();
                    UpdateProduct(prode.Id.ToString(), prod_order.Qty);
                }
            }

            if (vouchers != null)
            {
                foreach (var coop3 in coop2)
                {
                    foreach (var voucher in vouchers)
                    {
                        var checkorder = db.UserOrders.Where(x => x.UserId == user && x.CoopId.ToString() == coop3.CoopID && x.OStatus == "To Pay").OrderByDescending(x => x.Id).FirstOrDefault();
                        var voucherList = db.VoucherDetails.Where(x => x.CoopId.ToString() == coop3.CoopID && x.VoucherCode == voucher && x.Min_spend <= coop3.TotalEach).FirstOrDefault();
                        if (voucherList != null)
                        {
                            if (voucherList.DiscountType == "Percent")
                            {
                                var voucherUsed = new UserVoucherUsed();
                                voucherUsed.UserId = user;
                                voucherUsed.CoopId = Convert.ToInt32(coop3.CoopID);
                                voucherUsed.UserOrderId = userOrderId;
                                voucherUsed.VoucherCode = voucher;
                                voucherUsed.DateCreated = DateTime.Now;
                                db.VoucherUseds.Add(voucherUsed);
                                db.SaveChanges();
                                subTotal = checkorder.TotalPrice - (checkorder.TotalPrice * (Convert.ToDecimal(voucherList.Percent_Discount) / 100));
                                checkorder.TotalPrice = subTotal;
                                checkorder.CommissionFee = subTotal * (comRate.Rate / 100);
                                db.Entry(checkorder).State = EntityState.Modified;
                                db.SaveChanges();

                                CommissionSale commissionSale = new CommissionSale();
                                commissionSale.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                                commissionSale.CoopCode = Convert.ToInt32(coop3.CoopID);
                                commissionSale.Status = "Received";

                                commissionSale.Created_at = DateTime.Now;
                                commissionSale.UserOrderID = checkorder.Id;
                                commissionSale.CoopAdminId = coop3.CoopAdminId;
                                db.CommissionSales.Add(commissionSale);
                                db.SaveChanges();
                            }
                            else
                            {
                                var voucherUsed = new UserVoucherUsed();
                                voucherUsed.UserId = user;
                                voucherUsed.CoopId = Convert.ToInt32(coop3.CoopID);
                                voucherUsed.UserOrderId = userOrderId;
                                voucherUsed.VoucherCode = voucher;
                                voucherUsed.DateCreated = DateTime.Now;
                                db.VoucherUseds.Add(voucherUsed);
                                db.SaveChanges();
                                subTotal = checkorder.TotalPrice - (checkorder.TotalPrice * (Convert.ToDecimal(voucherList.Percent_Discount) / 100));
                                checkorder.TotalPrice = subTotal;
                                checkorder.CommissionFee = subTotal * (comRate.Rate / 100);
                                db.Entry(checkorder).State = EntityState.Modified;
                                db.SaveChanges();

                                CommissionSale commissionSale = new CommissionSale();
                                commissionSale.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                                commissionSale.CoopCode = Convert.ToInt32(coop3.CoopID);
                                commissionSale.Status = "Received";

                                commissionSale.Created_at = DateTime.Now;
                                commissionSale.UserOrderID = checkorder.Id;
                                commissionSale.CoopAdminId = coop3.CoopAdminId;
                                db.CommissionSales.Add(commissionSale);
                                db.SaveChanges();
                            }
                        }
                    }
                }
            }

            DelItemFromCart();
            return View("SuccessView");
        }
        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            this.payment = new Payment()
            {
                id = paymentId
            };
            return this.payment.Execute(apiContext, paymentExecution);
        }
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            //create itemlist and add item objects to it  
            var itemList = new ItemList()
            {
                items = new List<Item>()
            };
            string totalamount = Session["TotalAmount"].ToString();
            //Adding Item Details like name, currency, price etc  
            itemList.items.Add(new Item()
            {
                name = "Paying Order",
                currency = "PHP",
                price = "" + totalamount,
                quantity = "1",
                sku = "sku"
            });
            var payer = new Payer()
            {
                payment_method = "paypal"
            };
            // Configure Redirect Urls here with RedirectUrls object  
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };
            // Adding Tax, shipping and Subtotal details  
            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = "" + Session["TotalAmount"].ToString()
            };
            //Final amount with details  
            var amount = new Amount()
            {
                currency = "PHP",
                total = "" + Session["TotalAmount"].ToString(), // Total must be equal to sum of tax, shipping and subtotal.  
                details = details
            };
            var transactionList = new List<Transaction>();
            // Adding description about the transaction  
            transactionList.Add(new Transaction()
            {
                description = "PCart Online",
                invoice_number = Convert.ToString((new Random()).Next(100000)), //Generate an Invoice No  
                amount = amount,
                item_list = itemList
            });
            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };
            // Create a payment using a APIContext  
            return this.payment.Create(apiContext);
        }
        public ActionResult SuccessView()
        {
            return View();
        }

        public ActionResult FailureView()
        {
            return View();
        }
        public void DelItemFromCart()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var itemSelected = Session["Ids"].ToString();
            string[] itemsSelected = itemSelected.Split(',');
            var usercart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();

            foreach (var item in itemsSelected)
            {
                var prodcart = db.ProdCart.Where(x => x.CartId == usercart.Id.ToString() && x.Id.ToString() == item).FirstOrDefault();
                db.Entry(prodcart).State = EntityState.Deleted;
                db.SaveChanges();
            }
        }

        public void UpdateProduct(string id, int qty)
        {
            var db = new ApplicationDbContext();
            var prod = db.ProductDetails.Where(x => x.Id.ToString() == id).FirstOrDefault();
            prod.Product_qty = prod.Product_qty - qty;
            prod.Product_sold += qty;
            db.Entry(prod).State = EntityState.Modified;
            db.SaveChanges();
        }

    }

}