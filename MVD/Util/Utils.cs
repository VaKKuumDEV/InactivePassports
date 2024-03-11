namespace MVD.Util
{
    public static class Utils
    {
        public static string GetAppDir(string? subDir = null)
        {
            //DirectoryInfo dir = new(Environment.CurrentDirectory + "/Data");
            DirectoryInfo dir = new("D:\\Проекты\\asptest\\MVD\\MVD\\bin\\Debug\\net7.0\\Data");
            if (!dir.Exists) dir.Create();

            if (subDir != null)
            {
                dir = new(dir.FullName + "/" + subDir);
                if (!dir.Exists) dir.Create();
            }

            return dir.FullName;
        }
    }
}
