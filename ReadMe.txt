Open Store Adyen Payment Gateway
================================

https://www.openstore-ecommerce.com

Adyen Setup
-----------

In your Adyen account you need to craete a skin and put the details in OpenStore BO>Admin>OS_Adyen.
Make the ResultURL setting https://****/DesktopModules/NBright/OS_Adyen/return.aspx

Create a notification in Adyen ("Standard Notification") that sends the result to the OpenStore IPN for "HTTP POST", "AUTHORISATION".
the IPN url is: https://****/DesktopModules/NBright/OS_Adyen/notify.ashx
The setting in Adyen NEED a username and password, put these in the OpenStore Adyen settings.

The return page in Adyen merchant account should be set to: https://****/DesktopModules/NBright/OS_Adyen/return.aspx



