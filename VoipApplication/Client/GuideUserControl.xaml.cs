using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using VoIP_Server;
using VoIP_Server.Client;
using cscprotocol;
using VoipApplication;
using NAudio.Wave;

namespace VoIP_Client
{
    public partial class GuideUserControl : UserControl
    {
        CallingService callingService;
        Client client;

        public GuideUserControl(Client client, CallingService callingService, Window parentWindow)
        {
            this.client = client;
            this.callingService = callingService;
            InitializeComponent();
            GuideTextBlock.Text = GuideText();
        }
        private static string GuideText()
        {
            return
                  "";
        }
    }
}
