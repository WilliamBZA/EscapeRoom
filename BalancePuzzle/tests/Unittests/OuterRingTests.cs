namespace Unittests
{
    public class OuterRingTests
    {
        [Fact]
        public void ShouldBeZeroBased()
        {
            var nextLight = Calculate.CalculateOffset(1, 33, 0, -1);
            Assert.Equal(0, nextLight);
        }

        [Fact]
        public void NextLightInRingWithoutWrapping()
        {
            var nextLight = Calculate.CalculateOffset(10, 33, 0, 1);
            Assert.Equal(11, nextLight);
        }

        [Fact]
        public void NextLightInRingThatWraps()
        {
            var nextWrappedLight = Calculate.CalculateOffset(32, 33, 0, 1);
            Assert.Equal(0, nextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithoutWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(15, 33, 0, 10);
            Assert.Equal(25, tenNextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithWrapping()
        {
            var tenNextWrappedLight = Calculate.CalculateOffset(33, 33, 0, 10);
            Assert.Equal(10, tenNextWrappedLight);
        }

        [Fact]
        public void PreviousLightWithoutWrapping()
        {
            var previousLight = Calculate.CalculateOffset(15, 33, 0, -1);
            Assert.Equal(14, previousLight);
        }

        [Fact]
        public void PreviousLightWithWrapping()
        {
            var previousWrappedLight = Calculate.CalculateOffset(0, 33, 0, -1);
            Assert.Equal(32, previousWrappedLight);
        }
    }
}