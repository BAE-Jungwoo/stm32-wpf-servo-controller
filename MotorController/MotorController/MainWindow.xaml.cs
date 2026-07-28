using System.Windows;

namespace MotorController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Use MainViewModel to manage UART connection via UI controls.
            DataContext = new MainViewModel();
        }
    }
}