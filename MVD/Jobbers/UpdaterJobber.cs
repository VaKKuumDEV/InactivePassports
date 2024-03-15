using MVD.Util;
using System.Net;

namespace MVD.Jobbers
{
    public class UpdaterJobber : Jobber
    {
        public class UpdaterJobberTask : JobberTask
        {
            public UpdaterJobberTask() : base() { }

            public override bool CanExecute() => true;

            public override void Execute()
            {
                JobberTaskResult? result = Instance.ExecuteTask(new DownloadJobberTask()).GetAwaiter().GetResult();

                Dictionary<uint, List<ushort>> records = new();
                if (result != null)
                {
                    if (result is DownloadJobberTask.DownloadJobberTaskResult dowloadResult)
                    {
                        records = PassportPacker.ReadCSV(dowloadResult.File);
                        File.Delete(dowloadResult.File);

                        //сделать проверку на изменения

                        _ = PassportsJobber.Instance.ExecuteTask(new PassportsJobber.UploadDataJobberTask());
                    }
                }
            }
        }

        public class DownloadJobberTask : JobberTask
        {
            public class DownloadJobberTaskResult : JobberTaskResult
            {
                public string File { get; }

                public DownloadJobberTaskResult(string path)
                {
                    File = path;
                }

                public override object? Get() => File;
            }

            public override bool CanExecute() => true;

            public override void Execute()
            {
                string packedFilename = new FileInfo(Utils.GetAppDir("Temp") + "/list_of_expired_passports.csv").FullName;
                string archiveFilename = new FileInfo(Utils.GetAppDir("Temp") + "/archive.bz2").FullName;

                Logger.Info("Начало загрузки " + archiveFilename);
                int lastPercentage = 0;
                using WebClient wc = new();
                wc.DownloadProgressChanged += new((sender, args) => {
                    if (args.ProgressPercentage > lastPercentage)
                    {
                        Logger.Info("Прогресс " + new FileInfo(Instance.Link).Name + ": " + args.ProgressPercentage + "%");
                        lastPercentage = args.ProgressPercentage;
                    }
                });
                wc.DownloadFileCompleted += new((sender, args) =>
                {
                    BZipUnpacker.Unpack(archiveFilename, Utils.GetAppDir("Temp"));
                    File.Delete(archiveFilename);
                });
                Task task = wc.DownloadFileTaskAsync(Instance.Link, archiveFilename);
                task.GetAwaiter().GetResult();

                Result = new DownloadJobberTaskResult(packedFilename);
            }
        }

        private static UpdaterJobber? _instance = null;
        public static UpdaterJobber Instance
        {
            get
            {
                if (_instance == null) throw new NullReferenceException();
                return _instance;
            }
        }

        public string Link { get; }

        public UpdaterJobber(string link) : base("UpdaterJobber")
        {
            _instance = this;
            Link = link;
        }

        public override int GetMaxQueueSize() => 2;
    }
}
