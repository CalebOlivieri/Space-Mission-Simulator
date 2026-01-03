using System.Windows;
using System.Windows.Input;

namespace SpaceMissionSimulator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // default DataContext if none provided
            if (DataContext == null)
            {
                DataContext = new MainViewModel();
            }
            // Suscribirse al evento de volver al menú
            if (DataContext is MainViewModel vm)
            {
                vm.ReturnToMenuRequested += ViewModel_ReturnToMenuRequested;
            }
        }

        private void ViewModel_ReturnToMenuRequested(object? sender, EventArgs e)
        {
            MenuWindow menuWindow = new MenuWindow();
            menuWindow.Show();
            this.Close();
        }

        public MainWindow(SpaceMissionSimulator.MainViewModel.GameMode initialMode) : this()
        {
            // if launched with a specific mode, set ViewModel accordingly
            if (DataContext is MainViewModel vm)
            {
                if (initialMode == MainViewModel.GameMode.Missions) vm.SwitchToMissions();
                else vm.SwitchToSandbox();
            }
        }

        private void Body_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // DataContext of the template root is the bound body
                if (sender is FrameworkElement fe && fe.DataContext is SpaceMissionSimulator.Models.CelestialBodyBase body)
                {
                    vm.SelectBody(body);
                }
            }
        }

        private void SpaceCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.IsPlacingBlackHole)
            {
                var pos = e.GetPosition(SpaceCanvas);
                vm.PlaceBlackHoleAt(pos.X, pos.Y);
                e.Handled = true;
            }
        }

        private void Body_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Select body and open context menu
            if (sender is FrameworkElement fe && fe.DataContext is SpaceMissionSimulator.Models.CelestialBodyBase body)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.SelectBody(body);
                }
            }

            // Create context menu
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            var deleteItem = new System.Windows.Controls.MenuItem { Header = "Eliminar" };
            deleteItem.Click += (s, args) =>
            {
                if (DataContext is MainViewModel vm && vm.SelectedBody != null)
                {
                    vm.DeleteBodyCommand.Execute(null);
                }
            };
            contextMenu.Items.Add(deleteItem);

            var centerItem = new System.Windows.Controls.MenuItem { Header = "Centrar cámara" };
            centerItem.Click += (s, args) =>
            {
                if (DataContext is MainViewModel vm && vm.SelectedBody != null)
                {
                    vm.CenterOnBodyCommand.Execute(null);
                }
            };
            contextMenu.Items.Add(centerItem);

            contextMenu.IsOpen = true;
            e.Handled = true;
        }
    }
}