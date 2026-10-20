using System;
using Last.UI;
using HarmonyLib;

namespace FF5BoostOptions;

/// <summary>
/// Tracing used to map the Boost screen's control flow. Enabled with Debug.VerboseLogging.
/// </summary>
internal static class Diagnostics
{
    public static bool Enabled => BoostOptionsPlugin.Verbose.Value;

    public static void Log(string message)
    {
        if (!Enabled)
            return;

        try
        {
            BoostOptionsPlugin.LogSource.LogInfo("[Trace] " + message);
        }
        catch
        {
            // Never let logging break the game.
        }
    }

    public static string Describe(ConfigCommandsData data)
    {
        if (data == null)
            return "data=null";

        try
        {
            return "type=" + data.ConfigCommandType;
        }
        catch (Exception e)
        {
            return "type=<" + e.GetType().Name + ">";
        }
    }

    /// <summary>
    /// Logs how many Harmony patches are attached to the methods the mod depends on, so a silent
    /// "patch never attached" failure is visible in the log.
    /// </summary>
    public static void VerifyPatches()
    {
        Verify(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertIndexIntoRateValue");
        Verify(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertRateValueIntoIndex");
        Verify(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "ConvertIndexIntoRateValue");
        Verify(typeof(Serial.FF5.UI.KeyInput.ConfigActualDetailsController), "SetCommandNameList");
        Verify(typeof(Serial.FF5.UI.Touch.ConfigActualDetailsController), "SetCommandNameList");
        Verify(typeof(Last.UI.KeyInput.ConfigCommandController), "SetNameText");
        Verify(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetAbpRate");
        Verify(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetAbpRate");
    }

    private static void Verify(Type type, string methodName)
    {
        try
        {
            var method = AccessTools.Method(type, methodName);
            var info = method == null ? null : Harmony.GetPatchInfo(method);
            Log($"patch check {type.Name}.{methodName}: " +
                (method == null ? "method not found" : $"prefixes={info?.Prefixes.Count ?? 0} postfixes={info?.Postfixes.Count ?? 0}"));
        }
        catch (Exception e)
        {
            Log($"patch check {type.Name}.{methodName} failed: {e.GetType().Name}");
        }
    }
}

internal static class DiagnosticPatches
{
    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigController), "Initialize")]
    private static class KeyConfigInit
    {
        private static void Postfix() => Diagnostics.Log("KeyInput.ConfigController.Initialize");
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigController), "Initialize")]
    private static class TouchConfigInit
    {
        private static void Postfix() => Diagnostics.Log("Touch.ConfigController.Initialize");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigController), "InitializeGameBoosterSetting")]
    private static class KeyBoostInit
    {
        private static void Prefix() => Diagnostics.Log("KeyInput.ConfigController.InitializeGameBoosterSetting");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigController), "UpdateGameBoosterSetting")]
    private static class KeyBoostUpdate
    {
        private static int _count;

        private static void Prefix()
        {
            if (++_count % 200 == 1)
                Diagnostics.Log($"KeyInput.ConfigController.UpdateGameBoosterSetting (call {_count})");
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "Initialize")]
    private static class KeyDetailsInit
    {
        private static void Postfix(Last.UI.KeyInput.ConfigActualDetailsControllerBase __instance)
            => Diagnostics.Log($"KeyInput details.Initialize on {__instance.GetType().Name}");
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "Initialize")]
    private static class TouchDetailsInit
    {
        private static void Postfix(Last.UI.Touch.ConfigActualDetailsControllerBase __instance)
            => Diagnostics.Log($"Touch details.Initialize on {__instance.GetType().Name}");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetCommandNameList")]
    private static class KeyBaseNameList
    {
        private static void Prefix(Last.UI.KeyInput.ConfigActualDetailsControllerBase __instance)
            => Diagnostics.Log($"KeyInput.SetCommandNameList (base) on {__instance.GetType().Name}");
    }

    [HarmonyPatch(typeof(Serial.FF5.UI.KeyInput.ConfigActualDetailsController), "SetCommandNameList")]
    private static class KeyNameList
    {
        private static void Prefix(Last.UI.KeyInput.ConfigCommandController controller)
        {
            string type;
            try { type = Diagnostics.Describe(controller.ConfigCommandsData); }
            catch (Exception e) { type = "<" + e.GetType().Name + ">"; }
            Diagnostics.Log($"FF5.KeyInput.SetCommandNameList {type}");
        }

        private static void Postfix(Last.UI.KeyInput.ConfigCommandController controller)
        {
            try
            {
                var list = controller.NameList;
                if (list == null)
                {
                    Diagnostics.Log("FF5.KeyInput.SetCommandNameList -> nameList null");
                    return;
                }

                var names = new string[Math.Min(list.Count, 8)];
                for (int i = 0; i < names.Length; i++)
                    names[i] = list[i];

                Diagnostics.Log($"FF5.KeyInput.SetCommandNameList -> {list.Count} names: {string.Join(", ", names)}");
            }
            catch (Exception e)
            {
                Diagnostics.Log("FF5.KeyInput.SetCommandNameList -> failed: " + e.GetType().Name);
            }
        }
    }

    [HarmonyPatch(typeof(Serial.FF5.UI.Touch.ConfigActualDetailsController), "SetCommandNameList")]
    private static class TouchNameList
    {
        private static void Prefix(Last.UI.Touch.ConfigCommandController controller)
        {
            string type;
            try { type = Diagnostics.Describe(controller.ConfigCommandsData); }
            catch (Exception e) { type = "<" + e.GetType().Name + ">"; }
            Diagnostics.Log($"FF5.Touch.SetCommandNameList {type}");
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetNameText")]
    private static class KeySetNameText
    {
        private static void Prefix(string text) => Diagnostics.Log($"row.SetNameText(\"{text}\")");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetArrowChangeText")]
    private static class KeySetArrowText
    {
        private static void Prefix(string text) => Diagnostics.Log($"row.SetArrowChangeText(\"{text}\")");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetChangeArrowCommandText")]
    private static class KeySetChangeArrowText
    {
        private static void Prefix(Last.UI.KeyInput.ConfigCommandController __instance, int index)
        {
            string type;
            try { type = Diagnostics.Describe(__instance.ConfigCommandsData); }
            catch (Exception e) { type = "<" + e.GetType().Name + ">"; }
            Diagnostics.Log($"row.SetChangeArrowCommandText index={index} {type}");
        }
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigCommandController), "SetChangeArrowCommandText")]
    private static class TouchSetChangeArrowText
    {
        private static void Prefix(Last.UI.Touch.ConfigCommandController __instance, int index)
        {
            string type;
            try { type = Diagnostics.Describe(__instance.ConfigCommandsData); }
            catch (Exception e) { type = "<" + e.GetType().Name + ">"; }
            Diagnostics.Log($"touch row.SetChangeArrowCommandText index={index} {type}");
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetAbpRate")]
    private static class KeySetAbpRate
    {
        private static void Prefix(int index)
            => Diagnostics.Log($"details.SetAbpRate(index={index}) -> {BoostRates.IndexToRate(index)}x");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertIndexIntoRateValue")]
    private static class KeyConvertIndex
    {
        private static void Prefix(ConfigCommandType type, int index)
            => Diagnostics.Log($"ConvertIndexIntoRateValue type={type} index={index}");
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertRateValueIntoIndex")]
    private static class KeyConvertRate
    {
        private static void Prefix(ConfigCommandType type, float rateValue)
            => Diagnostics.Log($"ConvertRateValueIntoIndex type={type} rate={rateValue}");
    }
}
