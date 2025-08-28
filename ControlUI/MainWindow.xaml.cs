using System.Windows;
using ModbusTCP_Simplified;

namespace MachineControlUI
{
    public partial class MainWindow : Window
    {
        private Modbus _modbus;

        public MainWindow()
        {
            InitializeComponent();
            _modbus = new Modbus();
            _ = _modbus.StartAsync();
        }

        private void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            _modbus.SetModeAuto();
            TxtGemma.Text = "GEMMA mode: Auto";
        }

        private void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            _modbus.SetModeManual();
            TxtGemma.Text = "GEMMA mode: Manuel";
        }

        private void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            _modbus.StartInitCycle();
            TxtGemma.Text = "GEMMA mode: Initialisation";
        }
    }
}
