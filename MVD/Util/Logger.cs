using System.Globalization;

namespace MVD.Util
{
    public static class Logger
    {
        public static void Log(string message)
        {
            Console.WriteLine("[" + DateTime.Now.ToString("g", CultureInfo.GetCultureInfo("ru")) + "]" + message);
        }

        public static void Info(string message)
        {
            Log("[INFO]" + message);
        }
    }
}
