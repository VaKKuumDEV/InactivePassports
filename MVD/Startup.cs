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
