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