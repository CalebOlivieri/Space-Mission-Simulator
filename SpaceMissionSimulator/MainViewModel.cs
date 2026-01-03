using SpaceMissionSimulator.Helpers;
using SpaceMissionSimulator.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace SpaceMissionSimulator
{
    public class MainViewModel : INotifyPropertyChanged
    {
    public enum GameMode { Sandbox, Missions }

    private GameMode _mode = GameMode.Sandbox;
    public GameMode Mode { get => _mode; private set { _mode = value; OnPropertyChanged(nameof(Mode)); OnPropertyChanged(nameof(IsSandbox)); OnPropertyChanged(nameof(IsMissions)); } }

    public bool IsSandbox => Mode == GameMode.Sandbox;
    public bool IsMissions => Mode == GameMode.Missions;

    public ICommand SwitchToSandboxCommand { get; }
    public ICommand SwitchToMissionsCommand { get; }
    public ICommand ExtractMissionCommand { get; }
    public ICommand ReturnToMenuCommand { get; }

    private readonly DispatcherTimer _timer;
    private DateTime _lastTick;

    // Factor de velocidad de simulación (1 = normal)
    private double _timeScale = 1.0;
    public double TimeScale
    {
        get => _timeScale;
        set
        {
            _timeScale = value;
            OnPropertyChanged(nameof(TimeScale));
        }
    }

    public ObservableCollection<object> Bodies { get; } = new ObservableCollection<object>();
    public ObservableCollection<Mission> Missions { get; } = new ObservableCollection<Mission>();
    public int PlayerCredits { get; private set; } = 0;

    // Misión actualmente seleccionada en la UI de Misiones
    private Mission? _selectedMission;
    public Mission? SelectedMission
    {
        get => _selectedMission;
        set
        {
            _selectedMission = value;
            OnPropertyChanged(nameof(SelectedMission));
        }
    }

    public ICommand GenerateMissionsCommand { get; }
    public ICommand AcceptMissionCommand { get; }

    public ICommand AddStarCommand { get; }
    public ICommand AddPlanetCommand { get; }
    public ICommand AddMoonCommand { get; }
    public ICommand AddShipCommand { get; }
    public ICommand StartCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand SpeedBoostCommand { get; }
    public ICommand NormalSpeedCommand { get; }
    
    // Context menu commands
    public ICommand DeleteBodyCommand { get; }
    public ICommand CenterOnBodyCommand { get; }
    
    // Presets
    public ICommand CreateSolarSystemPresetCommand { get; }
    public ICommand CreateBinarySystemPresetCommand { get; }
    public ICommand CreateMultiPlanetPresetCommand { get; }
    
    // Save/Load
    public ICommand SaveScenarioCommand { get; }
    public ICommand LoadScenarioCommand { get; }

    // Sandbox special actions
    public ICommand StartPlacingBlackHoleCommand { get; }
    public ICommand ResetSandboxCommand { get; }

    private bool _isPlacingBlackHole;
    public bool IsPlacingBlackHole
    {
        get => _isPlacingBlackHole;
        set
        {
            _isPlacingBlackHole = value;
            OnPropertyChanged(nameof(IsPlacingBlackHole));
        }
    }

    public MainViewModel()
    {
        // timer
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(50) };
        _timer.Tick += Timer_Tick;
        _lastTick = DateTime.Now;

        AddStarCommand = new RelayCommand(_ => AddStar());
        AddPlanetCommand = new RelayCommand(_ => AddPlanet());
        AddMoonCommand = new RelayCommand(_ => AddMoon());
        AddShipCommand = new RelayCommand(_ => AddShip());
        GenerateMissionsCommand = new RelayCommand(_ => GenerateMissions());
        AcceptMissionCommand = new RelayCommand(m => AcceptMission(m as Mission));

        // mode commands
        SwitchToSandboxCommand = new RelayCommand(_ => SwitchToSandbox());
        SwitchToMissionsCommand = new RelayCommand(_ => SwitchToMissions());
        ExtractMissionCommand = new RelayCommand(m => ExtractMission(m as Mission));
        ReturnToMenuCommand = new RelayCommand(_ => ReturnToMenu());
        StartCommand = new RelayCommand(_ => Start());
        PauseCommand = new RelayCommand(_ => Pause());
    SpeedBoostCommand = new RelayCommand(_ => TimeScale = 4.0);
    NormalSpeedCommand = new RelayCommand(_ => TimeScale = 1.0);
        
        // context menu commands
        DeleteBodyCommand = new RelayCommand(_ => DeleteSelectedBody());
        CenterOnBodyCommand = new RelayCommand(_ => CenterOnSelectedBody());
        
        // presets
        CreateSolarSystemPresetCommand = new RelayCommand(_ => CreateSolarSystemPreset());
        CreateBinarySystemPresetCommand = new RelayCommand(_ => CreateBinarySystemPreset());
        CreateMultiPlanetPresetCommand = new RelayCommand(_ => CreateMultiPlanetPreset());
        
        // save/load
        SaveScenarioCommand = new RelayCommand(_ => SaveScenario());
        LoadScenarioCommand = new RelayCommand(_ => LoadScenario());

        // sandbox special actions
        StartPlacingBlackHoleCommand = new RelayCommand(_ => BeginPlacingBlackHole());
        ResetSandboxCommand = new RelayCommand(_ => ResetSandbox());

    // create a default star in center and start in Sandbox mode
    AddStar();
    Mode = GameMode.Sandbox;
    }

    private CelestialBodyBase? _selectedBody;
    public CelestialBodyBase? SelectedBody { get => _selectedBody; set { _selectedBody = value; OnPropertyChanged(nameof(SelectedBody)); OnPropertyChanged(nameof(SelectedBodyName)); } }

    // Called from UI when a body (planet/moon/star/ship) is clicked
    public void SelectBody(CelestialBodyBase? body)
    {
        // restore previous color if any
        if (_selectedBody != null && _originalColors.ContainsKey(_selectedBody))
        {
            _selectedBody.Color = _originalColors[_selectedBody];
            _originalColors.Remove(_selectedBody);
        }

        _selectedBody = body;
        OnPropertyChanged(nameof(SelectedBody));

        if (_selectedBody != null)
        {
            // store original color and highlight
            if (!_originalColors.ContainsKey(_selectedBody))
            {
                _originalColors[_selectedBody] = _selectedBody.Color;
            }
            _selectedBody.Color = System.Windows.Media.Brushes.Yellow;
        }
    }

    // keep track of original brushes when selecting
    private System.Collections.Generic.Dictionary<CelestialBodyBase, System.Windows.Media.Brush> _originalColors = new();

    public string SelectedBodyName => SelectedBody?.Name ?? "(ninguno)";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void Timer_Tick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double elapsed = (now - _lastTick).TotalSeconds * TimeScale;
        _lastTick = now;

        // choose a center for orbits - approx center of canvas
        double centerX = 400;
        double centerY = 300;

        // locate any black hole in the scene (solo 1 por reset)
        var blackHole = Bodies.OfType<BlackHole>().FirstOrDefault();

        // update celestial bodies (stars, planets, moons)
        foreach (var obj in Bodies.ToList())
        {
            switch (obj)
            {
                case CelestialBodyBase body:
                    // los hoyos negros no orbitan
                    if (body is BlackHole)
                    {
                        break;
                    }

                    // Si hay hoyo negro en Sandbox, pausamos las órbitas y dejamos que solo actúe la gravedad del hoyo negro
                    if (!(IsSandbox && blackHole != null))
                    {
                        body.UpdatePosition(elapsed, centerX, centerY);
                        // update moons if a planet
                        if (body is Planet p)
                        {
                            foreach (var m in p.Moons)
                            {
                                m.UpdatePosition(elapsed, centerX, centerY);
                            }
                        }
                    }
                    break;
                case Ship ship:
                    ship.Update(elapsed);
                    break;
            }
        }

        // aplicar atracción del hoyo negro si existe y estamos en Sandbox (modo arcade)
        if (IsSandbox && blackHole != null)
        {
            ApplyBlackHoleGravity(blackHole, elapsed);
        }

        // update missions using Mission.AdvanceTime (rewards are claimed via Extract)
        foreach (var m in Missions.ToList())
        {
            if (m.Status == MissionStatus.InTransit)
            {
                m.AdvanceTime(elapsed);
            }
        }

    }

    private void InitializeSystem()
    {
        // create 5 planets with increasing resources
        for (int i = 0; i < 5; i++)
        {
            AddPlanet();
        }

        // assign resources to planets
        int idx = 1;
        foreach (var p in Bodies.OfType<Planet>())
        {
            p.ResourceType = "Minerals";
            p.ResourceAmount = 50 + 30 * idx;
            idx++;
        }

        // generate some initial missions
        GenerateMissions();
    }

    // --- Sandbox: Hoyo negro y reset ---

    private void BeginPlacingBlackHole()
    {
        // solo permitirlo en Sandbox y si no existe ya un hoyo negro
        if (!IsSandbox) return;
        if (Bodies.OfType<BlackHole>().Any()) return;

        IsPlacingBlackHole = true;
    }

    public void PlaceBlackHoleAt(double x, double y)
    {
        if (!IsSandbox) return;
        if (!IsPlacingBlackHole) return;
        if (Bodies.OfType<BlackHole>().Any())
        {
            IsPlacingBlackHole = false;
            return;
        }

        var bh = new BlackHole
        {
            X = x,
            Y = y
        };
        Bodies.Add(bh);
        IsPlacingBlackHole = false;
    }

    private void ApplyBlackHoleGravity(BlackHole bh, double elapsed)
    {
        // parámetros simples para el efecto visual
    double influenceRadius = 800;   // gran área de efecto para modo arcade
    double destroyRadius = 35;      // distancia a la que traga el cuerpo

        var toRemove = new System.Collections.Generic.List<object>();

        foreach (var obj in Bodies)
        {
            if (ReferenceEquals(obj, bh)) continue;

            if (obj is CelestialBodyBase body)
            {
                double dx = bh.X - body.X;
                double dy = bh.Y - body.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < destroyRadius)
                {
                    toRemove.Add(obj);
                    continue;
                }

                if (dist < influenceRadius && dist > 0.1)
                {
                    // velocidad hacia el hoyo negro, muy rápida y dramática (arcade)
                    double strength = 900; // fuerza muy alta para colapso rápido
                    double factor = strength * elapsed / dist;
                    body.X += dx * factor;
                    body.Y += dy * factor;
                }
            }
        }

        foreach (var obj in toRemove)
        {
            Bodies.Remove(obj);
        }
    }

    private void ResetSandbox()
    {
        if (!IsSandbox)
        {
            // si estamos en misiones, solo cambiamos de modo y reseteamos
            SwitchToSandbox();
        }

        TimeScale = 1.0;
        IsPlacingBlackHole = false;
        Bodies.Clear();

        // estado inicial simple: una estrella y 3 planetas
        AddStar();
        for (int i = 0; i < 3; i++)
        {
            AddPlanet();
        }
    }

    public void SwitchToMissions()
    {
        // clear current sandbox objects and missions
        Bodies.Clear();
        Missions.Clear();

        // create star and planets with resources
        AddStar();
        for (int i = 0; i < 5; i++)
        {
            AddPlanet();
        }

        int idx = 1;
        foreach (var p in Bodies.OfType<Planet>())
        {
            p.ResourceType = "Minerals";
            p.ResourceAmount = 60 + 25 * idx;
            idx++;
        }

        // create a single player ship
        var star = Bodies.OfType<Star>().FirstOrDefault();
        var ship = new Ship { Name = "Explorer", Speed = 80 };
        if (star != null)
        {
            ship.X = star.X + star.DisplaySize + 10;
            ship.Y = star.Y + star.DisplaySize + 10;
            ship.CurrentSource = star;
        }
        Bodies.Add(ship);

        // generate missions
        GenerateMissions();

        Mode = GameMode.Missions;
    }

    public void SwitchToSandbox()
    {
        // keep existing bodies but clear missions
        Missions.Clear();
        Mode = GameMode.Sandbox;
    }

    public void ReturnToMenu()
    {
        // Dispara un evento que el code-behind escuchará
        ReturnToMenuRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ReturnToMenuRequested;

    private void ExtractMission(Mission? mission)
    {
        if (mission == null) return;
        if (mission.Status != MissionStatus.Completed) return;
        if (mission.RewardClaimed) return;

        // apply mission effects when player extracts the ship
        if (mission.Type == MissionType.Gather && mission.To is Planet p)
        {
            int taken = Math.Min(p.ResourceAmount, mission.ResourceAmount);
            p.ResourceAmount -= taken;
            PlayerCredits += mission.RewardCredits;
        }
        else
        {
            PlayerCredits += mission.RewardCredits;
        }

        mission.RewardClaimed = true;

        // return ship to source (or star) and clear targets
        if (mission.Ship != null)
        {
            var star = Bodies.OfType<Star>().FirstOrDefault();
            if (star != null)
            {
                mission.Ship.X = star.X + star.DisplaySize + 10;
                mission.Ship.Y = star.Y + star.DisplaySize + 10;
                mission.Ship.CurrentTarget = null;
                mission.Ship.CurrentSource = star;
            }
        }

    // remove mission from list and notify
    Missions.Remove(mission);
    OnPropertyChanged(nameof(PlayerCredits));

        // mostrar un mensaje de felicitación sencillo
        MessageBox.Show($"¡Misión completada! Has ganado {mission.RewardCredits} créditos.",
                        "Misión completada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
    }

    private void GenerateMissions()
    {
        var planets = Bodies.OfType<Planet>();
        var gens = MissionGenerator.GenerateGatherMissions(planets, 5);
        foreach (var m in gens)
        {
            Missions.Add(m);
        }

    }

    private void AcceptMission(Mission? mission)
    {
    if (mission == null) return;
        Ship? ship = null;
        if (Mode == GameMode.Missions)
        {
            // use the single ship in missions mode
            ship = Bodies.OfType<Ship>().FirstOrDefault();
            if (ship == null)
            {
                AddShip();
                ship = Bodies.OfType<Ship>().LastOrDefault();
            }
        }
        else
        {
            // find an idle ship in sandbox
            ship = Bodies.OfType<Ship>().FirstOrDefault(s => s.CurrentTarget == null);
            if (ship == null)
            {
                AddShip();
                ship = Bodies.OfType<Ship>().LastOrDefault();
            }
        }

        if (ship != null)
        {
            mission.AssignShip(ship);
            mission.From = ship.CurrentSource ?? mission.From;
            mission.Start();
            if (!Missions.Contains(mission)) Missions.Add(mission);
        }
    }

    public void Start()
    {
        _lastTick = DateTime.Now;
        if (!_timer.IsEnabled) _timer.Start();
    }
    public void Pause()
    {
        if (_timer.IsEnabled) _timer.Stop();
    }

    private void AddStar()
    {
        // ensure only one main star for simplicity
        if (Bodies.OfType<Star>().Any()) return;
        var star = new Star { Name = "Sol", Mass = 1000 };
        // position star near center
        star.X = 400 - star.DisplaySize/2.0;
        star.Y = 300 - star.DisplaySize/2.0;
        Bodies.Add(star);
    }

    private void AddPlanet()
    {
        var star = Bodies.OfType<Star>().FirstOrDefault();
        if (star == null) return;
        int index = Bodies.OfType<Planet>().Count() + 1;
        var planet = new Planet { Name = $"Planeta {index}", Mass = 10 * index, OrbitRadius = 60 + 40 * index, OrbitPeriod = 30 + 20 * index };
        planet.Parent = star;
        Bodies.Add(planet);
    }

    private void AddMoon()
    {
        // If in Sandbox and a planet is selected, add moon to that planet
        if (IsSandbox && SelectedBody is Planet selectedPlanet)
        {
            int index = selectedPlanet.Moons.Count + 1;
            var moon = new Moon { Name = $"Luna {selectedPlanet.Name}-{index}", Mass = 1, OrbitRadius = 18 + 6 * index, OrbitPeriod = 8 + 4 * index };
            moon.Parent = selectedPlanet;
            selectedPlanet.Moons.Add(moon);
            Bodies.Add(moon);
            return;
        }

        // fallback: add to last planet as previous behavior
        var lastPlanet = Bodies.OfType<Planet>().LastOrDefault();
        if (lastPlanet == null) return;
        int idx = lastPlanet.Moons.Count + 1;
        var moon2 = new Moon { Name = $"Luna {lastPlanet.Name}-{idx}", Mass = 1, OrbitRadius = 18 + 6 * idx, OrbitPeriod = 8 + 4 * idx };
        moon2.Parent = lastPlanet;
        lastPlanet.Moons.Add(moon2);
        Bodies.Add(moon2);
    }

    private void AddShip()
    {
        var ship = new Ship { Name = "Explorer", Speed = 80 };
        // place the ship near the star or at top-left if no star
        var star = Bodies.OfType<Star>().FirstOrDefault();
        if (star != null)
        {
            ship.X = star.X + star.DisplaySize + 10;
            ship.Y = star.Y + star.DisplaySize + 10;
            ship.CurrentSource = star;
        }
        else
        {
            ship.X = 100; ship.Y = 100;
        }
        // default target: first planet if available
        var planet = Bodies.OfType<Planet>().FirstOrDefault();
        if (planet != null)
        {
            ship.CurrentTarget = planet;
            // create mission and add to list
            var mission = new Mission { Description = $"{ship.Name} -> {planet.Name}", From = ship.CurrentSource, To = planet, Ship = ship };
            Missions.Add(mission);
        }
        Bodies.Add(ship);
    }

    // Context menu methods
    private void DeleteSelectedBody()
    {
        if (SelectedBody == null) return;
        Bodies.Remove(SelectedBody);
        SelectedBody = null;
    }

    private void CenterOnSelectedBody()
    {
        if (SelectedBody == null) return;
        // Could trigger camera center (requires Canvas manipulation from code-behind)
    }

    // Preset methods
    private void CreateSolarSystemPreset()
    {
        Bodies.Clear();
        var sun = new Star { Name = "Sol", Mass = 1000, X = 400, Y = 300 };
        Bodies.Add(sun);
        
        var earth = new Planet { Name = "Tierra", Mass = 1, OrbitRadius = 100, OrbitPeriod = 365, X = 500, Y = 300 };
        earth.Parent = sun;
        Bodies.Add(earth);
        
        var moon = new Moon { Name = "Luna", Mass = 0.1, OrbitRadius = 20, OrbitPeriod = 27 };
        moon.Parent = earth;
        earth.Moons.Add(moon);
        Bodies.Add(moon);
    }

    private void CreateBinarySystemPreset()
    {
        Bodies.Clear();
        var star1 = new Star { Name = "Estrella A", Mass = 500, X = 300, Y = 300 };
        var star2 = new Star { Name = "Estrella B", Mass = 400, X = 500, Y = 300 };
        Bodies.Add(star1);
        Bodies.Add(star2);
        
        var planet = new Planet { Name = "Planeta Binario", Mass = 2, OrbitRadius = 150, OrbitPeriod = 200, X = 350, Y = 400 };
        planet.Parent = star1;
        Bodies.Add(planet);
    }

    private void CreateMultiPlanetPreset()
    {
        Bodies.Clear();
        var star = new Star { Name = "Estrella Central", Mass = 800, X = 400, Y = 300 };
        Bodies.Add(star);
        
        // órbitas bien separadas para que se vean claramente en Sandbox
        double[] radii = new double[] { 120, 200, 280, 360 };

        for (int i = 0; i < radii.Length; i++)
        {
            double radius = radii[i];
            double angle = (Math.PI * 2 / radii.Length) * i;
            double x = 400 + radius * Math.Cos(angle);
            double y = 300 + radius * Math.Sin(angle);

            var planet = new Planet
            {
                Name = $"Planeta {i + 1}",
                Mass = 0.5 + i * 0.2,
                OrbitRadius = radius,
                OrbitPeriod = 80 + 40 * i,
                X = x,
                Y = y
            };
            planet.Parent = star;
            Bodies.Add(planet);
        }
    }

    // Save/Load methods
    private void SaveScenario()
    {
        // Serializar Bodies a JSON en archivo
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(Bodies.Cast<object>().ToList());
            System.IO.File.WriteAllText("scenario.json", json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
        }
    }

    private void LoadScenario()
    {
        // Desserializar Bodies desde JSON
        try
        {
            if (System.IO.File.Exists("scenario.json"))
            {
                var json = System.IO.File.ReadAllText("scenario.json");
                // Nota: esto requeriría un deserializador custom debido a herencia de CelestialBodyBase
                System.Diagnostics.Debug.WriteLine("Load not yet fully implemented");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading: {ex.Message}");
        }
    }
}
}
