using MVD.Services;
using System.Text.RegularExpressions;

namespace MVD.Endpoints
{
    public partial class CheckerEndpoint : Endpoint
    {
        public const string PARAM_NOT_GIVEN_ERROR = "Не переданы серия и номер паспорта";
        public const string PASSPORT_NOT_FOUND_MESSAGE = "Паспорт не найден в базе недействительных паспортов";
        public const string PASSPORT_FOUND_MESSAGE = "Паспорт найден в базе недействительных паспортов";
        public const string PARSING_NUMBER_ERROR = "Серия и номер паспорта должны содержать 10 цифр подряд";
        public const string NOT_INITED_ERROR = "База данных еще не загружена";
        public const string ERROR = "Ошибка обработки запроса";

        public CheckerEndpoint() : base("check", Types.GET, true) { }

        public override async Task<EndpointAnswer> Execute(params string[] httpParams)
        {
            if (httpParams.Length == 0) return new(EndpointAnswer.ERROR_CODE, PARAM_NOT_GIVEN_ERROR);

            string idParam = httpParams[0];
            if (!Validate(idParam)) return new(EndpointAnswer.ERROR_CODE, PARSING_NUMBER_ERROR);

            PassportsService.CheckPassportServiceTask task = new(idParam);
            if (!task.CanExecute()) return new(EndpointAnswer.ERROR_CODE, NOT_INITED_ERROR);

            ServiceTaskResult? checkResult = await PassportsService.Instance.ExecuteTask(task);
            if (checkResult == null) return new(EndpointAnswer.ERROR_CODE, ERROR);

            object? containsObj = checkResult.Get();
            if (containsObj == null) return new(EndpointAnswer.ERROR_CODE, ERROR);

            bool contains = (bool)containsObj;
            if (contains) return new(EndpointAnswer.SUCCESS_CODE, PASSPORT_FOUND_MESSAGE, new() { ["contains"] = true, });
            return new(EndpointAnswer.SUCCESS_CODE, PASSPORT_NOT_FOUND_MESSAGE, new() { ["contains"] = false, });
        }

        public static bool Validate(string value)
        {
            Regex regex = TenDigitsOnly();
            MatchCollection matches = regex.Matches(value);
            return matches.Count > 0;
        }

        [GeneratedRegex("^\\d{10}$")]
        private static partial Regex TenDigitsOnly();
    }
}
