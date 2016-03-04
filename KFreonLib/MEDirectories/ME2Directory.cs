﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace KFreonLib.MEDirectories
{
    public static class ME2Directory
    {
        private static string _gamePath = null;
        public static string gamePath
        {
            get // if you are trying to use gamePath variable and it's null it asks to locate ME3 exe file
            {
                if (_gamePath == null)
                    _gamePath = KFreonLib.Misc.Methods.SelectGameLoc(2);
                return _gamePath;
            }
            private set { _gamePath = value; }
        }

        public static string GamePath(string path = null)
        {
            if (path != null)
                _gamePath = path;
            return _gamePath;
        }

        public static string cookedPath { get { return (gamePath != null) ? Path.Combine(gamePath,  @"BioGame\CookedPC") : null; } }
        public static string DLCPath { get { return (gamePath != null) ? Path.Combine(gamePath, @"BioGame\DLC\") : null; } }

        // "C:\...\MyDocuments\BioWare\Mass Effect 2\" folder
        public static string BioWareDocPath { get { return Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\BioWare\Mass Effect 2\"; } }
        public static string GamerSettingsIniFile { get { return BioWareDocPath + @"BIOGame\Config\GamerSettings.ini"; } }

        public static string DLCFilePath(string DLCName)
        {
            string fullPath = DLCPath + DLCName + @"\CookedPC";
            if (File.Exists(fullPath))
                return fullPath;
            else
                throw new FileNotFoundException("Invalid DLC path " + fullPath);
        }

        static ME2Directory()
        {
            string hkey32 = @"HKEY_LOCAL_MACHINE\SOFTWARE\";
            string hkey64 = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\";
            string subkey = @"BioWare\Mass Effect 2";
            string keyName;

            keyName = hkey32 + subkey;
            string test = (string)Microsoft.Win32.Registry.GetValue(keyName, "Path", null);
            if (test != null)
            {
                gamePath = test;
                return;
            }

            /*if (gamePath != null)
            {
                gamePath = gamePath + "\\";
                return;
            }*/

            keyName = hkey64 + subkey;
            gamePath = (string)Microsoft.Win32.Registry.GetValue(keyName, "Path", null);
            if (gamePath != null)
            {
                gamePath = gamePath + "\\";
                return;
            }
        }
    }
}
