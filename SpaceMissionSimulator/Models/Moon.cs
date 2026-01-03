using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public class Moon : CelestialBodyBase
    {
        public Moon()
        {
            DisplaySize = 8;
            Color = Brushes.LightGray;
        }

        public override string Description => $"Luna - Masa: {Mass}, Orbita: {OrbitRadius}";
    }
}