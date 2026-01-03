using System.Collections.ObjectModel;
using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public class Planet : CelestialBodyBase
    {
        public ObservableCollection<Moon> Moons { get; } = new ObservableCollection<Moon>();

        // Resource available on this planet (simple model)
        public string ResourceType { get; set; } = "Minerals";
        public int ResourceAmount { get; set; } = 0;

        public Planet()
        {
            DisplaySize = 14;
            Color = Brushes.SteelBlue;
        }

        public override string Description => $"Planeta - Masa: {Mass}, Lunas: {Moons.Count}";
    }
}