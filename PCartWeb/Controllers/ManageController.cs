using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.IO;
using System.Linq;
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
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
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

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";
            var db = new ApplicationDbContext();
            var userId = User.Identity.GetUserId();
            var isactive = "";
            var getuser = db.UserDetails.Where(x => x.AccountId == userId).FirstOrDefault();
            var driverinfo = db.DriverDetails.Where(x => x.UserId == userId).FirstOrDefault();
            var coopde = db.CoopAdminDetails.Where(x => x.UserId == userId).FirstOrDefault();
            if (getuser != null)
            {
                isactive = getuser.IsActive;
            }
            else if (driverinfo != null)
            {
                isactive = driverinfo.IsActive;
            }
            else if (coopde != null)
            {
                if (coopde.IsResign == "False" || coopde.IsResign == null)
                {
                    isactive = "Active";
                }
            }
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                IsActive = isactive
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }

        public ActionResult ViewProfileAdmin()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getinfo = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getcoop = db.CoopDetails.Where(x => x.Id == getinfo.Coop_code).FirstOrDefault();
            CoopViewModel coopViewModel = new CoopViewModel
            {
                CoopName = getcoop.CoopName,
                Contact = getinfo.Contact,
                CoopContact = getcoop.Contact,
                CoopAddress = getcoop.Address,
                Address = getinfo.Address,
                Firstname = getinfo.Firstname,
                Lastname = getinfo.Lastname,
                Image = getinfo.Image,
                Created_at = getinfo.Created_at.ToString(),
                Updated_at = getinfo.Updated_at.ToString(),
                Gender = getinfo.Gender,
                Email = getinfo.Email,
                CStatus = getinfo.Status
            };
            return View(coopViewModel);
        }

        public ActionResult ViewProfile()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getinfo = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            return View(getinfo);
        }

        public ActionResult ViewProfileDriver()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var getinfo = db.DriverDetails.Where(x => x.UserId == user).FirstOrDefault();
            return View(getinfo);
        }

        public ActionResult ResignCoopAdmin()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();

            var coopAdminDetails = db.CoopAdminDetails.Where(x => x.UserId == user).FirstOrDefault();
            coopAdminDetails.IsResign = "True";
            db.Entry(coopAdminDetails).State = EntityState.Modified;
            db.SaveChanges();

            var ewallet = db.UserEWallet.Where(x => x.UserID == user && x.Status == "Active").FirstOrDefault();
            ewallet.Status = "On-Hold";
            db.Entry(ewallet).State = EntityState.Modified;
            db.SaveChanges();

            var superAdmin = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var coopDetails = db.CoopDetails.Where(x => x.Id == coopAdminDetails.Coop_code).FirstOrDefault();
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
            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToUser);

            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        public ActionResult EditMyProfile(Int32 id)
        {
            var db = new ApplicationDbContext();
            var model = new RegisterViewModel();
            var getinfo = db.UserDetails.Where(x => x.Id == id).FirstOrDefault();
            model.Address = getinfo.Address;
            model.Contact = getinfo.Contact;
            model.Firstname = getinfo.Firstname;
            model.Lastname = getinfo.Lastname;
            model.Image = getinfo.Image;
            model.Gender = getinfo.Gender;
            model.GenderList = new SelectList(new List<SelectListItem>
            {
                new SelectListItem {Selected = false, Text = "Male", Value = "Male"},
                new SelectListItem {Selected = false, Text = "Female", Value = "Female"},
                new SelectListItem {Selected = false, Text = "LGBTQ", Value = "LGBTQ"},
                new SelectListItem {Selected = false, Text = "Rather Not Say", Value = "Rather Not Say"}
            }, "Value", "Text", 1);
            TempData["Id"] = id;
            TempData.Keep();
            return View(model);
        }
        [HttpPost]
        public ActionResult EditMyProfile(RegisterViewModel model, HttpPostedFileBase file)
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

            var getuser = db.UserDetails.Where(x => x.Id == userid).FirstOrDefault();
            var location = db.Locations.Where(x => x.UserId == getuser.AccountId).FirstOrDefault();
            if (getuser != null)
            {
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
                location.Geolocation = DbGeography.FromText("POINT( " + model.Longitude + " " + model.Latitude + " )");
                location.Address = model.Address;
                getuser.Gender = model.Gender;
                getuser.Updated_at = DateTime.Now;
                db.Entry(location).State = EntityState.Modified;
                db.Entry(getuser).State = EntityState.Modified;
                db.SaveChanges();
                var db2 = new ApplicationDbContext();
                var logs = new UserLogs();
                var user = db.Users.Where(x => x.Id == getuser.AccountId).FirstOrDefault();
                var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = model.Email + " account updated";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }

        public ActionResult EditDriverProfile()
        {
            var viewmodel = new RegisterDriverViewModel();
            var db = new ApplicationDbContext();
            var id = User.Identity.GetUserId();
            var getuser = db.DriverDetails.Where(x => x.UserId == id).FirstOrDefault();
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
                viewmodel.Driver_License = getuser.Driver_License;
                viewmodel.Image = getuser.Image;
                TempData["id"] = id;
                TempData.Keep();
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
        public ActionResult EditDriverProfile(RegisterDriverViewModel model, HttpPostedFileBase file, HttpPostedFileBase file2)
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
                        getuser.Bdate = model.Bdate;
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
                    var user = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
                    var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
                    var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                    logs.Logs = model.Email + " account updated";
                    logs.Role = getuserrole.Name;
                    logs.Date = DateTime.Now.ToString();
                    db2.Logs.Add(logs);
                    db2.SaveChanges();
                    db.Entry(location).State = EntityState.Modified;
                    db.Entry(getuser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("ViewDriverList");
                }
                var allowedExtensions1 = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                string name1 = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                string extension1 = Path.GetExtension(model.ImageFile.FileName);
                if (allowedExtensions1.Contains(extension1))
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
                    return RedirectToAction("ViewProfileDriver");
                }
            }
            return View(model);
        }
        //
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        public ActionResult DeactivateAccnt()
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var detail = db.UserDetails.Where(x => x.AccountId == user).FirstOrDefault();
            if (detail != null)
            {
                detail.IsActive = "Deactive";
                db.Entry(detail).State = EntityState.Modified;
                db.SaveChanges();
                var logs = new UserLogs();
                var user2 = db2.Users.Where(x => x.Id == user).FirstOrDefault();
                var userrole = user2.Roles.Where(x => x.UserId == user).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = user2.Email + " account deactivated";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult DeactivateMember(string id)
        {
            var db = new ApplicationDbContext();
            var detail = db.UserDetails.Where(x => x.AccountId == id).FirstOrDefault();
            if (detail != null)
            {
                detail.MemberLock = "Deactive";
                db.Entry(detail).State = EntityState.Modified;
                db.SaveChanges();
                var db2 = new ApplicationDbContext();
                var logs = new UserLogs();
                var user2 = db2.Users.Where(x => x.Id == detail.AccountId).FirstOrDefault();
                var userrole = user2.Roles.Where(x => x.UserId == user2.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = user2.Email + " account deactivated";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
            }
            return RedirectToAction("ViewMemberList", "Coopadmin");
        }

        public ActionResult ActivateMember(string id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var detail = db.UserDetails.Where(x => x.AccountId == id).FirstOrDefault();
            if (detail != null)
            {
                detail.MemberLock = "Active";
                db.Entry(detail).State = EntityState.Modified;
                db.SaveChanges();
                var logs = new UserLogs();
                var user2 = db2.Users.Where(x => x.Id == id).FirstOrDefault();
                var userrole = user2.Roles.Where(x => x.UserId == id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = user2.Email + " account deactivated";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
            }
            return RedirectToAction("ViewMemberList", "Coopadmin");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
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

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

        #endregion
    }
}