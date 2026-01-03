using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpaceMissionSimulator.Models
{
    public enum MissionStatus { Planned, InTransit, Completed }
    public enum MissionType { Explore, Gather, Transport }

    public class Mission : INotifyPropertyChanged
    {
        public string Description { get; set; } = "Mission";
        public CelestialBodyBase? From { get; set; }
        public CelestialBodyBase? To { get; set; }
        public Ship? Ship { get; set; }

        private MissionStatus _status = MissionStatus.Planned;
        public MissionStatus Status { get => _status; private set { _status = value; OnPropertyChanged(); } }

        // Progress 0.0 .. 1.0
        private double _progress = 0.0;
        public double Progress { get => _progress; private set { _progress = value; OnPropertyChanged(); } }

        // Estimated duration in seconds (computed when mission starts)
        private double _estimatedDurationSeconds = 0.0;
        public double EstimatedDurationSeconds { get => _estimatedDurationSeconds; private set { _estimatedDurationSeconds = value; OnPropertyChanged(); } }

        // Mission type and resources
        public MissionType Type { get; set; } = MissionType.Explore;
        public string? ResourceType { get; set; }
        public int ResourceAmount { get; set; } = 0;
        public int RewardCredits { get; set; } = 0;
        // internal flag to avoid double-processing rewards
        public bool RewardClaimed { get; set; } = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // internal initial distance used to compute progress
        private double _initialDistance = 0.0;

        // Assign a ship to the mission (does not start it)
        public void AssignShip(Ship ship)
        {
            Ship = ship ?? throw new System.ArgumentNullException(nameof(ship));
        }

        // Start the mission: validate fields, position ship at source and set targets
        public void Start()
        {
            if (From == null) throw new System.InvalidOperationException("Mission 'From' is not set.");
            if (To == null) throw new System.InvalidOperationException("Mission 'To' is not set.");
            if (Ship == null) throw new System.InvalidOperationException("No ship assigned to mission.");

            // place ship at the source
            Ship.X = From.X;
            Ship.Y = From.Y;
            Ship.CurrentSource = From;
            Ship.CurrentTarget = To;

            // compute initial distance
            _initialDistance = DistanceBetween(Ship, To);
            if (_initialDistance < 1e-6) _initialDistance = 0.0;

            // compute estimated duration (seconds) guarding divide by zero
            if (Ship.Speed <= 0 || _initialDistance == 0.0)
            {
                EstimatedDurationSeconds = 0.0;
            }
            else
            {
                EstimatedDurationSeconds = _initialDistance / Ship.Speed;
            }

            Progress = 0.0;
            Status = MissionStatus.InTransit;
            OnPropertyChanged(nameof(Status));
        }

        // Advance mission by elapsed seconds: moves ship and updates progress/status
        public void AdvanceTime(double elapsedSeconds)
        {
            if (Status != MissionStatus.InTransit) return;
            if (Ship == null || To == null) return;

            // update the ship's position
            Ship.Update(elapsedSeconds);

            double remaining = DistanceBetween(Ship, To);

            if (_initialDistance <= 0)
            {
                // if no meaningful initial distance, mark complete when overlapping
                if (remaining < 1.0) Complete();
                return;
            }

            // progress: 0 .. 1
            Progress = 1.0 - (remaining / _initialDistance);
            if (Progress < 0) Progress = 0;
            if (Progress > 1) Progress = 1;

            // arrival threshold (in pixels)
            if (remaining <= 1.0 || Progress >= 0.9999)
            {
                Complete();
            }
        }

        // Completes the mission and clears ship targets
        public void Complete()
        {
            Status = MissionStatus.Completed;
            Progress = 1.0;

            if (Ship != null && To != null)
            {
                // snap ship to target center
                Ship.X = To.X;
                Ship.Y = To.Y;
                Ship.CurrentTarget = null;
                Ship.CurrentSource = To;
            }

            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Progress));
        }

        private static double DistanceBetween(Ship ship, CelestialBodyBase body)
        {
            double shipCenterX = ship.X + ship.DisplaySize / 2.0;
            double shipCenterY = ship.Y + ship.DisplaySize / 2.0;
            double bodyCenterX = body.X + body.DisplaySize / 2.0;
            double bodyCenterY = body.Y + body.DisplaySize / 2.0;
            double dx = bodyCenterX - shipCenterX;
            double dy = bodyCenterY - shipCenterY;
            return System.Math.Sqrt(dx * dx + dy * dy);
        }
    }
}