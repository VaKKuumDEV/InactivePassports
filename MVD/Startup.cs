using MVD.Endpoints;
using MVD.Jobbers;
using MVD.Schedulers;
using MVD.Util;
using Newtonsoft.Json;
using Quartz;
using Quartz.AspNetCore;
using System.Data.SQLite;

namespace MVD
{
    public class Startup
    {
        IConfiguration Configuration { get; }
        Config MainConfig { get; } = new();
        string DbPath { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            string configPath = new FileInfo(Utils.GetAppDir() + "/config.json").FullName;
            if (File.Exists(configPath)) MainConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new();
            else File.WriteAllText(configPath, JsonConvert.SerializeObject(MainConfig, Formatting.Indented));

            DbPath = new FileInfo(Utils.GetAppDir() + "/quartznet.db").FullName;
            if (!File.Exists(DbPath))
            {
                string stockDbPath = new FileInfo(Environment.CurrentDirectory + "/quartznet.db").FullName;
                Logger.Info("Копирование БД из " + stockDbPath);
                File.Copy(stockDbPath, DbPath);
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            DateTimeOffset startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, MainConfig.UpdateTime.Hour, MainConfig.UpdateTime.Minute, 0);

            PassportsJobber passportsJobber = new();
            ActionsJobber actionsJobber = new();
            UpdaterJobber updaterJobber = new(MainConfig.Link);

            services.AddSingleton(passportsJobber);
            services.AddSingleton(actionsJobber);
            services.AddSingleton(updaterJobber);

            Thread tickThread = new(new ThreadStart(() =>
            {
                while (true)
                {
                    passportsJobber.Tick();
                    actionsJobber.Tick();
                    updaterJobber.Tick();
                    Thread.Sleep(1);
                }
            }));
            tickThread.Start();

            services.AddSingleton(Configuration);

            IConfiguration quartsConfiguration = Configuration.GetSection("Quartz");
            quartsConfiguration["quartz.dataSource.default.connectionString"] = "Data Source=" + DbPath + ";Version=3;";
            services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));

            services.AddQuartz(q =>
            {
                q.SchedulerName = "Main";

                q.ScheduleJob<UpdateScheduler>(trigger => trigger
                    .WithIdentity("DBUpdater", "Jobbers")
                    .StartAt(startDate)
                    .WithSimpleSchedule(x => x.WithIntervalInHours(24).RepeatForever())
                );
            });

            services.AddQuartzServer(options =>
            {
                options.WaitForJobsToComplete = true;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", () => "Hello World!");

                endpoints.MapGet("/api/check/{id:regex(^\\d{{10}}$)}/", async (HttpContext context, PassportsJobber jobber, string id) =>
                {
                    PassportsJobber.CheckPassportJobberTask task = new(id);
                    if (!task.CanExecute()) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
                    else
                    {
                        if (await jobber.ExecuteTask(task) is not PassportsJobber.CheckPassportJobberTask.CheckPassportJobberTaskResult checkResult) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
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

                endpoints.MapGet("/api/actions/{dateFrom:regex([0-9]{{2}}.[0-9]{{2}}.[0-9]{{4}}$)}-{dateTo:regex([0-9]{{2}}.[0-9]{{2}}.[0-9]{{4}}$)}/", async (HttpContext context, string dateFrom, string dateTo) =>
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

                endpoints.MapGet("/api/find/{id:regex(^\\d{{10}}$)}/", async (HttpContext context, string id) =>
                {
                    ActionsJobber.FindActionsJobberTask task = new(id);
                    if (!task.CanExecute()) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.NOT_INITED_ERROR));
                    else
                    {
                        if (await ActionsJobber.Instance.ExecuteTask(task) is not ActionsJobber.FindActionsJobberTask.FindActionsJobberTaskResult result) await context.SetAnswer(new(EndpointAnswer.ERROR_CODE, Messages.ERROR));
                        else await context.SetAnswer(new(EndpointAnswer.SUCCESS_CODE, Messages.ACTIONS_BY_NUMBER_MESSAGE, new() { ["actions"] = result.Actions }));
                    }
                });
            });
        }
    }
}
