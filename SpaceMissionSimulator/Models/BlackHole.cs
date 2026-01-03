using System.Windows.Media;

namespace SpaceMissionSimulator.Models
{
    public class BlackHole : CelestialBodyBase
    {
        public BlackHole()
        {
            Name = "Hoyo Negro";
            Mass = 5000; // alto para efecto visual, no usamos física real
            DisplaySize = 30;
            Color = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 60));
        }

        // El hoyo negro no orbita, se queda fijo donde el jugador lo colocó
        public override void UpdatePosition(double elapsedSeconds, double centerX, double centerY)
        {
            // No hacer nada: X e Y se quedan donde se posicionó
        }
    }
}