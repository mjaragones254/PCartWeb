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

namespace PCartWeb.Controllers
{
    public class MembershippaypalController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        // GET: Membershippaypal
        public ActionResult PaymentWithPayPal(string money, string Cancel = null)
        {
            Session["money"] = money;
            Session["total"] = money;
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
                    "/Membershippaypal/PaymentWithPayPal?";
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
            decimal totalam = decimal.Parse(Session["total4"].ToString());
            var user = User.Identity.GetUserId();
            var userEwallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
            userEwallet.Balance += totalam;
            db.Entry(userEwallet).State = EntityState.Modified;
            db.SaveChanges();

            var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
            adminEwallet.Balance += totalam;
            db.Entry(adminEwallet).State = EntityState.Modified;
            db.SaveChanges();

            var ewalletHistory = new EWalletHistory();
            ewalletHistory.EWallet_ID = userEwallet.ID;
            ewalletHistory.Amount = totalam;
            ewalletHistory.Action = "Top-Up";
            ewalletHistory.Description = "Top-Up Successfully. New ballance is " + userEwallet.Balance;
            ewalletHistory.Created_At = DateTime.Now;
            db.EWalletHistories.Add(ewalletHistory);
            db.SaveChanges();

            var adminewallethistory = new EWalletHistory();
            adminewallethistory.EWallet_ID = adminEwallet.ID;
            adminewallethistory.Amount = totalam;
            adminewallethistory.Action = "Customer Top-Up";
            adminewallethistory.Description = "Top-Up Successfully.";
            adminewallethistory.Created_At = DateTime.Now;
            db.EWalletHistories.Add(adminewallethistory);
            db.SaveChanges();


            return View("SuccessView");
        }

        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            string amount = Session["total4"].ToString();
            Session["total4"] = amount;
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
            string totalamount = "";
            if (Session["money"] != null)
            {
                totalamount = Session["money"].ToString();

            }
            Session["total4"] = totalamount;
            //Adding Item Details like name, currency, price etc  
            itemList.items.Add(new Item()
            {
                name = "E-Wallet",
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
                subtotal = "" + totalamount
            };
            //Final amount with details  
            var amount = new Amount()
            {
                currency = "PHP",
                total = "" + totalamount, // Total must be equal to sum of tax, shipping and subtotal.  
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
            Session["total"] = totalamount;
            // Create a payment using a APIContext  
            return this.payment.Create(apiContext);
        }
        public ActionResult SuccessView()
        {
            return View();
        }
    }
}