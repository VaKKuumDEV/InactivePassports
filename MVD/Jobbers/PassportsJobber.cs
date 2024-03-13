using MVD.Util;
using System.Net;

namespace MVD.Jobbers
{
    public class PassportsJobber : Jobber
    {
        public const string PASSPORT_BASE_LINK = "http://xn--b1ab2a0a.xn--b1aew.xn--p1ai/upload/expired-passports/list_of_expired_passports.csv.bz2";
        
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
            public override bool CanExecute()
            {
                return true;
            }

            public override void Execute()
            {
                Logger.Info("Начало инициализации базы паспортов");

                string dataFilename = new FileInfo(Utils.GetAppDir() + "/data.csv").FullName;
                string packedFilename = new FileInfo(Utils.GetAppDir("Temp") + "/list_of_expired_passports.csv").FullName;
                string archiveFilename = new FileInfo(Utils.GetAppDir("Temp") + "/archive.bz2").FullName;

                Dictionary<uint, List<ushort>> records = new();
                if (File.Exists(dataFilename)) records = PassportPacker.ReadPreparedCsv(dataFilename);
                else if (File.Exists(packedFilename)) records = PassportPacker.ReadCSV(packedFilename);
                else {
                    Logger.Info("Начало загрузки " + archiveFilename);
                    int lastPercentage = 0;
                    using WebClient wc = new();
                    wc.DownloadProgressChanged += new((sender, args) => {
                        if (args.ProgressPercentage > lastPercentage)
                        {
                            Logger.Info("Прогресс " + new FileInfo(PASSPORT_BASE_LINK).Name + ": " + args.ProgressPercentage + "%");
                            lastPercentage = args.ProgressPercentage;
                        }
                    });
                    wc.DownloadFileCompleted += new((sender, args) =>
                    {
                        BZipUnpacker.Unpack(archiveFilename, Utils.GetAppDir("Temp"));
                        File.Delete(archiveFilename);
                        records = PassportPacker.ReadCSV(packedFilename);
                        File.Delete(packedFilename);
                    });
                    Task task = wc.DownloadFileTaskAsync(PASSPORT_BASE_LINK, archiveFilename);
                    task.GetAwaiter().GetResult();
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

        public static object locker = new();
        public Dictionary<uint, List<ushort>> _records = new();
        public bool DatabaseInited { get; private set; } = false;

        public PassportsJobber() : base("PassportsService")
        {
            _tasks.Add(new UploadDataJobberTask());

            _instance = this;
        }

        public override int GetMaxQueueSize()
        {
            return 3;
        }
    }
}
