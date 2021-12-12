using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
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
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        // GET: Admin
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
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

        public static string SampleString = "";

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        public ActionResult CreateCommissionRate()
        {
            return View();
        }

        [HttpGet]
        public JsonResult LoadComRate()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            var commision = db.CommissionDetails.OrderByDescending(c => c.Id).ToList();

            if (commision != null)
            {
                foreach (var com in commision)
                {
                    data.Add(new
                    {
                        date = com.Updated_at.ToString(),
                        comRate = com.Rate + " %"
                    });
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CreateCommissionRate(CommissionTable model)
        {
            var db = new ApplicationDbContext();
            var rate = new CommissionTable();
            rate.Rate = model.Rate;
            rate.Updated_at = DateTime.Now;
            db.CommissionDetails.Add(rate);
            db.SaveChanges();

            var notif = new NotificationModel
            {
                ToRole = "Coop Admin",
                ToUser = "",
                NotifFrom = "PCart Team",
                NotifHeader = "Commission Fee",
                NotifMessage = "The commission fee has been changed to " + model.Rate + "%.",
                NavigateURL = "Index",
                IsRead = false,
                DateReceived = DateTime.Now
            };
            db.Notifications.Add(notif);
            db.SaveChanges();
            NotificationHub objNotifHub = new NotificationHub();
            objNotifHub.SendNotification(notif.ToRole);

            return RedirectToAction("CreateCommissionRate");
        }

        public bool CheckCoop(string name)
        {
            var db = new ApplicationDbContext();
            var coop = db.CoopDetails.Where(p => p.CoopName == name).FirstOrDefault();
            if (coop == null)
            {
                return false;
            }

            return true;
        }

        public ActionResult ViewCommissionRateReport()
        {
            var db = new ApplicationDbContext();
            var commrates = db.CommissionDetails.OrderByDescending(p => p.Id).ToList();
            return View(commrates);
        }

        public ActionResult Index()
        {
            var db = new ApplicationDbContext();
            var user = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            List<SuperAdmin> superAdmin = new List<SuperAdmin>();
            var commission = db.CommissionDetails.OrderByDescending(p => p.Id).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Approval == "Approved").ToList();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user.Id).FirstOrDefault();
            string count = "";
            string comm = "";

            if (coop != null)
            {
                count = coop.Count.ToString();
            }
            else
            {
                count = "0";
            }

            if (commission != null)
            {
                comm = commission.Rate.ToString();
            }
            else
            {
                comm = "0";
                ViewBag.Message = "Kindly input a commission rate before accepting COOPs.";
            }

            superAdmin.Add(new SuperAdmin { Ewallet = ewallet.Balance, Commission = comm, Cooperatives = count });

            dynamic mymodel = new ExpandoObject();
            mymodel.Superadmin = superAdmin;
            return View(mymodel);
        }


        public ActionResult ViewPendingCoop()
        {
            var db = new ApplicationDbContext();
            var checkrates = db.CommissionDetails.OrderByDescending(p => p.Id).FirstOrDefault();
            if (checkrates == null)
            {
                return RedirectToAction("CreateCommissionRate");
            }

            return View();
        }

        [HttpGet]
        public JsonResult LoadPendingCoop()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            var coops = db.CoopDetails.Where(p => p.Approval == "Pending").ToList();

            if (coops != null)
            {
                foreach (var coop in coops)
                {
                    data.Add(new
                    {
                        name = coop.CoopName,
                        address = coop.Address,
                        contact = coop.Contact,
                        dateCreated = coop.Coop_Created.ToString(),
                        coopId = coop.Id
                    });
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewPendingAdminAccnts()
        {
            return View();
        }

        [HttpGet]
        public JsonResult LoadPendingCoopAdmin()
        {
            var data = new List<object>();
            var db = new ApplicationDbContext();
            var coopAdmins = db.CoopAdminDetails.Where(x => x.Approval == null).ToList();

            if (coopAdmins != null)
            {
                foreach (var coopAdmin in coopAdmins)
                {
                    data.Add(new
                    {
                        firstname = coopAdmin.Firstname,
                        lastname = coopAdmin.Lastname,
                        contact = coopAdmin.Contact,
                        address = coopAdmin.Address,
                        email = coopAdmin.Email,
                        dateCreated = coopAdmin.Created_at.ToString(),
                        coopAdminId = coopAdmin.ID
                    });
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult PendingAccntDetails(Int32 id)
        {
            var db = new ApplicationDbContext();
            var getcheck = db.CoopAdminDetails.Where(x => x.ID == id).FirstOrDefault();
            return View(getcheck);
        }

        public ActionResult RejectReqAdminAccnt(Int32 id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var user = db.CoopAdminDetails.Where(p => p.ID == id).FirstOrDefault();
            var getemail = db.Users.Where(p => p.Id == user.UserId).FirstOrDefault();
            if (user != null)
            {
                #region formatter
                string text = string.Format("Dear Coop,", "Account Confirmation", "");
                string html = "<br/><h4>This letter is to inform you that we received your application. However, as we validate your application, the requirements we received from your coop were not validated" +
                    " due to lack the of documents. <br />You can still apply and comply the requirements stated in the COOP application page.</h4><h4>By: PCart Online Management</h4>";
                #endregion
                user.Approval = "Denied";
                var logs = new UserLogs();
                var userrole = getemail.Roles.Where(x => x.UserId == getemail.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail.Email + " COOP account denied";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();

                //Notify Coop shops if their application is approved.
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                msg.To.Add(new MailAddress(getemail.Email));
                msg.Subject = "Account Confirmation";
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = credentials;
                smtpClient.EnableSsl = true;
                smtpClient.Send(msg);

                db.Entry(getemail).State = EntityState.Deleted;
                db.SaveChanges();
            }
            return RedirectToAction("ViewPendingAdminAccnts");
        }

        public ActionResult ApproveAccntAdminReq(Int32 id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var user = db.CoopAdminDetails.Where(p => p.ID == id).FirstOrDefault();
            var getemail = db.Users.Where(p => p.Id == user.UserId).FirstOrDefault();
            if (user != null)
            {
                #region formatter
                string text = string.Format("Hello! Welcome to PCart Online Shopping!", "Account Confirmation", "You are now officialy registered as COOP in PCart Shopping. You can now login anytime.");
                string html = "<br/><h4>You are now officialy registered as a COOP in PCart Shopping. You can now login at anytime.</h4></br></br><h4>By: PCart Online Management</h4>";
                #endregion
                user.Approval = "Approved";
                var logs = new UserLogs();
                var userrole = getemail.Roles.Where(x => x.UserId == getemail.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail.Email + " COOP account approved";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                getemail.EmailConfirmed = true;
                db.Entry(getemail).State = EntityState.Modified;
                db.SaveChanges();

                //Notify Coop shops if their application is approved.
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                msg.To.Add(new MailAddress(getemail.Email));
                msg.Subject = "Account Confirmation";
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = credentials;
                smtpClient.EnableSsl = true;
                smtpClient.Send(msg);

                var ewallet = new EWallet();
                ewallet.UserID = user.UserId;
                ewallet.COOP_ID = user.Coop_code.ToString();
                ewallet.Balance = 0;
                ewallet.Status = "Active";
                ewallet.Created_At = DateTime.Now;
                db.UserEWallet.Add(ewallet);
                db.SaveChanges();

                var userEwallet = db.UserEWallet.Where(x => x.COOP_ID == user.Coop_code.ToString() && x.Status == "On-Hold").FirstOrDefault();
                if (userEwallet != null)
                {
                    var currEwallet = db.UserEWallet.Where(x => x.COOP_ID == user.Coop_code.ToString() && x.Status == "Active").FirstOrDefault();
                    currEwallet.Balance += userEwallet.Balance;
                    db.Entry(currEwallet).State = EntityState.Modified;
                    db.SaveChanges();

                    userEwallet.Status = "Transfered";
                    db.Entry(userEwallet).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("ViewPendingAdminAccnts");
        }

        public ActionResult ViewAdmin(Int32 id)
        {
            var db = new ApplicationDbContext();
            var user = (from coop in db.CoopDetails
                        join users in db.CoopAdminDetails
                        on coop.Id equals users.Coop_code
                        join acc in db.Users
                        on users.UserId equals acc.Id
                        where coop.Id == id
                        select new CoopViewModel
                        {
                            CoopName = coop.CoopName,
                            CoopAddress = coop.Address,
                            CoopContact = coop.Contact,
                            Id = coop.Id,
                            Userid = users.ID,
                            Firstname = users.Firstname,
                            Lastname = users.Lastname,
                            Image = users.Image,
                            Gender = users.Gender,
                            CStatus = users.Status,
                            Contact = users.Contact,
                            Address = users.Address,
                            Bdate = users.Bdate,
                            Created_at = users.Created_at.ToString(),
                            Updated_at = users.Updated_at.ToString(),
                            Email = acc.Email
                        }).FirstOrDefault();

            return View(user);
        }

        public ActionResult DisplayCoopDetailReq(int id)
        {
            if(id == 0)
            {
                return RedirectToAction("ListOfCoops", "Admin");
            }

            var db = new ApplicationDbContext();
            List<CoopViewModel> coopViews = new List<CoopViewModel>();
            List<CoopDocumentImages> coopDocuments = new List<CoopDocumentImages>();
            var getuser = db.CoopDetails.Where(x => x.Id == id).FirstOrDefault();
            var userdetail = db.CoopAdminDetails.Where(x => x.Coop_code == getuser.Id).FirstOrDefault();
            var user = (from coop in db.CoopDetails
                        join users in db.CoopAdminDetails
                        on coop.Id equals users.Coop_code
                        join acc in db.Users
                        on users.UserId equals acc.Id
                        where coop.Id == id
                        select new CoopViewModel
                        {
                            CoopName = coop.CoopName,
                            CoopAddress = coop.Address,
                            CoopContact = coop.Contact,
                            Id = coop.Id,
                            AccntId = users.UserId,
                            Userid = users.ID,
                            Firstname = users.Firstname,
                            Lastname = users.Lastname,
                            Image = users.Image,
                            Gender = users.Gender,
                            CStatus = users.Status,
                            Contact = users.Contact,
                            Address = users.Address,
                            Bdate = users.Bdate,
                            Created_at = users.Created_at.ToString(),
                            Updated_at = users.Updated_at.ToString(),
                            Email = acc.Email,
                            Approval = coop.Approval
                        }).ToList();

            var dispdocs = db.CoopImages.Where(x => x.Userid == userdetail.UserId).ToList();

            foreach (var coop in user)
            {
                coopViews.Add(new CoopViewModel
                {
                    AccntId = coop.AccntId,
                    Address = coop.Address,
                    Bdate = coop.Bdate,
                    Contact = coop.Contact,
                    CoopAddress = coop.CoopAddress,
                    CoopContact = coop.CoopContact,
                    CoopName = coop.CoopName,
                    Created_at = coop.Created_at,
                    CStatus = coop.CStatus,
                    Email = coop.Email,
                    Firstname = coop.Firstname,
                    Gender = coop.Gender,
                    Id = coop.Id,
                    Image = coop.Image,
                    Lastname = coop.Lastname,
                    Userid = coop.Userid,
                    Approval = coop.Approval
                });
            }

            if(dispdocs!=null)
            {
                foreach (var images in dispdocs)
                {
                    coopDocuments.Add(new CoopDocumentImages { Userid = images.Userid, Document_image = images.Document_image });
                }
            }

            dynamic mymodel = new ExpandoObject();
            mymodel.Users = coopViews;
            mymodel.Images = coopDocuments;

            return View(mymodel);
        }

        public ActionResult DisplayCoopDocuments()
        {
            var data = new List<object>();
            var user = Request["myid"];
            var db = new ApplicationDbContext();
            var images = db.CoopImages.Where(x => x.Userid == user).ToList();

            if(images!=null)
            {
                foreach (var img in images)
                {
                    string source = img.Document_image;
                    string replace = source.Replace("~", "..");
                    data.Add(new
                    {
                        Image = replace
                    });
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RejectReqAccnt(Int32 id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var coop = db.CoopDetails.Where(p => p.Id == id).FirstOrDefault();
            var user = db.CoopAdminDetails.Where(p => p.Coop_code == coop.Id).FirstOrDefault();
            var images = db.CoopImages.Where(x => x.Userid == user.UserId).ToList();
            var getemail = db.Users.Where(p => p.Id == user.UserId).FirstOrDefault();
            var location = db.Locations.Where(x => x.UserId == user.UserId).FirstOrDefault();

            var coopLocation = db.CoopLocations.Where(x => x.CoopId == coop.Id.ToString()).FirstOrDefault();
            if (coop != null)
            {
                #region formatter
                string text = string.Format("Dear Coop,", "Account Confirmation", "");
                string html = "<br/><h4>This letter is to inform you that we received your application. However, as we validate your application, the requirements we received from your coop were not validated" +
                    " due to lack the of documents. <br />You can still apply and comply the requirements stated in the COOP application page.</h4><h4>By: PCart Online Management</h4>";
                #endregion
                coop.Approval = "Denied";
                user.Approval = "Denied";
                var logs = new UserLogs();
                var userrole = getemail.Roles.Where(x => x.UserId == getemail.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail.Email + " COOP account denied";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                db.Entry(coop).State = EntityState.Modified;
                db.SaveChanges();

                var coopRecord = new CoopApplicationRecords
                {
                    CoopName = coop.CoopName,
                    Address = coop.Address,
                    Contact = coop.Contact,
                    Firstname = user.Firstname,
                    Lastname = user.Lastname,
                    Image = user.Image,
                    Gender = user.Gender,
                    Status = user.Status,
                    AdminContact = user.Contact,
                    AdminAddress = user.Address,
                    Bdate = user.Bdate,
                    Approval = user.Approval,
                    Email = user.Email,
                    Created_at = user.Created_at,
                    DateAnswered = DateTime.Now
                };
                db.CoopRecords.Add(coopRecord);
                db.SaveChanges();

                foreach (var img in images)
                {
                    var coopDocu = new CoopApplicationDocu
                    {
                        COOP_RecordID = coopRecord.Id,
                        Document_image = img.Document_image
                    };
                    db.CoopRecordsDocu.Add(coopDocu);
                    db.SaveChanges();
                    db.Entry(img).State = EntityState.Deleted;
                    db.SaveChanges();
                }

                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                msg.To.Add(new MailAddress(getemail.Email));
                msg.Subject = "Account Confirmation";
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                smtpClient.Credentials = credentials;
                smtpClient.EnableSsl = true;
                smtpClient.Send(msg);

                db.Entry(location).State = EntityState.Deleted;
                db.SaveChanges();
                db.Entry(coopLocation).State = EntityState.Deleted;
                db.SaveChanges();
                db.Entry(userrole).State = EntityState.Deleted;
                db.SaveChanges();
                db.Entry(user).State = EntityState.Deleted;
                db.SaveChanges();
                db.Entry(coop).State = EntityState.Deleted;
                db.SaveChanges();
                db.Entry(getemail).State = EntityState.Deleted;
                db.SaveChanges();
            }

            return RedirectToAction("ViewPendingCoop");
        }

        public ActionResult ApproveAccntReq(Int32 id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var coop = db.CoopDetails.Where(p => p.Id == id).FirstOrDefault();
            var user = db.CoopAdminDetails.Where(p => p.Coop_code == coop.Id).FirstOrDefault();
            var getemail = db.Users.Where(p => p.Id == user.UserId).FirstOrDefault();
            if (coop != null)
            {
                #region formatter
                string text = string.Format("Hello! Welcome to PCart Online Shopping!", "Account Confirmation", "You are now officialy registered as COOP in PCart Shopping. You can now login anytime.");
                string html = "<br/><h4>You are now officialy registered as a COOP in PCart Shopping. You can now login at anytime.</h4><h4>By: PCart Online Management</h4>";
                #endregion
                coop.Approval = "Approved";
                user.Approval = "Approved";
                var logs = new UserLogs();
                var userrole = getemail.Roles.Where(x => x.UserId == getemail.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail.Email + " COOP account approved";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                db.Entry(coop).State = EntityState.Modified;
                db.SaveChanges();

                getemail.EmailConfirmed = true;
                db.Entry(getemail).State = EntityState.Modified;
                db.SaveChanges();

                //Notify Coop shops if their application is approved.
                MailMessage msg = new MailMessage();
                msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                msg.To.Add(new MailAddress(getemail.Email));
                msg.Subject = "Account Confirmation";
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
                msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
                SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = credentials;
                smtpClient.EnableSsl = true;
                smtpClient.Send(msg);

                var ewallet = new EWallet();
                ewallet.UserID = user.UserId;
                ewallet.COOP_ID = user.Coop_code.ToString();
                ewallet.Balance = 0;
                ewallet.Status = "Active";
                ewallet.Created_At = DateTime.Now;
                db.UserEWallet.Add(ewallet);
                db.SaveChanges();
            }

            return RedirectToAction("ViewPendingCoop");
        }

        public ActionResult ListOfCoops()
        {
            var db = new ApplicationDbContext();

            var listofcoops = db.CoopDetails.Where(x => x.Approval == "Approved").ToList();

            return View(listofcoops);
        }

        public ActionResult LockCoop(int? id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var getcoop = db.CoopDetails.Where(x => x.Id == id).FirstOrDefault();
            var getcoopdetail = db.CoopAdminDetails.Where(x => x.Coop_code == getcoop.Id).FirstOrDefault();
            var getmembers = db.UserDetails.Where(x => x.CoopId == getcoopdetail.UserId).ToList();
            var getemail1 = db.Users.Where(x => x.Id == getcoopdetail.UserId).FirstOrDefault();
            if (getcoop != null)
            {
                getcoop.IsLocked = "Locked";
                getcoop.DateAccLocked = DateTime.Now.ToString();
                var logs = new UserLogs();
                var userrole = getemail1.Roles.Where(x => x.UserId == getemail1.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail1.Email + " COOP locked";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                db.Entry(getcoop).State = EntityState.Modified;
                db.SaveChanges();
            }
            #region formatter
            string text = string.Format("Good Day our COOP Managers/Admins!", "Account Lock Information", "We temporarily lock your account due to severe violations.");
            string html = "<br/><h4>We will be notifying you through email once we have unlocked your account. Thank you, and have a great day ahead.</h4>";
            #endregion

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
            msg.To.Add(new MailAddress(getemail1.Email));
            msg.Subject = "Account Lock Information";
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = credentials;
            smtpClient.EnableSsl = true;
            smtpClient.Send(msg);

            if (getmembers != null)
            {
                foreach (var member in getmembers)
                {
                    var getemail2 = db.Users.Where(x => x.Id == member.AccountId).FirstOrDefault();
                    member.MemberLock = "Inactive";
                    member.DateAccLocked = DateTime.Now.ToString();
                    var logs = new UserLogs();
                    var userrole = getemail2.Roles.Where(x => x.UserId == getemail2.Id).FirstOrDefault();
                    var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                    logs.Logs = getemail2.Email + " account locked";
                    logs.Role = getuserrole.Name;
                    logs.Date = DateTime.Now.ToString();
                    db2.Logs.Add(logs);
                    db2.SaveChanges();
                    db.Entry(member).State = EntityState.Modified;
                    db.SaveChanges();

                    #region formatter
                    string text2 = string.Format("Good Day our COOP Members!", "Account Lock Information", "We temporarily lock your account due to the severe violations.");
                    string html2 = "<br/><h4>We will be notifying you through email once we have unlocked your account. Thank you, and have a great day ahead.</h4>";
                    #endregion

                    MailMessage msg2 = new MailMessage();
                    msg2.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                    msg2.To.Add(new MailAddress(getemail2.Email));
                    msg2.Subject = "Account Lock Information";
                    msg2.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text2, null, MediaTypeNames.Text.Plain));
                    msg2.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html2, null, MediaTypeNames.Text.Html));
                    SmtpClient smtpClient2 = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                    smtpClient2.UseDefaultCredentials = false;
                    smtpClient2.Credentials = credentials;
                    smtpClient2.EnableSsl = true;
                    smtpClient2.Send(msg2);
                }
            }

            return RedirectToAction("ListOfCoops");
        }

        public ActionResult UnlockCoop(int? id)
        {
            var db = new ApplicationDbContext();
            var db2 = new ApplicationDbContext();
            var getcoop = db.CoopDetails.Where(x => x.Id == id).FirstOrDefault();
            var getcoopdetail = db.CoopAdminDetails.Where(x => x.Coop_code == getcoop.Id).FirstOrDefault();
            var getmembers = db.UserDetails.Where(x => x.CoopId == getcoopdetail.UserId).ToList();
            var getemail1 = db.Users.Where(x => x.Id == getcoopdetail.UserId).FirstOrDefault();
            if (getcoop != null)
            {
                getcoop.IsLocked = "Unlock";
                getcoop.DateAccRetrieved = DateTime.Now.ToString();
                var logs = new UserLogs();
                var userrole = getemail1.Roles.Where(x => x.UserId == getemail1.Id).FirstOrDefault();
                var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                logs.Logs = getemail1.Email + " COOP unlocked";
                logs.Role = getuserrole.Name;
                logs.Date = DateTime.Now.ToString();
                db2.Logs.Add(logs);
                db2.SaveChanges();
                db.Entry(getcoop).State = EntityState.Modified;
                db.SaveChanges();
            }

            #region formatter
            string text = string.Format("Good Day our COOP Managers/Admins!", "Account Unlock Information", "We already unlocked your account");
            string html = "<br/><h4>You can now continue to manage your COOP Shop and members. Thank you for patiently waiing!</h4>";
            #endregion

            MailMessage msg = new MailMessage();
            msg.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
            msg.To.Add(new MailAddress(getemail1.Email));
            msg.Subject = "Account Unlock Information";
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text, null, MediaTypeNames.Text.Plain));
            msg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html, null, MediaTypeNames.Text.Html));
            SmtpClient smtpClient = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
            System.Net.NetworkCredential credentials = new System.Net.NetworkCredential(ConfigurationManager.AppSettings["AdminEmail"].ToString(), ConfigurationManager.AppSettings["Password"].ToString());
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = credentials;
            smtpClient.EnableSsl = true;
            smtpClient.Send(msg);

            if (getmembers != null)
            {
                foreach (var member in getmembers)
                {

                    var getemail2 = db.Users.Where(x => x.Id == member.AccountId).FirstOrDefault();
                    member.MemberLock = "Active";
                    member.DateAccRetrieved = DateTime.Now.ToString();
                    var logs = new UserLogs();
                    var userrole = getemail2.Roles.Where(x => x.UserId == getemail2.Id).FirstOrDefault();
                    var getuserrole = db2.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();
                    logs.Logs = getemail2.Email + " account unlocked";
                    logs.Role = getuserrole.Name;
                    logs.Date = DateTime.Now.ToString();
                    db2.Logs.Add(logs);
                    db2.SaveChanges();
                    db.Entry(member).State = EntityState.Modified;
                    db.SaveChanges();

                    #region formatter
                    string text2 = string.Format("Good Day our COOP Members!", "Account Unlock Information", "We already unlocked your account.");
                    string html2 = "<br/><h4>You can now continue to shop and enjoy products from your COOP Shop. Thank you for patiently waiing!</h4>";
                    #endregion

                    MailMessage msg2 = new MailMessage();
                    msg2.From = new MailAddress(ConfigurationManager.AppSettings["AdminEmail"].ToString());
                    msg2.To.Add(new MailAddress(getemail2.Email));
                    msg2.Subject = "Account Unlock Information";
                    msg2.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(text2, null, MediaTypeNames.Text.Plain));
                    msg2.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(html2, null, MediaTypeNames.Text.Html));
                    SmtpClient smtpClient2 = new SmtpClient("smtp.gmail.com", Convert.ToInt32(587));
                    smtpClient2.UseDefaultCredentials = false;
                    smtpClient2.Credentials = credentials;
                    smtpClient2.EnableSsl = true;
                    smtpClient2.Send(msg2);
                }
            }

            return RedirectToAction("ListOfCoops");
        }

        public List<UserOrder> GetUserOrders(string coop)
        {
            var db = new ApplicationDbContext();
            List<UserOrder> order = new List<UserOrder>();
            var userOrders = db.UserOrders.ToList();

            if (coop != null)
            {
                userOrders = db.UserOrders.Where(u => u.CoopId.ToString() == coop).ToList();
            }

            order = userOrders;

            return (order);
        }

        public List<ProdOrder> GetProdOrders(string coop)
        {
            var db = new ApplicationDbContext();
            var prodOrders = db.ProdOrders.ToList();
            List<ProdOrder> product = new List<ProdOrder>();

            if (coop != null)
            {
                prodOrders = db.ProdOrders.Where(u => u.CoopId.ToString() == coop).ToList();
            }

            product = prodOrders;

            return (product);
        }

        public List<UserVoucherUsed> GetVoucherUsed(string coop)
        {
            var db = new ApplicationDbContext();
            var voucherUsed = db.VoucherUseds.ToList();
            List<UserVoucherUsed> voucher = new List<UserVoucherUsed>();

            if (coop != null)
            {
                voucherUsed = db.VoucherUseds.Where(u => u.CoopId.ToString() == coop).ToList();
            }

            voucher = voucherUsed;

            return (voucher);
        }

        public List<AllCoop> GetCoop()
        {
            var db = new ApplicationDbContext();
            var coops = db.CoopDetails.ToList();
            List<AllCoop> allCoops = new List<AllCoop>();

            foreach (var coop in coops)
            {
                allCoops.Add(new AllCoop
                {
                    CoopId = coop.Id,
                    CoopName = coop.CoopName
                });
            }

            return (allCoops);
        }

        public ActionResult ViewCoopSales()
        {
            var db = new ApplicationDbContext();
            var model = new ViewSalesReport();

            List<SalesReport> orderlist = new List<SalesReport>();
            List<UserOrder> userOrders = GetUserOrders(model.CoopSearch);
            List<ProdOrder> prodOrders = GetProdOrders(model.CoopSearch);
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed(model.CoopSearch);
            List<AllCoop> allCoops = GetCoop();

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
            model.Coops = allCoops;

            return View(model);
        }

        [HttpPost]
        public ActionResult ViewCoopSales(ViewSalesReport model)
        {
            var db = new ApplicationDbContext();
            List<SalesReport> orderlist = new List<SalesReport>();
            List<ViewBySale> viewBySale = new List<ViewBySale>();
            List<UserOrder> userOrders = GetUserOrders(model.CoopSearch);
            List<ProdOrder> prodOrders = GetProdOrders(model.CoopSearch);
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed(model.CoopSearch);
            List<AllCoop> allCoops = GetCoop();
            List<string> date = new List<string>();
            List<string> coopId = new List<string>();
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
                    coop_id = order.CoopId;

                    if (!date.Contains(year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add(year.ToString());
                        coopId.Add(order.CoopId.ToString());
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
                coopId.Clear();
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

                    if (!date.Contains(monthName + " " + year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add(monthName + " " + year.ToString());
                        coopId.Add(order.CoopId.ToString());

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
                coopId.Clear();
                orderlist = null;
            }
            else if (model.ViewBy != null && model.ViewBy == "Weekly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    DateTime getdate = Convert.ToDateTime(order.OrderCreated_at, CultureInfo.CurrentCulture);
                    var week = GetWeekNumberOfMonth(getdate);
                    var month = DateTime.Parse(order.OrderCreated_at, culture).Month;
                    var year = DateTime.Parse(order.OrderCreated_at, culture).Year;
                    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                    total = order.TotalPrice;
                    coop_id = order.CoopId;

                    if (!date.Contains("Week " + week + " of " + monthName + " " + year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add("Week " + week + " of " + monthName + " " + year.ToString());
                        coopId.Add(order.CoopId.ToString());

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
                coopId.Clear();
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
            model.Coops = allCoops;
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

        public ActionResult ViewCommissionSale()
        {
            var db = new ApplicationDbContext();
            var model = new ViewSalesReport();

            List<SalesReport> orderlist = new List<SalesReport>();
            List<UserOrder> userOrders = GetUserOrders(model.CoopSearch);
            List<ProdOrder> prodOrders = GetProdOrders(model.CoopSearch);
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed(model.CoopSearch);
            List<AllCoop> allCoops = GetCoop();

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
            model.Coops = allCoops;

            return View(model);
        }

        [HttpPost]
        public ActionResult ViewCommissionSale(ViewSalesReport model)
        {
            var db = new ApplicationDbContext();
            List<SalesReport> orderlist = new List<SalesReport>();
            List<ViewBySale> viewBySale = new List<ViewBySale>();
            List<UserOrder> userOrders = GetUserOrders(model.CoopSearch);
            List<ProdOrder> prodOrders = GetProdOrders(model.CoopSearch);
            List<UserVoucherUsed> voucherUsed = GetVoucherUsed(model.CoopSearch);
            List<AllCoop> allCoops = GetCoop();
            List<string> date = new List<string>();
            List<string> coopId = new List<string>();
            decimal total = 0;
            var coop_id = 0;

            if (model.ViewBy != null && model.ViewBy == "Yearly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    DateTime getdate = Convert.ToDateTime(order.OrderCreated_at, CultureInfo.CurrentCulture);
                    var year = getdate.Year;

                    total = order.CommissionFee;
                    coop_id = order.CoopId;

                    if (!date.Contains(year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add(year.ToString());
                        coopId.Add(order.CoopId.ToString());
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
                coopId.Clear();
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

                    total = order.CommissionFee;
                    coop_id = order.CoopId;

                    if (!date.Contains(monthName + " " + year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add(monthName + " " + year.ToString());
                        coopId.Add(order.CoopId.ToString());

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
                coopId.Clear();
                orderlist = null;
            }
            else if (model.ViewBy != null && model.ViewBy == "Weekly")
            {
                foreach (var order in userOrders)
                {
                    CultureInfo culture = new CultureInfo("es-ES");
                    var week = GetWeekNumberOfMonth(DateTime.Parse(order.OrderCreated_at, culture));
                    var month = DateTime.Parse(order.OrderCreated_at, culture).Month;
                    var year = DateTime.Parse(order.OrderCreated_at, culture).Year;
                    var monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                    total = order.CommissionFee;
                    coop_id = order.CoopId;

                    if (!date.Contains("Week " + week + " of " + monthName + " " + year.ToString()) || !coopId.Contains(order.CoopId.ToString()))
                    {
                        date.Add("Week " + week + " of " + monthName + " " + year.ToString());
                        coopId.Add(order.CoopId.ToString());

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
                coopId.Clear();
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
            model.Coops = allCoops;
            model.ViewBySales = viewBySale;
            return View(model);
        }

        public ActionResult WithdrawPendingsReport()
        {
            var db = new ApplicationDbContext();
            WithdrawView withdraw = new WithdrawView();
            var pendinglists = db.Withdraw.Where(x => x.RequestStatus == "Pending").ToList();
            List<WithdrawViewModel> requests = new List<WithdrawViewModel>();
            if (pendinglists != null)
            {
                foreach (var pending in pendinglists)
                {
                    var coop = db.CoopDetails.Where(x => x.Id == pending.CoopId).FirstOrDefault();
                    requests.Add(new WithdrawViewModel
                    {
                        Amount = pending.Amount,
                        Contact = pending.Contact,
                        CoopId = pending.CoopId,
                        Fullname = pending.Fullname,
                        DateRequested = pending.DateReqeuested,
                        Id = pending.Id,
                        Method = pending.Method,
                        CoopName = coop.CoopName
                    });
                }
                withdraw.ViewModel = requests;
            }

            return View(withdraw);
        }

        public ActionResult WithdrawRequestDetail(int? id)
        {
            var db = new ApplicationDbContext();
            Session["RequestId"] = id;
            var detail = db.Withdraw.Where(x => x.Id == id).FirstOrDefault();
            var coop = db.CoopDetails.Where(x => x.Id == detail.CoopId).FirstOrDefault();
            var withdraw = new WithdrawViewModel
            {
                Id = detail.Id,
                Amount = detail.Amount,
                Contact = detail.Contact,
                CoopId = detail.CoopId,
                CoopName = coop.CoopName,
                DateRequested = detail.DateReqeuested,
                Email = detail.Email,
                Fullname = detail.Fullname,
                Method = detail.Method,
                RequestStatus = detail.RequestStatus
            };
            return View(withdraw);
        }

        [HttpPost]
        public ActionResult FulfilledRequest(WithdrawViewModel model, HttpPostedFileBase file, string submit)
        {
            Int32 id = int.Parse(Session["RequestId"].ToString());
            var db = new ApplicationDbContext();
            if (model.ReceiptFile == null)
            {
                return RedirectToAction("WithdrawRequestDetail", new { id = id });
            }
            var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
            string name = Path.GetFileNameWithoutExtension(model.ReceiptFile.FileName); //getting file name without extension  
            string extension = Path.GetExtension(model.ReceiptFile.FileName);
            if (ValidateFile(model.ReceiptFile)==true)
            {
                var withdraw = db.Withdraw.Where(x => x.Id == id).FirstOrDefault();
                if (withdraw != null)
                {
                    var coop = db.CoopDetails.Where(x => x.Id == withdraw.CoopId).FirstOrDefault();
                    var ewallet = db.UserEWallet.Where(x => x.COOP_ID == coop.Id.ToString() && x.Status == "Active").FirstOrDefault();
                    ewallet.Balance -= withdraw.Amount;
                    db.Entry(coop).State = EntityState.Modified;
                    db.SaveChanges();

                    var ewalletHistory = new EWalletHistory();
                    ewalletHistory.EWallet_ID = ewallet.ID;
                    ewalletHistory.Amount = withdraw.Amount;
                    ewalletHistory.Action = "Widthdraw";
                    ewalletHistory.Description = "Widthdraw request is successfull";
                    ewalletHistory.Created_At = DateTime.Now;
                    db.EWalletHistories.Add(ewalletHistory);
                    db.SaveChanges();

                    var myfile = name + extension;
                    var path = Path.Combine(Server.MapPath("../Receipts/"), myfile);
                    withdraw.Receipt = name + extension;
                    withdraw.RequestStatus = "Completed";
                    model.ReceiptFile.SaveAs(path);

                    db.Entry(withdraw).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return RedirectToAction("WithdrawPendingsReport");
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
        public ActionResult TransactionReport()
        {
            var db = new ApplicationDbContext();
            List<WithdrawViewModel> model = new List<WithdrawViewModel>();
            var transactions = db.Withdraw.Where(x => x.RequestStatus == "Completed").ToList();
            foreach (var transaction in transactions)
            {
                var coopde = db.CoopDetails.Where(x => x.Id == transaction.CoopId).FirstOrDefault();
                model.Add(new WithdrawViewModel
                {
                    Amount = transaction.Amount,
                    Contact = transaction.Contact,
                    CoopId = transaction.CoopId,
                    CoopName = coopde.CoopName,
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

        public ActionResult AdminComplaint()
        {
            return View();
        }

        public ActionResult ReturnRefundRequest()
        {
            var db = new ApplicationDbContext();
            var request = db.ReturnRefunds.Where(x => x.Status == "Accepted").ToList();

            return View(request);
        }

        public ActionResult ViewNotification()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var model = new ViewNotification();
            List<NotificationModel> unreadNotif = new List<NotificationModel>();
            List<NotificationModel> readNotif = new List<NotificationModel>();
            var unreadNotifications = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Admin") && n.IsRead == false).OrderByDescending(x => x.Id).ToList();
            var readNotifications = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Admin") && n.IsRead == true).OrderByDescending(x => x.Id).ToList();

            if (unreadNotifications != null)
            {
                foreach (var notification in unreadNotifications)
                {
                    unreadNotif.Add(new NotificationModel
                    {
                        Id = notification.Id,
                        NavigateURL = notification.NavigateURL,
                        ToUser = user,
                        ToRole = notification.ToRole,
                        NotifFrom = notification.NotifFrom,
                        NotifHeader = notification.NotifHeader,
                        NotifMessage = notification.NotifMessage,
                        IsRead = notification.IsRead,
                        DateReceived = notification.DateReceived
                    });
                }
            }
            else
            {
                unreadNotif = null;
            }

            if (readNotifications != null)
            {
                foreach (var notification in readNotifications)
                {
                    readNotif.Add(new NotificationModel
                    {
                        Id = notification.Id,
                        NavigateURL = notification.NavigateURL,
                        ToUser = user,
                        ToRole = notification.ToRole,
                        NotifFrom = notification.NotifFrom,
                        NotifHeader = notification.NotifHeader,
                        NotifMessage = notification.NotifMessage,
                        IsRead = notification.IsRead,
                        DateReceived = notification.DateReceived
                    });
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

            data.Add(new { mess = 1 });
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ViewEwalletHistory()
        {
            var db = new ApplicationDbContext();
            var user = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user.Id).FirstOrDefault();
            var ewalletHistory = db.EWalletHistories.Where(x => x.EWallet_ID == ewallet.ID).OrderByDescending(x => x.Created_At).ToList();

            return View(ewalletHistory);
        }

        public ActionResult EwalletHistoryDetails(int id)
        {
            var db = new ApplicationDbContext();
            var user = db.Users.Where(x => x.Email == "PCartTeam@gmail.com").FirstOrDefault();
            var ewallet = db.UserEWallet.Where(x => x.UserID == user.Id).FirstOrDefault();
            var ewalletHistory = db.EWalletHistories.Where(x => x.EWallet_ID == ewallet.ID && x.ID == id).FirstOrDefault();

            return View(ewalletHistory);
        }
    }
}