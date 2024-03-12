namespace MVDTest
{
    public partial class MethodsTest
    {
        [Fact]
        public void Test1()
        {
            (uint key, ushort val) = MVD.Util.PassportPacker.Convert("0000000001");
            Assert.True(key == 0 && val == 1);
        }
    }
}