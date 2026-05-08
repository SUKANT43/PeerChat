using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeerChat.Helper
{
    public class ThemeHelper
    {
        public static string GetWindowsTheme()
        {
            const string registryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string registryValueName = "AppsUseLightTheme";

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(registryKeyPath))
                {
                    if (key.GetValue(registryValueName) is int value)
                    {
                        return value == 1 ? "Light Theme" : "Dark Theme";
                    }
                }
            }
            catch { }

            return "Dark Theme";
        }

    }
}
