using MVD.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MVD.Jobbers
{
    public class ActionsJobber : Jobber
    {
        public class UpdateActionsJobberTask : JobberTask
        {
            public Dictionary<uint, List<ushort>> NewRecords { get; }

            public UpdateActionsJobberTask(Dictionary<uint, List<ushort>> records)
            {
                NewRecords = records;
            }

            public override bool CanExecute() => Instance.DatabaseInited && PassportsJobber.Instance.DatabaseInited;

            public override void Execute()
            {
                List<RecordAction> actions = new();
                Dictionary<uint, List<ushort>> oldRecords = PassportsJobber.Instance.GetRecords();
                foreach (KeyValuePair<uint, List<ushort>> record in oldRecords)
                {
                    if (NewRecords.TryGetValue(record.Key, out var nums))
                    {
                        List<ushort> newValues = nums.Except(record.Value).ToList();
                        List<ushort> leftValues = record.Value.Except(nums).ToList();

                        actions.AddRange(newValues.ConvertAll(item => new RecordAction(record.Key + item.ToString(), RecordAction.Actions.NEW)));
                        actions.AddRange(leftValues.ConvertAll(item => new RecordAction(record.Key + item.ToString(), RecordAction.Actions.OUT)));
                    }
                    else actions.AddRange(record.Value.ConvertAll(item => new RecordAction(record.Key + item.ToString(), RecordAction.Actions.OUT)));
                }

                foreach (KeyValuePair<uint, List<ushort>> record in NewRecords)
                {
                    if (!oldRecords.ContainsKey(record.Key)) actions.AddRange(record.Value.ConvertAll(item => new RecordAction(record.Key + item.ToString(), RecordAction.Actions.NEW)));
                }

                lock (Instance.locker)
                {
                    Instance.actions.AddRange(actions.Distinct());
                    PassportPacker.SaveActions(Instance.actions, new FileInfo(Utils.GetAppDir() + "/actions.json").FullName);
                }

                IsCompleted = true;
            }
        }

        public class LoadActionsJobberTask : JobberTask
        {
            public override bool CanExecute() => true;

            public override void Execute()
            {
                Logger.Info("Начало загрузки списка активности паспортов");

                List<RecordAction> actions = PassportPacker.LoadActions(new FileInfo(Utils.GetAppDir() + "/actions.json").FullName);
                lock (Instance.locker)
                {
                    Instance.actions = actions;
                    Instance.DatabaseInited = true;
                }

                Logger.Info("Конец загрузки списка активности паспортов");
                IsCompleted = true;
            }
        }

        public class FindActionsJobberTask : JobberTask
        {
            public class FindActionsJobberTaskResult : JobberTaskResult
            {
                public List<RecordAction> Actions { get; }

                public FindActionsJobberTaskResult(List<RecordAction> actions)
                {
                    Actions = actions;
                }

                public override object? Get()
                {
                    return Actions;
                }
            }

            public string Number { get; }

            public FindActionsJobberTask(string number)
            {
                Number = number;
            }

            public override bool CanExecute() => Instance.DatabaseInited;

            public override void Execute()
            {
                lock (Instance.locker)
                {
                    List<RecordAction> actions = Instance.actions.FindAll(item => item.Number == Number);
                    Result = new FindActionsJobberTaskResult(actions);
                }

                IsCompleted = true;
            }
        }

        public class DateActionsJobberTask : JobberTask
        {
            public class DateActionsJobberTaskResult : JobberTaskResult
            {
                public List<RecordAction> Actions { get; }

                public DateActionsJobberTaskResult(List<RecordAction> actions)
                {
                    Actions = actions;
                }

                public override object? Get()
                {
                    return Actions;
                }
            }

            public DateTime From { get; }
            public DateTime To { get; }

            public DateActionsJobberTask(DateTime from, DateTime to)
            {
                From = from;
                To = to;
            }

            public override bool CanExecute() => Instance.DatabaseInited;

            public override void Execute()
            {
                lock (Instance.locker)
                {
                    List<RecordAction> actions = Instance.actions.FindAll(item => item.ActionDate >= From && item.ActionDate <= To);
                    Result = new DateActionsJobberTaskResult(actions);
                }

                IsCompleted = true;
            }
        }

        public struct RecordAction
        {
            public enum Actions
            {
                NEW,
                OUT,
            };

            [JsonProperty("action")] public Actions Action { get; set; }
            [JsonProperty("number")] public string Number { get; set; }
            [JsonProperty("date")] public DateTime ActionDate { get; set; }

            public RecordAction(string number, Actions action)
            {
                Number = number;
                Action = action;
                ActionDate = DateTime.Now;
            }
        }

        private static ActionsJobber? _instance = null;
        public static ActionsJobber Instance
        {
            get
            {
                if (_instance == null) throw new NullReferenceException();
                return _instance;
            }
        }

        private readonly object locker = new();
        private List<RecordAction> actions = new();

        public bool DatabaseInited { get; private set; } = false;

        public ActionsJobber() : base("ActionsJobber")
        {
            _tasks.Add(new LoadActionsJobberTask());

            _instance = this;
        }

        public override int GetMaxQueueSize() => 3;
    }
}
