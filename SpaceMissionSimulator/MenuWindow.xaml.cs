using System.Windows;

namespace SpaceMissionSimulator
{
    public partial class MenuWindow : Window
    {
        public MenuWindow()
        {
            InitializeComponent();
        }

        private void SandboxBtn_Click(object sender, RoutedEventArgs e)
        {
            var main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void MissionsBtn_Click(object sender, RoutedEventArgs e)
        {
            var mw = new MissionsWindow();
            mw.Show();
            this.Close();
        }
    }
}
