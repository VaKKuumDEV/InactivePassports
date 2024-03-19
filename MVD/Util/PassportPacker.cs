using CsvHelper;
using CsvHelper.Configuration;
using MVD.Jobbers;
using Newtonsoft.Json;
using System.Globalization;

namespace MVD.Util
{
    public static class PassportPacker
    {
        public static (uint key, ushort value) Convert(string value)
        {
            uint key = uint.Parse(value[0..7]);
            ushort val = ushort.Parse(value[7..10]);
            return (key, val);
        }

        public static Dictionary<uint, List<ushort>> ReadCSV(string filename)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = true,
            };

            using var reader = new StreamReader(filename);
            using var csv = new CsvReader(reader, configuration);

            Dictionary<uint, List<ushort>> records = new();
            uint count = 0;
            while (csv.Read())
            {
                count++;
                if (count == 1) continue;

                for (int i = 0; csv.TryGetField(i, out string value); i++)
                {
                    value = value.Replace(",", "");
                    (uint key, ushort val) = Convert(value);

                    if (records.ContainsKey(key)) records[key].Add(val);
                    else records[key] = new() { val };
                }
            }

            string outputFilename = new FileInfo(Utils.GetAppDir() + "/data.csv").FullName;
            File.WriteAllText(outputFilename, "");

            foreach (var kv in records) File.AppendAllText(outputFilename, kv.Key + ":" + string.Join(",", kv.Value) + "\n");

            return records;
        }

        public static Dictionary<uint, List<ushort>> ReadPreparedCsv(string filename)
        {
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";",
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(filename);
            using var csv = new CsvReader(reader, configuration);

            Dictionary<uint, List<ushort>> records = new();
            uint count = 0;
            while (csv.Read())
            {
                count++;
                for (int i = 0; csv.TryGetField(i, out string value); i++)
                {
                    string[] pieces = value.Split(':');
                    uint key = uint.Parse(pieces[0]);
                    List<ushort> vals = new List<string>(pieces[1].Split(',')).ConvertAll(ushort.Parse);

                    records[key] = vals;
                }
            }

            return records;
        }

        public static List<ActionsJobber.RecordAction> LoadActions(string filename)
        {
            if (!File.Exists(filename)) return new();
            return JsonConvert.DeserializeObject<List<ActionsJobber.RecordAction>>(File.ReadAllText(filename)) ?? new();
        }

        public static void SaveActions(List<ActionsJobber.RecordAction> actions, string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(actions));
        }
    }
}
