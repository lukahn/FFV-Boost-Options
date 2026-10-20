using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace FFVBoostOptions;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class BoostOptionsPlugin : BasePlugin
{
    public const string PluginGuid = "com.lukahn.ff5.boostoptions";
    public const string PluginName = "FFV Boost Options";
    public const string PluginVersion = "1.0.0";

    internal static ManualLogSource LogSource;
    internal static ConfigEntry<int> MaxMultiplier;
    internal static ConfigEntry<bool> Verbose;

    public override void Load()
    {
        LogSource = Log;

        MaxMultiplier = Config.Bind(
            "Options",
            "MaxMultiplier",
            99,
            "Highest multiplier offered in the Boost menu. The menu becomes " +
            "0x, 0.5x, 1x, 2x, 3x, ... up to this value.");

        Verbose = Config.Bind(
            "Debug",
            "VerboseLogging",
            false,
            "Log how Boost menu rows are built and how option indices are converted to multipliers.");

        var harmony = new Harmony(PluginGuid);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        Diagnostics.VerifyPatches();

        LogSource.LogInfo(
            $"{PluginName} {PluginVersion} loaded (options 0x, 0.5x, 1x, 2x ... {MaxMultiplier.Value}x, verbose={Verbose.Value}).");
    }
}
