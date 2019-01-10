using UnityEngine;

namespace InstrumentPack
{
    public class Implementation
    {
        private const string NAME = "Instrument-Pack";

        private static Settings settings;

        internal static bool Mistakes
        {
            get => settings.Mistakes;
        }

        public static void OnLoad()
        {
            Log("Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            settings = Settings.Load();
            settings.AddToModSettings(NAME, ModSettings.MenuType.Both);
        }

        internal static void Log(string message)
        {
            Debug.LogFormat("[" + NAME + "] {0}", message);
        }

        internal static void Log(string message, params object[] parameters)
        {
            string preformattedMessage = string.Format("[" + NAME + "] {0}", message);
            Debug.LogFormat(preformattedMessage, parameters);
        }
    }
}