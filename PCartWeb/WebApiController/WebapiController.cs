using PCartWeb.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PCartWeb.WebApiController
{
    [Route("api/{controller}")]
    public class WebapiController : ApiController
    {
        [Route("api/GetProducts")]
        public IEnumerable<ProductDisplayModel> GetProducts()
        {
            var db = new ApplicationDbContext();
            List<ProductDisplayModel> prodlist = new List<ProductDisplayModel>();

            var getprod = db.ProductDetails.ToList();
            
            foreach(var item in getprod)
            {
                var getprice = db.Prices.Where(x => x.ProdId == item.Id).OrderByDescending(p => p.Id).FirstOrDefault();

                prodlist.Add(new ProductDisplayModel
                {
                    Id = item.Id,
                    Categoryname = item.Categoryname,
                    CoopId = item.CoopId.ToString(),
                    Product_price = getprice.Price,
                    ExpiryDate = item.ExpiryDate,
                    DiscountedPrice = item.DiscountedPrice,
                    Product_qty = item.Product_qty,
                    Product_image = item.Product_image
                }) ;
            }
            return prodlist;
        }


        [Route("api/Get")]
        public IEnumerable<UserViewModel> Get()
        {
            var db = new ApplicationDbContext();
            var person = (from u in db.UserDetails
                          join user in db.Users
                          on u.AccountId equals user.Id
                          where u.Role == "Member"
                          select new UserViewModel
                          {
                              Firstname = u.Firstname,
                              Lastname = u.Lastname,
                              Id = u.Id,
                              AccountId = u.AccountId,
                              Email = user.Email,
                              Created_at = u.Created_at.ToString(),
                              Updated_at = u.Updated_at.ToString(),
                              Role = u.Role
                          }).ToList();
            return person;
        }

        [HttpPost]
        [Route("api/GetOrderList/")]
        public HttpResponseMessage GetOrderList(UserViewModel model)
        {
            var db = new ApplicationDbContext();
            var getuser = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            var driverinfo = db.DriverDetails.Where(x => x.UserId == getuser.Id).FirstOrDefault();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("api/GetUserRole/")]
        public HttpResponseMessage GetUserRole(UserViewModel model)
        {
            UserViewModel userView = new UserViewModel();
            var db = new ApplicationDbContext();
            var user = db.Users.Where(x => x.Email == model.Email).FirstOrDefault();
            var getinfo = db.UserDetails.Where(x => x.AccountId == user.Id).FirstOrDefault();
            var userrole = user.Roles.Where(x => x.UserId == user.Id).FirstOrDefault();
            var getuserrole = db.Roles.Where(x => x.Id == userrole.RoleId).FirstOrDefault();

            if (getinfo != null)
            {
                model.Address = getinfo.Address;
                model.Contact = getinfo.Contact;
                model.Firstname = getinfo.Firstname;
                model.Lastname = getinfo.Lastname;
                model.Image = getinfo.Image;
                model.Role = getuserrole.Name;
                model.Created_at = getinfo.Created_at.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            var driverinfo = db.DriverDetails.Where(x => x.UserId == user.Id).FirstOrDefault();
            if (driverinfo != null)
            {
                var getrole = user.Roles.Where(x => x.UserId == driverinfo.UserId).FirstOrDefault();
                var roleget = db.Roles.Where(x => x.Id == getrole.RoleId).FirstOrDefault();
                if (getrole != null)
                {
                    model.Role = roleget.Name;
                    model.Address = driverinfo.Address;
                    model.Contact = driverinfo.Contact;
                    model.Firstname = driverinfo.Firstname;
                    model.Lastname = driverinfo.Lastname;
                    model.Image = driverinfo.Image;
                    model.Created_at = driverinfo.Created_at.ToString();
                    return Request.CreateResponse(HttpStatusCode.OK, model);
                }
            }
            else
            {
                model.Role = getuserrole.Name;
                return Request.CreateResponse(HttpStatusCode.OK, model);
            }
            return Request.CreateResponse(HttpStatusCode.NotFound);
        }

        [Route("api/GetCategory")]
        public IEnumerable<CategoryDetailsModel> GetCategory()
        {
            var db = new ApplicationDbContext();
            return db.CategoryDetails.AsNoTracking().ToList();
        }

        [HttpPost]
        [Route("api/PostCategory")]
        public IHttpActionResult PostCategory(CategoryDetailsModel model)
        {
            using(var db = new ApplicationDbContext())
            {
                model.Created_at = DateTime.Now;
                model.Updated_at = DateTime.Now;
                db.CategoryDetails.Add(model);
                db.SaveChanges();
            }
            return Ok();
        }

        [HttpGet]
        [Route("api/GetProd/{id}")]
        public ProductDetailsModel GetProd(int id)
        {
            ProductDetailsModel model = new ProductDetailsModel();
            var db = new ApplicationDbContext();
            var getprice = db.Prices.Where(x => x.ProdId == id).OrderByDescending(p => p.Id).FirstOrDefault();
            model = db.ProductDetails.Where(x => x.Id == id).FirstOrDefault();
            return model;
        }

        [HttpGet]
        [Route("api/GetCat/{id}")]
        public CategoryDetailsModel GetCat(int id)
        {
            using(var db = new ApplicationDbContext())
            {
                CategoryDetailsModel model = new CategoryDetailsModel();
                model = db.CategoryDetails.Where(p => p.Id == id).FirstOrDefault();
                return model;
            }
        }

        [HttpPut]
        [Route("api/UpdateCategory/{id}")]
        public IHttpActionResult UpdateCategory(CategoryDetailsModel model)
        {
            using(var db = new ApplicationDbContext())
            {
                var existing = db.CategoryDetails.Where(p => p.Id == model.Id).FirstOrDefault();

                if(existing != null)
                {
                    existing.Name = model.Name;
                    existing.Description = model.Description;
                    existing.Updated_at = DateTime.Now;

                    db.Entry(existing).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }
            return Ok();
        }
    }
}
