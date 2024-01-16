using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


namespace FeedReader
{
    public sealed partial class RenameDialog : ContentDialog
    {
        public string OldTitle { get; set; }
        public string NewTitle { get; set; }

        public bool IsNewTitleSameAsOldTitle { get; set; } = true;

        public RenameDialog(string oldTitle)
        {
            this.InitializeComponent();

            OldTitle = oldTitle;
            NewTitle = OldTitle;

            NewTitleTxtBox.SelectAll();
        }

        private void TitleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IsNewTitleSameAsOldTitle = OldTitle == NewTitle;
        }
    }
}
