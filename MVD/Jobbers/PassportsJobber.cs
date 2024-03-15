using MVD.Util;
using System.Net;

namespace MVD.Jobbers
{
    public class PassportsJobber : Jobber
    {
        public class CheckPassportJobberTask : JobberTask
        {
            public class CheckPassportJobberTaskResult : JobberTaskResult
            {
                public bool Result { get; }
                public CheckPassportJobberTaskResult(bool result)
                {
                    Result = result;
                }

                public override object? Get()
                {
                    return Result;
                }
            }

            private readonly string _serialNumber;
            public CheckPassportJobberTask(string serialNumber)
            {
                _serialNumber = serialNumber;
            }

            public override bool CanExecute() => Instance.DatabaseInited;

            public override void Execute()
            {
                bool contains = false;
                lock (locker)
                {
                    (uint key, ushort val) = PassportPacker.Convert(_serialNumber);

                    if (Instance._records.TryGetValue(key, out var values))
                    {
                        if (values.Contains(val))
                        {
                            contains = true;
                            Result = new CheckPassportJobberTaskResult(true);
                        }
                    }
                }

                if (!contains) Result = new CheckPassportJobberTaskResult(false);
                IsCompleted = true;
            }
        }

        public class UploadDataJobberTask : JobberTask
        {
            public override bool CanExecute() => true;

            public override void Execute()
            {
                Logger.Info("Начало инициализации базы паспортов");

                string dataFilename = new FileInfo(Utils.GetAppDir() + "/data.csv").FullName;

                Dictionary<uint, List<ushort>> records = new();
                if (File.Exists(dataFilename)) records = PassportPacker.ReadPreparedCsv(dataFilename);
                else {
                    JobberTaskResult? result = UpdaterJobber.Instance.ExecuteTask(new UpdaterJobber.DownloadJobberTask()).GetAwaiter().GetResult();

                    if (result != null)
                    {
                        if (result is UpdaterJobber.DownloadJobberTask.DownloadJobberTaskResult dowloadResult)
                        {
                            records = PassportPacker.ReadCSV(dowloadResult.File);
                            File.Delete(dowloadResult.File);
                        }
                    }
                }

                lock (locker)
                {
                    Instance._records = records;
                    Instance.DatabaseInited = true;
                }

                IsCompleted = true;
                Logger.Info("База паспортов загружена");
            }
        }

        private static PassportsJobber? _instance = null;
        public static PassportsJobber Instance
        {
            get
            {
                if (_instance == null) throw new NullReferenceException();
                return _instance;
            }
        }

        private static readonly object locker = new();
        private Dictionary<uint, List<ushort>> _records = new();
        public bool DatabaseInited { get; private set; } = false;
        
        public PassportsJobber() : base("PassportsService")
        {
            _tasks.Add(new UploadDataJobberTask());

            _instance = this;
        }

        public override int GetMaxQueueSize() => 3;
    }
}
