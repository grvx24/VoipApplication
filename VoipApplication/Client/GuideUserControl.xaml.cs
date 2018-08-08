using System.Windows;
using System.Windows.Controls;

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
        }
    }
}
