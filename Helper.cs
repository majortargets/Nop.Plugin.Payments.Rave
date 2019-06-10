using Nop.Core.Domain.Payments;
using System.Security.Cryptography;
using System.Text;

namespace Nop.Plugin.Payments.Rave
{
    /// <summary>
    /// Represents Rave helper
    /// </summary>
    public class Helper
    {
        #region Properties       

        #endregion

        #region Methods
        public static string GetRaveUrl()
        {

            return "https://api.ravepay.co/flwv3-pug/getpaidx/api/v2/hosted/pay";

        }

        public string GenerateSHA256String(string Amount, string country, string currency, string custom_description, string custom_logo,
             string custom_title, string customer_email, string customer_firstname, string customer_lastname, string customer_phone,
              string txref, string redirect_url, string PubKey, string SecretKey)
        {
            
            var hashh = PubKey + Amount + country + currency + custom_description + custom_logo + custom_title + customer_email
                    + customer_firstname + customer_lastname + customer_phone  + txref + redirect_url;
            var key = hashh + SecretKey;

            SHA256 sha256 = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(key);
            byte[] hash = sha256.ComputeHash(bytes);
            return GetStringFromHash(hash);
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
        #endregion
    }
}