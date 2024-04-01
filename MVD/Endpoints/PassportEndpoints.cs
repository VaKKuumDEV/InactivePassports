using Microsoft.AspNetCore.Mvc;
using MVD.Jobbers;
using MVD.Util;

namespace MVD.Endpoints
{
    public class PassportEndpoints : Controller
    {
        [HttpGet("/api/check/{id:regex(^\\d{{10}}$)}/")]
        public async Task Check([FromRoute] string id)
        {
            PassportsJobber.CheckPassportJobberTask task = new(id);
            if (!task.CanExecute()) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
            else
            {
                if (await PassportsJobber.Instance.ExecuteTask(task) is not PassportsJobber.CheckPassportJobberTask.CheckPassportJobberTaskResult checkResult) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
                else
                {
                    object? containsObj = checkResult.Get();
                    if (containsObj == null) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
                    else
                    {
                        bool contains = (bool)containsObj;
                        if (contains) await HttpContext.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.PASSPORT_FOUND_MESSAGE, new() { ["contains"] = true, }));
                        else await HttpContext.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.PASSPORT_NOT_FOUND_MESSAGE, new() { ["contains"] = false, }));
                    }
                }
            }
        }

        [HttpGet("/api/actions/{dateFrom:regex([[0-9]]{{2}}.[[0-9]]{{2}}.[[0-9]]{{4}}$)}-{dateTo:regex([[0-9]]{{2}}.[[0-9]]{{2}}.[[0-9]]{{4}}$)}/")]
        public async Task Actions([FromRoute] string dateFrom, [FromRoute] string dateTo)
        {
            DateTime dateFromObj;
            try
            {
                dateFromObj = DateTime.Parse(dateFrom);
            }
            catch (Exception)
            {
                await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.PARSING_DATE_FROM_ERROR));
                return;
            }

            DateTime dateToObj;
            try
            {
                dateToObj = DateTime.Parse(dateTo);
            }
            catch (Exception)
            {
                await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.PARSING_DATE_TO_ERROR));
                return;
            }

            ActionsJobber.DateActionsJobberTask task = new(dateFromObj, dateToObj);
            if (!task.CanExecute()) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
            else
            {
                if (await PassportsJobber.Instance.ExecuteTask(task) is not ActionsJobber.DateActionsJobberTask.DateActionsJobberTaskResult result) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
                else await HttpContext.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.ACTIONS_BY_DATE_MESSAGE, new() { ["actions"] = result.Actions }));
            }
        }

        [HttpGet("/api/find/{id:regex(^\\d{{10}}$)}/")]
        public async Task Find([FromRoute] string id)
        {
            ActionsJobber.FindActionsJobberTask task = new(id);
            if (!task.CanExecute()) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
            else
            {
                if (await PassportsJobber.Instance.ExecuteTask(task) is not ActionsJobber.FindActionsJobberTask.FindActionsJobberTaskResult result) await HttpContext.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
                else await HttpContext.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.ACTIONS_BY_NUMBER_MESSAGE, new() { ["actions"] = result.Actions }));
            }
        }
    }
}
