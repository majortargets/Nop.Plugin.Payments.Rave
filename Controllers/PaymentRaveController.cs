using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Rave.Models;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Messages;
using Microsoft.AspNetCore.Http;
using Nop.Services.Directory;
using Nop.Core.Domain.Directory;
using System.IO;
using System;
using Nop.Core.Domain.Payments;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Rave.Controllers
{
    public class PaymentRaveController : BasePaymentController
    {

        #region Fields
        private readonly INotificationService _notificationService;
        private readonly IWorkContext _workContext;
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPermissionService _permissionService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly ILogger _logger;
        private readonly IWebHelper _webHelper;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly RavePaymentSettings _RavePaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;

        #endregion

        #region Ctor

        public PaymentRaveController(
            INotificationService notificationService,
            IWorkContext workContext,
            ISettingService settingService,
            IPaymentService paymentService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPermissionService permissionService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            ILogger logger,
            IWebHelper webHelper,
            ShoppingCartSettings shoppingCartSettings,
            RavePaymentSettings ravePaymentSettings,
            ICurrencyService currencyService,
            CurrencySettings currencySettings)

        {
            this._notificationService = notificationService;
            this._workContext = workContext;
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._permissionService = permissionService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._storeContext = storeContext;
            this._logger = logger;
            this._webHelper = webHelper;
            this._shoppingCartSettings = shoppingCartSettings;
            this._RavePaymentSettings = ravePaymentSettings;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var ravePaymentSettings = _settingService.LoadSetting<RavePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {                
                SecretKey = ravePaymentSettings.SecretKey,
                PublicKey = ravePaymentSettings.PublicKey,
                EncryptKey = ravePaymentSettings.EncryptKey

            };
            if (storeScope > 0)
            {
               
                model.SecretKey_OverrideForStore = _settingService.SettingExists(ravePaymentSettings, x => x.SecretKey, storeScope);
                model.PublicKey_OverrideForStore = _settingService.SettingExists(ravePaymentSettings, x => x.PublicKey, storeScope);
                model.EncryptKey_OverrideForStore = _settingService.SettingExists(ravePaymentSettings, x => x.EncryptKey, storeScope);
                }

            return View("~/Plugins/Payments.Rave/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var ravePaymentSettings = _settingService.LoadSetting<RavePaymentSettings>(storeScope);

            //save settings
           
            ravePaymentSettings.PublicKey = model.PublicKey;
            ravePaymentSettings.SecretKey = model.SecretKey;
            ravePaymentSettings.EncryptKey = model.EncryptKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            
            _settingService.SaveSettingOverridablePerStore(ravePaymentSettings, x => x.PublicKey, model.PublicKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(ravePaymentSettings, x => x.SecretKey, model.SecretKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(ravePaymentSettings, x => x.EncryptKey, model.EncryptKey_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

           _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        //action displaying notification (warning) to a store owner about inaccurate Rave rounding
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult RoundingWarning(bool passProductNamesAndTotals)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //prices and total aren't rounded, so display warning
            if (passProductNamesAndTotals && !_shoppingCartSettings.RoundPricesDuringCalculation)
                return Json(new { Result = _localizationService.GetResource("Plugins.Payments.Rave.RoundingWarning") });

            return Json(new { Result = string.Empty });
        }
        public IActionResult CancelOrder()
        {
            var order = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1).FirstOrDefault();
            if (order != null)
                return RedirectToRoute("OrderDetails", new { orderId = order.Id });

            return RedirectToRoute("HomePage");
        }

        public ActionResult OrderUnSuccessful(string customorderid, string code, string message)
        {
            Order order = _orderService.GetOrderByCustomOrderNumber(customorderid);
            var model = new ReturnPaymentInfoModel();
                model.DescriptionText = "Your transaction was unsuccessful.";
                model.OrderId = order.Id;
                model.StatusCode = code;
                model.StatusMessage = message;
                return View("~/Plugins/Payments.Rave/Views/ReturnPaymentInfo.cshtml", model);
                    
        }

        //Use this form for redirection after payment.
        //[Authorize]
        //[AutoValidateAntiforgeryToken]
        //[HttpPost]
        //public ActionResult ReturnPaymentInfo(IFormCollection form)
        //{
        //    var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.Rave") as RavePaymentProcessor;
        //    if (processor == null || !_paymentService.IsPaymentMethodActive(processor) || !processor.PluginDescriptor.Installed)
        //    {
        //        throw new NopException("Rave module cannot be loaded");
        //    }
        //    string tranx_id = form["Rave_tranx_id"];
        //    string tranx_status_code = form["Rave_tranx_status_code"];
        //    string tranx_status_msg = form["Rave_tranx_status_msg"];
        //    string Rave_tranx_amt = form["Rave_tranx_amt"];
        //    string Rave_tranx_curr = form["Rave_tranx_curr"];
        //    string Rave_cust_id = form["Rave_cust_id"];
        //    string Rave_gway_name = form["Rave_gway_name"];
        //    string Rave_echo_data = form["Rave_echo_data"];


        //    _logger.Information("transid: " + tranx_id);
        //    _logger.Information("tranx_status_code: " + tranx_status_code);
        //    _logger.Information("tranx_status_msg: " + tranx_status_msg);
        //    _logger.Information("Rave_echo_data: " + Rave_echo_data);
        //    _logger.Information("Rave_tranx_amt: " + Rave_tranx_amt);
        //    _logger.Information("Rave_tranx_curr: " + Rave_tranx_curr);

        //    var orderGuid = Guid.Parse(Rave_echo_data);
        //    Order order = _orderService.GetOrderByGuid(orderGuid);

        //    if (!string.Equals(tranx_status_code, "00", StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        var model = new ReturnPaymentInfoModel();
        //        model.DescriptionText = "Your transaction was unsuccessful.";
        //        model.OrderId = order.Id;
        //        model.StatusCode = tranx_status_code;
        //        model.StatusMessage = tranx_status_msg;

        //        return View("~/Plugins/Payments.Rave/Views/ReturnPaymentInfo.cshtml", model);
        //    }

        //    order.PaymentStatus = PaymentStatus.Paid;
        //    _orderService.UpdateOrder(order);
        //    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        //    //return RedirectToAction("Completed", "Checkout");
        //}
        //[HttpPost]
        //public ActionResult ReturnPaymentInfo(string cancelled, string txref)
        //{
            

        //    _logger.Information("cancelled? : " + cancelled);
        //    _logger.Information("tranxref: " + txref);
            
        //    return RedirectToRoute("CheckoutCompleted", new { orderId = "30" });
        //    //return RedirectToAction("Completed", "Checkout");
        //}
        [HttpPost]
        public ActionResult ReturnPaymentInfo()
        {
           // var rjson = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            var querycol =  HttpContext.Request.Query;
            _logger.Information(string.Join("", querycol.ToList())); 
            
            string cancelled = querycol["cancelled"].ToString();
            string txref = querycol["txref"].ToString();

            var orderGuid = Guid.Parse(txref);
            Order order = _orderService.GetOrderByGuid(orderGuid);

            if (order != null && cancelled != "true")
            {
             order.PaymentStatus = PaymentStatus.Authorized;             
             _orderService.UpdateOrder(order);
             return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });      
            }
            else
            {
             var model = new ReturnPaymentInfoModel();
             model.DescriptionText = "Your transaction was Cancelled.";
              model.OrderId = order.Id;
              model.StatusCode = "";
              model.StatusMessage = "Cancelled";
             return View("~/Plugins/Payments.Rave/Views/ReturnPaymentInfo.cshtml", model);      
            
            }
        }
        [HttpPost]
        public void RaveWebHook()
        {
            string res="";
            try
            {
             res = new StreamReader(HttpContext.Request.Body).ReadToEnd();
            WebHookResponse response = JsonConvert.DeserializeObject<WebHookResponse>(res);
            _logger.Information("Webhook-->" +res);
             var orderGuid = Guid.Parse(response.txRef);
             Order order = _orderService.GetOrderByGuid(orderGuid);
            if(order != null && response.status == "successful")
             {            
             order.PaymentStatus = PaymentStatus.Paid;
             order.OrderNotes.Add(new OrderNote
                    {
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.Now,
                        Note = "Order paid! Status: " + response.status + " Ref: " + response.orderRef + " Amount: " + response.charged_amount + " Customer: " + response.customer + " Created: " + response.createdAt
             });
             _orderService.UpdateOrder(order);

                }            
             
              else
                {
                    order.PaymentStatus = PaymentStatus.Voided;
                    order.OrderNotes.Add(new OrderNote
                    {
                         DisplayToCustomer = false ,                          
                           CreatedOnUtc = DateTime.Now,
                            Note = "Payment voided! Status: "+ response.status + " Ref: " + response.orderRef + " Amount: " + response.charged_amount
                    });
                    _orderService.UpdateOrder(order);
                }             

            }
            catch(Exception ex)
            {
                _logger.Error("Webhook error: " + res, ex);
            }
           

        }
        #endregion
    }
}