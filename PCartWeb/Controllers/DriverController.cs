using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using PCartWeb.Models;
using System.Data.Entity;
using System.IO;
using PCartWeb.Hubs;

namespace PCartWeb.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {

        // GET: Driver

        public ActionResult Index()
        {
            var db = new ApplicationDbContext();
            return View();
        }

        public ActionResult DeliveryList()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var delivery = db.DeliverStatus.Where(x => x.DriverId == user && x.Status == "To Pick-Up").ToList();
            List<DeliveryListDisplay> deliveries = new List<DeliveryListDisplay>();
            var delivList = (from del in db.DeliverStatus
                             join uorder in db.UserOrders
                             on del.UserOrderId equals uorder.Id
                             join user2 in db.UserDetails
                             on uorder.UserId equals user2.AccountId
                             where del.Status == "To Pick-Up" &&
                             del.DriverId == user
                             select new
                             {
                                 OrderNo = uorder.Id,
                                 CoopId = uorder.CoopId,
                                 Customer = user2.Firstname + " " + user2.Lastname,
                                 Deliveryfee = uorder.Delivery_fee,
                                 CustomerId = user2.AccountId,
                                 Pickup = del.PickUpDate,
                                 Mode = uorder.ModeOfPay
                             }).ToList();
            decimal total = 0;
            foreach (var order in delivList)
            {
                total = 0;
                var prodorder = db.ProdOrders.Where(x => x.UOrderId == order.OrderNo.ToString()).ToList();
                foreach (var prod in prodorder)
                {
                    if (order.OrderNo.ToString() == prod.UOrderId)
                    {
                        total += prod.SubTotal;
                    }
                }
                decimal delfee = decimal.Parse(order.Deliveryfee.ToString());
                deliveries.Add(new DeliveryListDisplay { OrderNo = order.OrderNo.ToString(), Customer = order.Customer, TotalAmount = total + delfee, CustomerId = order.CustomerId, PickupDate = order.Pickup, Mode = order.Mode });
            }
            var riderinfo = db.DriverDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == riderinfo.CoopId).FirstOrDefault();

            return View(deliveries);
        }

        public ActionResult ToBeDeliveredLists()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var delivery = db.DeliverStatus.Where(x => x.DriverId == user && (x.Status == "To Pick-Up" || x.Status == "To Be Delivered")).ToList();
            List<DeliveryListDisplay> deliveries = new List<DeliveryListDisplay>();
            var delivList = (from del in db.DeliverStatus
                             join uorder in db.UserOrders
                             on del.UserOrderId equals uorder.Id
                             join user2 in db.UserDetails
                             on uorder.UserId equals user2.AccountId
                             where del.Status == "To Be Delivered" &&
                             del.DriverId == user
                             select new
                             {
                                 OrderNo = uorder.Id,
                                 CoopId = uorder.CoopId,
                                 Customer = user2.Firstname + " " + user2.Lastname,
                                 Deliveryfee = uorder.Delivery_fee,
                                 CustomerId = user2.AccountId,
                                 Pickup = del.PickUpDate
                             }).ToList();
            decimal total = 0;
            foreach (var order in delivList)
            {
                total = 0;
                var prodorder = db.ProdOrders.Where(x => x.UOrderId == order.OrderNo.ToString()).ToList();
                foreach (var prod in prodorder)
                {
                    if (order.OrderNo.ToString() == prod.UOrderId)
                    {
                        total += prod.SubTotal;
                    }
                }
                decimal delfee = decimal.Parse(order.Deliveryfee.ToString());
                deliveries.Add(new DeliveryListDisplay { OrderNo = order.OrderNo.ToString(), Customer = order.Customer, TotalAmount = total + delfee, CustomerId = order.CustomerId, PickupDate = order.Pickup });
            }
            var riderinfo = db.DriverDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == riderinfo.CoopId).FirstOrDefault();

            return View(deliveries);
        }

        public ActionResult DeliveryDetails(string id, string message)
        {
            if (id == "")
            {
                return RedirectToAction("DeliveryList");
            }

            if (message != null)
            {
                ViewBag.Message = message;
            }

            var db = new ApplicationDbContext();
            ViewCustomerOrder customerOrder = new ViewCustomerOrder();
            DeliveryStatus2 deliveryStatus = new DeliveryStatus2();
            DeliveryListDisplay delivery = new DeliveryListDisplay();
            List<ProdOrder2> products = new List<ProdOrder2>();
            UserOrder userOrder = new UserOrder();
            CustomerDetailsModel customer = new CustomerDetailsModel();
            decimal total = 0;
            var prods = db.ProdOrders.Where(x => x.UOrderId == id).ToList();
            if(prods!=null)
            {
                foreach (var prod in prods)
                {
                    var getprod = db.ProductDetails.Where(x => x.Id.ToString() == prod.ProdId).FirstOrDefault();

                    products.Add(new ProdOrder2
                    {
                        UOrderId = prod.UOrderId,
                        CoopId = prod.CoopId,
                        DiscountedPrice = prod.DiscountedPrice,
                        MemberDiscountedPrice = prod.MemberDiscountedPrice,
                        Price = prod.Price,
                        ProdId = prod.ProdId,
                        SubTotal = prod.SubTotal,
                        Qty = prod.Qty,
                        UserId = prod.UserId,
                        ProdImage = getprod.Product_image,
                        ProdName = prod.ProdName
                    });

                    total += prod.SubTotal;
                }
            }

            var order = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
            var cust = new CustomerDetailsModel();
            var cust_loc = new Location();
            var coop_loc = new CoopLocation();
            if (order!=null)
            {
                cust = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();
                cust_loc = db.Locations.Where(x => x.UserId == order.UserId).FirstOrDefault();
                coop_loc = db.CoopLocations.Where(x => x.CoopId == order.CoopId.ToString()).FirstOrDefault();
            }
            
            var deliverystatus = db.DeliverStatus.Where(x => x.UserOrderId.ToString() == id).FirstOrDefault();

            var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
            if (deliverystatus.PickUpSuccessDate == date)
            {
                deliveryStatus.PickUpSuccessDate = "";
            }
            else
            {
                deliveryStatus.PickUpSuccessDate = deliverystatus.PickUpSuccessDate.ToString();
               
            }

            userOrder.ModeOfPay = order.ModeOfPay;
            userOrder.Delivery_fee = order.Delivery_fee;
            delivery.TotalAmount = total + decimal.Parse(order.Delivery_fee.ToString());
            delivery.Customer = cust.Firstname + " " + cust.Lastname;

            customerOrder.DeliveryDetails = deliveryStatus;
            customerOrder.CoopAddress = coop_loc.Address;
            customerOrder.UserOrders = userOrder;
            customerOrder.Address = cust.Address;
            customerOrder.ProdOrders = products;
            customerOrder.Delivery = delivery;
            customerOrder.CustLat = cust_loc.Geolocation.Latitude.ToString();
            customerOrder.CustLong = cust_loc.Geolocation.Longitude.ToString();
            customerOrder.CoopLat = coop_loc.Geolocation.Latitude.ToString();
            customerOrder.CoopLong = coop_loc.Geolocation.Longitude.ToString();
            TempData["UOrderId"] = id;
            Session["UOrderId"] = id;
            TempData.Keep();
            return View(customerOrder);
        }
        [HttpPost]
        public ActionResult DeliveryDetails(ViewCustomerOrder model, HttpPostedFileBase file)
        {
            var db = new ApplicationDbContext();
            if (Session["UOrderId"] == null)
            {
                return RedirectToAction("DeliveryList");
            }
            ViewCustomerOrder customerOrder = new ViewCustomerOrder();
            string id = Session["UOrderId"].ToString();
            Session["UOrderId"] = id;
            DeliveryListDisplay delivery = new DeliveryListDisplay();
            List<ProdOrder2> products = new List<ProdOrder2>();
            DeliveryStatus2 deliveryStatus = new DeliveryStatus2();
            UserOrder userOrder = new UserOrder();
            CustomerDetailsModel customer = new CustomerDetailsModel();
            decimal total = 0;
            var prods = db.ProdOrders.Where(x => x.UOrderId == id).ToList();
            foreach (var prod in prods)
            {
                var getprod = db.ProductDetails.Where(x => x.Id.ToString() == prod.ProdId).FirstOrDefault();
                products.Add(new ProdOrder2
                {
                    UOrderId = prod.UOrderId,
                    CoopId = prod.CoopId,
                    DiscountedPrice = prod.DiscountedPrice,
                    MemberDiscountedPrice = prod.MemberDiscountedPrice,
                    Price = prod.Price,
                    ProdId = prod.ProdId,
                    SubTotal = prod.SubTotal,
                    Qty = prod.Qty,
                    UserId = prod.UserId,
                    ProdImage = getprod.Product_Name,
                    ProdName = prod.ProdName
                });
                total += prod.SubTotal;
            }

            var order = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
            var cust = db.UserDetails.Where(x => x.AccountId == order.UserId).FirstOrDefault();
            var cust_loc = db.Locations.Where(x => x.UserId == order.UserId).FirstOrDefault();
            var coop_loc = db.CoopLocations.Where(x => x.CoopId == order.CoopId.ToString()).FirstOrDefault();
            var deliverystatus = db.DeliverStatus.Where(x => x.UserOrderId.ToString() == id).FirstOrDefault();

            var date = Convert.ToDateTime("1/1/0001 12:00:00 AM");
            if (deliverystatus.PickUpSuccessDate == date)
            {
                deliveryStatus.PickUpSuccessDate = "";
            }
            else
            {
                deliveryStatus.PickUpSuccessDate = deliverystatus.PickUpSuccessDate.ToString();

            }

            userOrder.Delivery_fee = order.Delivery_fee;
            delivery.TotalAmount = total + decimal.Parse(order.Delivery_fee.ToString());
            delivery.Customer = cust.Firstname + " " + cust.Lastname;

            customerOrder.DeliveryDetails = deliveryStatus;
            customerOrder.CoopAddress = coop_loc.Address;
            customerOrder.UserOrders = userOrder;
            customerOrder.Address = cust.Address;
            customerOrder.ProdOrders = products;
            customerOrder.Delivery = delivery;
            customerOrder.CustLat = cust_loc.Geolocation.Latitude.ToString();
            customerOrder.CustLong = cust_loc.Geolocation.Longitude.ToString();
            customerOrder.CoopLat = coop_loc.Geolocation.Latitude.ToString();
            customerOrder.CoopLong = coop_loc.Geolocation.Longitude.ToString();

            if (model.ImageFile == null)
            {
                var user = User.Identity.GetUserId();
                var driver = db.DriverDetails.Where(d => d.UserId == user).FirstOrDefault();
                var coop = db.CoopDetails.Where(c => c.Id.ToString() == driver.CoopId).FirstOrDefault();
                var coopAdmin = db.CoopAdminDetails.Where(c => c.Coop_code.ToString() == driver.CoopId && c.IsResign == null).FirstOrDefault();
                var uorder = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
                var userDetails = db.UserDetails.Where(u => u.AccountId.ToString() == uorder.UserId).FirstOrDefault();
                var delivery2 = db.DeliverStatus.Where(x => x.UserOrderId.ToString() == id).FirstOrDefault();
                uorder.OStatus = "Complete";
                delivery2.Status = "Complete";
                delivery2.Proof = "deliverybox.png";
                delivery2.DateDelivered = DateTime.Now;

                db.Entry(uorder).State = EntityState.Modified;
                db.Entry(delivery2).State = EntityState.Modified;
                db.SaveChanges();

                var notif = new NotificationModel
                {
                    ToRole = "",
                    ToUser = userDetails.AccountId,
                    NotifFrom = user,
                    NotifHeader = "Order has been picked-up.",
                    NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                    NavigateURL = "OrderDetails/" + uorder.Id,
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
                    NotifHeader = "Order has been picked-up.",
                    NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                    NavigateURL = "OrderDetails/" + uorder.Id,
                    IsRead = false,
                    DateReceived = DateTime.Now
                };

                db.Notifications.Add(notif);
                db.SaveChanges();
                objNotifHub.SendNotification(notif.ToUser);

                return RedirectToAction("DeliveryList");
            }
            else
            {
                var allowedExtensions = new[] {
                        ".Jpg", ".png", ".jpg", "jpeg"
                    };
                string name = Path.GetFileNameWithoutExtension(model.ImageFile.FileName); //getting file name without extension  
                string extension = Path.GetExtension(model.ImageFile.FileName); //getting extension of the file
                if (allowedExtensions.Contains(extension))
                {
                    var user = User.Identity.GetUserId();
                    var driver = db.DriverDetails.Where(d => d.UserId == user).FirstOrDefault();
                    var coop = db.CoopDetails.Where(c => c.Id.ToString() == driver.CoopId).FirstOrDefault();
                    var coopAdmin = db.CoopAdminDetails.Where(c => c.Coop_code.ToString() == driver.CoopId && c.IsResign == null).FirstOrDefault();
                    var uorder = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
                    var userDetails = db.UserDetails.Where(u => u.AccountId.ToString() == uorder.UserId).FirstOrDefault();
                    var delivery2 = db.DeliverStatus.Where(x => x.UserOrderId.ToString() == id).FirstOrDefault();
                    uorder.OStatus = "Complete";
                    delivery2.Status = "Complete";
                    delivery2.Proof = name + extension;
                    delivery2.DateDelivered = DateTime.Now;
                    var myfile = name + extension;

                    var path = Path.Combine(Server.MapPath("~/Images/"), myfile);
                    db.Entry(uorder).State = EntityState.Modified;
                    db.Entry(delivery2).State = EntityState.Modified;
                    model.ImageFile.SaveAs(path);
                    db.SaveChanges();

                    var notif = new NotificationModel
                    {
                        ToRole = "",
                        ToUser = userDetails.AccountId,
                        NotifFrom = user,
                        NotifHeader = "Order has been picked-up.",
                        NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                        NavigateURL = "OrderDetails/" + uorder.Id,
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
                        NotifHeader = "Order has been picked-up.",
                        NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                        NavigateURL = "OrderDetails/" + uorder.Id,
                        IsRead = false,
                        DateReceived = DateTime.Now
                    };

                    db.Notifications.Add(notif);
                    db.SaveChanges();
                    objNotifHub.SendNotification(notif.ToUser);

                    return RedirectToAction("DeliveryList");
                }
                else
                {
                    ViewBag.ErrorMessage = "Please upload an image file.";
                }
            }
            return View(customerOrder);
        }

        public ActionResult PickupItem()
        {
            string id = Session["UOrderId"].ToString();
            Session["UOrderId"] = id;
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var driver = db.DriverDetails.Where(d => d.UserId == user).FirstOrDefault();
            var coop = db.CoopDetails.Where(c => c.Id.ToString() == driver.CoopId).FirstOrDefault();
            var coopAdmin = db.CoopAdminDetails.Where(c => c.Coop_code.ToString() == driver.CoopId && c.IsResign == null).FirstOrDefault();
            var delivery2 = db.DeliverStatus.Where(x => x.UserOrderId.ToString() == id).FirstOrDefault();
            var uorder = db.UserOrders.Where(x => x.Id.ToString() == id).FirstOrDefault();
            var userDetails = db.UserDetails.Where(u => u.AccountId.ToString() == uorder.UserId).FirstOrDefault();
            uorder.OStatus = "To Be Delivered";
            delivery2.Status = "To Be Delivered";
            delivery2.PickUpSuccessDate = DateTime.Now;
            db.Entry(uorder).State = EntityState.Modified;
            db.Entry(delivery2).State = EntityState.Modified;
            db.SaveChanges();

            var notif = new NotificationModel
            {
                ToRole = "",
                ToUser = userDetails.AccountId,
                NotifFrom = user,
                NotifHeader = "Order has been picked-up.",
                NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                NavigateURL = "OrderDetails/" + uorder.Id,
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
                NotifHeader = "Order has been picked-up.",
                NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " has been picked up",
                NavigateURL = "OrderDetails/" + uorder.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            objNotifHub.SendNotification(notif.ToUser);

            return RedirectToAction("DeliveryDetails", new { id = id });
        }

        public ActionResult ToBeReturned(int? id)
        {
            var db = new ApplicationDbContext();
            var reason = Request.Form["Reason"];

            if (reason == "" || reason == null)
            {
                return RedirectToAction("DeliveryDetails", new { id = id, message = "Kindly choose a reason as it is required." });
            }

            var user = User.Identity.GetUserId();
            var driver = db.DriverDetails.Where(d => d.UserId == user).FirstOrDefault();
            var uorder = db.UserOrders.Where(x => x.Id == id).FirstOrDefault();
            var userDetails = db.UserDetails.Where(u => u.AccountId.ToString() == uorder.UserId).FirstOrDefault();
            var delivery = db.DeliverStatus.Where(x => x.UserOrderId == id).FirstOrDefault();
            var coop = db.CoopDetails.Where(c => c.Id.ToString() == driver.CoopId).FirstOrDefault();
            var coopAdmin = db.CoopAdminDetails.Where(c => c.Coop_code.ToString() == driver.CoopId && c.IsResign == null).FirstOrDefault();
            uorder.OStatus = "To Be Returned";
            delivery.Status = "To Be Returned";
            delivery.ReturnedReason = reason;
            delivery.DateDelivered = DateTime.Now;

            db.Entry(uorder).State = EntityState.Modified;
            db.Entry(delivery).State = EntityState.Modified;
            db.SaveChanges();

            var notif = new NotificationModel
            {
                ToRole = userDetails.Role,
                ToUser = userDetails.AccountId,
                NotifFrom = user,
                NotifHeader = "Order is set to be returned.",
                NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " is set to be returned due to " + reason + ".",
                NavigateURL = "OrderDetails/" + uorder.Id,
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
                NotifHeader = "Order is set to be returned.",
                NotifMessage = "Order " + uorder.Id + "from " + coop.CoopName + " is set to be returned due to " + reason + ".",
                NavigateURL = "OrderDetails/" + uorder.Id,
                IsRead = false,
                DateReceived = DateTime.Now
            };

            db.Notifications.Add(notif);
            db.SaveChanges();
            objNotifHub.SendNotification(notif.ToUser);
            return View();
        }

        public ActionResult DeliveryHistory()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var delivery = db.DeliverStatus.Where(x => x.DriverId == user && x.Status == "Complete").ToList();
            List<DeliveryListDisplay> deliveries = new List<DeliveryListDisplay>();
            var delivList = (from del in db.DeliverStatus
                             join uorder in db.UserOrders
                             on del.UserOrderId equals uorder.Id
                             join user2 in db.UserDetails
                             on uorder.UserId equals user2.AccountId
                             where del.Status == "Complete" &&
                             del.DriverId == user
                             select new
                             {
                                 OrderNo = uorder.Id,
                                 CoopId = uorder.CoopId,
                                 Customer = user2.Firstname + " " + user2.Lastname,
                                 Deliveryfee = uorder.Delivery_fee,
                                 CustomerId = user2.AccountId,
                                 DateDelivered = del.DateDelivered,
                                 Total = uorder.TotalPrice
                             }).ToList();
            decimal total = 0;
            foreach (var order in delivList)
            {
                var prodorder = db.ProdOrders.Where(x => x.UOrderId == order.OrderNo.ToString()).ToList();
                foreach (var prod in prodorder)
                {
                    if (order.OrderNo.ToString() == prod.UOrderId)
                    {
                        total += prod.SubTotal;
                    }
                }
                decimal delfee = decimal.Parse(order.Deliveryfee.ToString());
                deliveries.Add(new DeliveryListDisplay { OrderNo = order.OrderNo.ToString(), Customer = order.Customer, TotalAmount = order.Total + delfee, CustomerId = order.CustomerId, DateDelivered = Convert.ToDateTime(order.DateDelivered) });
            }
            var riderinfo = db.DriverDetails.Where(x => x.UserId == user).FirstOrDefault();
            var getcoop = db.CoopAdminDetails.Where(x => x.UserId == riderinfo.CoopId).FirstOrDefault();

            return View(deliveries);
        }

        public ActionResult ViewNotification()
        {
            var db = new ApplicationDbContext();
            var user = User.Identity.GetUserId();
            var model = new ViewNotification();
            List<NotificationModel> unreadNotif = new List<NotificationModel>();
            List<NotificationModel> readNotif = new List<NotificationModel>();
            var unreadNotifications = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Driver") && n.IsRead == false).OrderByDescending(x => x.Id).ToList();
            var readNotifications = db.Notifications.Where(n => (n.ToUser == user || n.ToRole == "Driver") && n.IsRead == true).OrderByDescending(x => x.Id).ToList();

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
    }
}