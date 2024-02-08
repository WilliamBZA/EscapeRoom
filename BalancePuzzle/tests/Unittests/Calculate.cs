namespace Unittests
{
    internal class Calculate
    {
        public static int CalculateOffset(int currentLightNumber, int numberOfLightsInRing, int ringStartCount, int distanceOffCurrentLight)
        {
            if (numberOfLightsInRing < 0)
            {
                distanceOffCurrentLight = 0 - distanceOffCurrentLight;
                numberOfLightsInRing = 0 - numberOfLightsInRing;
            }

            int offset = currentLightNumber - ringStartCount + distanceOffCurrentLight;
            if (offset < 0)
            {
                offset += numberOfLightsInRing;
            }

            if (offset == 0)
            {
                return ringStartCount;
            }

            return offset % numberOfLightsInRing + ringStartCount;
        }
    }
}