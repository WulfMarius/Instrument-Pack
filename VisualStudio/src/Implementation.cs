using UnityEngine;

using ModComponentMapper;

namespace InstrumentPack
{
    public class Implementation
    {
        public static void OnLoad()
        {
            Debug.Log("[Instrument-Pack]: Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
        }
    }
}