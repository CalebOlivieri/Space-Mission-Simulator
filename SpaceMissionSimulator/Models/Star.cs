using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public class Star : CelestialBodyBase
    {
        public Star()
        {
            DisplaySize = 30;
            Color = Brushes.Gold;
        }

        public override string Description => $"Estrella - Masa: {Mass}";
    }
}