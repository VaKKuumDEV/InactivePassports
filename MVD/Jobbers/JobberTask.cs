namespace MVD.Jobbers
{
    public abstract class JobberTaskResult
    {
        public abstract object? Get();
    }

    public abstract class JobberTask
    {
        public int TaskId { get; }
        public bool IsBusy { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public JobberTaskResult? Result { get; set; } = null;

        public JobberTask()
        {
            TaskId = new Random().Next(10000, 99999);
        }

        public abstract void Execute();
        public abstract bool CanExecute();
    }
}
