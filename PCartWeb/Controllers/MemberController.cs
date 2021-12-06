using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using PCartWeb.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace PCartWeb.Controllers
{
    [Authorize(Roles = "Member")]
    public class MemberController : Controller
    {
        // GET: Member
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;


        public MemberController()
        {
        }

        public MemberController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
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


        public ActionResult ViewProfile()
        {
            var db = new ApplicationDbContext();
            var user2 = User.Identity.GetUserId();
            var user = (from u in db.UserDetails
                        where u.AccountId == user2
                        select new UserViewModel
                        {
                            Firstname = u.Firstname,
                            Lastname = u.Lastname,
                            Image = u.Image,
                            Address = u.Address,
                            Bdate = u.Bdate,
                            Gender = u.Gender,
                            Contact = u.Contact,
                            Role = u.Role,
                            Created_at = u.Created_at.ToString(),
                            Updated_at = u.Updated_at.ToString(),
                            Id = u.Id
                        }).FirstOrDefault();

            return View(user);
        }

        public ActionResult EditProfile(Int32 id)
        {
            var db = new ApplicationDbContext();
            var reg = new RegisterViewModel();
            var user = db.UserDetails.Where(x => x.Id == id).FirstOrDefault();
            reg.Firstname = user.Firstname;
            reg.Lastname = user.Lastname;
            reg.Gender = user.Gender;
            reg.Contact = user.Contact;
            reg.Address = user.Address;
            if (user != null)
            {
                TempData["id"] = id;
                TempData.Keep();
                return View(reg);
            }

            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(RegisterViewModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            int id = (int)TempData["id"];
            var user = new CustomerDetailsModel();
            user = db.UserDetails.Where(x => x.Id == id).FirstOrDefault();
            if (user != null)
            {
                if (model.Firstname == "")
                    user.Firstname = user.Firstname;
                else
                    user.Firstname = model.Firstname;
                if (model.Lastname == "")
                    user.Lastname = user.Lastname;
                else
                    user.Lastname = model.Lastname;
                if (model.ImageFile != null)
                {
                    var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    if (allowedExtensions.Contains(extension)) //check what type of extension  
                    {
                        user.Image = "~/Images/" + name + extension;
                        var myfile = name + extension;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        model.ImageFile.SaveAs(path);
                    }
                    else
                    {
                        ViewBag.message = "Please choose only Image file";
                    }
                }
                else
                    user.Image = user.Image;
                if (model.Contact == "")
                    user.Contact = user.Contact;
                else
                    user.Contact = model.Contact;
                if (model.Address == "")
                    user.Address = user.Address;
                else
                    user.Address = user.Address;
                string date = model.Bdate.ToString();
                if (date == "1/1/0001 12:00:00 AM")
                {
                    user.Bdate = user.Bdate;
                }
                else
                    user.Bdate = model.Bdate.ToString();
                if (model.Gender == null)
                    user.Gender = user.Gender;
                else
                    user.Gender = model.Gender;
                user.Updated_at = DateTime.Now;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("ViewProfile");
            }

            return View(model);
        }

        public ActionResult ProductListRequests()
        {
            var user2 = User.Identity.GetUserId();
            var db = new ApplicationDbContext();
            var product = (from prod in db.ProductDetails
                           join categ in db.CategoryDetails
                           on prod.Category_Id equals categ.Id
                           where prod.CustomerId == user2
                           where prod.Product_status == "Request"
                           select new ViewListProd
                           {
                               Id = prod.Id,
                               Product_name = prod.Product_Name,
                               Product_desc = prod.Product_desc,
                               Product_qty = prod.Product_qty,
                               Category = categ.Name,
                               Product_status = prod.Product_status,
                               Created_at = prod.Prod_Created_at.ToString(),
                               Updated_at = prod.Prod_Updated_at.ToString()
                           }).ToList();

            foreach (var item in product)
            {
                var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_price = getprice.Price;
                var getCost = db.Cost.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_cost = getCost.Cost;
                var getmanu = db.Manufacturer.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();
                item.Product_manufact = getmanu.Manufacturer;
            }

            return View(product);
        }

        [AllowAnonymous]
        public ActionResult AddProduct()
        {
            var db = new ApplicationDbContext();
            var catmodel = new AdminAddProductViewModel();
            catmodel.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name");
            return View(catmodel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProduct(AdminAddProductViewModel model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getuser = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            model.Categorylist = new SelectList(db.CategoryDetails, "Id", "Name", model.CategoryId);
            var categname = db.CategoryDetails.Where(x => x.Id == model.CategoryId).FirstOrDefault();
            if (ModelState.IsValid)
            {
                var user2 = User.Identity.GetUserId(); //Get the Current loggedin userid for foreign key
                string id = user2;
                var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", "jpeg"
                };


                var details = new ProductDetailsModel();

                if (model.ImageFile == null)
                {
                    details.Product_image = "products.png";
                }
                else if (model.ImageFile != null)
                {
                    if (ValidateFile(model.ImageFile) == true)
                    {
                        string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                        string extension = Path.GetExtension(model.ImageFile.FileName);
                        var path = Path.Combine(Server.MapPath("../Images/"), name + extension);
                        model.ImageFile.SaveAs(path);
                        details.Product_image = name + extension;
                    }
                }
                if (model.ExpiryDate == null)
                {
                    details.ExpiryDate = null;
                }
                else
                {
                    details.ExpiryDate = model.ExpiryDate;
                }
                details.Category_Id = model.CategoryId;
                details.Categoryname = categname.Name;
                //details.Product_image = model.Product_image;
                details.Product_Name = model.Product_name;
                details.Product_desc = model.Product_desc;
                details.Product_qty = model.Product_qty;
                details.Product_status = "Request";
                details.Product_sold = 0;
                details.CustomerId = user2;
                details.Prod_Created_at = DateTime.Now;
                details.Prod_Updated_at = DateTime.Now;
                details.Date_ApprovalStatus = DateTime.Now;
                details.CoopId = int.Parse(getuser.CoopId);
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
                //model.ImageFile.SaveAs(path);
                ModelState.Clear();
                ViewBag.message = model.Product_name + " is successfully created";
            }
            else
            {
                ViewBag.message = "Something went wrong, please check the fields properly.";
            }

            return View(model);
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
    }
}