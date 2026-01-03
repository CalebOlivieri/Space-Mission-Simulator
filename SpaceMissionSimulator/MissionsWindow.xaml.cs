using System.Windows;

namespace SpaceMissionSimulator
{
    public partial class MissionsWindow : Window
    {
        public MissionsWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            vm.SwitchToMissions();
            // subscribe to ViewModel's return-to-menu event
            vm.ReturnToMenuRequested += Vm_ReturnToMenuRequested;
        }

        private void Vm_ReturnToMenuRequested(object? sender, System.EventArgs e)
        {
            var menu = new MenuWindow();
            menu.Show();
            this.Close();
        }
    }
}
