using System;
using System.Globalization;
using Last.UI;

namespace FF5BoostOptions;

/// <summary>
/// The Boost menu builds its options from an index, and the game converts between that index and
/// the multiplier with <c>ConvertIndexIntoRateValue</c> / <c>ConvertRateValueIntoIndex</c>.
/// Vanilla only knows 0x, 0.5x, 1x, 2x, 4x; this replaces that table with
/// 0x, 0.5x, 1x, 2x, 3x, 4x, ... up to the configured maximum.
/// </summary>
internal static class BoostRates
{
    private static readonly ConfigCommandType[] RateCommands =
    {
        ConfigCommandType.ExpRate,
        ConfigCommandType.GilRate,
        ConfigCommandType.AbpRate,
        ConfigCommandType.LearningProficiencyRate,
        ConfigCommandType.MagicProficiencyRate,
        ConfigCommandType.ParameterUpRate,
        ConfigCommandType.MagicLearningProficiencyRate,
    };

    public static bool IsRateCommand(ConfigCommandType type) => Array.IndexOf(RateCommands, type) >= 0;

    public static int MaxIndex => Math.Max(2, BoostOptionsPlugin.MaxMultiplier.Value) + 1;

    public static float IndexToRate(int index) => index switch
    {
        <= 0 => 0f,
        1 => 0.5f,
        2 => 1f,
        _ => index - 1f,
    };

    public static int RateToIndex(float rate)
    {
        if (rate <= 0f)
            return 0;
        if (rate < 0.75f)
            return 1;
        if (rate < 1.5f)
            return 2;

        int index = (int)Math.Round(rate, MidpointRounding.AwayFromZero) + 1;
        return Math.Clamp(index, 3, MaxIndex);
    }

    public static string Label(int index) => index switch
    {
        <= 0 => "0x",
        1 => "0.5x",
        2 => "1x",
        _ => (index - 1).ToString(CultureInfo.InvariantCulture) + "x",
    };
}
