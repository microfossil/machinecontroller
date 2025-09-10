using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using ModbusTCP_Simplified;

namespace ControlUI
{
    public partial class MainWindow : Window
    {
        public Modbus Modbus { get; set; }
        private DispatcherTimer uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            // lancer la connexion après chargement de la fenêtre
            Loaded += async (s, e) => await ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            Modbus = new Modbus();
            await Modbus.StartAsync();

            if (Modbus.IsConnected)
            {
                TxtStatus.Text = "✅ Connection successful";

                // Timer UI pour rafraîchir les valeurs affichées
                uiTimer = new DispatcherTimer();
                uiTimer.Interval = TimeSpan.FromMilliseconds(500);
                uiTimer.Tick += (s2, e2) => UpdateUI();
                uiTimer.Start();
            }
            else
            {
                TxtStatus.Text = "❌ Connection failed";
            }
        }

        private void UpdateUI()
        {
            if (Modbus.IsConnected)
            {
                TxtGEMMAMode.Text = $"{Modbus.GemmaMode} (decimal)\n{Modbus.GemmaMode:X2} (hexa)\n({Modbus.GetGEMMADescription(Modbus.GemmaMode)})";
                TxtFiole.Text = $"Fiole n°{Modbus.FioleNumber}";
                TxtWord90.Text = Modbus.TxtWord90;
            }
            else
            {
                TxtStatus.Text = "❌ Disconnected";
            }
        }

        // private void ReadGemmaMode_Click(object sender, RoutedEventArgs e)
        // {
        //     int mode = Modbus.GetGEMMAMode();
        //     TxtGEMMAMode.Text = $"{mode} (decimal)\n{mode:X2} (hexa)\n({Modbus.GetGEMMADescription(mode)})";
        // }

        private async void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            await Modbus.SetAutoModeAsync(true);
            TxtStatus.Text = "Cde_Auto.Mode_Auto - mode auto";
        }

        private async void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            await Modbus.SetAutoModeAsync(false);
            TxtStatus.Text = "Cde_Auto.Mode_Auto - mode manuel";
        }

        private async void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            await Modbus.InitAsync();
            TxtStatus.Text = "Cde_Auto.Init - demande initialisation";
        }

        private async void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            await Modbus.StartCycleAsync();
            TxtStatus.Text = "Cde_Auto.Start - demande départ cycle";
        }

        private async void ValidateFiole_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FioleInput.Text, out int NewFioleNumber))
            {
                // int CurrentFioleNumber = await Modbus.ReadHoldingRegisterAsync(105);
                await Modbus.SetVialNbAsync(NewFioleNumber);
                TxtStatus.Text = $"Numéro de fiole demandé : {NewFioleNumber}";
                // TxtFiole.Text = $"Old: {CurrentFioleNumber}, New: {NewFioleNumber}";
                // MessageBox.Show($"Valeur validée : {NewFioleNumber}", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
