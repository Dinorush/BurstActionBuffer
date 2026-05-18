using System.IO;
using BepInEx;
using BepInEx.Configuration;
using GTFO.API.Utilities;

namespace BurstActionBuffer
{
    internal static class Configuration
    {
        public static float BufferTime => _bufferTime.Value;
        private static ConfigEntry<float> _bufferTime = null!;

        internal static void Init()
        {
            var config = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            _bufferTime = config.Bind("Base Settings", "Buffer Time", 0.5f, "Maximum allowed time between buffering an action and finishing the burst.");
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += (_) => config.Reload();
        }
    }
}
