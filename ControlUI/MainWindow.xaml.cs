using System;
using System.Windows;
using ModbusTCP_Simplified;

namespace ControlUI
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

        private void ReadGemmaMode_Click(object sender, RoutedEventArgs e)
        {
            int mode = _modbus.GetGEMMAMode();
            if (mode >= 0)
            {
                GemmaModeText.Text = $"Mode: {mode} ({_modbus.GetGEMMADescription(mode)})";
            }
            else
            {
                GemmaModeText.Text = "Erreur de lecture (non connecté ?)";
            }
        }

        private async void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            // await _modbus.SetAutoModeAsync(true);
            TxtGemma.Text = "GEMMA mode: Auto";
        }

        private async void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            // await _modbus.SetAutoModeAsync(false);
            TxtGemma.Text = "GEMMA mode: Manuel";
        }

        private void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Initialisation started");
            TxtGemma.Text = "GEMMA mode: Initialisation";
        }
    }
}
