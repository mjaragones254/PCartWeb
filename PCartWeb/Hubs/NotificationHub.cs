using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using PCartWeb.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace PCartWeb.Hubs
{
    [HubName("notificationHub")]
    public class NotificationHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserHubModels> Users =
        new ConcurrentDictionary<string, UserHubModels>(StringComparer.InvariantCultureIgnoreCase);

        private ApplicationDbContext context = new ApplicationDbContext();

        //Logged Use Call  
        public void GetNotification()
        {
            try
            {
                string loggedUser = Context.User.Identity.GetUserId();

                if (loggedUser != null)
                {
                    //Get TotalNotification
                    string totalNotif = LoadNotifData(loggedUser, "");

                    //Send To
                    UserHubModels receiver;
                    if (Users.TryGetValue(loggedUser, out receiver))
                    {
                        var cid = receiver.ConnectionIds.FirstOrDefault();
                        IHubContext context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                        context.Clients.Client(cid).broadcastNotif(totalNotif);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        //Specific User Call
        public void SendNotification(string SentTo)
        {
            try
            {
                if (SentTo == "Member")
                {
                    var members = context.UserDetails.Where(x => x.Role == SentTo).ToList();
                    foreach (var mem in members)
                    {
                        var id = mem.AccountId;
                        string totalNotif = LoadNotifData(id, SentTo);

                        //Send To
                        UserHubModels receiver;
                        if (Users.TryGetValue(id, out receiver))
                        {
                            var cid = receiver.ConnectionIds.FirstOrDefault();
                            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                            context.Clients.Client(cid).broadcastNotif(totalNotif);
                        }
                    }
                }
                else if (SentTo == "Non-member")
                {
                    var nonmembers = context.UserDetails.Where(x => x.Role == SentTo).ToList();
                    foreach (var nonmem in nonmembers)
                    {
                        var id = nonmem.AccountId;
                        string totalNotif = LoadNotifData(id, SentTo);

                        //Send To
                        UserHubModels receiver;
                        if (Users.TryGetValue(id, out receiver))
                        {
                            var cid = receiver.ConnectionIds.FirstOrDefault();
                            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                            context.Clients.Client(cid).broadcastNotif(totalNotif);
                        }
                    }
                }
                else if (SentTo == "Coop Admin")
                {
                    var coopAdmins = context.CoopAdminDetails.Where(x => x.IsResign == null).ToList();
                    foreach (var coodAdmin in coopAdmins)
                    {
                        var id = coodAdmin.UserId;
                        string totalNotif = LoadNotifData(id, SentTo);

                        //Send To
                        UserHubModels receiver;
                        if (Users.TryGetValue(id, out receiver))
                        {
                            var cid = receiver.ConnectionIds.FirstOrDefault();
                            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                            context.Clients.Client(cid).broadcastNotif(totalNotif);
                        }
                    }
                }
                else
                {
                    //Get TotalNotification
                    string totalNotif = LoadNotifData(SentTo, "");

                    //Send To
                    UserHubModels receiver;
                    if (Users.TryGetValue(SentTo, out receiver))
                    {
                        var cid = receiver.ConnectionIds.FirstOrDefault();
                        var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
                        context.Clients.Client(cid).broadcastNotif(totalNotif);
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToString();
            }
        }

        private string LoadNotifData(string userId, string role)
        {
            int total = 0;
            if (role != "")
            {
                var query = (from t in context.Notifications
                             where (t.ToUser == userId || t.ToRole == role) && t.IsRead == false
                             select t)
                        .ToList();
                total = query.Count;
            }
            else
            {
                var query = (from t in context.Notifications
                             where t.ToUser == userId && t.IsRead == false
                             select t)
                        .ToList();
                total = query.Count;
            }

            return total.ToString();
        }

        public override Task OnConnected()
        {
            string userName = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;

            if (userName != null)
            {
                var user = Users.GetOrAdd(userName, _ => new UserHubModels
                {
                    UserName = userName,
                    ConnectionIds = new HashSet<string>()
                });

                lock (user.ConnectionIds)
                {
                    user.ConnectionIds.Add(connectionId);
                    if (user.ConnectionIds.Count == 1)
                    {
                        Clients.Others.userConnected(userName);
                    }
                }
            }

            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userName = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;

            if (userName != null)
            {
                UserHubModels user;
                Users.TryGetValue(userName, out user);

                if (user != null)
                {
                    lock (user.ConnectionIds)
                    {
                        user.ConnectionIds.RemoveWhere(cid => cid.Equals(connectionId));
                        if (!user.ConnectionIds.Any())
                        {
                            UserHubModels removedUser;
                            Users.TryRemove(userName, out removedUser);
                            Clients.Others.userDisconnected(userName);
                        }
                    }
                }
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}