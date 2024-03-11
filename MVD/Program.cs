using MVD.Services;

List<Service> services = new()
{
    new PassportsService(),
};

Thread tickThread = new(new ThreadStart(() =>
{
    while (true)
    {
        foreach (Service service in services) service.Tick();
        Thread.Sleep(1);
    }
}));
tickThread.Start();

List<MVD.Endpoints.Endpoint> endpoints = new()
{
    new MVD.Endpoints.CheckerEndpoint(),
};

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

var app = builder.Build();

foreach (MVD.Endpoints.Endpoint endpoint in endpoints)
{
    if (endpoint.Type == MVD.Endpoints.Endpoint.Types.GET)
    {
        app.MapGet("/api/" + endpoint.Name, async () => (await endpoint.Execute()).ToJson());
        if (endpoint.HasIdParam) app.MapGet("/api/" + endpoint.Name + "/{id}", async (string id) => (await endpoint.Execute(id)).ToJson());
    }
}

app.MapGet("/", () => "Hello World!");

app.Run();