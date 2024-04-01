using Xunit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Hosting;
using System.Net;
using MVD.Jobbers;
using Xunit.Abstractions;

namespace MVDTest
{
    public partial class MethodsTest
    {
        private readonly ITestOutputHelper _output;
        private readonly HttpClient _client;

        public MethodsTest(ITestOutputHelper output)
        {
            if(!File.Exists(new FileInfo(MVD.Util.Utils.GetAppDir() + "/data.csv").FullName)) File.Copy(new FileInfo(Environment.CurrentDirectory + "/data.csv").FullName, new FileInfo(MVD.Util.Utils.GetAppDir() + "/data.csv").FullName);

            _output = output;
            var server = new TestServer(new WebHostBuilder().UseEnvironment("Development").UseStartup<MVD.Startup>());
            _client = server.CreateClient();
        }

        [Fact]
        public void Test1()
        {
            (uint key, ushort val) = MVD.Util.PassportPacker.Convert("0000000001");
            Assert.True(key == 0 && val == 1);
        }

        [Fact]
        public async void Test2()
        {
            await Task.Run(async () =>
            {
                while (!PassportsJobber.Instance.DatabaseInited) await Task.Delay(500);
            });

            Assert.True(PassportsJobber.Instance.DatabaseInited);
        }

        [Fact]
        public async void Test3()
        {
            await Task.Run(async () =>
            {
                while (!ActionsJobber.Instance.DatabaseInited) await Task.Delay(500);
            });

            var records = ActionsJobber.Instance.GetRecords();
            
            Assert.Empty(records);
        }

        [Fact]
        public void Test4()
        {
            var records = PassportsJobber.Instance.GetRecords();

            Assert.Single(records.Keys);
        }

        [Theory]
        [InlineData("GET", "0000000001")]
        public async void Test5(string method, string passportNumber)
        {
            await Task.Run(async () =>
            {
                while (!PassportsJobber.Instance.DatabaseInited) await Task.Delay(500);
            });

            var request = new HttpRequestMessage(new HttpMethod(method), $"/api/check/{passportNumber}");

            var response = await _client.SendAsync(request);

            _output.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("GET", "01.01.2024", "01.03.2024")]
        public async void Test6(string method, string dateFrom, string dateTo)
        {
            await Task.Run(async () =>
            {
                while (!ActionsJobber.Instance.DatabaseInited) await Task.Delay(500);
            });

            var request = new HttpRequestMessage(new HttpMethod(method), $"/api/actions/{dateFrom}-{dateTo}");

            var response = await _client.SendAsync(request);

            _output.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("GET", "0000000001")]
        public async void Test7(string method, string passportNumber)
        {
            await Task.Run(async () =>
            {
                while (!ActionsJobber.Instance.DatabaseInited) await Task.Delay(500);
            });

            var request = new HttpRequestMessage(new HttpMethod(method), $"/api/find/{passportNumber}");

            var response = await _client.SendAsync(request);

            _output.WriteLine(await response.Content.ReadAsStringAsync());
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}