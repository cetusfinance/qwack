using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDna.Integration;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;

namespace Qwack.Excel
{
    
    public static class Login
    {
        /// <summary>
        /// B2C tenant name
        /// </summary>
        private static readonly string TenantName = "fabrikamb2c";
        private static readonly string Tenant = $"{TenantName}.onmicrosoft.com";
        private static readonly string AzureAdB2CHostname = $"{TenantName}.b2clogin.com";
        public static IPublicClientApplication PublicClientApp;
        private static readonly string ClientId = "841e1190-d73a-450c-9d68-f5cf16b78e81";
        // Shouldn't need to change these:
        private static string AuthorityBase = $"https://{AzureAdB2CHostname}/tfp/{Tenant}/";
        /// <summary>
        /// From Azure AD B2C / UserFlows blade
        /// </summary>
        public static string PolicySignUpSignIn = "b2c_1_susi";
        public static string PolicyEditProfile = "b2c_1_edit_profile";
        public static string PolicyResetPassword = "b2c_1_reset";
        public static string[] ApiScopes = { "https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read" };
        public static string AuthoritySignUpSignIn = $"{AuthorityBase}{PolicySignUpSignIn}";
        /// <summary>
        /// Should be one of the choices on the Azure AD B2c / [This App] / Authentication blade
        /// </summary>
        private static readonly string RedirectUri = "https://fabrikamb2c.b2clogin.com/oauth2/nativeclient";

        private static JObject _userInfo;
        private static AuthenticationResult _result;

        [ExcelFunction]
        public static string LoginPltfm()
        {
            if (_result == null)
            {
                PublicClientApp = PublicClientApplicationBuilder.Create(ClientId)
                    .WithB2CAuthority(AuthoritySignUpSignIn)
                    .WithRedirectUri(RedirectUri)
                    .Build();

                _result = PublicClientApp.AcquireTokenInteractive(ApiScopes)
                    .ExecuteAsync().Result;
            }

            var token = _result.IdToken.Split('.')[1];
            token = Base64UrlDecode(token);
            var jobject = JObject.Parse(token);

            return $"Name : {jobject["name"]?.ToString()}, User Id {jobject["oid"]}";
        }

        private static string Base64UrlDecode(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            var byteArray = Convert.FromBase64String(s);
            var decoded = Encoding.UTF8.GetString(byteArray, 0, byteArray.Count());
            return decoded;
        }

        [ExcelFunction]
        public static string LogoutPltfm()
        {
            _result = null;
            return "logged out";
        }
    }
}
