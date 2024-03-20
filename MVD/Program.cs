using MVD.Endpoints;
using MVD.Jobbers;
using MVD.Schedulers;
using MVD.Util;
using Newtonsoft.Json;

string configPath = new FileInfo(Utils.GetAppDir() + "/config.json").FullName;
Config config = new();
if (File.Exists(configPath)) config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new();
else File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));

List<Jobber> jobbers = new()
{
    new PassportsJobber(),
    new ActionsJobber(),
    new UpdaterJobber(config.Link),
};

Thread tickThread = new(new ThreadStart(() =>
{
    while (true)
    {
        foreach (Jobber jobber in jobbers) jobber.Tick();
        Thread.Sleep(1);
    }
}));
tickThread.Start();

UpdateScheduler.Start(config.UpdateTime);

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

var app = builder.Build();
app.UseRouting();

app.MapGet("/", () => "Hello World!");

app.MapGet("/api/check/{id:regex(^\\d{{10}}$)}/", async (HttpContext context, string id) =>
{
    PassportsJobber.CheckPassportJobberTask task = new(id);
    if (!task.CanExecute()) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
    else
    {
        if (await PassportsJobber.Instance.ExecuteTask(task) is not PassportsJobber.CheckPassportJobberTask.CheckPassportJobberTaskResult checkResult) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
        else
        {
            object? containsObj = checkResult.Get();
            if (containsObj == null) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
            else
            {
                bool contains = (bool)containsObj;
                if (contains) await context.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.PASSPORT_FOUND_MESSAGE, new() { ["contains"] = true, }));
                else await context.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.PASSPORT_NOT_FOUND_MESSAGE, new() { ["contains"] = false, }));
            }
        }
    }
});

app.MapGet("/api/actions/{dateFrom:regex([0-9]{{2}}.[0-9]{{2}}.[0-9]{{4}}$)}-{dateTo:regex([0-9]{{2}}.[0-9]{{2}}.[0-9]{{4}}$)}/", async (HttpContext context, string dateFrom, string dateTo) =>
{
    DateTime dateFromObj;
    try
    {
        dateFromObj = DateTime.Parse(dateFrom);
    }
    catch (Exception)
    {
        await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.PARSING_DATE_FROM_ERROR));
        return;
    }

    DateTime dateToObj;
    try
    {
        dateToObj = DateTime.Parse(dateTo);
    }
    catch (Exception)
    {
        await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.PARSING_DATE_TO_ERROR));
        return;
    }

    ActionsJobber.DateActionsJobberTask task = new(dateFromObj, dateToObj);
    if (!task.CanExecute()) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
    else
    {
        if (await ActionsJobber.Instance.ExecuteTask(task) is not ActionsJobber.DateActionsJobberTask.DateActionsJobberTaskResult result) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
        else await context.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.ACTIONS_BY_DATE_MESSAGE, new() { ["actions"] = result.Actions }));
    }
});

app.MapGet("/api/find/{id:regex(^\\d{{10}}$)}/", async (HttpContext context, string id) =>
{
    ActionsJobber.FindActionsJobberTask task = new(id);
    if (!task.CanExecute()) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
    else
    {
        if (await ActionsJobber.Instance.ExecuteTask(task) is not ActionsJobber.FindActionsJobberTask.FindActionsJobberTaskResult result) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
        else await context.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.ACTIONS_BY_NUMBER_MESSAGE, new() { ["actions"] = result.Actions }));
    }
});

app.Run();