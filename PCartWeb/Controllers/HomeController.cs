using Microsoft.AspNet.Identity;
using PCartWeb.Models;
using System;
using PayPal.Api;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Owin;
using PCartWeb.Hubs;
using System.Globalization;

namespace PCartWeb.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        public HomeController() { }

        public HomeController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
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

        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        public static string IdSample = "";
        public ActionResult Index()
        {

            string conString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            string email = "PCartTeam@gmail.com";
            string emailconfirm = "";

            using (var db = new SqlConnection(conString))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT * FROM AspNetUsers WHERE Email = '" + email + "'";
                    SqlDataReader read = cmd.ExecuteReader();
                    while (read.Read())
                    {
                        emailconfirm = read["EmailConfirmed"].ToString();
                    }
                }
                db.Close();
            }
            if (emailconfirm != "True")
            {
                using (var db = new SqlConnection(conString))
                {
                    db.Open();
                    using (var cmd = db.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "UPDATE [AspNetUsers] SET EmailConfirmed = 'True' WHERE Email = '" + email + "'";
                        cmd.ExecuteNonQuery();
                    }
                    db.Close();
                }
            }
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (User.IsInRole("Coop Admin"))
            {
                return RedirectToAction("Index", "Coopadmin");
            }
            if (User.IsInRole("Driver"))
            {
                return RedirectToAction("DeliveryList", "Driver");
            }
            var dbase = new ApplicationDbContext();
            HomeDisplayModel home = new HomeDisplayModel();

            List<CategoryDetailsModel> categoryDetails = new List<CategoryDetailsModel>();
            var user2 = User.Identity.GetUserId();
            var getinfo = dbase.UserDetails.Where(x => x.AccountId == user2 && x.IsActive == "Deactive").FirstOrDefault();
            if (getinfo != null)
            {
                return RedirectToAction("IsActivateAccount");
            }

            var checkrequest = dbase.Memberships.Where(x => x.UserId == user2 && x.RequestStatus == "To be payed").OrderByDescending(p => p.Id).FirstOrDefault();
            var getwallet = dbase.UserEWallet.Where(x => x.UserID == user2 && x.Status == "Active").FirstOrDefault();
            if (getwallet != null)
            {
                home.CustomerEwallet = getwallet;
            }
            if (checkrequest != null)
            {
                home.RequestStatus = true;
            }

            var product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Product_status == "Approved"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).ToList();

            List<ViewListProd> viewListProds = new List<ViewListProd>();
            List<int> prodID = new List<int>();
            var discountCheck = dbase.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                CultureInfo culture2 = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture2);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in product)
                    {
                        var discountProdCheck = dbase.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (item.Id == Convert.ToInt32(disProdCheck.ProductId))
                            {
                                var getprice = dbase.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getCost = dbase.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getmanu = dbase.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                decimal prodPrice = getprice.Price - discountPrice;
                                viewListProds.Add(new ViewListProd
                                {
                                    Id = item.Id,
                                    DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                    Product_image = item.Product_image,
                                    Product_name = item.Product_name,
                                    Product_desc = item.Product_desc,
                                    Product_price = getprice.Price,
                                    Product_manufact = getmanu.Manufacturer,
                                    Product_qty = item.Product_qty,
                                    Product_cost = getCost.Cost,
                                    Category = item.Category,
                                    Created_at = item.Created_at,
                                    Updated_at = item.Updated_at,
                                    CustomerId = item.CustomerId,
                                    CurrentId = user2
                                });

                                prodID.Add(item.Id);
                            }
                        }
                    }
                }
            }
            var category = dbase.CategoryDetails.ToList();

            foreach (var item in product)
            {
                if (!prodID.Contains(item.Id))
                {
                    var getprice = dbase.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();

                    viewListProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Product_image = item.Product_image,
                        Product_name = item.Product_name,
                        Product_desc = item.Product_desc,
                        Product_price = getprice.Price,
                        Product_manufact = item.Product_manufact,
                        Product_qty = item.Product_qty,
                        Product_cost = item.Product_cost,
                        Category = item.Category,
                        Created_at = item.Created_at,
                        Updated_at = item.Updated_at,
                        CustomerId = item.CustomerId,
                        CurrentId = user2
                    });
                }
            }

            prodID.Clear();

            if (viewListProds.Count > 0)
            {
                foreach (var item in viewListProds)
                {
                    var wishlist = dbase.Wishlist.Where(x => x.UserId == user2).ToList();
                    if (wishlist.Count > 0)
                    {
                        foreach (var wi in wishlist)
                        {
                            if (wi.ProductId == item.Id.ToString())
                            {
                                item.Wish = wi.Favorite;
                            }
                        }
                    }
                }
            }
            if (category != null)
            {
                home.Categories = category;
            }
            if(Session["SuccessMessage"]!=null)
            {
                home.IsSuccess = true;
            }
            home.ListProds = viewListProds;
            return View(home);
        }
        [HttpPost]
        public ActionResult Index(string submit, string paysubmit)
        {
            var dbase = new ApplicationDbContext();
            HomeDisplayModel home = new HomeDisplayModel();
            List<CategoryDetailsModel> categoryDetails = new List<CategoryDetailsModel>();
            var user2 = User.Identity.GetUserId();
            var theamount = Request["amount"];
            decimal amount = 0;
            var getinfo = dbase.UserDetails.Where(x => x.AccountId == user2 && x.IsActive == "Deactive").FirstOrDefault();
            if (getinfo != null)
            {
                return RedirectToAction("IsActivateAccount");
            }

            var checkrequest = dbase.Memberships.Where(x => x.UserId == user2 && x.RequestStatus == "To be payed").OrderByDescending(p => p.Id).FirstOrDefault();
            var getwallet = dbase.UserEWallet.Where(x => x.UserID == user2 && x.Status == "Active").FirstOrDefault();
            if (getwallet != null)
            {
                home.CustomerEwallet = getwallet;
            }
            if (paysubmit == "Pay Load")
            {
                if (theamount != null)
                {
                    amount = decimal.Parse(theamount);
                }
                if (amount > 0)
                {
                    string money = amount.ToString();
                    return RedirectToAction("PaymentWithPayPal", "Membershippaypal", new { money = amount });
                }
            }
            List<ViewListProd> product = new List<ViewListProd>();
            if (submit == "All Categories")
            {
                product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Product_status == "Approved"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).ToList();
            }
            else if (submit == "Search")
            {
                var keyword = Request["prodname"];
                product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Product_Name.ToLower().Contains(keyword.ToLower())
                           && prod.Product_status == "Approved"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).ToList();
            }
            else
            {
                product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Categoryname == submit
                           && prod.Product_status == "Approved"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).ToList();
            }

            List<ViewListProd> viewListProds = new List<ViewListProd>();
            List<int> prodID = new List<int>();
            var discountCheck = dbase.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                CultureInfo culture2 = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture2);

                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in product)
                    {
                        var discountProdCheck = dbase.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (item.Id == Convert.ToInt32(disProdCheck.ProductId))
                            {
                                var getprice = dbase.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getCost = dbase.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getmanu = dbase.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                decimal prodPrice = getprice.Price - discountPrice;
                                viewListProds.Add(new ViewListProd
                                {
                                    Id = item.Id,
                                    DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                    Product_image = item.Product_image,
                                    Product_name = item.Product_name,
                                    Product_desc = item.Product_desc,
                                    Product_price = getprice.Price,
                                    Product_manufact = getmanu.Manufacturer,
                                    Product_qty = item.Product_qty,
                                    Product_cost = getCost.Cost,
                                    Category = item.Category,
                                    Created_at = item.Created_at,
                                    Updated_at = item.Updated_at,
                                    CustomerId = item.CustomerId,
                                    CurrentId = user2
                                });

                                prodID.Add(item.Id);
                            }
                        }
                    }
                }
            }
            var category = dbase.CategoryDetails.ToList();

            foreach (var item in product)
            {
                if (!prodID.Contains(item.Id))
                {
                    var getprice = dbase.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();

                    viewListProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Product_image = item.Product_image,
                        Product_name = item.Product_name,
                        Product_desc = item.Product_desc,
                        Product_price = getprice.Price,
                        Product_manufact = item.Product_manufact,
                        Product_qty = item.Product_qty,
                        Product_cost = item.Product_cost,
                        Category = item.Category,
                        Created_at = item.Created_at,
                        Updated_at = item.Updated_at,
                        CustomerId = item.CustomerId,
                        CurrentId = user2
                    });
                }
            }

            prodID.Clear();

            if (viewListProds.Count > 0)
            {
                foreach (var item in viewListProds)
                {
                    var wishlist = dbase.Wishlist.Where(x => x.UserId == user2).ToList();
                    if (wishlist.Count > 0)
                    {
                        foreach (var wi in wishlist)
                        {
                            if (wi.ProductId == item.Id.ToString())
                            {
                                item.Wish = wi.Favorite;
                            }
                        }
                    }
                }
            }
            if (category != null)
            {
                home.Categories = category;
            }
            home.ListProds = viewListProds;
            return View(home);
        }

        [HttpPost]
        public JsonResult Autocomplete(string prefix)
        {
            var db = new ApplicationDbContext();
            var ListOfProducts = db.ProductDetails.ToList();
            var result = (from prod in ListOfProducts
                          where (prod.Product_Name.ToLower().StartsWith(prefix.ToLower())
                          || prod.Categoryname.ToLower().StartsWith(prefix.ToLower()))
                           && prod.Product_status == "Approved"
                          select new
                          {
                              Id = prod.Id,
                              Product_Name = prod.Product_Name
                          });
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DisplayCategories()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            var categ = db.CategoryDetails.ToList();
            foreach (var cat in categ)
            {
                data.Add(new
                {
                    Id = cat.Id,
                    Name = cat.Name,
                });
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ShowCatProds()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();

            var user2 = User.Identity.GetUserId();
            var getinfo = db.UserDetails.Where(x => x.AccountId == user2 && x.IsActive == "Deactive").FirstOrDefault();
            if (getinfo != null)
            {
                return RedirectToAction("IsActivateAccount");
            }

            var product = (from prod in db.ProductDetails
                           join categ in db.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Product_status == "Approved"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).ToList();

            List<ViewListProd> viewListProds = new List<ViewListProd>();
            List<int> prodID = new List<int>();
            var discountCheck = db.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in product)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            if (item.Id == Convert.ToInt32(disProdCheck.ProductId))
                            {
                                var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                decimal prodPrice = getprice.Price - discountPrice;
                                viewListProds.Add(new ViewListProd
                                {
                                    Id = item.Id,
                                    DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero),
                                    Product_image = item.Product_image,
                                    Product_name = item.Product_name,
                                    Product_desc = item.Product_desc,
                                    Product_price = getprice.Price,
                                    Product_manufact = getmanu.Manufacturer,
                                    Product_qty = item.Product_qty,
                                    Product_cost = getCost.Cost,
                                    Category = item.Category,
                                    Created_at = item.Created_at,
                                    Updated_at = item.Updated_at,
                                    CustomerId = item.CustomerId,
                                    CurrentId = user2
                                });

                                prodID.Add(item.Id);
                            }
                        }
                    }
                }
            }
            var category = db.CategoryDetails.ToList();

            foreach (var item in product)
            {
                if (!prodID.Contains(item.Id))
                {
                    var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                    viewListProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Product_image = item.Product_image,
                        Product_name = item.Product_name,
                        Product_desc = item.Product_desc,
                        Product_price = getprice.Price,
                        Product_manufact = item.Product_manufact,
                        Product_qty = item.Product_qty,
                        Product_cost = item.Product_cost,
                        Category = item.Category,
                        Created_at = item.Created_at,
                        Updated_at = item.Updated_at,
                        CustomerId = item.CustomerId,
                        CurrentId = user2
                    });
                }
            }

            prodID.Clear();

            if (viewListProds.Count > 0)
            {
                foreach (var item in viewListProds)
                {
                    var wishlist = db.Wishlist.Where(x => x.UserId == user2).ToList();
                    if (wishlist.Count > 0)
                    {
                        foreach (var wi in wishlist)
                        {
                            if (wi.ProductId == item.Id.ToString())
                            {
                                item.Wish = wi.Favorite;
                            }
                        }
                    }
                }
            }

            foreach (var pr in viewListProds)
            {
                data.Add(new
                {

                });
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Non-member")]
        public ActionResult ViewListOfCoops()
        {
            var db = new ApplicationDbContext();
            var coop = db.CoopDetails.Where(x => x.Approval == "Approved").ToList();
            List<CoopDetailsModel> coopDetails = new List<CoopDetailsModel>();
            foreach (var coops in coop)
            {
                coopDetails.Add(new CoopDetailsModel
                {
                    Address = coops.Address,
                    Approval = coops.Approval,
                    Contact = coops.Contact,
                    CoopName = coops.CoopName,
                    Id = coops.Id,
                    Coop_Created = coops.Coop_Created
                });
            }
            return View(coopDetails);
        }

        [Authorize(Roles = "Non-member")]
        public ActionResult CoopDetailView(int? id)
        {
            var db = new ApplicationDbContext();
            var model = new CoopMembershipDetails();
            var coopDetails = db.CoopDetails.Where(x => x.Id == id).FirstOrDefault();

            if (coopDetails == null)
            {
                return RedirectToAction("ViewListOfCoops");
            }

            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.Coop_code == coopDetails.Id).FirstOrDefault();
            var memFee = db.MembershipFees.Where(x => x.COOP_ID == coopDetails.Id.ToString()).OrderByDescending(x => x.ID).FirstOrDefault();

            model.CoopDetails = coopDetails;
            model.CoopAdminDetails = coopAdminDetails;

            if (memFee != null)
            {
                model.MemFee = memFee.MemFee;
            }
            else
            {
                model.MemFee = 0;
            }

            return View(model);
        }
        public FileResult Download(string fileName)
        {
            string myfile = fileName.Replace("~/Documents/", "");
            string fullPath = Path.Combine(Server.MapPath("../Documents"), myfile);
            byte[] fileBytes = System.IO.File.ReadAllBytes(fullPath);
            return File(fileBytes, MediaTypeNames.Application.Octet, fileName);
        }

        [HttpPost]
        public ActionResult ApplyMembership(CoopAdminViewModel mode, HttpPostedFileBase file)
        {

            var model = new CustomerMembership();
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getuser = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            var checkcoop = db.CoopDetails.Where(x => x.Id.ToString() == mode.Coop_code).FirstOrDefault();
            var coopAdmin = db.CoopAdminDetails.Where(x => x.Coop_code.ToString() == mode.Coop_code && x.IsResign == null).FirstOrDefault();

            if (mode.DocFile == null || Session["Coopid"]==null)
            {
                return RedirectToAction("CoopDetailView", new { id = mode.Coop_code });
            }
            Int32 id = (int)Session["Coopid"];
            var allowedExtensions = new[] {
                        ".docx", ".pdf"
                    };
            string name = Path.GetFileNameWithoutExtension(mode.DocFile.FileName); //getting file name without extension  
            string extension = Path.GetExtension(mode.DocFile.FileName); //getting extension of the file
            var listdocs = db.Memberships.ToList();
            if (allowedExtensions.Contains(extension) && ValidateFile(model.DocFile) == true )
            {
                model.Formpath = name + extension;
                string code = mode.Coop_code;
                foreach (var item in listdocs)
                {
                    if (item.Formpath == model.Formpath)
                    {
                        return RedirectToAction("CoopDetailView", new { id = id });
                    }
                }
                var path = Path.Combine(Server.MapPath("../Documents/"), name + extension);
                mode.DocFile.SaveAs(path);
                model.Coop_code = mode.Coop_code;
                model.RequestStatus = "Pending";
                model.UserId = user;
                db.Memberships.Add(model);
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = coopAdmin.UserId,
                    NotifFrom = user,
                    NotifHeader = "New membership application.",
                    NotifMessage = "There is new new member applicant. Check it now!",
                    NavigateURL = "MemberApplicants",
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToUser);
            }
            else
            {
                return RedirectToAction("CoopDetailView", new { id = id });
            }
            return RedirectToAction("Index");
        }


        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
        [Authorize(Roles = "Member, Non-member")]
        public ActionResult ActivateAccnt()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var detail = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            if (detail != null)
            {
                detail.IsActive = "Active";
                db.Entry(detail).State = EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Member, Non-member")]
        public ActionResult IsActivateAccount()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var usercheck = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();

            if (usercheck != null)
            {
                if (usercheck.IsActive == "Deactive")
                {
                    return View();
                }
            }
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Non-member")]
        public ActionResult ListOfCoops()
        {
            var db = new ApplicationDbContext();
            var listofcoops = db.CoopDetails.ToList();
            return View(listofcoops);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult ItemDetails(int? id)
        {
            var dbase = new ApplicationDbContext();
            HomeDisplayModel model = new HomeDisplayModel();
            List<ReviewDisplay> reviewDisplays = new List<ReviewDisplay>();
            var user2 = User.Identity.GetUserId();

            var product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Id == id
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               GetQty = 1,
                               Product_image = prod.Product_image,
                               Product_qty = prod.Product_qty,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2,
                               CoopID = prod.CoopId.ToString()
                           }).FirstOrDefault();

            var reviews = dbase.Reviews.Where(x => x.ProdId == product.Id).ToList();

            if (reviews != null)
            {
                foreach (var review in reviews)
                {
                    var getuser = dbase.UserDetails.Where(x => x.AccountId == review.UserId).FirstOrDefault();

                    if (review.IsAnonymous == false)
                    {
                        reviewDisplays.Add(new ReviewDisplay
                        {
                            Created_at = review.Created_at,
                            Description = review.Desc,
                            Id = review.Id,
                            Name = getuser.Firstname,
                            ProdId = review.ProdId.ToString(),
                            Rate = review.Rate
                        });
                    }
                    else
                    {
                        reviewDisplays.Add(new ReviewDisplay
                        {
                            Created_at = review.Created_at,
                            Description = review.Desc,
                            Id = review.Id,
                            Name = "Anonymous",
                            ProdId = review.ProdId.ToString(),
                            Rate = review.Rate
                        });
                    }

                }
                model.Reviews = reviewDisplays;
            }
            var images = dbase.PImage.Where(x => x.ProductId == product.Id.ToString()).ToList();
            var wish = dbase.Wishlist.Where(x => x.ProductId == product.Id.ToString() && x.UserId == user2).FirstOrDefault();
            if (wish != null)
            {
                product.Wish = true;
            }
            model.VarDesc = "Plain";
            var getprice = dbase.Prices.Where(x => x.ProdId == id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_price = getprice.Price;
            var getCost = dbase.Cost.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_cost = getCost.Cost;
            var getmanu = dbase.Manufacturer.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_manufact = getmanu.Manufacturer;

            var discountCheck = dbase.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);

                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    var discountProdCheck = dbase.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                    foreach (var disProdCheck in discountProdCheck)
                    {
                        if (product.Id == Convert.ToInt32(disProdCheck.ProductId))
                        {
                            decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                            decimal prodPrice = getprice.Price - discountPrice;

                            product.DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }

            var getvariations = dbase.PVariation.Where(x => x.ProdId == id).ToList();
            model.VarDesc = "Plain";
            if (images != null)
            {
                model.Images = images;
            }
            if (getvariations != null)
            {
                model.Variations = getvariations;
            }

            Session["ItemId"] = id;

            model.Prod = product;
            return View(model);
        }

        [HttpPost]
        public ActionResult ItemDetails(string variation)
        {
            Int32 id = Convert.ToInt32(Session["ItemId"]);
            var get = Request["Qty"];
            var price = Request["Price"];
            var desc = Request["desc"];
            int qty = int.Parse(get);
            var dbase = new ApplicationDbContext();
            HomeDisplayModel model = new HomeDisplayModel();
            List<ReviewDisplay> reviewDisplays = new List<ReviewDisplay>();
            var user2 = User.Identity.GetUserId();

            var getvariations = dbase.PVariation.Where(x => x.ProdId == id).ToList();
            if (getvariations != null)
            {
                model.Variations = getvariations;
            }
            var product = (from prod in dbase.ProductDetails
                           join categ in dbase.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.Id == id
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               GetQty = 1,
                               Product_image = prod.Product_image,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString(),
                               CustomerId = prod.CustomerId,
                               CurrentId = user2
                           }).FirstOrDefault();

            var wish = dbase.Wishlist.Where(x => x.ProductId == product.Id.ToString() && x.UserId == user2).FirstOrDefault();
            var images = dbase.PImage.Where(x => x.ProductId == product.Id.ToString()).ToList();
            if (wish != null)
            {
                product.Wish = true;
            }

            var getprice = dbase.Prices.Where(x => x.ProdId == id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
            var getCost = dbase.Cost.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_cost = getCost.Cost;
            var getmanu = dbase.Manufacturer.Where(x => x.ProdId == product.Id).OrderByDescending(p => p.Id).FirstOrDefault();
            product.Product_manufact = getmanu.Manufacturer;

            var discountCheck = dbase.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    var discountProdCheck = dbase.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                    foreach (var disProdCheck in discountProdCheck)
                    {
                        if (product.Id == Convert.ToInt32(disProdCheck.ProductId))
                        {
                            decimal discountPrice = Convert.ToDecimal(getprice.Price) * (Convert.ToDecimal(disCheck.Percent) / 100);
                            decimal prodPrice = Convert.ToDecimal(getprice.Price) - discountPrice;

                            product.DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }

            //Check if the variation picked is just plain
            if (variation == "Plain")
            {
                model.VarDesc = "Plain";
                var categories1 = dbase.CategoryDetails.ToList();
                model.Categories = categories1;
                if (images != null)
                {
                    model.Images = images;
                }

                product.Product_price = getprice.Price;
                model.VarDesc = variation;
                model.Prod = product;
                return View(model);
            }
            var getvaria = dbase.PVariation.Where(x => x.ProdId == id).ToList();
            if (getvaria != null)
            {
                foreach (var variat in getvaria)
                {
                    if (variation == variat.Description) //Check if the variation picked is in the data
                    {
                        var varprice = dbase.Prices.Where(x => x.VarId == variat.Id).FirstOrDefault();
                        foreach (var disCheck in discountCheck)
                        {
                            CultureInfo culture = new CultureInfo("es-ES");
                            var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                            var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
                            if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                            {
                                var discountProdCheck = dbase.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                                foreach (var disProdCheck in discountProdCheck)
                                {
                                    if (product.Id == Convert.ToInt32(disProdCheck.ProductId))
                                    {
                                        decimal discountPrice = Convert.ToDecimal(varprice.Price) * (Convert.ToDecimal(disCheck.Percent) / 100);
                                        decimal prodPrice = Convert.ToDecimal(varprice.Price) - discountPrice;

                                        product.DiscountPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                    }
                                }
                            }
                        }

                        product.Product_price = varprice.Price;
                        var categories1 = dbase.CategoryDetails.ToList();
                        model.Categories = categories1;
                        if (images != null)
                        {
                            model.Images = images;
                        }
                        model.VarDesc = variation;
                        model.Prod = product;
                        return View(model);
                    }
                }

            }

            var categories = dbase.CategoryDetails.ToList();
            model.Categories = categories;
            if (images != null)
            {
                model.Images = images;
            }
            model.Prod = product;
            if (qty <= 0)
            {
                model.Price = price;
                ViewBag.errorMessage = "Please enter quantity.";
                return View(model);
            }
            if (qty > product.Product_qty)
            {
                model.Price = price;
                ViewBag.errorMessage = "The stocks available in this item is " + product.Product_qty + " pcs";
                return View(model);
            }
            var usercart = dbase.Cart.Where(x => x.UserId == user2).FirstOrDefault();
            if (usercart != null)
            {
                var checkprod = dbase.ProdCart.Where(x => x.ProductId == id.ToString() && x.CartId == usercart.Id.ToString() && x.Variation == desc).FirstOrDefault();
                if (checkprod != null)
                {
                    checkprod.Qty += qty;
                    dbase.Entry(checkprod).State = EntityState.Modified;
                    dbase.SaveChanges();
                    model.IsSuccess = true;
                }
                else
                {
                    if (desc == "Plain")
                    {
                        var prod = new ProductCart();
                        prod.Variation = desc;
                        prod.CartId = usercart.Id.ToString();
                        prod.Qty = qty;
                        prod.ProductId = id.ToString();
                        prod.Created_at = DateTime.Now;
                        dbase.ProdCart.Add(prod);
                        dbase.SaveChanges();
                        model.IsSuccess = true;
                    }
                    else
                    {
                        var prod = new ProductCart();
                        var getvaria3 = dbase.PVariation.Where(x => x.ProdId == id).ToList();
                        foreach (var getva in getvaria3)
                        {
                            if (getva.Description == desc)
                            {
                                prod.Variation = desc;
                                prod.CartId = usercart.Id.ToString();
                                prod.VarId = getva.Id;
                                prod.Qty = qty;
                                prod.ProductId = id.ToString();
                                prod.Created_at = DateTime.Now;
                                dbase.ProdCart.Add(prod);
                                dbase.SaveChanges();
                                model.IsSuccess = true;
                                model.Prod = product;
                                return View(model);
                            }
                        }

                    }
                }
            }
            model.Prod = product;
            return View(model);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult AddToWish(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var checkwish = db.Wishlist.Where(x => x.UserId == user2 && x.ProductId == id.ToString()).FirstOrDefault();
            var wish = new WishList();
            if (checkwish == null)
            {
                wish.Favorite = true;
                wish.ProductId = prod.Id.ToString();
                wish.UserId = user2;
                wish.Created_at = DateTime.Now;
                db.Wishlist.Add(wish);
                db.SaveChanges();
            }
            else
            {
                db.Entry(checkwish).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("ItemDetails", new { id = id });
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult AddToWishList(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var checkwish = db.Wishlist.Where(x => x.UserId == user2 && x.ProductId == id.ToString()).FirstOrDefault();
            var wish = new WishList();
            if (checkwish == null)
            {
                wish.Favorite = true;
                wish.ProductId = prod.Id.ToString();
                wish.UserId = user2;
                wish.Created_at = DateTime.Now;
                db.Wishlist.Add(wish);
                db.SaveChanges();
            }
            else
            {
                db.Entry(checkwish).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult RemoveFromWishlist(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var prod = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            var checkwish = db.Wishlist.Where(x => x.UserId == user2 && x.ProductId == id.ToString()).FirstOrDefault();
            var wish = new WishList();
            if (checkwish == null)
            {
                wish.Favorite = true;
                wish.ProductId = prod.Id.ToString();
                wish.UserId = user2;
                wish.Created_at = DateTime.Now;
                db.Wishlist.Add(wish);
                db.SaveChanges();
            }
            else
            {
                db.Entry(checkwish).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("WishlistDisplay");
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult MyOrders()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<OrderList> orderlist = new List<OrderList>();
            var userorder = db.UserOrders.Where(x => x.UserId == user && x.OStatus == "To Pay").ToList();
            if (userorder != null)
            {
                foreach (var item in userorder)
                {
                    var prodorder = db.ProdOrders.Where(x => x.UserId == user && x.UOrderId == item.Id.ToString()).ToList();
                    var coopde = db.CoopDetails.Where(x => x.Id == item.CoopId).FirstOrDefault();

                    orderlist.Add(new OrderList
                    {
                        Delivery_fee = item.Delivery_fee.ToString(),
                        OrderNo = item.Id.ToString(),
                        Id = item.Id,
                        TotalAmount = item.TotalPrice,
                        CustomerName = coopde.CoopName,
                        ModeOfPay = item.ModeOfPay
                    });
                }
            }

            return View(orderlist);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult OrderDetails(string id, string message)
        {
            if (message != null)
            {
                ViewBag.Message = message;
            }

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
            var returnRefund = db.ReturnRefunds.Where(x => x.UserOrderId == uid).FirstOrDefault();
            bool israted = false;

            foreach(var prod in prodOrder)
            {
                var rate = db.Reviews.Where(x => x.ProdOrderId == prod.Id).FirstOrDefault();
                if (rate != null)
                {
                    israted = true;
                }
            }
           
           
            if (comRate == null || id == null)
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
            model.IsRated = israted;

            if (returnRefund != null)
            {
                model.RefundStatus = returnRefund.Status;
            }

            if (deliveryDetails != null)
            {
                var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
                if (deliveryDetails.PickUpSuccessDate == date || deliveryDetails.ExpectedDeldate == date || deliveryDetails.DateDelivered == date)
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
                deliver.ReturnedReason = deliveryDetails.ReturnedReason;
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
            order.Delivery_fee = userOrder.Delivery_fee;


            if (cancel != null)
            {
                OrderCancel orderCancel = new OrderCancel();
                orderCancel.UserOrder_ID = userOrder.Id;

                if (cancel.CancelledBy == user)
                {
                    var userDetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
                    orderCancel.CancelledBy = userDetails.Firstname + " " + userDetails.Lastname + " (You)";
                }
                else
                {
                    var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId == cancel.CancelledBy).FirstOrDefault();
                    var coop = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
                    orderCancel.CancelledBy = coop.CoopName + " (Customer)";
                }

                orderCancel.Reason = cancel.Reason;
                orderCancel.Created_At = cancel.Created_At;
                model.CancelOrder = orderCancel;
            }

            if(prodOrder.Count > 0)
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
            var useroder = db.UserOrders.Where(x => x.UserId == user && x.Id == id).FirstOrDefault();

            if (useroder != null)
            {
                if (useroder.ModeOfPay == "E-Wallet")
                {
                    var userEwallet = db.UserEWallet.Where(x => x.UserID == useroder.UserId).FirstOrDefault();
                    userEwallet.Balance += useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(userEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = userEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund";
                    ewalletHistory.Description = "Order No. " + useroder.Id + "is cancelled. Payment refunded successfully.";
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
                    ewalletHistory.Description = "Order Cancelled. Payment refunded from Order No. " + useroder.Id + ".";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();
                }

                useroder.OStatus = "Cancelled";
                db.Entry(useroder).State = EntityState.Modified;
                db.SaveChanges();

                var prodOrder = db.ProdOrders.Where(x => x.UOrderId == useroder.Id.ToString()).ToList();
                foreach (var prod in prodOrder)
                {
                    var product = db.ProductDetails.Where(x => x.Id.ToString() == prod.ProdId).FirstOrDefault();
                    product.Product_qty += prod.Qty;
                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var cancel = new OrderCancel();
                cancel.UserOrder_ID = useroder.Id;
                cancel.CancelledBy = user;
                cancel.Reason = reason;
                cancel.Created_At = DateTime.Now;
                db.CancelOrders.Add(cancel);
                db.SaveChanges();

                var voucherUsed = db.VoucherUseds.Where(x => x.UserOrderId == useroder.Id.ToString()).FirstOrDefault();
                if (voucherUsed != null)
                {
                    voucherUsed.Status = "Cancelled";
                    db.Entry(voucherUsed).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var coopAdmin = db.CoopAdminDetails.Where(x => x.Coop_code == useroder.CoopId).FirstOrDefault();
                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = coopAdmin.UserId,
                    ToCOOP_ID = "",
                    NotifFrom = user,
                    NotifHeader = "Order No. " + useroder.Id + " is cancelled.",
                    NotifMessage = "Order No. " + useroder.Id + "was cancelled by the customer.",
                    NavigateURL = "OrderDetails/" + useroder.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToUser);
            }

            return RedirectToAction("OrderDetails", new { id = id.ToString() });
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CancelOrder(int? id)
        {
            var reason = Request["Reason"];

            if (reason == "" || reason == null)
            {
                var mess = "Kindly choose a reason for the cancellation.";

                return RedirectToAction("OrderDetails", new { id = id, message = mess });
            }

            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var useroder = db.UserOrders.Where(x => x.UserId == user && x.Id == id).FirstOrDefault();

            if (useroder != null)
            {
                if (useroder.ModeOfPay == "E-Wallet")
                {
                    var userEwallet = db.UserEWallet.Where(x => x.UserID == useroder.UserId).FirstOrDefault();
                    userEwallet.Balance += useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    db.Entry(userEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = userEwallet.ID;
                    ewalletHistory.Amount = useroder.TotalPrice + Convert.ToDecimal(useroder.Delivery_fee);
                    ewalletHistory.Action = "Refund";
                    ewalletHistory.Description = "Order No. " + useroder.Id + "is cancelled. Payment refunded successfully.";
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
                    ewalletHistory.Description = "Order Cancelled. Payment refunded from Order No. " + useroder.Id + ".";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();
                }

                useroder.OStatus = "Cancelled";
                db.Entry(useroder).State = EntityState.Modified;
                db.SaveChanges();

                var prodOrder = db.ProdOrders.Where(x => x.UOrderId == useroder.Id.ToString()).ToList();
                foreach(var prod in prodOrder)
                {
                    var product = db.ProductDetails.Where(x => x.Id.ToString() == prod.ProdId).FirstOrDefault();
                    product.Product_qty += prod.Qty;
                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var cancel = new OrderCancel();
                cancel.UserOrder_ID = useroder.Id;
                cancel.CancelledBy = user;
                cancel.Reason = reason;
                cancel.Created_At = DateTime.Now;
                db.CancelOrders.Add(cancel);
                db.SaveChanges();

                var voucherUsed = db.VoucherUseds.Where(x => x.UserOrderId == useroder.Id.ToString()).FirstOrDefault();
                if(voucherUsed != null)
                {
                    voucherUsed.Status = "Cancelled";
                    db.Entry(voucherUsed).State = EntityState.Modified;
                    db.SaveChanges();
                }

                var coopAdmin = db.CoopAdminDetails.Where(x => x.Coop_code == useroder.CoopId).FirstOrDefault();
                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = coopAdmin.UserId,
                    ToCOOP_ID = "",
                    NotifFrom = user,
                    NotifHeader = "Order No. " + useroder.Id + " is cancelled.",
                    NotifMessage = "Order No. " + useroder.Id + "was cancelled by the customer.",
                    NavigateURL = "OrderDetails/" + useroder.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                NotificationHub objNotifHub = new NotificationHub();
                objNotifHub.SendNotification(notif.ToUser);
            }

            return RedirectToAction("OrderDetails", new { id = id.ToString() });
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult AddToCart(Int32 id)
        {
            var db = new ApplicationDbContext();
            var getid = User.Identity.GetUserId();
            var usercart = db.Cart.Where(x => x.UserId == getid).FirstOrDefault();
            if (usercart != null)
            {
                var checkprod = db.ProdCart.Where(x => x.ProductId == id.ToString() && x.CartId == usercart.Id.ToString()).FirstOrDefault();
                if (checkprod != null)
                {
                    checkprod.Qty++;
                    db.Entry(checkprod).State = EntityState.Modified;
                    db.SaveChanges();
                }
                else
                {
                    var prod = new ProductCart();
                    prod.CartId = usercart.Id.ToString();
                    prod.Qty = 1;
                    prod.ProductId = id.ToString();
                    prod.Created_at = DateTime.Now;
                    db.ProdCart.Add(prod);
                    db.SaveChanges();
                }
            }
            Session["SuccessMessage"] = "Successfully added to Cart.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult DisplayCart()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var model = new ViewDisplayCart();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var product = (from prod1 in db.ProdCart
                           join prod2 in db.ProductDetails
                           on prod1.ProductId equals prod2.Id.ToString()
                           join coop in db.CoopDetails
                           on prod2.CoopId equals coop.Id
                           where prod1.CartId == cart.Id.ToString()
                           select new CartViewMdoel
                           {

                               VarId = prod1.VarId,
                               CoopName = coop.CoopName,
                               CartId = prod1.CartId,
                               ProdCartId = prod1.Id,
                               ProdId = prod1.ProductId,
                               CoopId = coop.Id,
                               ProdName = prod2.Product_Name,
                               Qty = prod1.Qty,
                               Image = prod2.Product_image,
                               Created_at = prod1.Created_at.ToString()
                           }).ToList();


            var discountCheck = db.DiscountModels.ToList();
            List<COOPShop2> coopShop = new List<COOPShop2>();
            List<ProductsInCart> products = new List<ProductsInCart>();
            List<ProductTotalQty> prodQty = new List<ProductTotalQty>();
            List<int> prodID2 = new List<int>();
            List<int> coopID = new List<int>();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in product)
                    {
                        var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                        foreach (var disProdCheck in discountProdCheck)
                        {
                            var prodID = Convert.ToInt32(item.ProdId);
                            if (prodID == Convert.ToInt32(disProdCheck.ProductId))
                            {

                                PriceTable getprice = new PriceTable();
                                if (item.VarId == 0 || item.VarId.ToString() == null)
                                {
                                    getprice = db.Prices.Where(x => x.ProdId == prodID && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                }
                                else
                                {
                                    getprice = db.Prices.Where(x => x.ProdId == prodID && x.VarId == item.VarId).FirstOrDefault();
                                }
                                var getCost = db.Cost.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getmanu = db.Manufacturer.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();

                                decimal price = getprice.Price;
                                decimal memPrice = 0, subtotal = 0, discountedPrice = 0;

                                if (item.CoopId.ToString() == userRole.CoopId)
                                {
                                    if (userRole.Role == "Member")
                                    {
                                        var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                        if (memDiscount != null)
                                        {
                                            decimal memDisc = memDiscount.MemberDiscount + Convert.ToDecimal(disCheck.Percent);
                                            decimal totDisc = getprice.Price * (memDisc / 100);
                                            memPrice = getprice.Price - totDisc;
                                            subtotal = item.Qty * memPrice;
                                            discountedPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                                        }
                                        else
                                        {
                                            decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                            decimal prodPrice = getprice.Price - discountPrice;

                                            subtotal = item.Qty * prodPrice;
                                            discountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                        }
                                    }
                                }
                                else
                                {
                                    decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                    decimal prodPrice = getprice.Price - discountPrice;

                                    subtotal = item.Qty * prodPrice;
                                    discountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                }

                                products.Add(new ProductsInCart
                                {
                                    CoopId = item.CoopId,
                                    ProdCartId = item.ProdCartId,
                                    ProdId = item.ProdId,
                                    CartId = item.CartId,
                                    ProdName = item.ProdName,
                                    Image = item.Image,
                                    Qty = item.Qty,
                                    Price = getprice.Price,
                                    MemberDiscountedPrice = 0,
                                    DiscountedPrice = discountedPrice,
                                    Subtotal = subtotal,
                                });

                                var prodDetails = db.ProductDetails.Where(pd => pd.Id.ToString() == item.ProdId).FirstOrDefault();

                                prodQty.Add(new ProductTotalQty
                                {
                                    CoopId = item.CoopId,
                                    ProdId = item.ProdId,
                                    Qty = prodDetails.Product_qty
                                });

                                if (!coopID.Contains(item.CoopId))
                                {
                                    coopID.Add(item.CoopId);
                                    coopShop.Add(new COOPShop2
                                    {
                                        CoopID = item.CoopId,
                                        CoopName = item.CoopName,
                                    });
                                }

                                prodID2.Add(prodID);
                            }
                        }
                    }
                }
            }

            foreach (var item in product)
            {
                var prodID = Convert.ToInt32(item.ProdId);
                if (!prodID2.Contains(prodID))
                {

                    PriceTable getprice = new PriceTable();
                    if (item.VarId == 0 || item.VarId.ToString() == null)
                    {
                        getprice = db.Prices.Where(x => x.ProdId == prodID && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                    }
                    else
                    {
                        getprice = db.Prices.Where(x => x.ProdId == prodID && x.VarId == item.VarId).FirstOrDefault();
                    }
                    var getCost = db.Cost.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();
                    var getmanu = db.Manufacturer.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();

                    decimal price = getprice.Price;
                    decimal subtotal = 0;
                    decimal memPrice = 0;

                    if (item.CoopId.ToString() == userRole.CoopId)
                    {
                        if (userRole.Role == "Member")
                        {
                            var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                            if (memDiscount != null)
                            {
                                decimal memDisc = memDiscount.MemberDiscount;
                                decimal totDisc = price * (memDisc / 100);
                                memPrice = price - totDisc;
                                subtotal = item.Qty * memPrice;
                                memPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                            }
                            else
                            {
                                subtotal = item.Qty * price;
                            }
                        }
                    }
                    else
                    {
                        subtotal = item.Qty * price;
                    }

                    products.Add(new ProductsInCart
                    {
                        CoopId = item.CoopId,
                        ProdCartId = item.ProdCartId,
                        ProdId = item.ProdId,
                        CartId = item.CartId,
                        ProdName = item.ProdName,
                        Image = item.Image,
                        Qty = item.Qty,
                        Price = price,
                        MemberDiscountedPrice = memPrice,
                        DiscountedPrice = 0,
                        Subtotal = subtotal,
                    });

                    var prodDetails = db.ProductDetails.Where(pd => pd.Id.ToString() == item.ProdId).FirstOrDefault();

                    prodQty.Add(new ProductTotalQty
                    {
                        CoopId = item.CoopId,
                        ProdId = item.ProdId,
                        Qty = prodDetails.Product_qty
                    });

                    if (!coopID.Contains(item.CoopId))
                    {
                        coopID.Add(item.CoopId);
                        coopShop.Add(new COOPShop2
                        {
                            CoopID = item.CoopId,
                            CoopName = item.CoopName,
                        });
                    }
                }
            }

            prodID2.Clear();
            coopID.Clear();

            model.coopShops = coopShop;
            model.productTotals = prodQty;
            model.productsInCarts = products;
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Member, Non-member")]
        public ActionResult DisplayCart(string submit, ViewDisplayCart cartmodel)
        {
            int flag = 0;
            switch (submit)
            {
                case "Update Qty":
                    var db4 = new ApplicationDbContext();
                    var user2 = User.Identity.GetUserId();
                    if (cartmodel.productsInCarts != null)
                    {
                        foreach (var item in cartmodel.productsInCarts)
                        {
                            if (item.Qty != 0)
                            {
                                var prodcart = db4.ProdCart.Where(x => x.Id == item.ProdCartId).FirstOrDefault();
                                prodcart.Qty = item.Qty;
                                db4.Entry(prodcart).State = EntityState.Modified;
                                db4.SaveChanges();
                            }
                        }
                        var db = new ApplicationDbContext();
                        var user = User.Identity.GetUserId();


                        var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
                        var model = new ViewDisplayCart();
                        var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
                        var product = (from prod1 in db.ProdCart
                                       join prod2 in db.ProductDetails
                                       on prod1.ProductId equals prod2.Id.ToString()
                                       join coop in db.CoopDetails
                                       on prod2.CoopId equals coop.Id
                                       where prod1.CartId == cart.Id.ToString()
                                       select new CartViewMdoel
                                       {

                                           VarId = prod1.VarId,
                                           CoopName = coop.CoopName,
                                           CartId = prod1.CartId,
                                           ProdCartId = prod1.Id,
                                           ProdId = prod1.ProductId,
                                           CoopId = coop.Id,
                                           ProdName = prod2.Product_Name,
                                           Qty = prod1.Qty,
                                           Image = prod2.Product_image,
                                           Created_at = prod1.Created_at.ToString()
                                       }).ToList();


                        var discountCheck = db.DiscountModels.ToList();
                        List<COOPShop2> coopShop = new List<COOPShop2>();
                        List<ProductsInCart> products = new List<ProductsInCart>();
                        List<ProductTotalQty> prodQty = new List<ProductTotalQty>();
                        List<int> prodID2 = new List<int>();
                        List<int> coopID = new List<int>();

                        foreach (var disCheck in discountCheck)
                        {
                            CultureInfo culture = new CultureInfo("es-ES");
                            var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                            var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
                            if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                            {
                                foreach (var item in product)
                                {
                                    var discountProdCheck = db.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id).ToList();

                                    foreach (var disProdCheck in discountProdCheck)
                                    {
                                        var prodID = Convert.ToInt32(item.ProdId);
                                        if (prodID == Convert.ToInt32(disProdCheck.ProductId))
                                        {

                                            PriceTable getprice = new PriceTable();
                                            if (item.VarId == 0 || item.VarId.ToString() == null)
                                            {
                                                getprice = db.Prices.Where(x => x.ProdId == prodID && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                            }
                                            else
                                            {
                                                getprice = db.Prices.Where(x => x.ProdId == prodID && x.VarId == item.VarId).FirstOrDefault();
                                            }
                                            var getCost = db.Cost.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();
                                            var getmanu = db.Manufacturer.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();

                                            decimal price = getprice.Price;
                                            decimal memPrice = 0, subtotal = 0, discountedPrice = 0;

                                            if (item.CoopId.ToString() == userRole.CoopId)
                                            {
                                                if (userRole.Role == "Member")
                                                {
                                                    var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                                    if (memDiscount != null)
                                                    {
                                                        decimal memDisc = memDiscount.MemberDiscount + Convert.ToDecimal(disCheck.Percent);
                                                        decimal totDisc = getprice.Price * (memDisc / 100);
                                                        memPrice = getprice.Price - totDisc;
                                                        subtotal = item.Qty * memPrice;
                                                        discountedPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                                                    }
                                                    else
                                                    {
                                                        decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                                        decimal prodPrice = getprice.Price - discountPrice;

                                                        subtotal = item.Qty * prodPrice;
                                                        discountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                                decimal prodPrice = getprice.Price - discountPrice;

                                                subtotal = item.Qty * prodPrice;
                                                discountedPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                            }

                                            products.Add(new ProductsInCart
                                            {
                                                CoopId = item.CoopId,
                                                ProdCartId = item.ProdCartId,
                                                ProdId = item.ProdId,
                                                CartId = item.CartId,
                                                ProdName = item.ProdName,
                                                Image = item.Image,
                                                Qty = item.Qty,
                                                Price = getprice.Price,
                                                MemberDiscountedPrice = 0,
                                                DiscountedPrice = discountedPrice,
                                                Subtotal = subtotal,
                                            });

                                            var prodDetails = db.ProductDetails.Where(pd => pd.Id.ToString() == item.ProdId).FirstOrDefault();

                                            prodQty.Add(new ProductTotalQty
                                            {
                                                CoopId = item.CoopId,
                                                ProdId = item.ProdId,
                                                Qty = prodDetails.Product_qty
                                            });

                                            if (!coopID.Contains(item.CoopId))
                                            {
                                                coopID.Add(item.CoopId);
                                                coopShop.Add(new COOPShop2
                                                {
                                                    CoopID = item.CoopId,
                                                    CoopName = item.CoopName,
                                                });
                                            }

                                            prodID2.Add(prodID);
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var item in product)
                        {
                            var prodID = Convert.ToInt32(item.ProdId);
                            if (!prodID2.Contains(prodID))
                            {

                                PriceTable getprice = new PriceTable();
                                if (item.VarId == 0 || item.VarId.ToString() == null)
                                {
                                    getprice = db.Prices.Where(x => x.ProdId == prodID && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                }
                                else
                                {
                                    getprice = db.Prices.Where(x => x.ProdId == prodID && x.VarId == item.VarId).FirstOrDefault();
                                }
                                var getCost = db.Cost.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();
                                var getmanu = db.Manufacturer.Where(x => x.ProdId == prodID).OrderByDescending(p => p.Id).FirstOrDefault();

                                decimal price = getprice.Price;
                                decimal subtotal = 0;
                                decimal memPrice = 0;

                                if (item.CoopId.ToString() == userRole.CoopId)
                                {
                                    if (userRole.Role == "Member")
                                    {
                                        var memDiscount = db.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                        if (memDiscount != null)
                                        {
                                            decimal memDisc = memDiscount.MemberDiscount;
                                            decimal totDisc = price * (memDisc / 100);
                                            memPrice = price - totDisc;
                                            subtotal = item.Qty * memPrice;
                                            memPrice = decimal.Round(memPrice, 2, MidpointRounding.AwayFromZero);
                                        }
                                        else
                                        {
                                            subtotal = item.Qty * price;
                                        }
                                    }
                                }
                                else
                                {
                                    subtotal = item.Qty * price;
                                }

                                products.Add(new ProductsInCart
                                {
                                    CoopId = item.CoopId,
                                    ProdCartId = item.ProdCartId,
                                    ProdId = item.ProdId,
                                    CartId = item.CartId,
                                    ProdName = item.ProdName,
                                    Image = item.Image,
                                    Qty = item.Qty,
                                    Price = price,
                                    MemberDiscountedPrice = memPrice,
                                    DiscountedPrice = 0,
                                    Subtotal = subtotal,
                                });

                                var prodDetails = db.ProductDetails.Where(pd => pd.Id.ToString() == item.ProdId).FirstOrDefault();

                                prodQty.Add(new ProductTotalQty
                                {
                                    CoopId = item.CoopId,
                                    ProdId = item.ProdId,
                                    Qty = prodDetails.Product_qty
                                });

                                if (!coopID.Contains(item.CoopId))
                                {
                                    coopID.Add(item.CoopId);
                                    coopShop.Add(new COOPShop2
                                    {
                                        CoopID = item.CoopId,
                                        CoopName = item.CoopName,
                                    });
                                }
                            }
                        }

                        prodID2.Clear();
                        coopID.Clear();

                        model.coopShops = coopShop;
                        model.productTotals = prodQty;
                        model.productsInCarts = products;
                        return View(model);
                    }
                    break;
            }
            if (Request.Form["isCheck"] == null)
            {
                return RedirectToAction("DisplayCart");
            }
            switch (submit)
            {
                case "Delete":
                    string selected = Request.Form["isCheck"].ToString();
                    string[] selectedList = selected.Split(',');
                    var db3 = new ApplicationDbContext();

                    foreach (string item in selectedList)
                    {
                        int prodItem = Convert.ToInt32(item);
                        var cartItem = db3.ProdCart.Where(p => p.Id == prodItem).OrderByDescending(p => p.Id).FirstOrDefault();
                        db3.Entry(cartItem).State = EntityState.Deleted;
                        db3.SaveChanges();
                    }
                    break;
                case "Checkout":
                    var db5 = new ApplicationDbContext();
                    foreach (var item in cartmodel.productsInCarts)
                    {
                        if (item.Qty != 0)
                        {
                            var prodcart = db5.ProdCart.Where(x => x.Id == item.ProdCartId).FirstOrDefault();
                            prodcart.Qty = item.Qty;
                            db5.Entry(prodcart).State = EntityState.Modified;
                            db5.SaveChanges();
                        }
                    }
                    string itemSelected = Request.Form["isCheck"].ToString();
                    Session["Ids"] = itemSelected;
                    flag = 1;
                    break;
            }

            if (flag == 1)
            {
                return RedirectToAction("CheckoutPage");
            }

            return RedirectToAction("DisplayCart");
        }

        [HttpPost]
        public ActionResult UpdateQ()
        {
            return View();
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CalculateItems()
        {
            var data = new List<object>();
            if (Request["itemSelected"] == null)
            {
                return RedirectToAction("Index");
            }
            var db2 = new ApplicationDbContext();
            var itemSelected = Request["itemSelected"];
            string[] itemsSelected = itemSelected.Split(',');
            decimal total = 0;
            var user = User.Identity.GetUserId();
            var userRole = db2.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            List<string> prodID = new List<string>();
            decimal totalCoopPrice = 0;
            decimal memTotal = 0;
            string role = "";

            var discountCheck = db2.DiscountModels.ToList();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (string item in itemsSelected)
                    {
                        var prodCart = db2.ProdCart.Where(p => p.Id.ToString() == item).OrderByDescending(p => p.Id).FirstOrDefault();
                        if (prodCart != null)
                        {
                            var discountProdCheck = db2.DiscountedProducts.Where(x => x.DiscountID == disCheck.Id && x.ProductId.ToString() == prodCart.ProductId).FirstOrDefault();

                            if (discountProdCheck != null)
                            {
                                var prodget = db2.ProductDetails.Where(x => x.Id.ToString() == prodCart.ProductId).FirstOrDefault();
                                PriceTable getprice = new PriceTable();
                                if (prodCart.VarId == 0 || prodCart.VarId.ToString() == null)
                                {
                                    getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                }
                                else
                                {
                                    getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && x.VarId == prodCart.VarId).FirstOrDefault();
                                    if (getprice == null)
                                    {
                                        getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                                    }
                                }
                                decimal discountPrice = getprice.Price * (Convert.ToDecimal(disCheck.Percent) / 100);
                                decimal prodPrice = getprice.Price - discountPrice;

                                decimal itemPrice = decimal.Round(prodPrice, 2, MidpointRounding.AwayFromZero);
                                int qty = prodCart.Qty;
                                decimal subtotal = itemPrice * qty;

                                if (prodget.CoopId.ToString() == userRole.CoopId)
                                {
                                    if (userRole.Role == "Member")
                                    {
                                        var memDiscount = db2.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                        if (memDiscount != null)
                                        {
                                            decimal memDisc = memDiscount.MemberDiscount + (Convert.ToDecimal(disCheck.Percent));
                                            decimal totDisc = getprice.Price * (memDisc / 100);
                                            memTotal = getprice.Price - totDisc;
                                            memTotal = memTotal * qty;
                                            totalCoopPrice = totalCoopPrice + memTotal;
                                            role = userRole.Role;
                                        }
                                        else
                                        {
                                            total += subtotal;
                                        }
                                    }
                                }
                                else
                                {
                                    total += subtotal;
                                }

                                prodID.Add(item);
                            }
                        }
                    }
                }
            }

            foreach (string item in itemsSelected)
            {
                if (!prodID.Contains(item))
                {
                    var prodCart = db2.ProdCart.Where(p => p.Id.ToString() == item).OrderByDescending(p => p.Id).FirstOrDefault();

                    if (prodCart != null)
                    {
                        var prodget = db2.ProductDetails.Where(x => x.Id.ToString() == prodCart.ProductId).FirstOrDefault();
                        PriceTable getprice = new PriceTable();
                        if (prodCart.VarId == 0 || prodCart.VarId.ToString() == null)
                        {
                            getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                        }
                        else
                        {
                            getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && x.VarId == prodCart.VarId).FirstOrDefault();
                            if (getprice == null)
                            {
                                getprice = db2.Prices.Where(x => x.ProdId.ToString() == prodCart.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                            }
                        }

                        decimal itemPrice = getprice.Price;
                        int qty = prodCart.Qty;
                        decimal subtotal = itemPrice * qty;

                        if (prodget.CoopId.ToString() == userRole.CoopId)
                        {
                            if (userRole.Role == "Member")
                            {
                                var memDiscount = db2.CoopMemberDiscounts.Where(d => d.COOP_ID.ToString() == userRole.CoopId).OrderByDescending(d => d.ID).FirstOrDefault();

                                if (memDiscount != null)
                                {
                                    decimal memDisc = memDiscount.MemberDiscount;
                                    decimal totDisc = getprice.Price * (memDisc / 100);
                                    memTotal = getprice.Price - totDisc;
                                    memTotal = memTotal * qty;
                                    totalCoopPrice = totalCoopPrice + memTotal;
                                    role = userRole.Role;
                                }
                                else
                                {
                                    total += subtotal;
                                }
                            }
                        }
                        else
                        {
                            total += subtotal;
                        }

                        prodID.Add(item);
                    }
                }
            }

            prodID.Clear();

            data.Add(new
            {
                total = decimal.Round(total + totalCoopPrice, 2, MidpointRounding.AwayFromZero),
                memberTotal = decimal.Round(total + totalCoopPrice, 2, MidpointRounding.AwayFromZero),
                role = role
            });

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CheckoutPage()
        {
            var dbase = new ApplicationDbContext();
            var data = new List<object>();

            if (Session["Ids"] == null)
            {
                return RedirectToAction("Index");
            }
            var coopId = Request["coopId"];
            var itemSelected = Session["Ids"].ToString();
            string[] itemsSelected = itemSelected.Split(',');
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();

            List<string> arrid = new List<string>();
            double? dist = 0;
            int final = 0;
            double? deliver_fee = 0;
            List<COOPShop> coop2 = new List<COOPShop>();
            List<UserDetails2> user2 = new List<UserDetails2>();
            List<ProductToCheckout> prodDetails = new List<ProductToCheckout>();
            List<VoucherList> voucherList = new List<VoucherList>();
            List<string> IDCoop = new List<string>();
            decimal totaleach = 0;
            decimal itemPrice = 0;
            decimal memPrice = 0;
            var model = new ViewCheckOutPage();

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
                memPrice = 0;
                itemPrice = 0;

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
                    var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                    var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
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

                    coop2.Add(new COOPShop { CoopID = coopde2.Id.ToString(), Delivery = deliver_fee.Value, CoopName = coopde2.CoopName });

                    var vouchers = db.VoucherDetails.Where(v => v.CoopId == coopde2.Id).ToList();
                    foreach (var vouch in vouchers)
                    {
                        CultureInfo culture = new CultureInfo("es-ES");
                        var datestart1 = DateTime.Parse(vouch.DateStart, culture);
                        var dateend1 = DateTime.Parse(vouch.ExpiryDate, culture);
                        if (DateTime.Now >= datestart1 && DateTime.Now < dateend1)
                        {
                            var voucherUsed = db.VoucherUseds.Where(x => x.UserId == user && x.VoucherCode == vouch.VoucherCode && x.Status == "Used").FirstOrDefault();
                            if (voucherUsed == null)
                            {
                                CoopVouchers voucherDetails = new CoopVouchers();
                                if (vouch.DiscountType == "Percent")
                                {
                                    voucherDetails.Name = vouch.Name;
                                    voucherDetails.DiscountType = vouch.DiscountType;
                                    voucherDetails.Percent_Discount = vouch.Percent_Discount;
                                    voucherDetails.UserType = vouch.UserType;
                                    voucherDetails.VoucherCode = vouch.VoucherCode;
                                    voucherDetails.VoucherDetails = vouch.Name + " " + vouch.Percent_Discount + "% OFF" + "(For " + vouch.UserType + ")";
                                    voucherDetails.Min_spend = vouch.Min_spend;
                                    voucherDetails.ExpiryDate = dateend1;
                                }
                                else
                                {
                                    voucherDetails.Name = vouch.Name;
                                    voucherDetails.DiscountType = vouch.DiscountType;
                                    voucherDetails.Percent_Discount = vouch.Percent_Discount;
                                    voucherDetails.UserType = vouch.UserType;
                                    voucherDetails.VoucherCode = vouch.VoucherCode;
                                    voucherDetails.VoucherDetails = vouch.Name + " PHP " + vouch.Percent_Discount + "OFF" + "(For " + vouch.UserType + ")";
                                    voucherDetails.Min_spend = vouch.Min_spend;
                                    voucherDetails.ExpiryDate = dateend1;
                                }

                                voucherList.Add(new VoucherList { coopID = coopde2.Id, Vouchers2 = voucherDetails });
                            }
                        }
                    }
                }

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
                    }
                }

                user2.Add(new UserDetails2
                {
                    Userid = user,
                    Name = userdetails.Firstname + " " + userdetails.Lastname,
                    Address = userdetails.Address
                });

                if (itemPrice != 0)
                {
                    prodDetails.Add(new ProductToCheckout
                    {
                        Userid = user,
                        CoopID2 = coopde2.Id.ToString(),
                        ProdCartId = prodcart.Id.ToString(),
                        CartId = prodcart.CartId,
                        ProdId = prodcart.ProductId,
                        ProdName = prode.Product_Name,
                        Image = replace,
                        Qty = prodcart.Qty.ToString(),
                        Price = getprice.Price,
                        MemberDiscountedPrice = 0,
                        DiscountedPrice = decimal.Round(itemPrice, 2, MidpointRounding.AwayFromZero),
                        Subtotal = Convert.ToDecimal(prodcart.Qty.ToString()) * itemPrice,
                        Created_at = prodcart.Created_at.ToString()
                    });
                }
                else
                {
                    prodDetails.Add(new ProductToCheckout
                    {
                        Userid = user,
                        CoopID2 = coopde2.Id.ToString(),
                        ProdCartId = prodcart.Id.ToString(),
                        CartId = prodcart.CartId,
                        ProdId = prodcart.ProductId,
                        ProdName = prode.Product_Name,
                        Image = replace,
                        Qty = prodcart.Qty.ToString(),
                        Price = getprice.Price,
                        MemberDiscountedPrice = memPrice,
                        DiscountedPrice = 0,
                        Subtotal = Convert.ToDecimal(prodcart.Qty.ToString()) * getprice.Price,
                        Created_at = prodcart.Created_at.ToString()
                    });
                }
            }

            IDCoop.Clear();
            model.coopShops = coop2;
            model.Products = prodDetails;
            model.userDetails = user2;
            model.VouchersList = voucherList;

            return View(model);
        }


        [HttpPost]
        public ActionResult CheckoutPage(ViewCheckOutPage model, string voucher)
        {
            var dbase = new ApplicationDbContext();
            var data = new List<object>();

            if (Session["Ids"] == null)
            {
                return RedirectToAction("Index");
            }
            var coopId = Request["coopId"];
            var itemSelected = Session["Ids"].ToString();
            string[] itemsSelected = itemSelected.Split(',');
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();

            List<string> arrid = new List<string>();
            double? dist = 0;
            int final = 0;
            double? deliver_fee = 0;
            List<COOPShop> coop2 = new List<COOPShop>();
            List<UserDetails2> user2 = new List<UserDetails2>();
            List<ProductToCheckout> prodDetails = new List<ProductToCheckout>();
            List<VoucherList> voucherList = new List<VoucherList>();
            List<string> IDCoop = new List<string>();
            decimal totaleach = 0;
            decimal itemPrice = 0;
            decimal memPrice = 0;

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
                memPrice = 0;
                itemPrice = 0;

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
                    var dateStart = DateTime.Parse(disCheck.DateStart, culture);
                    var dateEnd = DateTime.Parse(disCheck.DateEnd, culture);
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

                    coop2.Add(new COOPShop { CoopID = coopde2.Id.ToString(), Delivery = deliver_fee.Value, CoopName = coopde2.CoopName });

                    var vouchers = db.VoucherDetails.Where(v => v.CoopId == coopde2.Id).ToList();
                    foreach (var vouch in vouchers)
                    {
                        CultureInfo culture = new CultureInfo("es-ES");
                        var datestart = DateTime.Parse(vouch.DateStart, culture);
                        var expirydate = DateTime.Parse(vouch.ExpiryDate, culture);
                        if (DateTime.Now >= datestart && DateTime.Now < expirydate)
                        {
                            CoopVouchers voucherDetails = new CoopVouchers();

                            if (vouch.DiscountType == "Percent")
                            {
                                voucherDetails.Name = vouch.Name;
                                voucherDetails.DiscountType = vouch.DiscountType;
                                voucherDetails.Percent_Discount = vouch.Percent_Discount;
                                voucherDetails.UserType = vouch.UserType;
                                voucherDetails.VoucherCode = vouch.VoucherCode;
                                voucherDetails.VoucherDetails = vouch.Name + " " + vouch.Percent_Discount + "% OFF";
                                voucherDetails.Min_spend = vouch.Min_spend;
                                voucherDetails.ExpiryDate = expirydate;
                            }
                            else
                            {
                                voucherDetails.Name = vouch.Name;
                                voucherDetails.DiscountType = vouch.DiscountType;
                                voucherDetails.Percent_Discount = vouch.Percent_Discount;
                                voucherDetails.UserType = vouch.UserType;
                                voucherDetails.VoucherCode = vouch.VoucherCode;
                                voucherDetails.VoucherDetails = vouch.Name + " PHP " + vouch.Percent_Discount + "OFF";
                                voucherDetails.Min_spend = vouch.Min_spend;
                                voucherDetails.ExpiryDate = expirydate;
                            }

                            voucherList.Add(new VoucherList { coopID = coopde2.Id, Vouchers2 = voucherDetails });
                        }
                    }
                }

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
                    }
                }

                user2.Add(new UserDetails2
                {
                    Userid = user,
                    Name = userdetails.Firstname + " " + userdetails.Lastname,
                    Address = userdetails.Address
                });

                if (itemPrice != 0)
                {
                    prodDetails.Add(new ProductToCheckout
                    {
                        Userid = user,
                        CoopID2 = coopde2.Id.ToString(),
                        ProdCartId = prodcart.Id.ToString(),
                        CartId = prodcart.CartId,
                        ProdId = prodcart.ProductId,
                        ProdName = prode.Product_Name,
                        Image = replace,
                        Qty = prodcart.Qty.ToString(),
                        Price = getprice.Price,
                        MemberDiscountedPrice = 0,
                        DiscountedPrice = decimal.Round(itemPrice, 2, MidpointRounding.AwayFromZero),
                        Subtotal = Convert.ToDecimal(prodcart.Qty.ToString()) * itemPrice,
                        Created_at = prodcart.Created_at.ToString()
                    });
                }
                else
                {
                    prodDetails.Add(new ProductToCheckout
                    {
                        Userid = user,
                        CoopID2 = coopde2.Id.ToString(),
                        ProdCartId = prodcart.Id.ToString(),
                        CartId = prodcart.CartId,
                        ProdId = prodcart.ProductId,
                        ProdName = prode.Product_Name,
                        Image = replace,
                        Qty = prodcart.Qty.ToString(),
                        Price = getprice.Price,
                        MemberDiscountedPrice = memPrice,
                        DiscountedPrice = 0,
                        Subtotal = Convert.ToDecimal(prodcart.Qty.ToString()) * getprice.Price,
                        Created_at = prodcart.Created_at.ToString()
                    });
                }
            }

            foreach (var coops in model.coopShops)
            {
                if (coops.VoucherCode != null)
                {
                    foreach (var coop4 in coop2)
                    {
                        if (coop4.CoopID == coops.CoopID)
                        {
                            coop4.VoucherCode = coops.VoucherCode;
                        }
                    }
                }
            }

            IDCoop.Clear();
            model.coopShops = coop2;
            model.Products = prodDetails;
            model.userDetails = user2;

            decimal discPrice = 0;

            if (voucher != null)
            {
                foreach (var coops in model.coopShops)
                {
                    if (coops.VoucherCode != null)
                    {
                        foreach (var coop4 in coop2)
                        {
                            if (coop4.CoopID == coops.CoopID)
                            {
                                if (coops.VoucherCode == voucher)
                                {
                                    coop4.VoucherCode = null;
                                    coops.VoucherCode = null;
                                    voucher = null;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var coop1 in model.coopShops)
            {
                if (coop1.VoucherCode != null)
                {
                    var voucherList2 = db.VoucherDetails.Where(x => x.CoopId.ToString() == coop1.CoopID && x.VoucherCode == coop1.VoucherCode && x.Min_spend <= coop1.TotalEach).FirstOrDefault();
                    if (voucherList2 != null)
                    {
                        CultureInfo culture = new CultureInfo("es-ES");
                        if (DateTime.Now >= Convert.ToDateTime(voucherList2.DateStart, culture) && DateTime.Now < Convert.ToDateTime(voucherList2.ExpiryDate, culture))
                        {
                            if (voucherList2.UserType == userRole.Role || voucherList2.UserType == "Both")
                            {
                                if (voucherList2.DiscountType == "Percent")
                                {
                                    double discount = Convert.ToDouble(coop1.TotalEach) * (Convert.ToDouble(voucherList2.Percent_Discount) / 100);
                                    discPrice = coop1.TotalEach - Convert.ToDecimal(discount);
                                    coop1.discountedTotalPrice = discPrice;
                                    coop1.VoucherCode = coop1.VoucherCode;
                                    coop1.voucherUsed = voucherList2.Name + " " + voucherList2.Percent_Discount + "% OFF";
                                }
                                else
                                {
                                    discPrice = coop1.TotalEach - Convert.ToDecimal(voucherList2.Percent_Discount);
                                    coop1.discountedTotalPrice = discPrice;
                                    coop1.voucherUsed = voucherList2.Name + " PHP" + voucherList2.Percent_Discount + " OFF";
                                }
                            }
                            else
                            {
                                coop1.VoucherError = "You are not applicable in using this voucher.";
                            }
                        }
                        else
                        {
                            coop1.VoucherError = "Voucher is already expired.";
                        }
                    }
                    else
                    {
                        coop1.VoucherCode = null;
                        coop1.VoucherError = "Voucher does not exist, kindly check your voucher code.";
                    }
                }
            }

            model.coopShops = model.coopShops;
            model.Products = model.Products;
            model.userDetails = model.userDetails;
            model.VouchersList = voucherList;

            return View(model);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult PlaceYourOrder()
        {
            var dbase = new ApplicationDbContext();
            var data = new List<object>();
            var select = Request["select"];
            var user_id = Request["id"];
            Session["select"] = select;
            Session["user_id"] = user_id;

            var vouch = Request["vouchCode"];
            var itemSelected = Session["Ids"].ToString();
            Session["vouchCode"] = vouch;
            string totalamount = Request["total"];
            Session["TotalAmount"] = totalamount;
            string[] itemsSelected = itemSelected.Split(',');
            string[] vouchers = vouch.Split(',');
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userRole = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            var comRate = db.CommissionDetails.OrderByDescending(cr => cr.Id).FirstOrDefault();

            List<string> arrid = new List<string>();
            double? dist = 0;
            int final = 0;
            double? deliver_fee = 0;
            List<COOPShop> coop2 = new List<COOPShop>();
            List<UserDetails2> user2 = new List<UserDetails2>();
            List<ProductToCheckout> prodDetails = new List<ProductToCheckout>();
            List<string> IDCoop = new List<string>();
            decimal tots = 0;
            decimal finaltot = 0;
            var clear = "";
            var userOrderId = "";
            decimal subTotal = 0;

            foreach (string item in itemsSelected)
            {
                List<string> coopID = new List<string>();
                var prodcart = db.ProdCart.Where(x => x.Id.ToString() == item && x.CartId == cart.Id.ToString()).FirstOrDefault();
                var prode = db.ProductDetails.Where(x => x.Id.ToString() == prodcart.ProductId).FirstOrDefault();
                using (var db1 = new SqlConnection(connectionString))
                {
                    db1.Open();
                    using (var cmd = db1.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT * from ProductCarts AS prod1 JOIN ProductDetailsModels AS prod2 on prod1.ProductId = prod2.Id JOIN UserCarts AS UC ON prod1.CartId = UC.Id WHERE prod1.ProductId = '" + prode.Id + "' and UC.UserId = '" + user_id + "'";
                        SqlDataReader reader = cmd.ExecuteReader();
                        reader.Read();

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
                        var coopAdminId = reader["CoopAdminId"].ToString();
                        var coopAdmin = db.CoopAdminDetails.Where(x => x.UserId.ToString() == coopAdminId).FirstOrDefault();
                        var coopde2 = db.CoopDetails.Where(x => x.Id == coopAdmin.Coop_code).FirstOrDefault();
                        tots = Convert.ToDecimal(reader["Qty"].ToString()) * Convert.ToDecimal(getprice.Price);
                        finaltot = finaltot + tots;
                    }

                    db1.Close();
                }
            }
            if (select == "E-Wallet")
            {
                var checkwallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
                if (checkwallet.Balance < finaltot)
                {
                    clear = "not clear";
                }
                else
                {
                    clear = "clear";
                }
            }
            else
            {
                clear = "clear";
            }

            var date = "";
            IDCoop.Clear();

            if (clear == "clear")
            {
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
                        user_order.ModeOfPay = select;
                        user_order.OStatus = "To Pay";
                        user_order.UserId = user;
                        user_order.Delivery_fee = deliver_fee.Value;
                        user_order.OrderCreated_at = DateTime.Now.ToString();
                        date = user_order.OrderCreated_at;
                        db.UserOrders.Add(user_order);
                        db.SaveChanges();

                        var checkorder = db.UserOrders.Where(x => x.UserId == user && x.Delivery_fee == deliver_fee && x.CoopId == coopde2.Id
                            && x.OStatus == "To Pay" && x.ModeOfPay == select && x.OrderCreated_at == date).FirstOrDefault();

                        var notif = new NotificationModel
                        {
                            ToRole = "",
                            ToUser = coopAdmin.UserId,
                            NotifFrom = user,
                            NotifHeader = userRole.Firstname + " " + userRole.Lastname + " ordered.",
                            NotifMessage = "View to see what he/she ordered and ready to prepare it!",
                            NavigateURL = "OrderDetails/" + checkorder.Id,
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
                        objNotifHub.SendNotification(notif.ToUser);
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
                            && x.OStatus == "To Pay" && x.ModeOfPay == select && x.OrderCreated_at == date).FirstOrDefault();
                            checkorder.TotalPrice = coop3.TotalEach;
                            checkorder.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                            db.Entry(checkorder).State = EntityState.Modified;
                            db.SaveChanges();

                            CommissionSale commissionSale = new CommissionSale();
                            commissionSale.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                            commissionSale.CoopCode = coopde2.Id;
                            commissionSale.CoopAdminId = coopAdmin.UserId;
                            if (select == "Cash On Delivery")
                                commissionSale.Status = "Pending";
                            else
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
                            && x.OStatus == "To Pay" && x.ModeOfPay == select && x.OrderCreated_at == date).FirstOrDefault();
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
                            && x.OStatus == "To Pay" && x.ModeOfPay == select && x.OrderCreated_at == date).FirstOrDefault();
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

                foreach (var coop3 in coop2)
                {
                    var checkorder = db.UserOrders.Where(x => x.UserId == user && x.CoopId.ToString() == coop3.CoopID && x.OStatus == "To Pay").OrderByDescending(x => x.Id).FirstOrDefault();

                    foreach (var voucher in vouchers)
                    {
                        var voucherList = db.VoucherDetails.Where(x => x.CoopId.ToString() == coop3.CoopID && x.VoucherCode == voucher && x.Min_spend <= coop3.TotalEach).FirstOrDefault();
                        if (voucherList != null)
                        {
                            if (voucherList.DiscountType == "Percent")
                            {
                                var voucherUsed = new UserVoucherUsed();
                                voucherUsed.UserId = user;
                                voucherUsed.CoopId = Convert.ToInt32(coop3.CoopID);
                                voucherUsed.UserOrderId = checkorder.Id.ToString();
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
                                if (select == "Cash On Delivery")
                                {
                                    commissionSale.Status = "Pending";
                                }
                                else
                                {
                                    commissionSale.Status = "Received";
                                }

                                commissionSale.Created_at = DateTime.Now;
                                commissionSale.Updated_at = DateTime.Now;
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
                                voucherUsed.UserOrderId = checkorder.Id.ToString();
                                voucherUsed.VoucherCode = voucher;
                                voucherUsed.DateCreated = DateTime.Now;
                                db.VoucherUseds.Add(voucherUsed);
                                db.SaveChanges();
                                subTotal = checkorder.TotalPrice - (Convert.ToDecimal(voucherList.Percent_Discount));
                                checkorder.TotalPrice = subTotal;
                                checkorder.CommissionFee = subTotal * (comRate.Rate / 100);
                                db.Entry(checkorder).State = EntityState.Modified;
                                db.SaveChanges();

                                CommissionSale commissionSale = new CommissionSale();
                                commissionSale.CommissionFee = coop3.TotalEach * (comRate.Rate / 100);
                                commissionSale.CoopCode = Convert.ToInt32(coop3.CoopID);
                                if (select == "Cash On Delivery")
                                {
                                    commissionSale.Status = "Pending";
                                }
                                else
                                {
                                    commissionSale.Status = "Received";
                                }

                                commissionSale.Created_at = DateTime.Now;
                                commissionSale.Updated_at = DateTime.Now;
                                commissionSale.UserOrderID = checkorder.Id;
                                commissionSale.CoopAdminId = coop3.CoopAdminId;
                                db.CommissionSales.Add(commissionSale);
                                db.SaveChanges();
                            }
                        }
                    }

                    if (select == "E-Wallet")
                    {
                        checkorder.OStatus = "Paid";
                        db.Entry(checkorder).State = EntityState.Modified;
                        db.SaveChanges();

                        var userEwallet = db.UserEWallet.Where(x => x.UserID == user).FirstOrDefault();
                        userEwallet.Balance -= checkorder.TotalPrice + Convert.ToDecimal(checkorder.Delivery_fee);
                        db.Entry(userEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        var ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = userEwallet.ID;
                        ewalletHistory.Amount = checkorder.TotalPrice + Convert.ToDecimal(checkorder.Delivery_fee);
                        ewalletHistory.Action = "Payment";
                        ewalletHistory.Description = "Order No. " + checkorder.Id + "is paid successfully.";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                        adminEwallet.Balance += checkorder.TotalPrice + Convert.ToDecimal(checkorder.Delivery_fee);
                        db.Entry(adminEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = adminEwallet.ID;
                        ewalletHistory.Amount = checkorder.TotalPrice + Convert.ToDecimal(checkorder.Delivery_fee);
                        ewalletHistory.Action = "Order Payment";
                        ewalletHistory.Description = "Payment received from Order No. " + checkorder.Id + ".";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        
                    }
                }

                DelItemFromCart();

                data.Add(new
                {
                    mess = 1,
                });
            }
            else
            {
                data.Add(new
                {
                    mess = 2,
                });
            }

            IDCoop.Clear();

            return Json(data, JsonRequestBehavior.AllowGet);
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
            var userCarts = db.ProdCart.Where(pc => pc.Qty >= prod.Product_qty).ToList();

            foreach (var userProd in userCarts)
            {
                userProd.Qty = userProd.Qty - qty;
                db.Entry(userProd).State = EntityState.Modified;
                db.SaveChanges();
            }
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CheckoutItems()
        {
            var dbase = new ApplicationDbContext();
            var data = new List<object>();
            var data2 = new List<object>();

            if (Session["Ids"].ToString() == null)
            {
                return RedirectToAction("Index");
            }

            var itemSelected = Session["Ids"].ToString();
            string[] itemsSelected = itemSelected.Split(',');
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var cart = db.Cart.Where(x => x.UserId == user).FirstOrDefault();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            List<string> arrid = new List<string>();
            double? dist = 0;
            int final = 0;
            double? deliver_fee = 0;

            foreach (string item2 in itemsSelected)
            {
                var prodcart = db.ProdCart.Where(x => x.Id.ToString() == item2 && x.CartId == cart.Id.ToString()).FirstOrDefault();
                var prode = db.ProductDetails.Where(x => x.Id.ToString() == prodcart.ProductId).FirstOrDefault();
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

                if (!arrid.Contains(cooploc.CoopId))
                {
                    arrid.Add(cooploc.CoopId);
                    dist = 0;

                    if (cooploc.Cooplocation >= 0 && cooploc.Cooplocation <= 1000)
                    {
                        deliver_fee = deliver_fee + 55;
                    }
                    else
                    {
                        final = (int)Math.Round(cooploc.Cooplocation.Value);
                        dist = (int)Math.Round(0.001 * (float)(final));
                        deliver_fee = deliver_fee + 55 + (10 * (dist - 1));
                    }
                }
            }

            arrid.Clear();

            foreach (string item in itemsSelected)
            {
                using (var db1 = new SqlConnection(connectionString))
                {
                    db1.Open();
                    using (var cmd = db1.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "SELECT * from ProductCarts AS prod1 JOIN ProductDetailsModels AS prod2 on prod1.ProductId = prod2.Id JOIN UserCarts AS UC ON prod1.CartId = UC.Id WHERE prod1.Id = '" + item + "' and UC.UserId = '" + user + "'";
                        SqlDataReader reader = cmd.ExecuteReader();
                        reader.Read();

                        string source = reader["Product_image"].ToString();
                        var replace = source.Replace("~", "..");

                        var prodID = reader["ProductId"].ToString();
                        var getprice = db.Prices.Where(x => x.ProdId.ToString() == prodID).OrderByDescending(p => p.Id).FirstOrDefault();

                        data.Add(new
                        {
                            Delivery_fee = deliver_fee,
                            Name = userdetails.Firstname + " " + userdetails.Lastname,
                            Address = userdetails.Address,
                            ProdCartId = reader["Id"].ToString(),
                            CartId = reader["CartId"].ToString(),
                            ProdId = reader["ProductId"].ToString(),
                            ProdName = reader["Product_Name"].ToString(),
                            Image = replace,
                            Qty = reader["Qty"].ToString(),
                            Price = getprice.Price,
                            Subtotal = Convert.ToDecimal(reader["Qty"].ToString()) * getprice.Price,
                            Created_at = reader["Created_at"].ToString()
                        });
                    }

                    db1.Close();
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewCoop(int coopID)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var model = new ViewCoopHomePage();
            var coop = db.CoopDetails.Where(x => x.Id == coopID).FirstOrDefault();
            var vouchers = db.VoucherDetails.Where(v => v.CoopId == coopID).ToList();
            var products = (from prod in db.ProductDetails
                            join categ in db.CategoryDetails
                            on prod.Category_Id equals categ.Id
                            where prod.CoopId == coopID
                            select new ViewListProd
                            {
                                Id = prod.Id,
                                Product_image = prod.Product_image,
                                Product_name = prod.Product_Name,
                                Product_desc = prod.Product_desc,
                                Product_qty = prod.Product_qty,
                                Category = categ.Name,
                                Created_at = prod.Prod_Created_at.ToString(),
                                Updated_at = prod.Prod_Updated_at.ToString(),
                                CustomerId = prod.CustomerId,
                                CurrentId = user
                            }).ToList();

            var discountCheck = db.DiscountModels.Where(d => d.CoopID == coopID).ToList();
            List<ViewListProd> viewListProds = new List<ViewListProd>();
            List<VoucherDetailsModel> voucherDetails = new List<VoucherDetailsModel>();
            List<int> prodID = new List<int>();

            foreach (var disCheck in discountCheck)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                var dateStart = Convert.ToDateTime(disCheck.DateStart, culture);
                var dateEnd = Convert.ToDateTime(disCheck.DateEnd, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < dateEnd.Ticks)
                {
                    foreach (var item in products)
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
                                Product_image = item.Product_image,
                                Product_name = item.Product_name,
                                Product_desc = item.Product_desc,
                                Product_price = getprice.Price,
                                Product_manufact = getmanu.Manufacturer,
                                Product_qty = item.Product_qty,
                                Product_cost = getCost.Cost,
                                Category = item.Category,
                                CurrentId = user
                            });

                            prodID.Add(item.Id);
                        }
                    }
                }
            }

            foreach (var item in products)
            {
                if (!prodID.Contains(item.Id))
                {
                    var getprice = db.Prices.Where(x => x.ProdId == item.Id && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();

                    viewListProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Product_image = item.Product_image,
                        Product_name = item.Product_name,
                        Product_desc = item.Product_desc,
                        Product_price = getprice.Price,
                        Product_manufact = item.Product_manufact,
                        Product_qty = item.Product_qty,
                        Product_cost = item.Product_cost,
                        Category = item.Category,
                        CustomerId = item.CustomerId,
                        CurrentId = user
                    });
                }
            }

            prodID.Clear();

            foreach (var vouch in vouchers)
            {
                CultureInfo culture = new CultureInfo("es-ES");
                DateTime dateStart = Convert.ToDateTime(vouch.DateStart, culture);
                DateTime expiryDate = Convert.ToDateTime(vouch.ExpiryDate, culture);
                if (DateTime.Now.Ticks >= dateStart.Ticks && DateTime.Now.Ticks < expiryDate.Ticks)
                {
                    voucherDetails.Add(new VoucherDetailsModel
                    {
                        Name = vouch.Name,
                        DiscountType = vouch.DiscountType,
                        Percent_Discount = vouch.Percent_Discount,
                        UserType = vouch.UserType,
                        VoucherCode = vouch.VoucherCode,
                        ExpiryDate = vouch.ExpiryDate
                    });
                }
            }

            model.Coop = coop;
            model.Products = viewListProds;
            model.Vouchers = voucherDetails;
            return View(model);
        }

        public ActionResult ViewNotification()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var model = new ViewNotification();
            List<NotificationModel> unreadNotif = new List<NotificationModel>();
            List<NotificationModel> readNotif = new List<NotificationModel>();
            var userDetails = db.Users.Where(r => r.Id == user).FirstOrDefault();

            if (User.IsInRole("Member"))
            {
                var unreadNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Member") && n.IsRead == false).ToList();

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
                }
                else
                {
                    unreadNotif = null;
                }

                var readNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Member") && n.IsRead == true).ToList();

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
                }
                else
                {
                    readNotif = null;
                }
            }
            else
            {
                var unreadNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Non-member") && n.IsRead == false).ToList();

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
                }
                else
                {
                    unreadNotif = null;
                }

                var readNotification = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Non-member") && n.IsRead == true).ToList();

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
                }
                else
                {
                    readNotif = null;
                }
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
            var userDetails = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.UserId == user).ToList();
            List<ViewInbox> inbox = new List<ViewInbox>();
            var model = new ViewChat();

            if (coopChat != null)
            {
                foreach (var chat in coopChat)
                {
                    var chatMessage = db.ChatMessages.Where(cm => cm.CoopChatId == chat.Id).OrderByDescending(cm => cm.Id).FirstOrDefault();
                    var coopDetails = db.CoopDetails.Where(cd => cd.Id.ToString() == chat.CoopId).FirstOrDefault();

                    inbox.Add(new ViewInbox
                    {
                        InboxId = chat.Id,
                        ReceiversName = coopDetails.CoopName,
                        ReceiversId = coopDetails.Id.ToString(),
                        SenderName = userDetails.Firstname + " " + userDetails.Lastname,
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

        public ActionResult CoopChat(int id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userDetails = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id == id).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.UserId == user && cc.CoopId == id.ToString()).FirstOrDefault();
            List<ChatMessage> messages = new List<ChatMessage>();
            var model = new ViewChat();

            if (coopChat != null)
            {
                var chatMessages = db.ChatMessages.Where(cm => cm.CoopChatId == coopChat.Id).ToList();

                if (chatMessages != null)
                {
                    foreach (var message in chatMessages)
                    {
                        var from = "";

                        if (message.From == userDetails.AccountId)
                        {
                            from = userDetails.Firstname + " " + userDetails.Lastname;
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

            model.SenderName = userDetails.Firstname + " " + userDetails.Lastname;
            model.ReceiversName = coopDetails.CoopName;
            model.ReceiversId = coopDetails.Id.ToString();
            model.Messages = messages;

            return View(model);
        }

        [HttpPost]
        public ActionResult CoopChat(ViewChat model)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userDetails = db.UserDetails.Where(u => u.AccountId == user).FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(c => c.Id.ToString() == model.ReceiversId).FirstOrDefault();
            var coopChat = db.CoopChats.Where(cc => cc.UserId == user && cc.CoopId == model.ReceiversId.ToString()).FirstOrDefault();
            List<ChatMessage> messages = new List<ChatMessage>();

            if (coopChat != null)
            {
                var chatMessage = new ChatMessage
                {
                    CoopChatId = coopChat.Id,
                    From = user,
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
                    UserId = user
                };

                db.CoopChats.Add(coopChat);
                db.SaveChanges();

                coopChat = db.CoopChats.Where(cc => cc.UserId == user && cc.CoopId == model.ReceiversId.ToString()).FirstOrDefault();

                var chatMessage = new ChatMessage
                {
                    CoopChatId = coopChat.Id,
                    From = user,
                    MessageBody = model.MessageToSend,
                    DateSent = DateTime.Now,
                    IsRead = false,
                };

                db.ChatMessages.Add(chatMessage);
                db.SaveChanges();
            }

            if (coopChat != null)
            {
                var chatMessages = db.ChatMessages.Where(cm => cm.CoopChatId == coopChat.Id).ToList();
                if (chatMessages != null)
                {
                    foreach (var message in chatMessages)
                    {
                        var from = "";

                        if (message.From == userDetails.AccountId)
                        {
                            from = userDetails.Firstname + " " + userDetails.Lastname;
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

            model.SenderName = userDetails.Firstname + " " + userDetails.Lastname;
            model.ReceiversName = coopDetails.CoopName;
            model.ReceiversId = coopDetails.Id.ToString();
            model.Messages = messages;
            model.MessageToSend = "";

            return View(model);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult ToShipOrders()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<OrderList> orderlist = new List<OrderList>();
            var userorder = db.UserOrders.Where(x => x.UserId == user && (x.OStatus == "Ready for pick-up." || x.OStatus == "Paid")).ToList();
            decimal total = 0;
            if (userorder != null)
            {
                foreach (var item in userorder)
                {
                    total = (int)item.Delivery_fee;
                    var prodorder = db.ProdOrders.Where(x => x.UserId == user && x.UOrderId == item.Id.ToString()).ToList();
                    var coopde = db.CoopDetails.Where(x => x.Id == item.CoopId).FirstOrDefault();

                    foreach (var item2 in prodorder)
                    {
                        total += (int)item2.SubTotal;
                    }

                    orderlist.Add(new OrderList
                    {
                        Delivery_fee = item.Delivery_fee.ToString(),
                        OrderNo = item.Id.ToString(),
                        Id = item.Id,
                        TotalAmount = total,
                        CustomerName = coopde.CoopName,
                        ModeOfPay = item.ModeOfPay
                    });
                }
            }

            return View(orderlist);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult ToReceiveOrder()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<OrderList> orderlist = new List<OrderList>();
            var userorder = db.UserOrders.Where(x => x.UserId == user && x.OStatus == "To Be Delivered").ToList();
            decimal total = 0;
            if (userorder != null)
            {
                foreach (var item in userorder)
                {
                    total = (int)item.Delivery_fee;
                    var prodorder = db.ProdOrders.Where(x => x.UserId == user && x.UOrderId == item.Id.ToString()).ToList();
                    var coopde = db.CoopDetails.Where(x => x.Id == item.CoopId).FirstOrDefault();

                    foreach (var item2 in prodorder)
                    {
                        total += (int)item2.SubTotal;
                    }

                    orderlist.Add(new OrderList
                    {
                        Delivery_fee = item.Delivery_fee.ToString(),
                        OrderNo = item.Id.ToString(),
                        Id = item.Id,
                        TotalAmount = total,
                        CustomerName = coopde.CoopName,
                        ModeOfPay = item.ModeOfPay
                    });
                }
            }

            return View(orderlist);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CompleteOrder()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<OrderList> orderlist = new List<OrderList>();
            var userorder = db.UserOrders.Where(x => x.UserId == user && (x.OStatus == "Complete" || x.OStatus == "Transferred")).ToList();
            decimal total = 0;
            if (userorder != null)
            {
                foreach (var item in userorder)
                {
                    total = (int)item.Delivery_fee;
                    var prodorder = db.ProdOrders.Where(x => x.UserId == user && x.UOrderId == item.Id.ToString()).ToList();
                    var coopde = db.CoopDetails.Where(x => x.Id == item.CoopId).FirstOrDefault();

                    foreach (var item2 in prodorder)
                    {
                        total += (int)item2.SubTotal;
                    }

                    orderlist.Add(new OrderList
                    {
                        Delivery_fee = item.Delivery_fee.ToString(),
                        OrderNo = item.Id.ToString(),
                        Id = item.Id,
                        TotalAmount = total,
                        CustomerName = coopde.CoopName,
                        ModeOfPay = item.ModeOfPay
                    });
                }
            }

            return View(orderlist);
        }

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult CancelledOrder()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<OrderList> orderlist = new List<OrderList>();
            var userorder = db.UserOrders.Where(x => x.UserId == user && x.OStatus == "Cancelled").ToList();
            decimal total = 0;
            if (userorder != null)
            {
                foreach (var item in userorder)
                {
                    total = (int)item.Delivery_fee;
                    var prodorder = db.ProdOrders.Where(x => x.UserId == user && x.UOrderId == item.Id.ToString()).ToList();
                    var coopde = db.CoopDetails.Where(x => x.Id == item.CoopId).FirstOrDefault();

                    foreach (var item2 in prodorder)
                    {
                        total += (int)item2.SubTotal;
                    }

                    orderlist.Add(new OrderList
                    {
                        Delivery_fee = item.Delivery_fee.ToString(),
                        OrderNo = item.Id.ToString(),
                        Id = item.Id,
                        TotalAmount = total,
                        CustomerName = coopde.CoopName,
                        ModeOfPay = item.ModeOfPay
                    });
                }
            }

            return View(orderlist);
        }

        [Authorize(Roles = "Member, Non-member")]
        [HttpGet]
        public ActionResult CompleteOrderDetails(string id)
        {
            var db = new ApplicationDbContext();
            var uid = Convert.ToInt32(id);
            var userOrder = db.UserOrders.Where(u => u.Id == uid).FirstOrDefault();
            var prodOrder = db.ProdOrders.Where(p => p.UOrderId == id).ToList();
            var voucherUsed = db.VoucherUseds.Where(v => v.UserOrderId == id).FirstOrDefault();
            var deliveryDetails = db.DeliverStatus.Where(o => o.UserOrderId == uid).FirstOrDefault();
            var comRate = db.CommissionDetails.OrderByDescending(c => c.Id).FirstOrDefault();
            var rates = db.Reviews.Where(r => r.UserId == userOrder.UserId).ToList();
            var refundRequest = db.ReturnRefunds.Where(r => r.UserOrderId == userOrder.Id).FirstOrDefault();
            bool isRated = false;

            foreach (var item in prodOrder)
            {
                foreach (var rate in rates)
                {
                    if (item.Id == rate.ProdOrderId)
                    {
                        isRated = true;
                    }
                    else
                    {
                        isRated = false;
                    }
                }
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

            if (deliveryDetails != null)
            {
                var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
                if (deliveryDetails.PickUpSuccessDate == date || deliveryDetails.ExpectedDeldate == date || deliveryDetails.DateDelivered == date)
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
            order.Delivery_fee = userOrder.Delivery_fee;

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

            model.UserOrders = order;
            model.ProdOrders = product;
            model.DeliveryDetails = deliver;
            model.IsRated = isRated;
            if (refundRequest != null)
            {
                model.RefundStatus = refundRequest.Status;
            }

            return View(model);
        }

        public ActionResult ProceedToPayment()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> ProceedToPayment(string submit)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userAcc = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            var userEwallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
            var userIdentity = db.Users.Where(x => x.Id == user).FirstOrDefault();
            var checkRole = userIdentity.Roles.Where(x => x.UserId == user).FirstOrDefault();
            var roles = db.Roles.Where(x => x.Id == checkRole.RoleId).FirstOrDefault();
            var checkMemRequest = db.Memberships.Where(x => x.UserId == user && x.RequestStatus == "To be payed").FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id.ToString() == checkMemRequest.Coop_code).FirstOrDefault();
            var getFee = db.MembershipFees.Where(x => x.COOP_ID == coopDetails.Id.ToString()).FirstOrDefault();
            var coopEwallet = db.UserEWallet.Where(x => x.COOP_ID == coopDetails.Id.ToString() && x.Status == "Active").FirstOrDefault();

            if (userAcc != null)
            {
                if (checkMemRequest != null)
                {
                    if (userEwallet.Balance < getFee.MemFee)
                    {
                        ViewBag.ErrorMessage = "Your e-wallet balance is not enough.";
                        return View();
                    }
                    else
                    {
                        decimal commisionFee = (getFee.MemFee * (2 / 100));
                        decimal toCoop = getFee.MemFee - (getFee.MemFee * (2 / 100));

                        coopEwallet.Balance += toCoop;
                        db.Entry(coopEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        var ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = coopEwallet.ID;
                        ewalletHistory.Amount = toCoop;
                        ewalletHistory.Action = "Membership Fee Payment";
                        ewalletHistory.Description = "Membership payment received from Mr./Ms. " + userAcc.Lastname + ", " + userAcc.Firstname;
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        userAcc.Role = "Member";
                        db.Entry(userAcc).State = EntityState.Modified;
                        db.SaveChanges();

                        userEwallet.Balance -= getFee.MemFee;
                        db.Entry(userEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = userEwallet.ID;
                        ewalletHistory.Amount = getFee.MemFee;
                        ewalletHistory.Action = "Payment";
                        ewalletHistory.Description = "Membership payment for " + coopDetails.CoopName + " is successfully paid.";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        checkMemRequest.RequestStatus = "Approved";
                        checkMemRequest.Date_joined = DateTime.Now.ToString();
                        db.Entry(checkMemRequest).State = EntityState.Modified;
                        db.SaveChanges();

                        db.Entry(checkRole).State = EntityState.Deleted;
                        db.SaveChanges();

                        var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                        adminEwallet.Balance += commisionFee;
                        db.Entry(adminEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = adminEwallet.ID;
                        ewalletHistory.Amount = commisionFee;
                        ewalletHistory.Action = "Membership Commission Payment";
                        ewalletHistory.Description = "Payment received from " + coopDetails.CoopName + "(" + coopDetails.Id + ").";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        await this.UserManager.AddToRoleAsync(user, "Member");
                        ViewBag.SuccessMessage = "Membership fee has been paid. You are officialy a member of " + coopDetails.CoopName + "!";
                    }
                }
            }

            return View();
        }

        public ActionResult ContactUs()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ContactUs(CompalintsModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            string name = "";
            string extension = "";
            string title = "[" + model.Category + "]" + " " + model.Reason;
            string path = "";
            var complaint = new Complaints();

            if (model.File != null)
            {
                var validate = ValidateFile(model.File);

                if (validate == true)
                {
                    name = Path.GetFileNameWithoutExtension(model.File.FileName);
                    extension = Path.GetExtension(model.File.FileName);
                }

                complaint.PostFile = name + extension;
                path = Path.Combine(Server.MapPath("../ComplaintFiles"), complaint.PostFile);
            }

            complaint.Email = model.Email;
            complaint.Fullname = model.Fullname;
            complaint.Category = model.Category;
            complaint.Reason = model.Reason;
            complaint.Description = model.Description;
            complaint.Status = "Pending";
            complaint.DateCreated = DateTime.Now.ToString();

            db.Complaints.Add(complaint);
            if (!String.IsNullOrEmpty(path))
            {
                model.File.SaveAs(path);
            }

            ViewBag.Message = "Kindly check your email as we will get back to you to the email that you provided. It may take 2-3 days.";

            return View();
        }

        private bool ValidateFile(HttpPostedFileBase file)
        {
            string fileExtension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedFileTypes = { ".Jpg", ".png", ".jpg", "jpeg", ".pdf", ".docx" };
            if ((file.ContentLength > 0 && file.ContentLength < 2097152) && allowedFileTypes.Contains(fileExtension))
            {
                return true;
            }
            return false;
        }

        [Authorize(Roles = "Member, Non-member")]
        [HttpGet]
        public ActionResult RateOrder(string id)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            Session["myorderdetail"] = id;
            List<CoopProdOrder> coopProds = new List<CoopProdOrder>();
            var prodorder = (from prod in db.ProdOrders
                             join prod2 in db.ProductDetails
                             on prod.ProdId equals prod2.Id.ToString()
                             where prod.UOrderId == id
                             select new CoopProdOrder
                             {
                                 ProdOrderId = prod.Id.ToString(),
                                 UOrderId = prod.UOrderId,
                                 ProdId = prod.ProdId,
                                 ProdName = prod.ProdName,
                                 Qty = prod.Qty.ToString(),
                                 Price = prod.Price,
                                 Subtotal = prod.SubTotal,
                                 Image = prod2.Product_image
                             }).ToList();

            foreach (var prod in prodorder)
            {
                var checkrev = db.Reviews.Where(x => x.ProdOrderId.ToString() == prod.ProdOrderId).FirstOrDefault();
                if (checkrev != null)
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = true
                    });
                }
                else
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = false
                    });
                }
            }

            return View(coopProds);
        }

        [HttpPost]
        public ActionResult RateOrder()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            string id = Session["myorderdetail"].ToString();
            string prodid = Request["prodid"];
            string orderid = Request["orderid"];
            string rate = Request["rate"];
            string description = Request["description"];
            int inrate = int.Parse(rate);
            if (inrate == 0)
            {
                inrate = 1;
            }
            List<CoopProdOrder> coopProds = new List<CoopProdOrder>();

            if (inrate > 5)
            {
                var prodorder1 = (from prod in db.ProdOrders
                                  join prod2 in db.ProductDetails
                                  on prod.ProdId equals prod2.Id.ToString()
                                  where prod.UOrderId == id
                                  select new CoopProdOrder
                                  {
                                      ProdOrderId = prod.Id.ToString(),
                                      UOrderId = prod.UOrderId,
                                      ProdId = prod.ProdId,
                                      ProdName = prod.ProdName,
                                      Qty = prod.Qty.ToString(),
                                      Price = prod.Price,
                                      Subtotal = prod.SubTotal,
                                      Image = prod2.Product_image
                                  }).ToList();
                ViewBag.ErrorMessage = "The rate maximum is 5, and we'll take that as a compliment.";
                return View(prodorder1);
            }

            if (Request["anonymous"] == null)
            {
                var prodorder1 = (from prod in db.ProdOrders
                                  join prod2 in db.ProductDetails
                                  on prod.ProdId equals prod2.Id.ToString()
                                  where prod.UOrderId == id
                                  select new CoopProdOrder
                                  {
                                      ProdOrderId = prod.Id.ToString(),
                                      UOrderId = prod.UOrderId,
                                      ProdId = prod.ProdId,
                                      ProdName = prod.ProdName,
                                      Qty = prod.Qty.ToString(),
                                      Price = prod.Price,
                                      Subtotal = prod.SubTotal,
                                      Image = prod2.Product_image
                                  }).ToList();
                ViewBag.ErrorMessage2 = "Please select an option.";
                return View(prodorder1);
            }
            string selected = Request.Form["anonymous"].ToString();
            var review = new Review();

            if (selected == "Yes")
                review.IsAnonymous = true;
            else
                review.IsAnonymous = false;

            review.Desc = description;
            review.Rate = inrate;
            review.ProdOrderId = int.Parse(orderid);
            review.UserId = user;
            review.Created_at = DateTime.Now.ToString();
            review.ProdId = int.Parse(prodid);
            db.Reviews.Add(review);
            db.SaveChanges();

            var prodorder = (from prod in db.ProdOrders
                             join prod2 in db.ProductDetails
                             on prod.ProdId equals prod2.Id.ToString()
                             where prod.UOrderId == id
                             select new CoopProdOrder
                             {
                                 ProdOrderId = prod.Id.ToString(),
                                 UOrderId = prod.UOrderId,
                                 ProdId = prod.ProdId,
                                 ProdName = prod.ProdName,
                                 Qty = prod.Qty.ToString(),
                                 Price = prod.Price,
                                 Subtotal = prod.SubTotal,
                                 Image = prod2.Product_image
                             }).ToList();

            foreach (var prod in prodorder)
            {
                var checkrev = db.Reviews.Where(x => x.ProdOrderId.ToString() == prod.ProdOrderId).FirstOrDefault();
                if (checkrev != null)
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = true
                    });
                }
                else
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = false
                    });
                }
            }

            var userorder = db.UserOrders.Where(x => x.UserId == user && x.Id.ToString() == orderid).FirstOrDefault();
            var returnrefunds = db.ReturnRefunds.Where(x => x.UserOrderId == userorder.Id).FirstOrDefault();
            if (userorder != null)
            {
                if(userorder.ModeOfPay == "E-Wallet" || userorder.ModeOfPay == "Paypal")
                {
                    decimal money = 0;

                    if (returnrefunds != null)
                    {
                        money = ((userorder.TotalPrice + Convert.ToDecimal(userorder.Delivery_fee)) - userorder.CommissionFee) - returnrefunds.RefundAmount;
                    }
                    else
                    {
                        money = ((userorder.TotalPrice + Convert.ToDecimal(userorder.Delivery_fee)) - userorder.CommissionFee);
                    }

                    if (money != 0)
                    {
                        var coopEwallet = db.UserEWallet.Where(x => x.COOP_ID == userorder.CoopId.ToString() && x.Status == "Active").FirstOrDefault();
                        coopEwallet.Balance += money;
                        db.Entry(coopEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        var ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = coopEwallet.ID;
                        ewalletHistory.Amount = money;
                        ewalletHistory.Action = "Order Payment";
                        ewalletHistory.Description = "Payment received from Order No. " + userorder.Id + ".";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        var admin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var adminEwallet = db.UserEWallet.Where(x => x.UserID == admin.Id).FirstOrDefault();
                        adminEwallet.Balance -= money;
                        db.Entry(adminEwallet).State = EntityState.Modified;
                        db.SaveChanges();

                        var coopDetails = db.CoopDetails.Where(x => x.Id.ToString() == coopEwallet.COOP_ID).FirstOrDefault();
                        ewalletHistory = new EWalletHistory();
                        ewalletHistory.EWallet_ID = adminEwallet.ID;
                        ewalletHistory.Amount = money;
                        ewalletHistory.Action = "Payment Send";
                        ewalletHistory.Description = "Payment from Order No. " + userorder.Id + " is sent to " + coopDetails.CoopName + "(" + coopDetails.Id + ").";
                        ewalletHistory.Created_At = DateTime.Now;
                        db.EWalletHistories.Add(ewalletHistory);
                        db.SaveChanges();

                        userorder.OStatus = "Transferred";
                        db.Entry(userorder).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                }
            }

            return RedirectToAction("OrderDetails", new { id = orderid });
        }

        public ActionResult ReturnRefundItem(string id)
        {
            var db = new ApplicationDbContext();
            Session["myorderdetail"] = id;
            var model = new ReturnView();
            List<CoopProdOrder> coopProds = new List<CoopProdOrder>();
            var prodorder = (from prod in db.ProdOrders
                             join prod2 in db.ProductDetails
                             on prod.ProdId equals prod2.Id.ToString()
                             where prod.UOrderId == id
                             select new CoopProdOrder
                             {
                                 ProdOrderId = prod.Id.ToString(),
                                 UOrderId = prod.UOrderId,
                                 ProdId = prod.ProdId,
                                 ProdName = prod.ProdName,
                                 Qty = prod.Qty.ToString(),
                                 Price = prod.Price,
                                 Subtotal = prod.SubTotal,
                                 Image = prod2.Product_image
                             }).ToList();
            foreach (var prod in prodorder)
            {
                var checkrev = db.Reviews.Where(x => x.ProdOrderId.ToString() == prod.ProdOrderId).FirstOrDefault();
                if (checkrev != null)
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = true
                    });
                }
                else
                {
                    coopProds.Add(new CoopProdOrder
                    {
                        ProdOrderId = prod.ProdOrderId,
                        UOrderId = prod.UOrderId,
                        ProdId = prod.ProdId,
                        ProdName = prod.ProdName,
                        Qty = prod.Qty.ToString(),
                        Price = prod.Price,
                        Subtotal = prod.Subtotal,
                        Image = prod.Image,
                        IsRated = false
                    });
                }

                model.UOrderId = Convert.ToInt32(prod.UOrderId);
            }

            model.CustomerOrder = coopProds;
            return View(model);
        }

        [HttpPost]
        public ActionResult ReturnRefundItem(ReturnView model)
        {
            if (Request.Form["isCheck"] == null)
            {
                var db2 = new ApplicationDbContext();
                string id2 = Session["myorderdetail"].ToString();
                var model2 = new ReturnView();
                List<CoopProdOrder> coopProds = new List<CoopProdOrder>();
                var prodorder = (from prod in db2.ProdOrders
                                 join prod2 in db2.ProductDetails
                                 on prod.ProdId equals prod2.Id.ToString()
                                 where prod.UOrderId == id2
                                 select new CoopProdOrder
                                 {
                                     ProdOrderId = prod.Id.ToString(),
                                     UOrderId = prod.UOrderId,
                                     ProdId = prod.ProdId,
                                     ProdName = prod.ProdName,
                                     Qty = prod.Qty.ToString(),
                                     Price = prod.Price,
                                     Subtotal = prod.SubTotal,
                                     Image = prod2.Product_image
                                 }).ToList();
                foreach (var prod in prodorder)
                {
                    var checkrev = db2.Reviews.Where(x => x.ProdOrderId.ToString() == prod.ProdOrderId).FirstOrDefault();
                    if (checkrev != null)
                    {
                        coopProds.Add(new CoopProdOrder
                        {
                            ProdOrderId = prod.ProdOrderId,
                            UOrderId = prod.UOrderId,
                            ProdId = prod.ProdId,
                            ProdName = prod.ProdName,
                            Qty = prod.Qty.ToString(),
                            Price = prod.Price,
                            Subtotal = prod.Subtotal,
                            Image = prod.Image,
                            IsRated = true
                        });
                    }
                    else
                    {
                        coopProds.Add(new CoopProdOrder
                        {
                            ProdOrderId = prod.ProdOrderId,
                            UOrderId = prod.UOrderId,
                            ProdId = prod.ProdId,
                            ProdName = prod.ProdName,
                            Qty = prod.Qty.ToString(),
                            Price = prod.Price,
                            Subtotal = prod.Subtotal,
                            Image = prod.Image,
                            IsRated = false
                        });
                    }

                    model.UOrderId = Convert.ToInt32(prod.UOrderId);
                }

                ViewBag.ErrorMessage = "Please select an item(s).";
                model.CustomerOrder = coopProds;
                return View(model);
            }

            string selected = Request.Form["isCheck"].ToString();
            string[] selectedList = selected.Split(',');
            string isRefundOnly = Request["return"];
            string orderid = Request["orderid"];
            string refundAmount = Request["refundAmount"];
            string description = Request["description"];
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var userdetails = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            var dateNow = DateTime.Now.AddDays(7);
            //var deliveryStatus = db.DeliverStatus.Where(ds => Convert.ToDateTime(ds.DateDelivered) <= dateNow).FirstOrDefault();

            var userOrder = db.UserOrders.Where(uo => uo.Id.ToString() == orderid).FirstOrDefault();
            var returnRefund = new ReturnRefund();
            returnRefund.UserOrderId = userOrder.Id;
            returnRefund.UserId = userOrder.UserId;
            returnRefund.CoopId = userOrder.CoopId;
            returnRefund.Type = isRefundOnly;
            returnRefund.Status = "Request";
            returnRefund.Reason = description;
            returnRefund.RefundAmount = Convert.ToDecimal(refundAmount);
            returnRefund.IsAccepted = false;
            returnRefund.Created_At = DateTime.Now;
            db.ReturnRefunds.Add(returnRefund);
            db.SaveChanges();

            foreach (var item in selectedList)
            {
                var prodOrder = db.ProdOrders.Where(po => po.Id.ToString() == item).FirstOrDefault();
                var returnId = db.ReturnRefunds.Where(r => r.UserOrderId == userOrder.Id).OrderByDescending(x => x.Id).FirstOrDefault();

                var returnRefundItem = new ReturnRefundItem();
                returnRefundItem.ReturnId = returnId.Id;
                returnRefundItem.ProdOrderId = prodOrder.Id;
                db.ReturnRefundItems.Add(returnRefundItem);
                db.SaveChanges();
            }

            var coopAdmin = db.CoopAdminDetails.Where(x => x.Coop_code == userOrder.CoopId).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == userOrder.CoopId).FirstOrDefault();
            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = user,
                NotifFrom = user,
                NotifHeader = "Return/refund request to order no. " + userOrder.Id + ".",
                NotifMessage = "Your return/refund request to order no. " + userOrder.Id + "has been send to " + coop.CoopName + ". \nKindly wait patienly as it may take time to process it. \nThank you and have a great day.",
                NavigateURL = "ReturnRefundDetails/" + returnRefund.Id,
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
                ToUser = coopAdmin.UserId,
                NotifFrom = user,
                NotifHeader = "Return/refund request to order no. " + userOrder.Id + ".",
                NotifMessage = userdetails.Firstname + " " + userdetails.Lastname + " request a return/refund to order no. " + userOrder.Id + ". Kindly check it and response as soon as possible.",
                NavigateURL = "ReturnRefundDetails/" + returnRefund.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            objNotifHub.SendNotification(notif.ToUser);

            return RedirectToAction("ReturnRefundList");
        }

        public ActionResult ReturnRefundList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.UserId == user).ToList();
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
            var returnRefunds = db.ReturnRefunds.Where(rr => rr.Id.ToString() == id).FirstOrDefault();
            
            var returnRefundItems = db.ReturnRefundItems.Where(rr => rr.ReturnId.ToString() == id).ToList();
            var model = new ReturnRefundDetails();
            List<ProdOrder2> returnProd = new List<ProdOrder2>();
            List<ProdOrder2> customerOrder = new List<ProdOrder2>();
            ReturnRefund details = new ReturnRefund();

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

        [Authorize(Roles = "Member, Non-member")]
        public ActionResult WishlistDisplay()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<ViewListProd> listProds = new List<ViewListProd>();
            var getwishlists = db.Wishlist.Where(x => x.UserId == user).ToList();

            if (getwishlists != null)
            {
                foreach (var item in getwishlists)
                {
                    var product = db.ProductDetails.Where(x => x.Id.ToString() == item.ProductId).FirstOrDefault();
                    var getprice = db.Prices.Where(x => x.ProdId.ToString() == item.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                    listProds.Add(new ViewListProd
                    {
                        Id = item.Id,
                        Category = product.Categoryname,
                        CoopID = product.CoopId.ToString(),
                        DiscountPrice = product.DiscountedPrice,
                        Product_image = product.Product_image,
                        Product_name = product.Product_Name,
                        Product_qty = product.Product_qty,
                        Product_desc = product.Product_desc,
                        Product_price = getprice.Price,
                        ProdId = product.Id
                    });
                }
            }

            return View(listProds);
        }
        [HttpPost]
        public ActionResult WishlistDisplay(string submit)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            List<ViewListProd> listProds = new List<ViewListProd>();
            var getwishlists = db.Wishlist.Where(x => x.UserId == user).ToList();
            string prodname = Request["prodname"];
            if (submit == "Search")
            {
                if (getwishlists != null)
                {
                    foreach (var item in getwishlists)
                    {
                        var product = db.ProductDetails.Where(x => x.Id.ToString() == item.ProductId && x.Product_Name.ToLower().Contains(prodname)
                        || x.Categoryname.ToLower().Contains(prodname)).FirstOrDefault();
                        if (product != null)
                        {
                            var getprice = db.Prices.Where(x => x.ProdId.ToString() == item.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                            listProds.Add(new ViewListProd
                            {
                                Id = item.Id,
                                Category = product.Categoryname,
                                CoopID = product.CoopId.ToString(),
                                DiscountPrice = product.DiscountedPrice,
                                Product_image = product.Product_image,
                                Product_name = product.Product_Name,
                                Product_qty = product.Product_qty,
                                Product_desc = product.Product_desc,
                                Product_price = getprice.Price,
                                ProdId = product.Id
                            });
                        }
                    }
                }
            }
            else
            {
                if (getwishlists != null)
                {
                    foreach (var item in getwishlists)
                    {
                        var product = db.ProductDetails.Where(x => x.Id.ToString() == item.ProductId).FirstOrDefault();
                        if (product != null)
                        {
                            var getprice = db.Prices.Where(x => x.ProdId.ToString() == item.ProductId && (x.VarId.ToString() == null || x.VarId == 0)).OrderByDescending(p => p.Id).FirstOrDefault();
                            listProds.Add(new ViewListProd
                            {
                                Id = item.Id,
                                Category = product.Categoryname,
                                CoopID = product.CoopId.ToString(),
                                DiscountPrice = product.DiscountedPrice,
                                Product_image = product.Product_image,
                                Product_name = product.Product_Name,
                                Product_qty = product.Product_qty,
                                Product_desc = product.Product_desc,
                                Product_price = getprice.Price,
                                ProdId = product.Id
                            });
                        }
                    }
                }
            }

            return View(listProds);
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