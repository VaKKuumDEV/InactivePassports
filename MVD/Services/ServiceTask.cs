namespace MVD.Services
{
    public abstract class ServiceTaskResult
    {
        public abstract object? Get();
    }

    public abstract class ServiceTask
    {
        public int TaskId { get; }
        public bool IsBusy { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public ServiceTaskResult? Result { get; set; } = null;

        public ServiceTask()
        {
            TaskId = new Random().Next(10000, 99999);
        }

        public abstract void Execute();
        public abstract bool CanExecute();
    }
}
