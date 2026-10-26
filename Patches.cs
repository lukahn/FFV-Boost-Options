using System;
using System.Globalization;
using HarmonyLib;
using Last.Management;
using Last.UI;
using Il2CppStringList = Il2CppSystem.Collections.Generic.List<string>;

namespace FFVBoostOptions;

internal static class BoostMenuHooks
{
    /// <summary>
    /// Replaces the option labels of a Boost row with 0x, 0.5x, 1x, 2x, 3x, ...
    /// The game's own (localised) labels are kept for the first four entries.
    /// Returns the label for the row's current selection, or null when the row is not a rate row.
    /// </summary>
    public static string ExtendNameList(ConfigCommandsData data, Il2CppStringList list, int targetIndex)
    {
        if (data == null || list == null)
            return null;

        ConfigCommandType type;
        try
        {
            type = data.ConfigCommandType;
        }
        catch
        {
            return null;
        }

        if (!BoostRates.IsRateCommand(type))
            return null;

        int max = BoostRates.MaxIndex;
        if (list.Count != max + 1)
        {
            var vanilla = new string[list.Count];
            for (int i = 0; i < vanilla.Length; i++)
                vanilla[i] = list[i];

            list.Clear();
            for (int i = 0; i <= max; i++)
                list.Add(i < vanilla.Length && i <= 3 ? vanilla[i] : BoostRates.Label(i));

            if (BoostOptionsPlugin.Verbose.Value)
            {
                BoostOptionsPlugin.LogSource.LogInfo(
                    $"[Boost] {type}: {vanilla.Length} vanilla option(s) -> {list.Count} options " +
                    $"(last = {list[list.Count - 1]}, targetIndex = {targetIndex}).");
            }
        }

        // The game filled the row's text from the old five-entry list, which is out of range for
        // custom values; rebuild it from the multiplier the game has stored.
        int index = targetIndex;
        float stored;
        bool hasStored = StoredBoostRate.TryGet(type, out stored);
        if (hasStored)
            index = BoostRates.RateToIndex(stored);

        int clamped = Math.Clamp(index, 0, max);

        if (BoostOptionsPlugin.Verbose.Value)
        {
            BoostOptionsPlugin.LogSource.LogInfo(
                $"[Boost] {type}: stored={(hasStored ? stored.ToString("0.###") : "?")}x " +
                $"targetIndex={targetIndex} -> index {clamped} ({BoostRates.Label(clamped)})");
        }

        return BoostRates.Label(clamped);
    }

    public static void LogArrowText(ConfigCommandsData data, int index)
    {
        if (!BoostOptionsPlugin.Verbose.Value || data == null)
            return;

        try
        {
            BoostOptionsPlugin.LogSource.LogInfo($"[Boost] SetChangeArrowCommandText {data.ConfigCommandType} index={index}");
        }
        catch
        {
            // Ignore logging failures.
        }
    }

    public static void LogSelectionMove(Il2CppStringList list, int index, string what)
    {
        if (!BoostOptionsPlugin.Verbose.Value)
            return;

        try
        {
            BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {what} targetIndex={index} nameListCount={list?.Count.ToString() ?? "null"}");
        }
        catch
        {
            // Ignore logging failures.
        }
    }

    /// <summary>
    /// Writes a multiplier straight into the game's CheatSettingsClient, which is the only layer
    /// that stores the value; the menu helpers are just conversions on top of it.
    /// </summary>
    public static void SetClientRate(ConfigCommandType type, float rate)
    {
        CheatSettingsClient client = CheatSettingsClient.Instance();
        if (client == null)
            return;

        switch (type)
        {
            case ConfigCommandType.ExpRate:
                client.SetExpRate(rate);
                break;
            case ConfigCommandType.GilRate:
                client.SetGilRate(rate);
                break;
            case ConfigCommandType.AbpRate:
                client.SetAbpRate(rate);
                break;
            case ConfigCommandType.LearningProficiencyRate:
                client.SetLearningProficiencyRate(rate);
                break;
            case ConfigCommandType.MagicProficiencyRate:
                client.SetMagicProficiencyRate(rate);
                break;
            case ConfigCommandType.ParameterUpRate:
                client.SetParameterUpRate(rate);
                break;
            case ConfigCommandType.MagicLearningProficiencyRate:
                client.SetMagicLearningProficiencyRate(rate);
                break;
        }
    }

    /// <summary>
    /// The game's own Set*Rate(index) helpers keep a private five-entry table and read past its end
    /// for custom indices, silently storing 0. Drive the client directly instead.
    /// </summary>
    public static bool ApplyRate(ConfigCommandType type, int index)
    {
        if (!BoostRates.IsRateCommand(type))
            return true;

        try
        {
            float rate = BoostRates.IndexToRate(index);
            SetClientRate(type, rate);

            if (BoostOptionsPlugin.Verbose.Value)
                BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {type}: set index {index} -> {rate}x");

            return false;
        }
        catch (Exception e)
        {
            BoostOptionsPlugin.LogSource.LogWarning($"[Boost] {type}: set index {index} failed: {e.Message}");
            return true;
        }
    }

    /// <summary>
    /// Puts the multiplier the row is showing back into the save data once the game has finished
    /// with an arrow press.
    /// <para>
    /// Every rate row except EXP goes through a virtual <c>Set*Rate(int)</c> call that the prefixes
    /// above can replace. The EXP case is compiled straight into
    /// <c>ConfigActualDetailsControllerBase.SwitchArrowSelectTypeProcess</c>: the game decodes its
    /// own five-entry table there and calls <c>CheatSettingsClient.SetExpRate</c> itself, so the
    /// <c>SetExpRate(int)</c> method the prefix is attached to is never called and an index above 4
    /// still stores 0. Re-asserting the row's own index afterwards fixes that, and is a no-op for
    /// the rows that were already stored correctly.
    /// </para>
    /// </summary>
    public static void ApplyStoredRate(ConfigCommandsData data, int index, string what)
    {
        if (data == null)
            return;

        ConfigCommandType type;
        try
        {
            type = data.ConfigCommandType;
        }
        catch
        {
            return;
        }

        if (!BoostRates.IsRateCommand(type))
            return;

        try
        {
            float rate = BoostRates.IndexToRate(index);

            float stored;
            bool hasStored = StoredBoostRate.TryGet(type, out stored);
            if (hasStored && Math.Abs(stored - rate) < 0.0001f)
            {
                if (BoostOptionsPlugin.Verbose.Value)
                    BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {what} {type}: index {index} already stored as {stored}x");
                return;
            }

            SetClientRate(type, rate);

            if (BoostOptionsPlugin.Verbose.Value)
            {
                BoostOptionsPlugin.LogSource.LogInfo(
                    $"[Boost] {what} {type}: index {index} -> {rate}x " +
                    $"(game had stored {(hasStored ? stored.ToString("0.###", CultureInfo.InvariantCulture) : "?")}x)");
            }
        }
        catch (Exception e)
        {
            BoostOptionsPlugin.LogSource.LogWarning($"[Boost] {what} {type}: re-apply index {index} failed: {e.Message}");
        }
    }
}

internal static class BoostRatesPatch
{
    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertIndexIntoRateValue")]
    private static class KeyInputIndexToRate
    {
        private static bool Prefix(ConfigCommandType type, int index, ref float __result)
        {
            if (!BoostRates.IsRateCommand(type))
                return true;

            __result = BoostRates.IndexToRate(index);
            if (BoostOptionsPlugin.Verbose.Value)
                BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {type}: index {index} -> {__result}x");

            return false;
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "ConvertRateValueIntoIndex")]
    private static class KeyInputRateToIndex
    {
        private static bool Prefix(ConfigCommandType type, float rateValue, ref int __result)
        {
            if (!BoostRates.IsRateCommand(type))
                return true;

            __result = BoostRates.RateToIndex(rateValue);
            if (BoostOptionsPlugin.Verbose.Value)
                BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {type}: {rateValue}x -> index {__result}");

            return false;
        }
    }

    /// <summary>
    /// Vanilla only has descriptions for indices 0-4; keep the lookup in range for the extra
    /// options instead of letting the game index past the end of its description table.
    /// </summary>
    [HarmonyPatch(typeof(Serial.FF5.UI.KeyInput.ConfigActualDetailsController), "SetDescriptionText")]
    private static class KeyInputDescription
    {
        private static void Prefix(ConfigCommandType type, ref int value)
        {
            if (!BoostRates.IsRateCommand(type) || value <= 4)
                return;

            if (BoostOptionsPlugin.Verbose.Value)
                BoostOptionsPlugin.LogSource.LogInfo($"[Boost] {type}: description value {value} clamped to 4");

            value = 4;
        }
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "ConvertIndexIntoRateValue")]
    private static class TouchIndexToRate
    {
        private static bool Prefix(ConfigCommandType type, int index, ref float __result)
        {
            if (!BoostRates.IsRateCommand(type))
                return true;

            __result = BoostRates.IndexToRate(index);
            return false;
        }
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "ConvertRateValueIntoIndex")]
    private static class TouchRateToIndex
    {
        private static bool Prefix(ConfigCommandType type, float rateValue, ref int __result)
        {
            if (!BoostRates.IsRateCommand(type))
                return true;

            __result = BoostRates.RateToIndex(rateValue);
            return false;
        }
    }

    [HarmonyPatch(typeof(Serial.FF5.UI.KeyInput.ConfigActualDetailsController), "SetCommandNameList")]
    private static class KeyInputNameList
    {
        private static void Postfix(Last.UI.KeyInput.ConfigCommandController controller)
        {
            try
            {
                string label = BoostMenuHooks.ExtendNameList(
                    controller.ConfigCommandsData, controller.NameList, controller.targetIndex);

                if (label != null)
                {
                    controller.SetArrowChangeText(label);
                    if (BoostOptionsPlugin.Verbose.Value)
                        BoostOptionsPlugin.LogSource.LogInfo($"[Boost] KeyInput row text -> {label} (targetIndex={controller.targetIndex})");
                }
            }
            catch (Exception e)
            {
                BoostOptionsPlugin.LogSource.LogWarning($"[Boost] KeyInput name list failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Serial.FF5.UI.Touch.ConfigActualDetailsController), "SetCommandNameList")]
    private static class TouchNameList
    {
        private static void Postfix(Last.UI.Touch.ConfigCommandController controller)
        {
            try
            {
                string label = BoostMenuHooks.ExtendNameList(
                    controller.ConfigCommandsData, controller.NameList, controller.targetIndex);

                if (label != null)
                {
                    controller.SetArrowChangeText(label);
                    if (BoostOptionsPlugin.Verbose.Value)
                        BoostOptionsPlugin.LogSource.LogInfo($"[Boost] Touch row text -> {label} (targetIndex={controller.targetIndex})");
                }
            }
            catch (Exception e)
            {
                BoostOptionsPlugin.LogSource.LogWarning($"[Boost] Touch name list failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetChangeArrowCommandText")]
    private static class KeyInputArrowText
    {
        private static void Postfix(Last.UI.KeyInput.ConfigCommandController __instance, int index)
        {
            try
            {
                BoostMenuHooks.LogArrowText(__instance.ConfigCommandsData, index);
            }
            catch
            {
                // Ignore.
            }
        }
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigCommandController), "SetChangeArrowCommandText")]
    private static class TouchArrowText
    {
        private static void Postfix(Last.UI.Touch.ConfigCommandController __instance, int index)
        {
            try
            {
                BoostMenuHooks.LogArrowText(__instance.ConfigCommandsData, index);
            }
            catch
            {
                // Ignore.
            }
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetNextSelect")]
    private static class KeyInputNextSelect
    {
        private static void Postfix(Last.UI.KeyInput.ConfigCommandController __instance)
        {
            try
            {
                BoostMenuHooks.LogSelectionMove(__instance.NameList, __instance.TargetIndex, "next");
            }
            catch
            {
                // Ignore.
            }
        }
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigCommandController), "SetPrevSelect")]
    private static class KeyInputPrevSelect
    {
        private static void Postfix(Last.UI.KeyInput.ConfigCommandController __instance)
        {
            try
            {
                BoostMenuHooks.LogSelectionMove(__instance.NameList, __instance.TargetIndex, "prev");
            }
            catch
            {
                // Ignore.
            }
        }
    }

    // The game's Set*Rate(index) helpers read past the end of their private five-entry table for
    // custom indices and store 0. Route every rate row through the client's own setters instead.
    // (SetExpRate(int) turns out to be dead code — see KeyInputArrowRate below — but it is patched
    // anyway so the behaviour stays correct if the game ever calls it.)
    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetExpRate")]
    private static class KeySetExpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.ExpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetExpRate")]
    private static class TouchSetExpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.ExpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetGilRate")]
    private static class KeySetGilRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.GilRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetGilRate")]
    private static class TouchSetGilRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.GilRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetAbpRate")]
    private static class KeySetAbpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.AbpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetAbpRate")]
    private static class TouchSetAbpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.AbpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetLearningProficiencyRate")]
    private static class KeySetLearningProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.LearningProficiencyRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetLearningProficiencyRate")]
    private static class TouchSetLearningProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.LearningProficiencyRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetMagicProficiencyRate")]
    private static class KeySetMagicProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.MagicProficiencyRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetMagicProficiencyRate")]
    private static class TouchSetMagicProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.MagicProficiencyRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetParameterUpRate")]
    private static class KeySetParameterUpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.ParameterUpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetParameterUpRate")]
    private static class TouchSetParameterUpRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.ParameterUpRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SetMagicLearningProficiencyRate")]
    private static class KeySetMagicLearningProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.MagicLearningProficiencyRate, index);
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SetMagicLearningProficiencyRate")]
    private static class TouchSetMagicLearningProficiencyRate
    {
        private static bool Prefix(int index)
            => BoostMenuHooks.ApplyRate(ConfigCommandType.MagicLearningProficiencyRate, index);
    }

    /// <summary>
    /// Runs once the game has finished handling an arrow press on a Boost row.
    /// <para>
    /// EXP is the odd one out: instead of calling SetExpRate(int), the game converts the index
    /// inline in SwitchArrowSelectTypeProcess with its own five-entry table and calls
    /// CheatSettingsClient.SetExpRate itself. Index 5 and above therefore store 0, and the prefix on
    /// SetExpRate(int) is never reached. Re-assert the multiplier the row is displaying, which is
    /// a no-op for the rows the game already stored correctly.
    /// </para>
    /// </summary>
    [HarmonyPatch(typeof(Last.UI.KeyInput.ConfigActualDetailsControllerBase), "SwitchArrowSelectTypeProcess")]
    private static class KeyInputArrowRate
    {
        private static void Postfix(Last.UI.KeyInput.ConfigCommandController controller)
        {
            try
            {
                if (controller != null)
                    BoostMenuHooks.ApplyStoredRate(controller.ConfigCommandsData, controller.targetIndex, "keyInput arrow");
            }
            catch
            {
                // Ignore.
            }
        }
    }

    [HarmonyPatch(typeof(Last.UI.Touch.ConfigActualDetailsControllerBase), "SwitchArrowTypeProcess")]
    private static class TouchArrowRate
    {
        private static void Postfix(Last.UI.Touch.ConfigCommandController controller)
        {
            try
            {
                if (controller != null)
                    BoostMenuHooks.ApplyStoredRate(controller.ConfigCommandsData, controller.targetIndex, "touch arrow");
            }
            catch
            {
                // Ignore.
            }
        }
    }
}
