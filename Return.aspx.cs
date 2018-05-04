// --- Copyright (c) notice NevoWeb ---
//  Copyright (c) 2014 SARL NevoWeb.  www.nevoweb.com. The MIT License (MIT).
// Author: D.C.Lee
// ------------------------------------------------------------------------
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
// TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
// THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// ------------------------------------------------------------------------
// This copyright notice may NOT be removed, obscured or modified without written consent from the author.
// --- End copyright notice --- 

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework;
using NBrightCore.common;
using NBrightCore.render;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Admin;
using Nevoweb.DNN.NBrightBuy.Components;
using Nevoweb.DNN.NBrightBuy.Components.Interfaces;
using System.Web.UI;
using System.IO;

namespace OS_Adyen.OpenStore
{
    /// -----------------------------------------------------------------------------
    /// <summary>
    /// The ViewNBrightGen class displays the content
    /// </summary>
    /// -----------------------------------------------------------------------------
    public partial class Return : CDefault
    {
        #region Event Handlers

        override protected void OnInit(EventArgs e)
        {
            base.OnInit(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                var modCtrl = new NBrightBuyController();
                var info = modCtrl.GetPluginSinglePageData("OS_Adyenpayment", "OS_AdyenPAYMENT", Utils.GetCurrentCulture());

                if (info.GetXmlPropertyBool("genxml/checkbox/debugmode"))
                {
                    File.WriteAllText(PortalSettings.Current.HomeDirectoryMapPath + "\\debug_OS_Adyenreturn.html", HttpContext.Current.Request.Url.AbsoluteUri);
                }

                var orderid =  Utils.RequestQueryStringParam(HttpContext.Current, "merchantReference");
                if (orderid != "")
                {
                    orderid = orderid.Split('_')[0];
                    var param = "?orderid=" + orderid;
                    var lang = Utils.RequestQueryStringParam(HttpContext.Current, "shopperLocale");
                    if (lang != "")
                    {
                        param += "&language=" + lang;
                    }
                    var authResult = Utils.RequestQueryStringParam(HttpContext.Current, "authResult");

                    switch (authResult)
                    {
                        case "AUTHORISED":
                            param += "&status=1";
                            break;
                        case "CANCELLED":
                            param += "&status=0";
                            break;
                        case "REFUSED":
                            param += "&status=0";
                            break;
                        case "PENDING":
                            param += "&status=0";
                            break;
                        case "ERROR":
                            param += "&status=0";
                            break;
                    }

                    Response.Redirect(info.GetXmlProperty("genxml/textbox/paymentreturn") + param);

                }

            }
            catch (Exception exc) //Module failed to load
            {
                //display the error on the template (don;t want to log it here, prefer to deal with errors directly.)
                var l = new Literal();
                l.Text = exc.ToString();
                phData.Controls.Add(l);
            }
        }

        #endregion
    }

}