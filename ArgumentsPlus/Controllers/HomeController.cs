// Paradigm Mentors videos, notes, and source code are provided without any assumed liability.
// Amateur and professional programmers are 100% responsible for how they apply our training content to their own projects.
// Amateur or professional, you need to test thoroughly to avoid unpleasant and unexpected surprises. 

// LICENSE:  Public Domain.  Authorization:  Gerry Lowry.  IANAL
// Hope:     -- it would be nice to be acknowleged for my contributions.
//           -- i will not lose sleep if you do not bother to acknowledge my contributions.
//           -- worth reading:  http://blog.codinghorror.com/pick-a-license-any-license/, Jeff Atwood 
//           -- please read lines 1, 2, and 3
// Buts:     -- obviously, other licences apply, partial list:
//           -- MICROSOFT PUBLIC LICENSE (Ms-PL) (ask Microsoft if you're not sure)
//           -- PayPal https://github.com/paypal/rest-api-sdk-dotnet/blob/master/LICENSE.txt
//           -- et alia

// NOTE:  there is code in the ASP.NET MVC 5 Controller that really belongs in the ASP.NET MVC 5 Model;
//        this was done to keep this example web application as simple as possible since its focus in on
//        implementing PayPal's REST API SDK.

// vs2013 free Web Express edition was used to develop and test this sample PayPal application.

// APPLICATION NAME:  Arguments Plus
// PURPOSE:           -- to demonstrate how to use the PayPal REST API SDK to bill e-commerce customers via PayPal.
// VERSION:           0.0.a    [treat this application as you would any alpha code]
// NOTES & LINKS:     -- https://g47.org/cash
// SOURCE CODE:       -- http://paradigmmentors.net//files/code/argumentsPlus/argumentsplus.exe ASP.NET MVC 5 Solution
//                    -- format:  self-extracting .zip
// SIGNED BY/AUTHOR:  -- Gerry Lowry
// SIGNED WITH:       -- COMODO Code Signing CA 2
// VALID FROM:        -- ‎2013 ‎June ‎11 ‎Tuesday  20:00:00
// VALID TO:          -- ‎2014 ‎June ‎12 ‎Thursday 19:59:59
// THUMBPRINT:        -- ‎31:f0:55:c1:75:a6:fa:5a:e8:fa:aa:ed:65:09:17:d1:a6:9a:f1:d8

// ENHANCEMENT NOTE:  for a PRODUCTION version of this code, YOU need to ensure
//                    that Exceptions are caught and handled appropriately.

// CAVEAT:  many of the try/catch blocks are incomplete ... the PayPal documentation
//          is not yet complete, so as best as possible, errors were identified
//          and coded around as they were encountered.

// N.B.:  one condition is more or less beyond your control:
//        when you redirect your customer to PayPal's website,
//        there is NO way to force your customer to complete her/his purchase at PayPal;
//        i.e., your customer may simply walk away.

// OVERVIEW:  this ASP.NET MVC 5 application sells arguments, either as singles, or 10 packs.
//            Customers pay for their purchase using PayPal.
//            The new-ish PayPal REST API SDK is used to interface with PayPal, an eBay company.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ArgumentsPlus.Controllers
{
    // N.B.:  the Entity Framework "Code First" code is
    //        described in our video 
    //        "Entity Framework:  Code First, A Very Brief Introduction"
    //        and in our notes at https://g47.org/cfbi/

    //==== Entity Framework 6 Code First "invoice"
    // 
    public class TinyInvoice  // POCO  Plain Old CLR Object 
    {                         // domain class for Code First
        public Int32  TinyInvoiceID            { get; set; }  // Code First requires a Primary Key
        public Guid   ReconnectToOurBuyer      { get; set; }  // this is our Secondary Key
        public String PayPalPayment_Identifier { get; set; }  // this is PayPal's "invoice id"
    }

    public class ArgumentPlusContext : System.Data.Entity.DbContext
    {
        public ArgumentPlusContext()              // need connection string in web.config
               : base("name=ArgumentPlusContext") // “name=<connection string name>”
        {                                          
        }
        public System.Data.Entity.DbSet<TinyInvoice> TinyInvoices { get; set; } // our collection of TinyInvoices
    }

    public class HomeController : Controller
    {
        // Note:  we ignore POSTs from PayPal because PayPal uses GETs;
        //        i.e., a POST is likely bogus.

        [HttpPost] // From PayPal, Request.RequestType == GET
        public ActionResult ArgumentsNotSold()                                                       
        {
            return View();
        } // this ArgumentsNotSold() override prevents the POST verb
        // ................................

        [HttpGet]  // From PayPal, Request.RequestType == GET 
        public ActionResult ArgumentsNotSold(String id,      // your Guid ID
                                             String token)   // PayPal's token
        // N.B.:  PayerID is NOT applicable for "cancelled" purchases.
        {
            ViewBag.Guid  = id;     // our Guid
            ViewBag.Token = token;  // PayPal's Token
            return View();
        } // this ArgumentsNotSold() override ALLOWS the GET verb
        // ................................


        [HttpPost] // From PayPal, Request.RequestType == GET
        public ActionResult ArgumentsSold() // PayPal only posts
        {
            return View();  // todo     best NOT to ignore this                     
        } // this ArgumentsSold() override prevents the POST verb
        // ................................

        [HttpGet]  // From PayPal, Request.RequestType == GET 
        public ActionResult ArgumentsSold(String id,      // your ID
                                          String token,   // PayPal token
                                          String PayerID) // PayPal's ID for the "invoice"
        {   
            String payment_id                   = String.Empty;
            String executeUrlFromCreatedPayment = String.Empty;
            // ................................
            //  ======>      // THINGS WE NEED TO USE THE PAYPAL REST API
            //  ======>      String argumentsPlusPayPalClentID = " ... your user ID for Arguments Plus ...";
            //  ======>      String argumentsPlusPayPalSecret  = " ... this is YOUR PASSWORD ...";
            String argumentsPlusPayPalClentID = "AU ... 6G"; // todo   ... your PayPal REST API SDK user ID
            String argumentsPlusPayPalSecret  = "EA ... -f"; // todo   ... your PayPal REST API SDK PASSWORD
            //  ======>      String payPalRestApiEndpoint      = "api.sandbox.paypal.com"; // For TESTING !!!
            //  ======>                                     // = "api.paypal.com";         // For LIVE    !!!
            Dictionary<String, String> sdkConfig = new Dictionary<String, String>();
            sdkConfig.Add("mode", "sandbox");
            // ................................
            // POTENTIAL POINT OF FAILURE
            // example:  "Exception in HttpConnection Execute: Invalid HTTP response The operation has timed out"
            String oAuthAccessTokenForPayPal = String.Empty; // scope outside of try/catch
            try
            {
                oAuthAccessTokenForPayPal = new PayPal.OAuthTokenCredential(argumentsPlusPayPalClentID,
                                                                            argumentsPlusPayPalSecret,
                                                                            sdkConfig).GetAccessToken();
            }                                                                      // .GetAccessToken takes us over to PayPal
            catch (Exception)
            {
                throw;  // todo ... make this code more robust
            }
            // ................................
            PayPal.APIContext apiContext = new PayPal.APIContext(oAuthAccessTokenForPayPal);
            apiContext.Config = sdkConfig;
            // ................................
            // id is our Guid for reconnecting to our customer's invoice
            PayPal.Api.Payments.Payment createdInvoice 
                       = GetSavedPayPalInvoice(id);
            // ................................
            // now we convert the approved invoice into an actual payment
            PayPal.Api.Payments.Payment payment
                       = new PayPal.Api.Payments.Payment();
            payment.id = createdInvoice.id; // use the previously returned "invoice" identifier 

            PayPal.Api.Payments.PaymentExecution askPayPalForMoney = new PayPal.Api.Payments.PaymentExecution();
            askPayPalForMoney.payer_id = PayerID; // from Query string ==> ?token=EC-3LN850372M719842K&PayerID=JJT6YSAZSFJTC
            PayPal.Api.Payments.Payment paidInvoice = null;  // scope outside of try/catch    
            try
            {   //  an APPROVED PAYMENT morphs into a SALE !!!!!!!!!!!!!!!!!!!!!
                paidInvoice = payment.Execute(apiContext, askPayPalForMoney);  // off to PayPal    
   		        // Here, if all has gone well, we're just right back from PayPal
                // and the money for the above invoice is now in our PayPal Merchant Account.
                ViewBag.PaidInvoice = paidInvoice;
         	}
            catch (PayPal.Exception.ConnectionException connectionExceptionPayPal)
            {
                if (String.Compare(connectionExceptionPayPal.Message,
                    "Invalid HTTP response The remote server returned an error: (400) Bad Request.")
                   == 0)
                {
                    String response_PAYMENT_STATE_INVALID =
                             "{\"name\":\"PAYMENT_STATE_INVALID\",\"message\":\"This request is invalid due to the current state of the payment\",\"information_link\":\"https://developer.paypal.com/webapps/developer/docs/api/#PAYMENT_STATE_INVALID\",\"debug_id\":\"";
                    Int32  response_PAYMENT_STATE_INVALID_Length = response_PAYMENT_STATE_INVALID.Length;
                    if (String.Compare(connectionExceptionPayPal.Response.Substring(0,response_PAYMENT_STATE_INVALID_Length), 
                                       response_PAYMENT_STATE_INVALID) == 0)
                    {
                        // todo Take appropriate action
                    }
                    else
                    {
                        // todo Take appropriate action
                    }
                }
                else
                {
                    // todo Take appropriate action
                }
                throw;        // todo complete this code ... for PRODUCTION, more robustness is a must!
            }
         	catch (Exception generalExceptionGettingMoney)
         	{
         		throw;  // todo ... deal with this error appropriately ("throw" is not appropriate)
         	}
            // ................................
            // PayPal has provided data -- we capture as much or as little as when require.
            // We can save the PayPal data and/or display it on our web page, et cetera.
            // Remember:  respect your customers' privacy.  Protect their data.

            String payment_state    = String.Empty;       // N.B.:  this is NOT the province/state/region
            payment_state           = paidInvoice.state;  // Possible Values:  pending, approved
            ViewBag.Payment_State   = payment_state;
                                   
            String payment_intent   = String.Empty;     
            payment_intent          = paidInvoice.intent;  // Expected Value:  "sale"
            ViewBag.Payment_Intent  = payment_intent;

            String payment_method   = String.Empty;
            payment_method          = paidInvoice.payer.payment_method;  // Expected Value:  "paypal"
            ViewBag.Payment_Method  = payment_method;
            
            String parent_payment = String.Empty;
            parent_payment          = paidInvoice.transactions[0].related_resources[0].sale.parent_payment;
            ViewBag.Parent_Payment  = parent_payment;
                                    
            String update_time      = String.Empty;
            update_time             = paidInvoice.update_time; // "2014-02-05T04:06:39Z"     string
            ViewBag.Update_Time     = update_time;
                                    
            String create_time      = String.Empty;
            create_time             = paidInvoice.create_time; // "2014-02-05T04:06:39Z"     string
            ViewBag.Create_Time     = create_time;
            
            // ................................
            String payPal_email_address  = String.Empty;  //  paidInvoice::email  "someone@somedomain.com"  string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.email))
                payPal_email_address     = paidInvoice.payer.payer_info.email;
            ViewBag.PayPal_Email_Address = payPal_email_address;

            String payPal_first_name     = String.Empty;  //  paidInvoice::first_name	"gerry"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.first_name))
                payPal_first_name        = paidInvoice.payer.payer_info.first_name;
            ViewBag.PayPal_First_Name    = payPal_first_name;
            
            String payPal_last_name      = String.Empty;  //  paidInvoice::last_name	"lowry"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.last_name))
                payPal_last_name         = paidInvoice.payer.payer_info.last_name;
            ViewBag.PayPal_Last_Name     = payPal_last_name;
            
            String payPal_phone  = String.Empty;  //  paidInvoice::phone null string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.phone))
                payPal_phone     = paidInvoice.payer.payer_info.phone;
            ViewBag.PayPal_Phone = payPal_phone;


            String payPal_city = String.Empty;  //  paidInvoice::city	"SAN Jose"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.city))
                payPal_city    = paidInvoice.payer.payer_info.shipping_address.city;
            ViewBag.City       = payPal_city;

            String payPal_country_code  = String.Empty;  //  paidInvoice::country_code	"US"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.country_code))
                payPal_country_code     = paidInvoice.payer.payer_info.shipping_address.country_code;
            ViewBag.PayPal_Country_Code = payPal_country_code;

            String payPal_address_line1  = String.Empty;  //  paidInvoice::line1	"1 Main St"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.line1))
                payPal_address_line1     = paidInvoice.payer.payer_info.shipping_address.line1;
            ViewBag.PayPal_Address_Line1 = payPal_address_line1;

            String payPal_address_line2  = String.Empty;  //  paidInvoice::line2	null	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.line2))
                payPal_address_line2     = paidInvoice.payer.payer_info.shipping_address.line2;
            ViewBag.PayPal_Address_Line2 = payPal_address_line2;

            String payPal_postal_code  = String.Empty;  //  paidInvoice::postal_code	"95131"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.postal_code))
                payPal_postal_code     = paidInvoice.payer.payer_info.shipping_address.postal_code;
            ViewBag.PayPal_Postal_Code = payPal_postal_code;

            String payPal_address_state  = String.Empty;  //  paidInvoice::state	"CA"	string
            if (!String.IsNullOrWhiteSpace(paidInvoice.payer.payer_info.shipping_address.state))
                payPal_address_state     = paidInvoice.payer.payer_info.shipping_address.state;
            ViewBag.PayPal_Address_State = payPal_address_state;

            // ................................
            String payPal_transaction_amount_total  = String.Empty;  //  paidInvoice::transation[0].amount" string
            if (!String.IsNullOrWhiteSpace(paidInvoice.transactions[0].amount.total))
                payPal_transaction_amount_total     = paidInvoice.transactions[0].amount.total;
            ViewBag.PayPal_Transaction_Amount_Total = payPal_transaction_amount_total;

            String payPal_transaction_amount_currency  = String.Empty;  //  paidInvoice::transation[0].amount" string
            if (!String.IsNullOrWhiteSpace(paidInvoice.transactions[0].amount.total))
                payPal_transaction_amount_currency     = paidInvoice.transactions[0].amount.currency;
            ViewBag.PayPal_Transaction_Amount_Currency = payPal_transaction_amount_currency;

            String payPal_transaction_descripition = String.Empty;  //  paidInvoice::transation[0].amount" string
            if (!String.IsNullOrWhiteSpace(paidInvoice.transactions[0].amount.total))
                payPal_transaction_descripition = paidInvoice.transactions[0].description;
            ViewBag.PayPal_Transaction_Description = payPal_transaction_descripition;
            // ................................
            return View();
        }

        private PayPal.Api.Payments.Payment GetSavedPayPalInvoice(string id)
        {
            Guid savedInvoiceKey = new Guid(); // will be all zeros
            try
            {
                savedInvoiceKey  = new Guid(id);  
            }
            catch (Exception)    // could happen; probability low
            {
                // todo for now ignore ==> TIMTOWTDI
            }
            // 
            PayPal.Api.Payments.Payment createdInvoice
                = new PayPal.Api.Payments.Payment(); // scope outside using
            // ................................
            using (ArgumentsPlus.Controllers.ArgumentPlusContext
             argumentsPlusDb
             = new ArgumentPlusContext())
            {
                var approvedPayPalPayment
                    = argumentsPlusDb.TinyInvoices// define the query
                     .Where(guid => guid.ReconnectToOurBuyer == savedInvoiceKey);
                var getOnePayPalPendingPayment = approvedPayPalPayment.ToList();
                          // .ToList() will force the query to hit the database
                Int32 numberOfInvoices = getOnePayPalPendingPayment.Count;
                                         // should be ZERO or ONE
                if (numberOfInvoices == 1)
                {
                    createdInvoice.id
                        = getOnePayPalPendingPayment[0].PayPalPayment_Identifier;
                } // if (numberOfInvoices == 1)
                else // should ALWAYS be ONE !!!!!!!!!
                {
                    throw new NotImplementedException();  // todo "well, this is embarrassing"
                }
            }
            return createdInvoice;
        }

        [HttpGet]
        public ActionResult ArgumentsPlus()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ArgumentsPlus(String singleOr10, String yourname)
        {
            if(String.IsNullOrWhiteSpace(singleOr10)) throw new Exception("something has gone wrong");
            String customer = "Arguments Plus customer";
            // this could cause us to exceed 127 character limit for PayPal's description field
            if (!String.IsNullOrWhiteSpace(yourname))
                // PayPal does not allow some characters in its description field
            {
                // todo ... a more complete substitution
                customer = yourname.Trim();
                customer = customer.Replace('č', 'c');  // Z. Bouček, Czech entomologist
                customer = customer.Replace('ø', 'o');  // Adam Bøving, Danish entomologist
                // without the two lines above, Bouček and/or Bøving
                // experience an "INTERNAL_SERVICE_ERROR"
//              customer = customer.Replace('&', '+');  // Ampersand is OKAY
            }
                
            String productDescription = String.Empty;
            Int32 productCost         = 0;
            if(String.Compare(singleOr10, "5") == 0)
            {
                productCost = 1;
                productDescription = "five minute argument (Quantity 1)";
            }
            else if(String.Compare(singleOr10, "50") == 0)
            {
                productCost = 8;
                productDescription = "five minute arguments (Quantity 10)";
            }
            else throw new Exception("unexpected value ");
            String redirect_approvalUrlFromCreatedPayment = String.Empty;
            Int32 payPalSuccessLevel = AndNowItIsTimeToTalkToPayPal(productCost,
                                                                    productDescription,
                                                                    customer,
                                     out redirect_approvalUrlFromCreatedPayment);
            if (payPalSuccessLevel == 100)
                // if the "invoice" was created successfully, we re-direct to PayPal.
                // PayPal will give our arugument buyer customer
                // the opportunity to approve or reject this "invoice".
                // (a) rejected invoices come back to our Arguments Plus website
                //     via redirUrls.cancel_url
                // (b) approved invoices come back to our Arguments Plus website
                //     via redirUrls.return_url
                //     NOTE:  an "approved invoice" is NOT a done deal;
                //            we still need to ask PayPal to apply (execute)
                //            our customer's payment.
                return new RedirectResult(redirect_approvalUrlFromCreatedPayment);
            // ................................
            // the next two lines are unlikely to be executed             ??????? PayPal ~ reliable
            // unless something failed during our attempt to
            // get an "invoice" approved by our customer.
            ViewBag.SomethingWentWrongWithCurrentPurchasePreparation =
                "No, you do NOT want to buy an argument! " + payPalSuccessLevel;
            return View();
        }

        private Int32 AndNowItIsTimeToTalkToPayPal(Int32 productCost,
                                                   String productDescription,
                                                   String yourname,
                                               out String redirect_approvalUrlFromCreatedPayment)
        {
            // ................................
            //   Miscellaneous Error Messages from PayPal ... *****  "debug_id" is NOT a constant, examples:  "67f86e19d300d",  "36f998b61c785"
            String response_VALIDATION_ERROR =
    "{\"name\":\"VALIDATION_ERROR\",\"details\":[{\"field\":\"transactions[0].amount\",\"issue\":\"Required field missing\"}],\"message\":\"Invalid request - see details\",\"information_link\":\"https://developer.paypal.com/webapps/developer/docs/api/#VALIDATION_ERROR\",\"debug_id\":\"";
            Int32  response_VALIDATION_ERROR_Length = response_VALIDATION_ERROR.Length;
            // https://developer.paypal.com/webapps/developer/docs/api/#VALIDATION_ERROR
            // VALIDATION_ERROR
            // Invalid request                                - see details
            // There was a validation issue with your request - see details
            // ==> https://developer.paypal.com/webapps/developer/docs/api/#validation-issues

            // ................................
            String response_INTERNAL_SERVICE_ERROR =
                 "{\"name\":\"INTERNAL_SERVICE_ERROR\",\"message\":\"An internal service error has occurred\",\"information_link\":\"https://developer.paypal.com/webapps/developer/docs/api/#INTERNAL_SERVICE_ERROR\",\"debug_id\":\"";
            Int32  response_INTERNAL_SERVICE_ERROR_Length = response_INTERNAL_SERVICE_ERROR.Length;
            // https://developer.paypal.com/webapps/developer/docs/api/#INTERNAL_SERVICE_ERROR
            // INTERNAL_SERVICE_ERROR
            // An internal service error has occurred
            // Resend the request at another time. If this error continues,
            //      contact PayPal Merchant Technical Support.
            //      https://developer.paypal.com/webapps/developer/support

            // ................................
            Int32 payPalSuccessLevel = 999; // 999 =. unknown
            redirect_approvalUrlFromCreatedPayment = String.Empty;

            // THINGS WE NEED TO USE THE PAYPAL REST API
            String argumentsPlusPayPalClentID = "AU ... 6G"; // todo   ... your PayPal REST API SDK user ID
            String argumentsPlusPayPalSecret  = "EA ... -f"; // todo   ... your PayPal REST API SDK PASSWORD
//          String payPalRestApiEndpoint      = "api.sandbox.paypal.com"; // For TESTING !!!
                                           // = "api.paypal.com";         // For LIVE    !!!
            // ................................
            Dictionary<String, String> sdkConfig = new Dictionary<String, String>();
            sdkConfig.Add("mode", "sandbox");
            // ................................
            // POTENTIAL POINT OF FAILURE
            // example:  "Exception in HttpConnection Execute: Invalid HTTP response The operation has timed out"
            String oAuthAccessTokenForPayPal = String.Empty; // scope outside of try/catch
            try
            {
                oAuthAccessTokenForPayPal
                    = new PayPal
                          .OAuthTokenCredential(argumentsPlusPayPalClentID,
                                                argumentsPlusPayPalSecret,
                                                sdkConfig).GetAccessToken();
            }                                          // .GetAccessToken takes us over to PayPal
            catch (Exception)
            {
                throw;  // todo ... make this code more robust
            }
            // ................................
            // We're back from PayPal with our access token.
            PayPal.APIContext apiContext // use the "Bearer" token from PayPal
                = new PayPal.APIContext(oAuthAccessTokenForPayPal);
            apiContext.Config = sdkConfig;
            // ................................
            PayPal.Api.Payments.Amount argumentsPlusPayPalPaymentAmount
                = new PayPal.Api.Payments.Amount();
            // https://developer.paypal.com/docs/classic/api/currency_codes/
            argumentsPlusPayPalPaymentAmount.currency = "GBP"; // UK Pound Sterling; "USD" for US$
            argumentsPlusPayPalPaymentAmount.total = productCost.ToString();
            // ................................
            String description = String.Empty; // 127 character maximum length
            description        = productDescription + " "
                               + DateTime.Now.ToLongDateString()
                               + " -- " + DateTime.Now.ToLongTimeString()
                               + " -- " + productCost + " U.K. pounds"
                               + " -- Thank you " + yourname
                               + " for your purchase.";
            // ................................
            // borrowed code from:  http://msdn.microsoft.com/en-us/library/xwewhkd1(v=vs.110).aspx
            // Regex.Replace Method (String, String)
            // ...  match ALL one or more white-space characters in our description text,
            //      replace EACH instance found with just one space.
            System.Text.RegularExpressions.Regex regexPattern
                        = new System.Text.RegularExpressions.Regex(@"\s+");
            description = regexPattern.Replace(description, " ");
            //  PayPal's HTML will drop extra whilespace, so we do not send any extra whitespace.
            // ................................
            PayPal.Api.Payments.Transaction argumentsSale
                = new PayPal.Api.Payments.Transaction();
            if (description.Length > 127) description = description.Substring(0, 127);
                                       // allow no more that 127 characters (PayPal's Limit)
            argumentsSale.description                 = description;
            argumentsSale.amount = argumentsPlusPayPalPaymentAmount;
            // ................................
            List<PayPal.Api.Payments.Transaction> transactionList
                = new List<PayPal.Api.Payments.Transaction>();
            transactionList.Add(argumentsSale);
            // ................................
            PayPal.Api.Payments.Payer argumentBuyer
                = new PayPal.Api.Payments.Payer();
            // The payment method "paypal" lets the customer choose
            // whether to pay us using a PayPal account
            // or to pay us with their credit card as a PayPal guest.
            argumentBuyer.payment_method        = "paypal";
            // ................................
            // Note:  we need some way to connect PayPal's response
            //        back to our customer ... TIMTOWTDI
            Guid reconnectToOurBuyer = Guid.NewGuid();          
            PayPal.Api.Payments.RedirectUrls redirUrls
                = new PayPal.Api.Payments.RedirectUrls();
            String cancelURL = "http://localhost:53326/home/argumentsnotsold/"
                             + reconnectToOurBuyer.ToString("N");  // customer changes her/his mind
            String returnURL = "http://localhost:53326/home/argumentssold/"
                             + reconnectToOurBuyer.ToString("N");  // Hurrah!  A sale!
            redirUrls.cancel_url = cancelURL;
            redirUrls.return_url = returnURL;
            // ................................
            // Next, we create a Payment object to send to PayPal
            PayPal.Api.Payments.Payment getInvoiceFromPayPal = new PayPal.Api.Payments.Payment();
            getInvoiceFromPayPal.intent        = "sale";
            getInvoiceFromPayPal.payer         = argumentBuyer;
            getInvoiceFromPayPal.transactions  = transactionList;
            getInvoiceFromPayPal.redirect_urls = redirUrls;
            // ................................

            PayPal.Api.Payments.Payment createdInvoice = null; // scope outside of try/catch
            try
            {
                // the next line can fail, for example:  http://paradigmmentors.net//files/code/argumentsPlus/500_internal_Server_Error_catch.txt
                createdInvoice = getInvoiceFromPayPal.Create(apiContext); //  <==   
                // SUCCESSFUL CREATION
                // we must persist createdInvoice.id (the Payment.id value)
                payPalSuccessLevel = 100; // successful
                SavePayPalInvoice(reconnectToOurBuyer, createdInvoice);
            }
            
            catch (PayPal.Exception.ConnectionException connectionExceptionPayPal)
             {
                 payPalSuccessLevel = 111; 
                if(String.Compare(connectionExceptionPayPal.Message,
                    "Invalid HTTP response The remote server returned an error: (400) Bad Request.")
                   == 0)
                {
                    payPalSuccessLevel += 10000;
                    if(String.Compare(connectionExceptionPayPal.Response.Substring(0, response_VALIDATION_ERROR_Length),
                                      response_VALIDATION_ERROR) == 0)
                    {
                        // todo Take appropriate action
                    }
                    else
                    {
                        payPalSuccessLevel += 20000;
                        // todo Take appropriate action
                    }
                }
                else if(String.Compare(connectionExceptionPayPal.Message,
                    "Invalid HTTP response The remote server returned an error: (500) Internal Server Error.")
                   == 0)
                {
                    payPalSuccessLevel = 500;
                    if (String.Compare(connectionExceptionPayPal.Response.Substring(0, response_INTERNAL_SERVICE_ERROR_Length),
                                       response_INTERNAL_SERVICE_ERROR) == 0)
                    {
                        payPalSuccessLevel += 30000;
                        // todo Take appropriate action
                    }
                    else
                    {
                        payPalSuccessLevel += 40000;
                        // todo Take appropriate action
                    }
                }
                else        
                {
                    payPalSuccessLevel = 666;
                    // todo Take appropriate action
                }
//              throw;        // todo complete
            }
            catch (PayPal.Exception.PayPalException paypalException)
            {
                if (String.Compare(paypalException.Message,
                    "Exception in HttpConnection Execute: Invalid HTTP response The remote server returned an error: (500) Internal Server Error.")
                   == 0)
                {   // the "may" be an inner exception
                    payPalSuccessLevel = 777;
                    if (paypalException.InnerException != null) // we take a chance that the InnerException is PayPal.Exception.ConnectionException
                    {
                        try
                        {
                            PayPal.Exception.ConnectionException payPalInnerException = (PayPal.Exception.ConnectionException)paypalException.InnerException;
                            if (String.Compare(payPalInnerException.Response.Substring(0, response_INTERNAL_SERVICE_ERROR_Length),
                   response_INTERNAL_SERVICE_ERROR) == 0)
                            {
                                payPalSuccessLevel = 30777;       // fine tune our error code
                                // todo Take appropriate action
                            }
                            else
                            {
                                payPalSuccessLevel = 40777;       // fine tune our error code
                                // todo Take appropriate action
                            }
                        }
                        catch (Exception)
                        {
                            payPalSuccessLevel = 50777;           // fine tune our error code
                            // we probably guessed wrong
                            // todo ........... handle appropriately
                        }
                    }
                }
                else
                {
                    payPalSuccessLevel = 888;
                    // todo Take appropriate action
                }
                //              throw;        // todo complete
            }
            catch (Exception genericException)  // todo complete
            {
                // example causes:
                // (a) "No connection string named 'ArgumentPlusContext'
                //      could be found in the application config file."
                // (b) "Directory lookup for the file
                //      c:\argumentsPlus\argumentClinic.mdf\" failed
                //      with the operating system error 
                //      The system cannot find the file specified.
                //         CREATE DATABASE failed.
                payPalSuccessLevel = 2222;
                // todo Take appropriate action
            }

            // ................................
            Boolean approval_url_found = false;
            if(payPalSuccessLevel == 100)
            {
                List<PayPal.Api.Payments.Links> linksFromCreatedPayment = createdInvoice.links;
                foreach (PayPal.Api.Payments.Links rel_approval in linksFromCreatedPayment)
                {
                    if (rel_approval.rel.ToLower().Equals("approval_url"))
                    {
                        redirect_approvalUrlFromCreatedPayment = Server.UrlDecode(rel_approval.href);
                        approval_url_found = true;
                        break;
                    }
                }
                if (!approval_url_found) payPalSuccessLevel = 69;
            }
            return payPalSuccessLevel;
        }
        
        private void SavePayPalInvoice(Guid reconnectToOurBuyer,
                PayPal.Api.Payments.Payment createdInvoice)
        {   // we will retrieve this so that PayPal can apply,
            // i.e., "execute", the invoice that the customer
            // has approved.  For this example, we are using
            // Entity Framework 6.0.2 with Code First.
            TinyInvoice invoiceToSave              = new TinyInvoice();
            invoiceToSave.ReconnectToOurBuyer      = reconnectToOurBuyer;
            invoiceToSave.PayPalPayment_Identifier = createdInvoice.id;
            using (ArgumentsPlus.Controllers.ArgumentPlusContext
                   argumentsPlusDb
                   = new ArgumentPlusContext())
            {
                argumentsPlusDb.TinyInvoices.Add(invoiceToSave);
                argumentsPlusDb.SaveChanges();
            }
        }


        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "The Argument Clinic, a hommage to Monty Python";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "The Argument Clinic, a hommage to Monty Python";
            return View();
        }
    }
}
