using System;

namespace WAT_Planner
{
    public struct Data
    {
        public const int weekCount = 22;
        private static string DocsPath { get => Environment.GetFolderPath(Environment.SpecialFolder.Personal); }
        public static string HomePath { get => DocsPath + "\\wat_plan\\"; }
        public static string KeyPath { get => HomePath + "key.wat"; }
        public static string PasswordPath { get => HomePath + "password.wat"; }
        public static string LoginPath { get => HomePath + "login.wat"; }
        public static string ConfigPath { get => HomePath + "config.conf"; }
    }
}
