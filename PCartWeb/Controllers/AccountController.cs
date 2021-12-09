using System;
using System.Collections.Generic;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PCartWeb.Hubs;
using PCartWeb.Models;

namespace PCartWeb.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!Request.IsAuthenticated)
            {
                return View();
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (User.IsInRole("Non-member"))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await UserManager.FindByNameAsync(model.Email);
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);

            var db2 = new ApplicationDbContext();
            var getinfo = db2.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            if (user != null)
            {
                var check = VerifyCoop(model.Email);
                if (check == "Pending" || User.IsInRole("Coop Admin"))
                {
                    ViewBag.errorMessage = "Please wait for your account approval. You will be notified thru your email.";
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                    return View("Login");
                }
                else if (!await UserManager.IsEmailConfirmedAsync(user.Id) && !User.IsInRole("Coop Admin"))
                {
                    ViewBag.errorMessage = "You must have a confirmed email to log on.";
                    AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                    return View("Login");
                }
                else if (User.IsInRole("Member"))
                {
                    if (getinfo != null)
                    {
                        var getuserde2 = db2.UserDetails.Where(x => x.AccountId == getinfo.Id).FirstOrDefault();
                        if (getuserde2.MemberLock == "Locked")
                        {
                            ViewBag.errorMessage = "This account has been temporarily locked.";
                            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                            return View("Login");
                        }
                    }
                }
                var getuserde = db2.CoopAdminDetails.Where(x => x.UserId == getinfo.Id).FirstOrDefault();
                if (getuserde != null)
                {
                    var getcoop = db2.CoopDetails.Where(x => x.Id == getuserde.Coop_code).FirstOrDefault();
                    if (getcoop.IsLocked == "Locked")
                    {
                        ViewBag.errorMessage = "This COOP has been temporarily locked.";
                        AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                        return View("Login");
                    }
                    else if (getuserde.IsResign == "True")
                    {
                        var checkother = db2.CoopAdminDetails.Where(x => x.Coop_code == getcoop.Id && (x.IsResign == "False" || x.IsResign == null)).ToList();
                        if (checkother.Count != 0)
                        {
                            ViewBag.errorMessage = "Please login the new Coop Admin User.";
                            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                            return View("Login");
                        }
                        AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                        return RedirectToAction("RegisterUserAdmin", new { email = model.Email });
                    }

                }
            }
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            switch (result)
            {
                case SignInStatus.Success:
                    {
                        var logs = new UserLogs();
                        var userrole = getinfo.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + "Logged in";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();
                        return RedirectToLocal(returnUrl);
                    }
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult RegisterUserAdmin(string email)
        {
            var model = new RegisterUserAdminViewModel
            {
                CStatusList = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                    new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                    new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                    new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                    new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
                }, "Value", "Text", 1),
                GenderList = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                    new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                    new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                    new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
                }, "Value", "Text", 1)
            };

            TempData["email"] = email;
            TempData.Keep();
            ModelState.Clear();
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterUserAdminAsync(RegisterUserAdminViewModel model, HttpPostedFileBase file)
        {
            if (TempData["email"] == null)
            {
                return RedirectToAction("Login");
            }

            var db = new ApplicationDbContext();
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

            int gen = 0, stat = 0;
            if (model.Gender == null)
            {
                gen = 1;
            }
            if (model.CStatus == null)
            {
                stat = 1;
            }
            if (gen == 1 && stat == 1)
            {
                ViewBag.GenderError = "Please select your gender.";
                ViewBag.StatusError = "Please select your marital status.";
                model.Address = string.Empty;
                model.Latitude = string.Empty;
                model.Longitude = string.Empty;
                return View(model);
            }
            else if (gen == 1)
            {
                ViewBag.GenderError = "Please select your gender.";
                model.Address = string.Empty;
                model.Latitude = string.Empty;
                model.Longitude = string.Empty;
                return View(model);
            }
            else if (stat == 1)
            {
                ViewBag.StatusError = "Please select your marital status.";
                model.Address = string.Empty;
                model.Latitude = string.Empty;
                model.Longitude = string.Empty;
                return View(model);
            }
            if (ModelState.IsValid)
            {
                if (model.ImageFile != null )
                {
                    if(ValidateFile(model.ImageFile) != true)
                    {
                        ViewBag.message = "Please choose one Image File";
                        return View(model);
                    }
                }
                string email = TempData["email"].ToString();
                var getprev = db.CoopAdminDetails.Where(x => x.Email == email).FirstOrDefault();
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                    if (model.ImageFile == null)
                    {
                        var id = user.Id;
                        var db2 = new ApplicationDbContext();
                        var coop = new CoopDetailsModel();
                        var location = new Location
                        {
                            Address = model.Address,
                            Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )"),
                            UserId = user.Id,
                            Created_at = DateTime.Now
                        };
                        db.Locations.Add(location);
                        var details = new CoopAdminDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Status = model.CStatus, Contact = model.Contact, Gender = model.Gender, UserId = user.Id, Coop_code = getprev.Coop_code };
                        details.Email = model.Email;
                        details.Image = "defaultprofile.jpg";
                        details.PreviousEmail = email;
                        db.CoopAdminDetails.Add(details);
                        db.SaveChanges();
                        Session["CoopId"] = user.Id;
                        await this.UserManager.AddToRoleAsync(user.Id, "Coop Admin"); // creating role
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " COOP account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();

                        var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.Email == model.Email).FirstOrDefault();

                        var superAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var notif = new NotificationModel
                        {
                            ToRole = "Admin",
                            ToUser = superAdmin.Id,
                            NotifFrom = coopAdminDetails.Coop_code.ToString(),
                            NotifHeader = "COOP Application",
                            NotifMessage = "A new COOP wants to be part of PCart! Check it's details now!",
                            NavigateURL = "DisplayCoopDetailReq/" + coopAdminDetails.Coop_code,
                            IsRead = false,
                            DateReceived = DateTime.Now
                        };
                        db2.Notifications.Add(notif);
                        db2.SaveChanges();

                        return RedirectToAction("Login");
                    }

                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName); //getting extension of the file
                    if (allowedExtensions.Contains(extension)) //check what type of extension  
                    {
                        model.Image = name + extension;
                        var myfile = name + extension;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        var id = user.Id;
                        var db2 = new ApplicationDbContext();
                        var coop = new CoopDetailsModel();
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db.Locations.Add(location);
                        var details = new CoopAdminDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Status = model.CStatus, Contact = model.Contact, Gender = model.Gender, UserId = user.Id, Coop_code = getprev.Coop_code };
                        details.Email = model.Email;
                        details.PreviousEmail = email;
                        db.CoopAdminDetails.Add(details);
                        db.SaveChanges();
                        model.ImageFile.SaveAs(path); // saving image file to folder
                        Session["CoopId"] = user.Id;
                        await this.UserManager.AddToRoleAsync(user.Id, "Coop Admin"); // creating role
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " COOP account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();

                        var coopAdminDetails = db.CoopAdminDetails.Where(ca => ca.Email == model.Email).FirstOrDefault();

                        var superAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var notif = new NotificationModel
                        {
                            ToRole = "Super Admin",
                            ToUser = superAdmin.Id,
                            NotifFrom = coopAdminDetails.Coop_code.ToString(),
                            NotifHeader = "COOP Application",
                            NotifMessage = "A new COOP wants to be part of PCart! Check it's details now!",
                            NavigateURL = "DisplayCoopDetailReq/" + coopAdminDetails.Coop_code,
                            IsRead = false,
                            DateReceived = DateTime.Now
                        };
                        db2.Notifications.Add(notif);
                        db2.SaveChanges();

                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ViewBag.message = "Please choose only Image file";
                    }

                    /*var user2 = await UserManager.FindByIdAsync(User.Identity.GetUserId());*/ //Get the Current loggedin userid for foreign key


                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            model.Latitude = string.Empty;
            model.Longitude = string.Empty;
            return View(model);
        }

        public string VerifyCoop(string email)
        {
            var db = new ApplicationDbContext();
            var user = db.Users.Where(p => p.Email == email).FirstOrDefault();
            if (user != null)
            {
                var coop = db.CoopAdminDetails.Where(p => p.Email == email && (p.Approval == "Pending" || p.Approval == null)).FirstOrDefault();
                if (coop != null)
                {
                    return "Pending";
                }
            }
            return "Clear";
        }

        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            var model = new RegisterViewModel();
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);

            return View(model);
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, HttpPostedFileBase file)
        {
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);

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
                var db = new ApplicationDbContext();
                var db2 = new ApplicationDbContext();
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (model.ImageFile == null)
                    {
                        model.Image = "defaultprofile.jpg";
                        var details = new CustomerDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Contact = model.Contact, Gender = model.Gender, Role = "Non-Member", AccountId = user.Id };
                        details.IsActive = "Active";
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
                        await this.UserManager.AddToRoleAsync(user.Id, "Non-member");
                        string code2 = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl2 = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code2 }, protocol: Request.Url.Scheme);
                        await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl2 + "\">here</a>");
                        ModelState.Clear();
                        ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                         + "before you can log in.";
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();

                        var ewallet = new EWallet();
                        ewallet.UserID = user.Id;
                        ewallet.Balance = 0;
                        ewallet.Created_At = DateTime.Now;
                        ewallet.Status = "Active";
                        db.UserEWallet.Add(ewallet);
                        db.SaveChanges();
                        return View(model);
                    }
                    var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };

                    if (model.ImageFile != null)
                    {
                        if(ValidateFile(model.ImageFile) == true)
                        {
                            string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                            string extension = Path.GetExtension(model.ImageFile.FileName);
                            if (allowedExtensions.Contains(extension)) //check what type of extension  
                            {
                                model.Image = name + extension;
                                var myfile = name + extension;
                                var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                                var details = new CustomerDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Contact = model.Contact, Gender = model.Gender, Role = "Non-Member", AccountId = user.Id };
                                details.IsActive = "Active";
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
                                await this.UserManager.AddToRoleAsync(user.Id, "Non-member");
                                ModelState.Clear();
                                string code2 = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                                var callbackUrl2 = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code2 }, protocol: Request.Url.Scheme);
                                await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl2 + "\">here</a>");

                                await this.UserManager.AddToRoleAsync(user.Id, "Non-member");

                                ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                                     + "before you can log in.";
                                var logs = new UserLogs();
                                var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                                logs.Logs = model.Email + " account created";
                                logs.Role = getuserrole.Name;
                                logs.Date = DateTime.Now.ToString();
                                db2.Logs.Add(logs);
                                db2.SaveChanges();

                                var ewallet = new EWallet();
                                ewallet.UserID = user.Id;
                                ewallet.Balance = 0;
                                ewallet.Created_At = DateTime.Now;
                                ewallet.Status = "Active";
                                db.UserEWallet.Add(ewallet);
                                db.SaveChanges();
                                return View(model);
                            }
                            else
                            {
                                ViewBag.message = "Please choose only Image file";
                            }
                        }
                        else
                        {
                            ViewBag.message = "Please choose only Image file";
                        }

                    }

                    //await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    await this.UserManager.AddToRoleAsync(user.Id, "Non-member");

                    ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                         + "before you can log in.";

                    return View(model);
                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
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

        [AllowAnonymous]
        public ActionResult RegisterCoopadmin()
        {
            var model = new RegisterCoopAdminViewmodel
            {
                CStatusList = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem {Selected = false, Text = "Single", Value = "Single"},
                    new SelectListItem {Selected = false, Text = "Married", Value = "Married"},
                    new SelectListItem {Selected = false, Text = "Divorced", Value = "Divorced"},
                    new SelectListItem {Selected = false, Text = "Separated", Value = "Separated"},
                    new SelectListItem {Selected = false, Text = "Widowed", Value = "Widowed"}
                }, "Value", "Text", 1),
                GenderList = new SelectList(new List<SelectListItem>
                {
                    new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                    new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                    new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                    new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
                }, "Value", "Text", 1)
            };

            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterCoopadmin(RegisterCoopAdminViewmodel model, HttpPostedFileBase file)
        {
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

            if (ModelState.IsValid)
            {
                bool checkcoop = CheckCoop(model.CoopName, model.CoopAddress);
                if (checkcoop == false)
                {
                    model.Latitude = string.Empty;
                    model.Latitude1 = string.Empty;
                    model.Longitude = string.Empty;
                    model.Longitude1 = string.Empty;
                    ViewBag.message = "This COOP is already registered.";
                    return View(model);
                }
                if (model.ImageFile != null)
                {
                    if(ValidateFile(model.ImageFile) ==false)
                    {
                        ViewBag.message = "Please choose one Image File";
                        return View(model);
                    }
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                    if (model.ImageFile == null)
                    {
                        var id = user.Id;
                        var db = new ApplicationDbContext();
                        var db2 = new ApplicationDbContext();
                        var coop = new CoopDetailsModel();
                        var location = new Location
                        {
                            Address = model.Address,
                            Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )"),
                            UserId = user.Id,
                            Created_at = DateTime.Now
                        };
                        db.Locations.Add(location);
                        coop.CoopName = model.CoopName;
                        coop.Address = model.CoopAddress;
                        coop.Contact = model.CoopContact;
                        coop.Coop_Created = DateTime.Now;
                        coop.Coop_Updated = DateTime.Now;
                        coop.Approval = "Pending";
                        coop.IsLocked = "Unlock";
                        db2.CoopDetails.Add(coop);
                        db2.SaveChanges();
                        var coopid = db2.CoopDetails.Where(p => p.CoopName == model.CoopName && p.Address == model.CoopAddress).FirstOrDefault();
                        var cooploc = new CoopLocation();
                        string longandlat = model.Longitude1 + " " + model.Latitude1;
                        cooploc.Address = model.CoopAddress;
                        cooploc.Geolocation = DbGeography.FromText("POINT( " + model.Longitude1 + " " + model.Latitude1 + " )");
                        cooploc.Created_at = DateTime.Now;
                        cooploc.CoopId = coopid.Id.ToString();
                        db.CoopLocations.Add(cooploc);
                        var details = new CoopAdminDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Status = model.CStatus, Contact = model.Contact, Gender = model.Gender, UserId = user.Id, Coop_code = coopid.Id };
                        details.Email = model.Email;
                        details.Image = "defaultprofile.jpg";
                        details.Approval = "Pending";
                        db.CoopAdminDetails.Add(details);
                        db.SaveChanges();
                        Session["CoopId"] = user.Id;
                        await this.UserManager.AddToRoleAsync(user.Id, "Coop Admin"); // creating role
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " COOP account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();

                        var userAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var notif = new NotificationModel
                        {
                            ToRole = "Admin",
                            ToUser = userAdmin.Id,
                            NotifFrom = "",
                            NotifHeader = coop.CoopName + " sign up to be a seller. Kindly check it.",
                            NotifMessage = "",
                            NavigateURL = "DisplayCoopDetailReq/" + coop.Id,
                            IsRead = false,
                            DateReceived = DateTime.Now
                        };

                        db.Notifications.Add(notif);
                        db.SaveChanges();
                        NotificationHub objNotifHub = new NotificationHub();
                        objNotifHub.SendNotification(notif.ToUser);

                        return RedirectToAction("AddImageDocuments", "Account");
                    }

                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName); //getting extension of the file

                    if (ValidateFile(model.ImageFile)==true) //check what type of extension  
                    {
                        model.Image = name + extension;
                        var myfile = name + extension;
                        var path = Path.Combine(Server.MapPath("../Images/"), myfile);
                        var id = user.Id;
                        var db = new ApplicationDbContext();
                        var db2 = new ApplicationDbContext();
                        var coop = new CoopDetailsModel();
                        var location = new Location();
                        location.Address = model.Address;
                        location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                        location.UserId = user.Id;
                        location.Created_at = DateTime.Now;
                        db.Locations.Add(location);
                        coop.CoopName = model.CoopName;
                        coop.Address = model.CoopAddress;
                        coop.Contact = model.CoopContact;
                        coop.Approval = "Pending";
                        coop.IsLocked = "Unlock";
                        coop.Coop_Created = DateTime.Now;
                        coop.Coop_Updated = DateTime.Now;
                        db2.CoopDetails.Add(coop);
                        db2.SaveChanges();
                        var coopid = db2.CoopDetails.Where(p => p.CoopName == model.CoopName && p.Address == model.CoopAddress).FirstOrDefault();
                        var cooploc = new CoopLocation();
                        cooploc.Address = model.CoopAddress;
                        cooploc.Geolocation = DbGeography.FromText("POINT( " + model.Longitude1 + " " + model.Latitude1 + " )");
                        cooploc.Created_at = DateTime.Now;
                        cooploc.CoopId = coopid.Id.ToString();
                        db.CoopLocations.Add(cooploc);
                        var details = new CoopAdminDetailsModel { Firstname = model.Firstname, Lastname = model.Lastname, Image = model.Image, Created_at = DateTime.Now, Updated_at = DateTime.Now, Address = model.Address, Bdate = model.Bdate.ToString(), Status = model.CStatus, Contact = model.Contact, Gender = model.Gender, UserId = user.Id, Coop_code = coopid.Id };
                        details.Email = model.Email;
                        details.Approval = "Pending";
                        db.CoopAdminDetails.Add(details);
                        db.SaveChanges();

                        model.ImageFile.SaveAs(path); // saving image file to folder
                        Session["CoopId"] = user.Id;
                        await this.UserManager.AddToRoleAsync(user.Id, "Coop Admin"); // creating role
                        var logs = new UserLogs();
                        var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                        var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                        logs.Logs = model.Email + " COOP account created";
                        logs.Role = getuserrole.Name;
                        logs.Date = DateTime.Now.ToString();
                        db2.Logs.Add(logs);
                        db2.SaveChanges();

                        var userAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
                        var notif = new NotificationModel
                        {
                            ToRole = "Admin",
                            ToUser = userAdmin.Id,
                            NotifFrom = "",
                            NotifHeader = coop.CoopName + " sign up to be a seller. Kindly check it.",
                            NotifMessage = "",
                            NavigateURL = "DisplayCoopDetailReq/" + coop.Id,
                            IsRead = false,
                            DateReceived = DateTime.Now
                        };

                        db.Notifications.Add(notif);
                        db.SaveChanges();
                        NotificationHub objNotifHub = new NotificationHub();
                        objNotifHub.SendNotification(notif.ToUser);

                        return RedirectToAction("AddImageDocuments", "Account");
                    }
                    else
                    {
                        ViewBag.message = "Please choose only Image file";
                    }

                    /*var user2 = await UserManager.FindByIdAsync(User.Identity.GetUserId());*/ //Get the Current loggedin userid for foreign key

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                }

                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            model.Latitude = string.Empty;
            model.Latitude1 = string.Empty;
            model.Longitude = string.Empty;
            model.Longitude1 = string.Empty;
            return View(model);
        }

        public bool CheckCoop(string name, string address)
        {
            var db = new ApplicationDbContext();
            var coop = db.CoopDetails.Where(p => p.CoopName == name).FirstOrDefault();
            if (coop != null)
            {
                if (coop.Address == address)
                {
                    return false;
                }
            }
            return true;
        }
        [AllowAnonymous]
        public ActionResult AddImageDocuments()
        {
            if (Session["CoopId"] == null)
            {
                return RedirectToAction("RegisterCoopAdmin");
            }
            string id = Session["CoopId"].ToString();
            Session["CoopId"] = id;
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult AddImageDocuments(AddDocumentImages model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            var allowedExtensions = new[] {
                    ".Jpg", ".png", ".jpg", "jpeg"
                };
            if (String.IsNullOrEmpty(Session["CoopId"].ToString()))
            {
                return RedirectToAction("Login");
            }
            string user2 = Session["CoopId"].ToString();
            var getid = db.CoopAdminDetails.Where(x => x.UserId == user2).FirstOrDefault();
            var getu = db.CoopDetails.Where(x => x.Id == getid.Coop_code).FirstOrDefault();
            if (model.ImageFile == null)
            {
                return RedirectToAction("ViewCoopDocuments");
            }
            if (model.ImageFile != null)
            {
                if(ValidateFile(model.ImageFile) == true)
                {
                    string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                    string extension = Path.GetExtension(model.ImageFile.FileName);
                    var imagefile = new CoopDocumentImages();
                    if (allowedExtensions.Contains(extension))
                    {
                        var myfile = name + extension;
                        string path = Path.Combine(Server.MapPath("~/Images/"), myfile);
                        imagefile.Document_image = name + extension;
                        imagefile.Userid = getid.UserId;
                        db.CoopImages.Add(imagefile);
                        db.SaveChanges();
                        model.ImageFile.SaveAs(path);
                        return RedirectToAction("ViewCoopDocuments");
                    }
                }
                
            }

            return View();
        }
        [AllowAnonymous]
        public ActionResult ViewCoopDocuments()
        {
            if (String.IsNullOrEmpty(Session["CoopId"].ToString()))
            {
                return RedirectToAction("Login");
            }
            string user = Session["CoopId"].ToString();
            var db = new ApplicationDbContext();
            var getid = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getuser = db.CoopDetails.Where(x => x.Id == getid.Coop_code).FirstOrDefault();

            var getpics = db.CoopImages.Where(x => x.Userid == getid.UserId).ToList();
            //ViewBag.message = Session["Message"].ToString();
            if (Session["Message"] != null)
            {
                ViewBag.message = Session["Message"].ToString();
            }
            return View(getpics);
        }
        [AllowAnonymous]
        public ActionResult CoopVerification()
        {
            string user = Session["CoopId"].ToString();
            var db = new ApplicationDbContext();
            var getid = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getu = db.CoopDetails.Where(x => x.Id == getid.Coop_code).FirstOrDefault();
            var checkpics = db.CoopImages.Where(x => x.Userid == getid.UserId).ToList();

            if (checkpics == null)
            {
                Session["Message"] = "Please upload one or more images of your documents to be verified.";
                return RedirectToAction("ViewCoopDocuments");
            }

            return RedirectToAction("Login");
        }

        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Logout()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

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

        private ActionResult RedirectToLocal(string returnUrl)
        {
            var db = new ApplicationDbContext();
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}