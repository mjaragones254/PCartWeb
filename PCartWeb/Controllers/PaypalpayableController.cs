using PayPal.Api;
using PCartWeb.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PCartWeb.Controllers
{
    public class PaypalpayableController : Controller
    {
        ApplicationDbContext db = new ApplicationDbContext();
        // GET: Membershippaypal
        public ActionResult Paymentwithpaypal(string Cancel = null)
        {
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
                    "/Paypalpayable/Paymentwithpaypal?";
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
            string total = Session["total5"].ToString();
            string filename = Session["filename"].ToString();
            string extensions = Session["extension"].ToString();
            int coopids = int.Parse(Session["coopid"].ToString());
            string accountids = Session["accountid"].ToString();
            string modes = Session["modes"].ToString();
            string emails = Session["emails"].ToString();

            var coop = db.CoopDetails.Where(x => x.Id == coopids).FirstOrDefault();
            var accountspay = new AccountsReceived();
            accountspay.AccountId = accountids;
            accountspay.CoopId = coopids;
            accountspay.Receipt = filename + extensions;
            accountspay.TotalAmount = decimal.Parse(total);
            accountspay.Created_at = DateTime.Now;
            accountspay.ModeOfPayment = modes;
            accountspay.Email = emails;
            db.AccountsPayable.Add(accountspay);
            db.SaveChanges();


            var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
            adminEwallet.Balance += decimal.Parse(total);
            db.Entry(adminEwallet).State = EntityState.Modified;
            db.SaveChanges();

            var ewalletHistory = new EWalletHistory();
            ewalletHistory = new EWalletHistory();
            ewalletHistory.EWallet_ID = adminEwallet.ID;
            ewalletHistory.Amount = decimal.Parse(total);
            ewalletHistory.Action = "Account Payable";
            ewalletHistory.Description = "Commission fee payments received from " + coop.CoopName + ".";
            ewalletHistory.Created_At = DateTime.Now;
            db.EWalletHistories.Add(ewalletHistory);
            db.SaveChanges();

            var pendings = db.CommissionSales.Where(x => x.CoopCode == coopids && x.Status == "Pending").ToList();
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
            return View("SuccessView");
        }

        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            string totalamount = Session["total4"].ToString();
            Session["total5"] = totalamount;
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
            string filename = "";
            string extension = "";
            string coopid = "";
            string accountid = "";
            if (Session["money"] != null)
            {
                totalamount = Session["money"].ToString();
            }
            if (Session["filename"] != null)
            {
                filename = Session["filename"].ToString();
            }
            if (Session["extension"] != null)
            {
                extension = Session["extension"].ToString();
            }
            if (Session["coopid"] != null)
            {
                coopid = Session["coopid"].ToString();
            }
            if (Session["accountid"] != null)
            {
                accountid = Session["accountid"].ToString();
            }
            if (Session["mode"] != null)
            {
                string mode = Session["mode"].ToString();
                Session["modes"] = mode;
            }
            if(Session["email"] != null)
            {
                string email = Session["email"].ToString();
                Session["emails"] = email;
            }
            Session["filename"] = filename;
            Session["extension"] = extension;
            Session["coopid"] = coopid;
            Session["accountid"] = accountid;
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
            // Create a payment using a APIContext  
            return this.payment.Create(apiContext);
        }
        public ActionResult SuccessView()
        {
            return View();
        }
    }
}