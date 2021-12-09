
namespace PCartWeb.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using System;
    using System.Configuration;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using PCartWeb.Models;
    internal sealed class Configuration : DbMigrationsConfiguration<PCartWeb.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(PCartWeb.Models.ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            // In Startup iam creating first Admin Role and creating a default Admin User     
            if (!roleManager.RoleExists("Admin"))
            {

                // first we create Admin rool    
                var role = new IdentityRole();
                role.Name = "Admin";
                roleManager.Create(role);

                //Here we create a Admin super user who will maintain the website                   

                var user = new ApplicationUser();
                user.UserName = "PCartTeam@gmail.com";
                user.Email = "PCartTeam@gmail.com";

                string userPWD = "PcartTeam@2021";

                var chkUser = UserManager.Create(user, userPWD);

                //Here we create the super admin ewallet
                var adminEwallet = new EWallet();
                adminEwallet.UserID = user.Id;
                adminEwallet.Balance = 0;
                adminEwallet.Status = "Active";
                adminEwallet.Created_At = DateTime.Now;
                context.UserEWallet.Add(adminEwallet);
                context.SaveChanges();

                //Add default User to Role Admin    
                if (chkUser.Succeeded)
                {
                    var result1 = UserManager.AddToRole(user.Id, "Admin");
                }
            }

            // creating Creating Coop Admin role     
            if (!roleManager.RoleExists("Coop Admin"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Coop Admin";
                roleManager.Create(role);
            }

            // creating Creating Non-member role     
            if (!roleManager.RoleExists("Non-member"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Non-member";
                roleManager.Create(role);
            }

            // creating Creating Member role     
            if (!roleManager.RoleExists("Member"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Member";
                roleManager.Create(role);
            }

            // creating Creating Member role     
            if (!roleManager.RoleExists("Driver"))
            {
                var role = new Microsoft.AspNet.Identity.EntityFramework.IdentityRole();
                role.Name = "Driver";
                roleManager.Create(role);
            }
        }
    }
}