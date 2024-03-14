using Quartz;
using MVD.Jobbers;
using Quartz.Impl;

namespace MVD.Schedulers
{
    public class UpdateScheduler
    {
        public class Updater : IJob
        {
            public async Task Execute(IJobExecutionContext context)
            {
                await UpdaterJobber.Instance.ExecuteTask(new UpdaterJobber.UpdaterJobberTask());
            }
        }

        public static async void Start(TimeOnly startTime)
        {
            DateTimeOffset startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startTime.Hour, startTime.Minute, 0);

            IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<Updater>().Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartAt(startDate)
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }
    }
}
