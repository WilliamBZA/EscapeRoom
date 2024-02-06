namespace Unittests
{
    internal class Calculate
    {
        public static int CalculateOffset(int currentLightNumber, int numberOfLightsInRing, int ringStartCount, int distanceOffCurrentLight)
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