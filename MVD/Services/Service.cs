namespace MVD.Services
{
    public abstract class Service
    {
        public string Name { get; set; }
        protected List<ServiceTask> _tasks = new();
        protected int _inWorkCount = 0;

        public Service(string name)
        {
            Name = name;
        }

        public abstract int GetMaxQueueSize();

        public int? GetNextTask()
        {
            for (int i = 0; i < _tasks.Count; i++) if (!_tasks[i].IsBusy && _tasks[i].CanExecute()) return i;
            return null;
        }

        public void Tick()
        {
            int? nextTaskIndex = null;
            while (GetMaxQueueSize() - _inWorkCount > 0 && (nextTaskIndex = GetNextTask()) != null)
            {
                _tasks[nextTaskIndex.Value].IsBusy = true;
                Thread thread = new(new ThreadStart(_tasks[nextTaskIndex.Value].Execute));
                thread.Start();
                _inWorkCount++;
            }
        }

        public async Task<ServiceTaskResult?> ExecuteTask(ServiceTask task)
        {
            _tasks.Add(task);
            await Task.Run(() =>
            {
                while (true)
                {
                    int taskIndex = _tasks.FindIndex(new(listTask => listTask.TaskId == task.TaskId));
                    if (taskIndex == -1) break;

                    if (_tasks[taskIndex].IsCompleted)
                    {
                        task = _tasks[taskIndex];
                        break;
                    }
                }
            });

            _inWorkCount--;
            _tasks.Remove(task);
            return task.Result;
        }
    }
}
