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
                    Instance.actions.AddRange(actions);
                }
            }
        }

        public readonly struct RecordAction
        {
            public enum Actions
            {
                NEW,
                OUT,
            };

            public Actions Action { get; }
            public string Number { get; }
            public DateTime ActionDate { get; }

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
            _instance = this;
        }

        public override int GetMaxQueueSize() => 3;
    }
}
