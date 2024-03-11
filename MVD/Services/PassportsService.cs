using MVD.Util;
using static MVD.Services.PassportsService.CheckPassportServiceTask;

namespace MVD.Services
{
    public class PassportsService : Service
    {
        public class CheckPassportServiceTask : ServiceTask
        {
            public class CheckPassportServiceTaskResult : ServiceTaskResult
            {
                public bool Result { get; }
                public CheckPassportServiceTaskResult(bool result)
                {
                    Result = result;
                }

                public override object? Get()
                {
                    return Result;
                }
            }

            private readonly string _serialNumber;
            public CheckPassportServiceTask(string serialNumber)
            {
                _serialNumber = serialNumber;
            }

            public override bool CanExecute()
            {
                return Instance.DatabaseInited;
            }

            public override void Execute()
            {
                bool contains = false;
                lock (locker)
                {
                    (uint key, ushort val) = PassportPacker.Convert(_serialNumber);

                    if (Instance._records.TryGetValue(key, out List<ushort> values))
                    {
                        if (values.Contains(val))
                        {
                            contains = true;
                            Result = new CheckPassportServiceTaskResult(true);
                        }
                    }
                }

                if (!contains) Result = new CheckPassportServiceTaskResult(false);
                IsCompleted = true;
            }
        }

        public class UploadDataServiceTask : ServiceTask
        {
            public override bool CanExecute()
            {
                return true;
            }

            public override void Execute()
            {
                Logger.Info("Начало инициализации базы паспортов");

                string dataFilename = new FileInfo(Utils.GetAppDir() + "/data.csv").FullName;

                Dictionary<uint, List<ushort>> records;
                if (!File.Exists(dataFilename)) records = PassportPacker.ReadCSV(new FileInfo(Utils.GetAppDir("Temp") + "/list_of_expired_passports.csv").FullName);
                else records = PassportPacker.ReadPreparedCsv(dataFilename);

                lock (locker)
                {
                    Instance._records = records;
                    Instance.DatabaseInited = true;
                }

                IsCompleted = true;
                Logger.Info("База паспортов загружена");
            }
        }

        private static PassportsService? _instance = null;
        public static PassportsService Instance
        {
            get
            {
                if (_instance == null) throw new NullReferenceException();
                return _instance;
            }
        }

        public static object locker = new();
        public Dictionary<uint, List<ushort>> _records = new();
        public bool DatabaseInited { get; private set; } = false;

        public PassportsService() : base("PassportsService")
        {
            _tasks.Add(new UploadDataServiceTask());

            _instance = this;
        }

        public override int GetMaxQueueSize()
        {
            return 3;
        }
    }
}
