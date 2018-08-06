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
            GuideTextBlock.Text = GuideText();
        }
        private static string GuideText()
        {
            return
                  "Ustawienia:\tumożliwia zmianę adresu email, zmianę hasła, wybór mikrofonu.\n\n" +
                  "Użytkownicy:\t\n\n" +
                  "\tWyszukaj:\tumożliwia wyszukiwanie użytkowników na serwerze.\n\n" +
                  "\tOnline:\t\tpokazuje aktualnie zalogowanych uzytkowników.\n\n" +
                  "\tUlubione:\tpokazuje ulubionych użytkowników.\n\n" +
                  "\tZadzwoń:\tnawiązuje połączenie głosowe z danym użytkownikiem.\n\n" +
                  "\tEdytuj:\t\tumożliwia dodanie użytkownika do ulubionych po podaniu nazwy, zmianę tej nazwy lub usuniecie z ulubionych po usunięciu nazwy.\n\n" +
                  "Instrukcja:\tzawiera skrót funkcji aplikacji.\n\n" +
                  "Wyloguj się:\twylogowuje użytkownika.";
        }
    }
}
