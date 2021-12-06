using System;
using PayPal.Api;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCartWeb.Models
{
    public static class PaypalConfiguration
    {
        //Variables for storing the clientID and clientSecret key  
        public readonly static string ClientId;
        public readonly static string ClientSecret;
        //Constructor  
        static PaypalConfiguration()
        {
            var config = GetConfig();
            ClientId = "AX7i7vzGTTGuwlPM-xhcfLixmS5kFmRWNKvkVQ-ARuC5mccAob2scwWYNZAzG6legRpUKEIEqa15h1jn";
            ClientSecret = "ECAjaVsz7_IAWhTjbaYFrTMhGIhdzq6116ZjnnRSEIoo2JzKnO6RfISM1RFyOjRpQtkPg4eXO8IN7HFv";
        }
        // getting properties from the web.config  
        public static Dictionary<string, string> GetConfig()
        {
            return PayPal.Api.ConfigManager.Instance.GetProperties();
        }
        private static string GetAccessToken()
        {
            // getting accesstocken from paypal               
            string accessToken = new OAuthTokenCredential
                    (ClientId, ClientSecret, GetConfig()).GetAccessToken();
            return accessToken;
        }
        public static APIContext GetAPIContext()
        {
            // return apicontext object by invoking it with the accesstoken
            APIContext apiContext = new APIContext(GetAccessToken());
            apiContext.Config = GetConfig();
            return apiContext;
        }
    }

    public class PaypalUrl
    {
        public string url { get; set; }
    }
}