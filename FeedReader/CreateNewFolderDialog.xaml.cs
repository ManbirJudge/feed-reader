using Windows.UI.Xaml.Controls;

namespace FeedReader
{
    public sealed partial class CreateNewFolderDialog : ContentDialog
    {
        public string FolderName { get; set; }

        public CreateNewFolderDialog()
        {
            this.InitializeComponent();
        }
    }
}
