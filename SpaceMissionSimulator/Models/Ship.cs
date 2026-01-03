using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public class Ship : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        public string Name { get; set; } = "Nave";
        public double Speed { get; set; } = 60.0; // pixels per second
        public double DisplaySize { get; set; } = 10.0;
        public Brush Color { get; set; } = Brushes.White;

        public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
        public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }

        public Models.CelestialBodyBase? CurrentTarget { get; set; }
        public Models.CelestialBodyBase? CurrentSource { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string Description => $"{Name} - Vel: {Speed}";

        public void Update(double elapsedSeconds)
        {
            // If there's no target but there is a current source (docked/landed), follow the source
            if (CurrentTarget == null)
            {
                if (CurrentSource != null)
                {
                    // keep ship positioned on the moving source body
                    X = CurrentSource.X;
                    Y = CurrentSource.Y;
                }
                return;
            }
            double dx = (CurrentTarget.X + CurrentTarget.DisplaySize/2.0) - (X + DisplaySize/2.0);
            double dy = (CurrentTarget.Y + CurrentTarget.DisplaySize/2.0) - (Y + DisplaySize/2.0);
            double dist = System.Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1) return; // arrived or too close
            double maxMove = Speed * elapsedSeconds;
            double t = maxMove / dist;
            if (t > 1) t = 1;
            X += dx * t;
            Y += dy * t;
        }
    }
}