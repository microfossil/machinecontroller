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
            await _modbus.SetAutoModeAsync(true);
            TxtStatus.Text = "Cde_Auto.Mode_Auto - mode auto";
        }

        private async void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            await _modbus.SetAutoModeAsync(false);
            TxtStatus.Text = "Cde_Auto.Mode_Auto - mode manuel";
        }

        private async void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            await InitAsync();
            TxtStatus.Text = "Cde_Auto.Init - demande initialisation";
        }

        private async void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            await StartCycleAsync();
            TxtStatus.Text = "Cde_Auto.Start - demande départ cycle";
        }

        private async void ValidateFiole_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FioleInput.Text, out int fioleNumber))
            {
                await SetVialNbAsync(fioleNumber);
                TxtFiole.Text = fioleNumber.ToString();
                MessageBox.Show($"Valeur validée : {fioleNumber}", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
