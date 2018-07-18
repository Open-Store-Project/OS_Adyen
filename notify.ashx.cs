using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using NBrightCore.common;
using Nevoweb.DNN.NBrightBuy.Components;

namespace OS_Adyen.OpenStore
{
    /// <summary>
    /// Summary description for XMLconnector
    /// </summary>
    public class OS_AdyenNotify : IHttpHandler
    {
        private String _lang = "";

        /// <summary>
        /// This function needs to process and returned message from the bank.
        /// This processing may vary widely between banks.
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            var modCtrl = new NBrightBuyController();
            var info = modCtrl.GetPluginSinglePageData("OS_Adyenpayment", "OS_AdyenPAYMENT", Utils.GetCurrentCulture());

            var debugMode = info.GetXmlPropertyBool("genxml/checkbox/debugmode");
            var debugMsg = "START CALL" + DateTime.Now.ToString("s") + " </br>";
            var rtnMsg = "";

            info.SetXmlProperty("genxml/debugmsg", "");
            modCtrl.Update(info);


            string hmacKey = info.GetXmlProperty("genxml/textbox/testhmac");
            if (!info.GetXmlPropertyBool("genxml/checkbox/testmode"))
            {
                hmacKey = info.GetXmlProperty("genxml/textbox/livehmac");
            }

            /**
             * Check authentication
             * 
             * We recommend you to secure your notification server. You can secure it using a username/password which can be
             * configured in the CA. The username and password will be available in the Authorization header of the request.
             * Alternatively, is to allow only traffic that comes from Adyen servers.
             */
            String notificationUser = info.GetXmlProperty("genxml/textbox/ipnuser");
            String notificationPassword = info.GetXmlProperty("genxml/textbox/ipnpass");

            string authHeader = context.Request.Headers["Authorization"];

            if (authHeader == null || authHeader == "")
            {
                debugMsg += " Unauthorized - NO 'Authorization' header, usually caused by username and password not being specified in Adyen Notification Admin. ";
                info.SetXmlProperty("genxml/debugmsg", debugMsg);
                modCtrl.Update(info);
                throw new HttpException(401, "Unauthorized");
            }
            else
            {
                string encodedAuth = authHeader.Split(' ')[1];
                string decodedAuth = Encoding.UTF8.GetString(Convert.FromBase64String(encodedAuth));

                var requestUser = decodedAuth.Split(':')[0];
                var requestPassword = decodedAuth.Split(':')[1];

                if (!notificationUser.Equals(requestUser) || !notificationPassword.Equals(requestPassword))
                {
                    debugMsg += " Unauthorized - username and password not matching between Adyen Notification Admin and OS Adyen Admin. ";
                    info.SetXmlProperty("genxml/debugmsg", debugMsg);
                    modCtrl.Update(info);
                    throw new HttpException(403, "Forbidden");
                }
            }

            int OS_AdyenStoreOrderID = 0;

            var orderid = context.Request.Form["merchantReference"];
            if (orderid != null)
            {
                orderid = orderid.Split('_')[0];
            }
            debugMsg += "orderid: " + orderid + "</br>";

            if (Utils.IsNumeric(orderid))
            {
                if (orderid.Length >= 9)
                {
                    orderid = orderid.Substring(0, 9); // substring for test IPN, adyen sends too large number for int32
                }
                OS_AdyenStoreOrderID = Convert.ToInt32(orderid); 

                debugMsg += "OrderId: " + orderid + " </br>";

                var orderData = new OrderData(OS_AdyenStoreOrderID);


                /**
                 * Handle notification
                 * 
                 * The following request parameters are available (see Integration Manual):
                 * - live
                 * - eventCode
                 * - pspReference
                 * - originalReference
                 * - merchantReference
                 * - merchantAccountCode
                 * - eventDate
                 * - success
                 * - paymentMethod
                 * - operations
                 * - reason
                 * - currency
                 * 
                 * We recommend you to handle the notifications based on the eventCode types available, please refer to the
                 * integration manual for a comprehensive list. We also recommend you to save the notification itself.
                 */
                var rtnstatus = context.Request.Form["eventCode"];
                var success = context.Request.Form["success"];
                var reason = context.Request.Form["reason"];
                switch (rtnstatus)
                {
                    case "AUTHORISATION":
                        // Handle AUTHORISATION notification.
                        // Confirms whether the payment was authorised successfully.
                        // The authorisation is successful if the "success" field has the value true.
                        // In case of an error or a refusal, it will be false and the "reason" field
                        // should be consulted for the cause of the authorisation failure.
                        if (success == "true")
                        {
                            orderData.PaymentOk();
                        }
                        else
                        {
                            orderData.PaymentFail();
                            orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        }
                        break;
                    case "CANCELLATION":
                        // Handle CANCELLATION notification.
                        // Confirms that the payment was cancelled successfully.
                        orderData.PaymentFail();
                        break;
                    case "REFUND":
                        // Handle REFUND notification.
                        // Confirms that the payment was refunded successfully.
                        orderData.PaymentFail();
                        break;
                    case "CANCEL_OR_REFUND":
                        // Handle CANCEL_OR_REFUND notification.
                        // Confirms that the payment was refunded or cancelled successfully.
                        orderData.PaymentFail();
                        break;
                    case "CAPTURE":
                        // Handle CAPTURE notification.
                        // Confirms that the payment was successfully captured.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "REFUNDED_REVERSED":
                        // Handle REFUNDED_REVERSED notification.
                        // Tells you that the refund for this payment was successfully reversed.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "CAPTURE_FAILED":
                        // Handle AUTHORISATION notification.
                        // Tells you that the capture on the authorised payment failed.
                        orderData.PaymentFail();
                        break;
                    case "REQUEST_FOR_INFORMATION":
                        // Handle REQUEST_FOR_INFORMATION notification.
                        // Information requested for this payment.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "NOTIFICATION_OF_CHARGEBACK":
                        // Handle NOTIFICATION_OF_CHARGEBACK notification.
                        // Chargeback is pending, but can still be defended.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "CHARGEBACK":
                        // Handle CHARGEBACK notification.
                        // Payment was charged back. This is not sent if a REQUEST_FOR_INFORMATION or NOTIFICATION_OF_CHARGEBACK
                        // notification has already been sent.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "CHARGEBACK_REVERSED":
                        // Handle CHARGEBACK_REVERSED notification.
                        // Chargeback has been reversed (cancelled).
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                    case "REPORT_AVAILABLE":
                        // Handle REPORT_AVAILABLE notification.
                        // There is a new report available, the URL of the report is in the "reason" field.
                        orderData.AddAuditMessage("Adyen IPN: " + reason, "paymsg", "notify.ashx", "False");
                        break;
                }
                orderData.Save();
            }

            // ------------------------------------------------------------------------
            // In this case the payment provider passes back data via form POST.
            // Get the data we need.
            if (debugMode)
            {
                debugMsg += "Return Message: " + rtnMsg;
                info.SetXmlProperty("genxml/debugmsg", debugMsg);
                modCtrl.Update(info);
            }
            sendAccepted();
        }

        private void sendAccepted()
        {
            HttpContext.Current.Response.Clear();
            HttpContext.Current.Response.Write("[accepted]");
            HttpContext.Current.Response.ContentType = "text/plain";
            HttpContext.Current.Response.CacheControl = "no-cache";
            HttpContext.Current.Response.Expires = -1;
            HttpContext.Current.Response.Flush();

        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

    }
}