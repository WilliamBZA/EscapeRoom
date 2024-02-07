namespace Unittests
{
    public class InnerRingTests
    {
        [Fact]
        public void NextLightInRingWithoutWrapping()
        {
            var nextLight = Calculate.CalculateOffset(65, -18, 60, 2);
            Assert.Equal(63, nextLight);
        }

        [Fact]
        public void NextLightInRingThatWraps()
        {
            var nextWrappedLight = Calculate.CalculateOffset(60, -18, 60, 1);
            Assert.Equal(77, nextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithoutWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(75, -18, 60, 10);
            Assert.Equal(65, tenNextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(65, -18, 60, 10);
            Assert.Equal(73, tenNextWrappedLight);
        }

        [Fact]
        public void PreviousLightWithoutWrapping()
        {
            var previousLight = Calculate.CalculateOffset(65, -18, 60, -1);
            Assert.Equal(66, previousLight);
        }

        [Fact]
        public void PreviousLightWithWrapping()
        {
            var previousWrappedLight = Calculate.CalculateOffset(78, -18, 60, -1);
            Assert.Equal(61, previousWrappedLight);
        }
    }
}