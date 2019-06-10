using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Rave
{
    public partial class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="routeBuilder">Route builder</param>
        public void RegisterRoutes(IRouteBuilder routeBuilder)
        {            
           
            routeBuilder.MapRoute("Plugin.Payments.Rave.ReturnPaymentInfo", "Plugins/PaymentRave/ReturnPaymentInfo",
                 new { controller = "PaymentRave", action = "ReturnPaymentInfo" });

            routeBuilder.MapRoute("Plugin.Payments.Rave.OrderUnSuccessful", "Plugins/PaymentRave/OrderUnSuccessful",
                 new { controller = "PaymentRave", action = "OrderUnSuccessful" });
           
            routeBuilder.MapRoute("Plugin.Payments.Rave.CancelOrder", "Plugins/PaymentRave/CancelOrder",
                 new { controller = "PaymentRave", action = "CancelOrder" });

            routeBuilder.MapRoute("Plugin.Payments.Rave.RaveWebHook", "Plugins/PaymentRave/RaveWebHook",
                 new { controller = "PaymentRave", action = "RaveWebHook" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority
        {
            get { return -1; }
        }
    }
}
