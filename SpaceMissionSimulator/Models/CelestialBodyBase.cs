using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public abstract class CelestialBodyBase : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        private double _angle;

        public string Name { get; set; } = "Body";
        public double Mass { get; set; } = 1.0;
        public double OrbitRadius { get; set; } = 0.0; // px in canvas units
        public double OrbitPeriod { get; set; } = 60.0; // seconds for a full orbit
        public CelestialBodyBase? Parent { get; set; }

        // Visual properties for binding
        
        public double DisplaySize { get; set; } = 10.0;
        public Brush Color { get; set; } = Brushes.Gray;

    public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
    public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }

        public double Angle { get => _angle; protected set { _angle = value; OnPropertyChanged(); } }

        public virtual string Description => $"Masa: {Mass}, Radio orbit: {OrbitRadius}, Periodo: {OrbitPeriod}s";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public virtual void UpdatePosition(double elapsedSeconds, double centerX, double centerY)
        {
            if (OrbitPeriod <= 0 || OrbitRadius <= 0)
            {
                // static, place at parent's position or center
                if (Parent != null)
                {
                    X = Parent.X;
                    Y = Parent.Y;
                }
                else
                {
                    X = centerX;
                    Y = centerY;
                }
                return;
            }

            // update angle according to period (full circle = 2pi)
            Angle += (elapsedSeconds / OrbitPeriod) * 360.0;
            if (Angle >= 360) Angle -= 360;
            double rad = Angle * System.Math.PI / 180.0;

            double baseX = Parent?.X ?? centerX;
            double baseY = Parent?.Y ?? centerY;

            X = baseX + OrbitRadius * System.Math.Cos(rad) - DisplaySize / 2.0;
            Y = baseY + OrbitRadius * System.Math.Sin(rad) - DisplaySize / 2.0;
        }
    }
}