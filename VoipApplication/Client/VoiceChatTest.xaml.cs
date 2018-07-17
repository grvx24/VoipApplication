using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;

namespace VoIP_Client
{
    /// <summary>
    /// Interaction logic for VoiceChatTest.xaml
    /// </summary>
    public partial class VoiceChatTest : UserControl
    {
        VoiceChatClient voiceChatClient = new VoiceChatClient(2999);

        public VoiceChatTest()
        {
            InitializeComponent();
        }

        private void OnConnectButton(object sender, RoutedEventArgs e)
        {
            voiceChatClient.Connect(new IPEndPoint(IPAddress.Parse(IpTextBox.Text),int.Parse(PortTextBox.Text)));
        }

        private void OnListenButton(object sender, RoutedEventArgs e)
        {
            if (!voiceChatClient.IsListening)
            {
                ListenButton.Content = "Stop";
                voiceChatClient.StartUdpListeningThread();
            }
            else
            {
                ListenButton.Content = "Start";
                voiceChatClient.Disconnect();
            }
                
        }

        private void PrintDebugMsg(string msg)
        {
            MessageBox.Show(msg);
        }
    }
}
