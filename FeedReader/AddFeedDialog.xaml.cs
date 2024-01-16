using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace FeedReader
{
    public sealed partial class AddFeedDialog : ContentDialog
    {
        public string FeedUrl { get; set; }

        private readonly ObservableCollection<FeedFolder> AvailableFolders = new();

        public AddFeedDialog(List<FeedFolder> availableFolders)
        {
            InitializeComponent();

            foreach (var idk in availableFolders)
            {
                AvailableFolders.Add(idk);
            }
        }

        public int GetSelectedFolderIndex()
        {
            return AddFeedDialogFolderCombo.SelectedIndex;
        }
    }
}
