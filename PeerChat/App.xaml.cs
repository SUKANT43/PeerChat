using Microsoft.Win32;
using PeerChat.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PeerChat
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string currentTheme = ThemeHelper.GetWindowsTheme();

            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var themeDictionary = dictionaries[0];

            if (currentTheme== "Dark Theme")
            {
                themeDictionary.Source = new Uri("themes/DarkTheme.xaml", UriKind.Relative);
            }
            else
            {
                themeDictionary.Source = new Uri("themes/LightTheme.xaml", UriKind.Relative);
            }


        }
    }
}
