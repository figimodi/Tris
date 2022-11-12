using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Tris
{
    public partial class MainWindow : Window
    {
        public IPEndPoint ipRem;
        string[] sign = new string[2];
        Label[] grid = new Label[9];

        public MainWindow()
        {
            InitializeComponent();
            Reset();
            ReciveData();
        }

        private void SendMessage(string message, IPEndPoint ep)
        {
            byte[] datiTx;

            UdpClient client = new UdpClient();

            try
            {
                client.Connect(ep);
                datiTx = Encoding.ASCII.GetBytes(message);
                client.Send(datiTx, datiTx.Length);
                if (message == "START") {
                    txtIP.Foreground = Brushes.Gray;
                    txtIP.Text = "Aspettando risposta...";
                }
            }
            catch (Exception e) { MessageBox.Show(e.ToString()); }
        }

        private async void ReciveData()
        {

            byte[] datiRx;
            string result;

            UdpClient reciver = new UdpClient(2025);

            while (true)
            {
                var temp = await reciver.ReceiveAsync();
                datiRx = temp.Buffer;
                result = Encoding.ASCII.GetString(datiRx);
                switch (result)
                {
                    case "START":
                        var dialogResult = MessageBox.Show("Hai ricevuto un invito a giocare da " + temp.RemoteEndPoint.Address.ToString() + ", vuoi unirti alla partita?", "ATTENZIONE", MessageBoxButton.YesNo);
                        if (dialogResult.ToString() == "Yes") {
                            ipRem = new IPEndPoint( temp.RemoteEndPoint.Address, 2025);
                            SendMessage("OK", ipRem);
                            txtIP.Foreground = Brushes.Green;
                            txtIP.Text = "Connesso a " + ipRem.Address.ToString();
                            txtIP.IsReadOnly = true;
                            btnDisconnect.Content = "Disconnetti";
                        }                          
                        else
                            SendMessage("NO", temp.RemoteEndPoint);
                        break;
                    case "NO":
                        txtIP.Text = "";
                        MessageBox.Show(temp.RemoteEndPoint.Address.ToString() + "Ha rifiutato la tua richiesta");
                        break;
                    case "OK":
                        txtIP.Foreground = Brushes.Green;
                        txtIP.Text = "Connesso a " + temp.RemoteEndPoint.Address.ToString();
                        txtIP.IsReadOnly = true;
                        btnDisconnect.Content = "Disconnetti";
                        Random r = new Random();
                        if (r.Next(0, 2) == 0)
                        {
                            sign[0] = "X";
                            sign[1] = "O";
                            SendMessage("O", ipRem);
                            MessageBox.Show("Inizia l'avversario");
                        }
                        else {
                            sign[0] = "O";
                            sign[1] = "X";
                            SendMessage("X", ipRem);
                            MessageBox.Show("Inizi tu");
                            EnableLabel();
                        }
                        break;
                    case "DISCONNECT":
                        MessageBox.Show(temp.RemoteEndPoint.Address.ToString() + " è uscito dalla partita!");
                        txtIP.IsReadOnly = false;
                        txtIP.Text = "";
                        Reset();
                        break;
                    case "END":  
                        var dialogResult2 = MessageBox.Show("Hai perso! Vuoi chiedere la rivincita?", "ATTENZIONE", MessageBoxButton.YesNo);
                        if (dialogResult2.ToString() == "Yes")
                            SendMessage("REMATCH", ipRem);
                        else
                            SendMessage("NO_REMATCH", ipRem);
                        Reset();
                        break;
                    case "REMATCH":
                        var dialogResult3 = MessageBox.Show("L'avversario ha chiesto la rivincita, desideri giocare ancora?", "ATTENZIONE", MessageBoxButton.YesNo);
                        if (dialogResult3.ToString() == "Yes")
                            SendMessage("OK", ipRem);
                        else
                            SendMessage("NO", ipRem);
                        Reset();
                        break;
                    case "NO_REMATCH":
                        MessageBox.Show("l'avversario ha abbandonato", "ATTENZIONE");
                        Reset();
                        break;
                    case "O":
                        sign[0] = "O";
                        sign[1] = "X";
                        MessageBox.Show("Inizi tu");
                        EnableLabel();
                        break;
                    case "X":
                        sign[0] = "X";
                        sign[1] = "O";
                        MessageBox.Show("Inizia l'avversario");
                        break;
                    default:
                        grid[Convert.ToInt16(result.Substring(6, 1))].Content = sign[1];
                        EnableLabel();
                        break;
                }
            }
        }

        private void TxtIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Validate();
                SendMessage("START", ipRem);
                btnDisconnect.IsEnabled = true;
            }
        }

        private void TxtIP_GotFocus(object sender, RoutedEventArgs e)
        {
            txtIP.Text = "";
        }

        private void Click(object sender, MouseButtonEventArgs e)
        {
            string name = ((Label)sender).Name.ToString();
            grid[Convert.ToInt16(name.Substring(3, 1))].Content = sign[0];
            DisableLabel();
            SendMessage("LABEL/" + name.Substring(3, 1), ipRem);
            if (Win(Convert.ToInt16(name.Substring(3, 1)))) {
                MessageBox.Show("Hai vinto!!");
                SendMessage("END", ipRem);
            }               
        }

        private void Validate() {
            ipRem = new IPEndPoint(IPAddress.Parse(txtIP.Text), 2025);
        }

        private void Reset() {
            grid[0] = lbl0;
            grid[1] = lbl1;
            grid[2] = lbl2;
            grid[3] = lbl3;
            grid[4] = lbl4;
            grid[5] = lbl5;
            grid[6] = lbl6;
            grid[7] = lbl7;
            grid[8] = lbl8;
            for (int i = 0; i < 9; i++) {
                grid[i].IsEnabled = false;
                grid[i].Content = "";
            }
            btnDisconnect.Content = "Stop";
            btnDisconnect.IsEnabled = false;
            txtIP.Text = "inserire qui l'ip";
            txtIP.IsReadOnly = false;
        }
        private void EnableLabel() {
            int counter = 0;
            for (int i = 0; i < 9; i++) {
                if ((string)grid[i].Content == "")
                {
                    counter++;
                    grid[i].IsEnabled = true;
                }
            }
            if (counter == 0) {
                var dialogResult3 = MessageBox.Show("La partita è finita in pareggio, vuoi fare un' altra partita?", "ATTENZIONE", MessageBoxButton.YesNo);
                if (dialogResult3.ToString() == "Yes")
                    SendMessage("REMATCH", ipRem);
                else
                    SendMessage("NO_REMATCH", ipRem);
                Reset();
            }     
        }

        private void DisableLabel() {
            for (int i = 0; i < 9; i++)
            {
                grid[i].IsEnabled = false;
            }
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (btnDisconnect.Content == "Stop")
                Reset();
            else
            {
                var dialogResult = MessageBox.Show("Sei sicuro di volere abbandonare la partita?", "ATTENZIONE", MessageBoxButton.YesNo);
                if (dialogResult.ToString() == "Yes")
                {
                    SendMessage("DISCONNECT", ipRem);
                    txtIP.IsReadOnly = false;
                    txtIP.Text = "inserisci qui l'ip";
                    Reset();
                }
            }
        }

        private bool Win(int x) {
            if (x == 0 || x == 1 || x == 2)
            {
                if (grid[x].Content == grid[x + 3].Content && grid[x].Content == grid[x + 6].Content) return true;       
            }
            if (x == 3 || x == 4 || x == 5)
            {
                if (grid[x].Content == grid[x + 3].Content && grid[x].Content == grid[x - 3].Content) return true;
            }
            if (x == 6 || x == 7 || x == 8)
            {
                if (grid[x].Content == grid[x - 3].Content && grid[x].Content == grid[x - 6].Content) return true;
            }
            if (x == 0 || x == 3 || x == 6)
            {
                if (grid[x].Content == grid[x + 1].Content && grid[x].Content == grid[x + 2].Content) return true;
            }
            if (x == 1 || x == 4 || x == 7)
            {
                if (grid[x].Content == grid[x + 1].Content && grid[x].Content == grid[x - 1].Content) return true;
            }
            if (x == 2 || x == 5 || x == 8)
            {
                if (grid[x].Content == grid[x - 1].Content && grid[x].Content == grid[x - 2].Content) return true;
            }
            if (x == 0 || x == 4 || x == 8)
            {
                if (grid[0].Content == grid[4].Content && grid[0].Content == grid[8].Content) return true;
            }
            if (x == 2 || x == 4 || x == 6)
            {
                if (grid[2].Content == grid[4].Content && grid[2].Content == grid[6].Content) return true;
            }
            return false;
        }
    }
}
