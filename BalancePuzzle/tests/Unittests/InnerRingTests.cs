namespace Unittests
{
    public class InnerRingTests
    {
        [Fact]
        public void NextLightInRingWithoutWrapping()
        {
            var nextLight = Calculate.CalculateOffset(65, -18, 58, 1);
            Assert.Equal(64, nextLight);
        }

        [Fact]
        public void NextLightInRingThatWraps()
        {
            var nextWrappedLight = Calculate.CalculateOffset(58, -18, 58, 1);
            Assert.Equal(75, nextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithoutWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(75, -18, 58, 10);
            Assert.Equal(65, tenNextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(65, -18, 58, 10);
            Assert.Equal(73, tenNextWrappedLight);
        }

        [Fact]
        public void PreviousLightWithoutWrapping()
        {
            var previousLight = Calculate.CalculateOffset(65, -18, 58, -1);
            Assert.Equal(66, previousLight);
        }

        [Fact]
        public void PreviousLightWithWrapping()
        {
            var previousWrappedLight = Calculate.CalculateOffset(76, -18, 58, -1);
            Assert.Equal(59, previousWrappedLight);
        }
    }
}