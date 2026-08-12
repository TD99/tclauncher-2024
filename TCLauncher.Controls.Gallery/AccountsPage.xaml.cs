using System;
using System.Windows.Controls;
using TCLauncher.Models;
using TCLauncher.MVVM.Controls;

namespace TCLauncher.Controls.Gallery
{
    [Story("Accounts", Description = "Account presentation states with gallery-safe sample data.")]
    public partial class AccountsPage : UserControl
    {
        public AccountsPage()
        {
            InitializeComponent();
            OfflineAccountHost.Content = new OfflineAccountControl(new OfflineProfile
            {
                Id = Guid.NewGuid(),
                Username = "Gallery player",
                CreatedAtUtc = DateTime.UtcNow
            }, false);
        }
    }
}
