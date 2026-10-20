using Last.Data;
using Last.Management;
using Last.UI;

namespace FFVBoostOptions;

/// <summary>
/// Reads the multiplier the game currently has stored for a Boost row.
/// </summary>
internal static class StoredBoostRate
{
    public static bool TryGet(ConfigCommandType type, out float rate)
    {
        rate = 0f;

        try
        {
            UserDataManager manager = UserDataManager.instance;
            if (manager == null)
                return false;

            CheatSettingsData data = manager.CheatSettingsData;
            if (data == null)
                return false;

            switch (type)
            {
                case ConfigCommandType.ExpRate:
                    rate = data.expRate;
                    return true;
                case ConfigCommandType.GilRate:
                    rate = data.gilRate;
                    return true;
                case ConfigCommandType.AbpRate:
                    rate = data.abpRate;
                    return true;
                case ConfigCommandType.LearningProficiencyRate:
                    rate = data.learningProficiencyRate;
                    return true;
                case ConfigCommandType.MagicProficiencyRate:
                    rate = data.magicProficiencyRate;
                    return true;
                case ConfigCommandType.ParameterUpRate:
                    rate = data.parameterUpRate;
                    return true;
                case ConfigCommandType.MagicLearningProficiencyRate:
                    rate = data.magicLearningProficiencyRate;
                    return true;
                default:
                    return false;
            }
        }
        catch
        {
            return false;
        }
    }
}
