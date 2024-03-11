namespace MVDTest
{
    public partial class MethodsTest
    {
        [Fact]
        public void Test1()
        {
            bool valid = MVD.Endpoints.CheckerEndpoint.Validate("0000000001");
            Assert.True(valid);
        }

        [Fact]
        public void Test2()
        {
            (uint key, ushort val) = MVD.Util.PassportPacker.Convert("0000000001");
            Assert.True(key == 0 && val == 1);
        }
    }
}