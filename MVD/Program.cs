using MVD.Endpoints;
using MVD.Jobbers;
using MVD.Util;

List<Jobber> jobbers = new()
{
    new PassportsJobber(),
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
        JobberTaskResult? checkResult = await PassportsJobber.Instance.ExecuteTask(task);
        if (checkResult == null) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
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

app.Run();