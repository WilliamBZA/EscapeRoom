namespace Unittests
{
    public class MiddleRingTests
    {
        [Fact]
        public void NextLightInRingWithoutWrapping()
        {
            var nextLight = Calculate.CalculateOffset(35, 25, 33, 1);
            Assert.Equal(36, nextLight);
        }

        [Fact]
        public void NextLightInRingThatWraps()
        {
            var nextWrappedLight = Calculate.CalculateOffset(58, 25, 33, 1);
            Assert.Equal(34, nextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithoutWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(35, 25, 33, 10);
            Assert.Equal(45, tenNextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(58, 25, 33, 10);
            Assert.Equal(43, tenNextWrappedLight);
        }

        [Fact]
        public void PreviousLightWithoutWrapping()
        {
            var previousLight = Calculate.CalculateOffset(35, 25, 33, -1);
            Assert.Equal(34, previousLight);
        }

        [Fact]
        public void PreviousLightWithWrapping()
        {
            var previousWrappedLight = Calculate.CalculateOffset(34, 25, 33, -1);
            Assert.Equal(33, previousWrappedLight);
        }
    }
}