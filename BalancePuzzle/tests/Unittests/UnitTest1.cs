namespace Unittests
{
    public class UnitTest1
    {
        [Fact]
        public void NextLightInRingWithoutWrapping()
        {
            var nextLight = CalculateOffset(35, 25, 33, 1);
            Assert.Equal(36, nextLight);
        }

        [Fact]
        public void NextLightInRingThatWraps()
        {
            var nextWrappedLight = CalculateOffset(58, 25, 33, 1);
            Assert.Equal(34, nextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithoutWrapping()
        {
            var tenNextWrappedLight = CalculateOffset(35, 25, 33, 10);
            Assert.Equal(45, tenNextWrappedLight);
        }

        [Fact]
        public void TenLightsAfterWithWrapping()
        {
            var tenNextWrappedLight = CalculateOffset(58, 25, 33, 10);
            Assert.Equal(43, tenNextWrappedLight);
        }

        [Fact]
        public void PreviousLightWithoutWrapping()
        {
            var previousLight = CalculateOffset(35, 25, 33, -1);
            Assert.Equal(34, previousLight);
        }

        [Fact]
        public void PreviousLightWithWrapping()
        {
            var previousWrappedLight = CalculateOffset(34, 25, 33, -1);
            Assert.Equal(58, previousWrappedLight);
        }

        public int CalculateOffset(int currentLightNumber, int numberOfLightsInRing, int ringStartCount, int distanceOffCurrentLight)
        {
            if (distanceOffCurrentLight < 0)
            {
                return currentLightNumber + distanceOffCurrentLight + (currentLightNumber + distanceOffCurrentLight <= ringStartCount ? numberOfLightsInRing : 0);
            }

            var o = currentLightNumber - numberOfLightsInRing + distanceOffCurrentLight;
            return o % numberOfLightsInRing + numberOfLightsInRing;
        }
    }
}