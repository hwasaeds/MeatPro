using MeatPro.Data;
using MeatPro.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace MeatPro.Services;

public sealed class AlertViewFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        if (resultContext.Controller is Controller controller)
        {
            var alertService = controller.HttpContext.RequestServices.GetRequiredService<IAlertService>();
            var alerts = await alertService.GetActiveAlertsAsync();
            var unreadCount = await alertService.GetUnreadCountAsync();

            controller.ViewBag.AlertCount = unreadCount;
            controller.ViewBag.ActiveAlerts = alerts;
        }
    }
}
