using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using DotNetNuke.Common;
using DotNetNuke.Entities.Portals;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using DotNetNuke.Common.Utilities;
using System.Security.Cryptography;

namespace OS_Adyen
{
    public class ProviderUtils
    {

        public static NBrightInfo GetProviderSettings()
        {
            var objCtrl = new NBrightBuyController();
            var info = objCtrl.GetPluginSinglePageData("OS_Adyenpayment", "OS_AdyenPAYMENT", Utils.GetCurrentCulture());
            return info;
        }


        public static String GetBankRemotePost(OrderData orderData)
        {
            var rPost = new RemotePost();

            var objCtrl = new NBrightBuyController();
            var info = objCtrl.GetPluginSinglePageData("OS_Adyenpayment", "OS_AdyenPAYMENT", orderData.Lang);

            /**
             * General HPP settings
             * - hppUrl: URL of the Adyen HPP to submit the form to
             * - hmacKey: shared secret key used to encrypt the signature
             *
             * Both variables are dependent on the environment which should be used (Test/Live).
             * HMAC key can be set up: Adyen CA >> Skins >> Choose your Skin >> Edit Tab >> Edit HMAC key for Test & Live.
             */

            string hppUrl = info.GetXmlProperty("genxml/textbox/testurl");
            string hmacKey = info.GetXmlProperty("genxml/textbox/testhmac");
            if (!info.GetXmlPropertyBool("genxml/checkbox/testmode"))
            {
                hppUrl = info.GetXmlProperty("genxml/textbox/liveurl");
                hmacKey = info.GetXmlProperty("genxml/textbox/livehmac");
            }


             /**
             * Defining variables
             * The HPP requires certain variables to be posted in order to create a payment possibility for the shopper.
             *
             * The variables that you can post to the HPP are the following:
             *
             * merchantReference    : Your reference for this payment.
             * paymentAmount        : The transaction amount in minor units (e.g. EUR 1,00 = 100).
             * currencyCode         : The three character ISO currency code.
             * shipBeforeDate       : The date by which the goods or services specifed in the order must be shipped.
             *                        Format: YYYY-MM-DD
             * skinCode             : The code of the skin to be used for the payment.
             * merchantAccount      : The merchant account for which you want to process the payment.
             * sessionValidity      : The time by which a payment needs to have been made.
             *                        Format: YYYY-MM-DDThh:mm:ssTZD
             * shopperLocale        : A combination of language code and country code used to specify the language to be
             *                        used in the payment session (e.g. en_GB).
             * countryCode          : Country code according to ISO_3166-1_alpha-2 standard. (optional)
             * shopperEmail         : The shopper's email address. (recommended)
             * shopperReference     : An ID that uniquely identifes the shopper, such as a customer id. (recommended)
             * allowedMethods       : A comma-separated list of allowed payment methods, i.e. "ideal,mc,visa". (optional)
             * blockedMethods       : A comma-separated list of blocked payment methods, i.e. "ideal,mc,visa". (optional)
             * offset               : An integer that is added to the normal fraud score. (optional)
             * merchantSig          : The HMAC signature used by Adyen to test the validy of the form.
             */
            //string merchantReference =  "PAYMENT-" + DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            string merchantReference = orderData.PurchaseInfo.ItemID.ToString() + "_" + DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss");
            string paymentAmount = orderData.PurchaseInfo.GetXmlProperty("genxml/appliedtotal").Replace(".", "").Replace(",", "");
            string currencyCode = info.GetXmlProperty("genxml/textbox/currencycode");
            string shipBeforeDate = DateTime.Now.AddDays(3).ToString("yyyy-MM-dd");
            string skinCode = info.GetXmlProperty("genxml/textbox/skincode");
            string merchantAccount = info.GetXmlProperty("genxml/textbox/merchantaccount");
            string sessionValidity = DateTime.Now.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssK");
            string shopperLocale = Utils.GetCurrentCulture();
            //string orderData = CompressString("Orderdata to display on the HPP can be put here");
            string countryCode = orderData.PurchaseInfo.GetXmlProperty("genxml/billaddress/genxml/dropdownlist/country");
            string shopperEmail = orderData.PurchaseInfo.GetXmlProperty("genxml/billaddress/genxml/textbox/email");
            string shopperReference = orderData.PurchaseInfo.ItemID.ToString();
            string allowedMethods = info.GetXmlProperty("genxml/textbox/allowedmethods");
            string blockedMethods = info.GetXmlProperty("genxml/textbox/blockedmethods");
            string offset = info.GetXmlProperty("genxml/textbox/offset");

            /**
             * Signing the form
             *
             * The merchant signature is used by Adyen to verify if the posted data is not altered by the shopper. The
             * signature must be encrypted according to the procedure below.
             *
             * Please note: You will need to add all parameters that you post to this list in order for the signature to be correct.
             */
            Dictionary<string, string> paramlist = new Dictionary<string, string>();

            paramlist.Add("allowedMethods", allowedMethods);
            paramlist.Add("blockedMethods", blockedMethods);
            paramlist.Add("countryCode", countryCode);
            paramlist.Add("currencyCode", currencyCode);
            paramlist.Add("merchantAccount", merchantAccount);
            paramlist.Add("merchantReference", merchantReference);
            paramlist.Add("offset", offset);
            paramlist.Add("orderData", "");
            paramlist.Add("paymentAmount", paymentAmount);
            paramlist.Add("sessionValidity", sessionValidity);
            paramlist.Add("shipBeforeDate", shipBeforeDate);
            paramlist.Add("shopperEmail", shopperEmail);
            paramlist.Add("shopperLocale", shopperLocale);
            paramlist.Add("shopperReference", shopperReference);
            paramlist.Add("skinCode", skinCode);

            string signingString = BuildSigningString(paramlist);
            string merchantSig = CalculateHMAC(hmacKey, signingString);



            var url = new UriBuilder(hppUrl);
            var query = HttpUtility.ParseQueryString(url.Query, Encoding.UTF8);

            query["allowedMethods"] = allowedMethods;
            query["blockedMethods"] = blockedMethods;
            query["countryCode"] = countryCode;
            query["currencyCode"] = currencyCode;
            query["merchantAccount"] = merchantAccount;
            query["merchantReference"] = merchantReference;
            query["offset"] = offset;
            query["orderData"] = "";
            query["paymentAmount"] = paymentAmount;
            query["sessionValidity"] = sessionValidity;
            query["shipBeforeDate"] = shipBeforeDate;
            query["shopperEmail"] = shopperEmail;
            query["shopperLocale"] = shopperLocale;
            query["shopperReference"] = shopperReference;
            query["skinCode"] = skinCode;
            query["merchantSig"] = merchantSig;

            url.Query = query.ToString();
            string paymentUrl = url.ToString();

            rPost.Url = paymentUrl;


            //Build the re-direct html 
            var rtnStr = "";
            rtnStr = rPost.GetPostHtml();

            if (info.GetXmlPropertyBool("genxml/checkbox/debugmode"))
            {
                File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_OS_Adyenpost.html", rtnStr);
            }
            return rtnStr;
        }


        private static string EscapeVal(string val)
        {
            if (val == null)
            {
                return string.Empty;
            }

            val = val.Replace(@"\", @"\\");
            val = val.Replace(":", @"\:");
            return val;
        }

        private static string BuildSigningString(IDictionary<string, string> dict)
        {
            //Dictionary<string, string> signDict = dict.Where(x => x.Value != "").OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            Dictionary<string, string> signDict = dict.OrderBy(d => d.Key).ToDictionary(pair => pair.Key, pair => pair.Value);

            string keystring = string.Join(":", signDict.Keys);
            string valuestring = string.Join(":", signDict.Values.Select(EscapeVal));


            return string.Format("{0}:{1}", keystring, valuestring);
        }

        // Computes the Base64 encoded signature using the HMAC algorithm with the HMACSHA256 hashing function.
        private static string CalculateHMAC(string hmacKey, string signingstring)
        {
            byte[] key = PackH(hmacKey);
            byte[] data = Encoding.UTF8.GetBytes(signingstring);

            try
            {
                using (HMACSHA256 hmac = new HMACSHA256(key))
                {
                    // Compute the hmac on input data bytes
                    byte[] rawHmac = hmac.ComputeHash(data);

                    // Base64-encode the hmac
                    return Convert.ToBase64String(rawHmac);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to generate HMAC : " + e.Message);
            }
        }

        private static byte[] PackH(string hex)
        {
            if ((hex.Length % 2) == 1)
            {
                hex += '0';
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }

    }
}
