using MVD.Jobbers;
using MVD.Schedulers;
using MVD.Util;
using Newtonsoft.Json;
using Quartz;
using Quartz.AspNetCore;

namespace MVD
{
    public class Startup
    {
        IConfiguration Configuration { get; }
        IWebHostEnvironment WebHostEnvironment { get; }
        Config MainConfig { get; } = new();

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            WebHostEnvironment = env;

            string configPath = new FileInfo(Utils.GetAppDir() + "/config.json").FullName;
            if (File.Exists(configPath)) MainConfig = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath)) ?? new();
            else File.WriteAllText(configPath, JsonConvert.SerializeObject(MainConfig, Formatting.Indented));

            if (WebHostEnvironment.IsProduction() && !File.Exists(Utils.DbPath))
            {
                string stockDbPath = new FileInfo(Environment.CurrentDirectory + "/quartznet.db").FullName;
                Logger.Info("Копирование БД из " + stockDbPath);
                File.Copy(stockDbPath, Utils.DbPath);
            }

            PassportsJobber passportsJobber = new();
            ActionsJobber actionsJobber = new();
            UpdaterJobber updaterJobber = new(MainConfig.Link);

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
        }

        public void ConfigureServices(IServiceCollection services)
        {
            DateTimeOffset startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, MainConfig.UpdateTime.Hour, MainConfig.UpdateTime.Minute, 0);

            services.AddSingleton(Configuration);

            if (WebHostEnvironment.IsProduction())
            {
                IConfiguration quartsConfiguration = Configuration.GetSection("Quartz");
                quartsConfiguration["quartz.dataSource.default.connectionString"] = "Data Source=" + Utils.DbPath + ";Version=3;";
                services.Configure<QuartzOptions>(Configuration.GetSection("Quartz"));
            }

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

            services.AddControllers();
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
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
