using MVD;

Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder =>
{
    webBuilder.ConfigureLogging(options =>
    {
        options.ClearProviders();
        options.AddConsole();
    });

    webBuilder.UseStartup<Startup>();
}).Build().Run();