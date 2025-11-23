using UnityEngine;

public static class PlayerProgressionService
{
    private const int BaseXpPerLevel = 500;
    private const float GrowthFactor = 1.25f;

    public static int GetLevel(int xp)
    {
        xp = Mathf.Max(0, xp);
        int level = 1;
        int totalRequiredForNext = GetTotalXpForLevel(level + 1);
        while (xp >= totalRequiredForNext)
        {
            level++;
            totalRequiredForNext = GetTotalXpForLevel(level + 1);
        }
        return level;
    }

    public static int GetTotalXpForLevel(int level)
    {
        level = Mathf.Max(1, level);
        int total = 0;
        for (int i = 1; i < level; i++)
            total += GetXpRequirementForLevel(i);
        return total;
    }

    public static int GetXpRequirementForLevel(int level)
    {
        level = Mathf.Max(1, level);
        float requirement = BaseXpPerLevel * Mathf.Pow(GrowthFactor, level - 1);
        return Mathf.Max(BaseXpPerLevel, Mathf.RoundToInt(requirement));
    }

    public static int GetXpIntoCurrentLevel(int xp)
    {
        xp = Mathf.Max(0, xp);
        int level = GetLevel(xp);
        int totalForCurrent = GetTotalXpForLevel(level);
        return xp - totalForCurrent;
    }

    public static int GetXpToNextLevel(int xp)
    {
        xp = Mathf.Max(0, xp);
        int level = GetLevel(xp);
        int totalForNext = GetTotalXpForLevel(level + 1);
        return Mathf.Max(0, totalForNext - xp);
    }

    public static float GetProgress01(int xp)
    {
        xp = Mathf.Max(0, xp);
        int level = GetLevel(xp);
        int xpIntoLevel = GetXpIntoCurrentLevel(xp);
        int requirement = GetXpRequirementForLevel(level);
        if (requirement <= 0)
            return 1f;
        return Mathf.Clamp01((float)xpIntoLevel / requirement);
    }
}



