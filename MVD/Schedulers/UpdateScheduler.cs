using Quartz;
using MVD.Jobbers;
using Quartz.Impl;

namespace MVD.Schedulers
{
    public class UpdateScheduler : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await UpdaterJobber.Instance.ExecuteTask(new UpdaterJobber.UpdaterJobberTask());
        }
    }
}
