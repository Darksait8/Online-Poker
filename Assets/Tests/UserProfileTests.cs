#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ручной запуск набора проверок UserProfile без NUnit.
/// </summary>
public static class UserProfileTests
{
    [MenuItem("Tests/Run UserProfile Checks")]
    public static void RunAll()
    {
        Run("AddDeposit_IncreasesBalanceAndWeeklyCounter", AddDeposit_IncreasesBalanceAndWeeklyCounter);
        Run("AddDeposit_RespectsWeeklyLimit", AddDeposit_RespectsWeeklyLimit);
        Run("GetRemainingWeeklyDeposit_UpdatesAfterDeposit", GetRemainingWeeklyDeposit_UpdatesAfterDeposit);
        Run("WeeklyLimitResetsWhenNewWeekStarts", WeeklyLimitResetsWhenNewWeekStarts);
        Run("UpdateGameStats_TracksWinsAndLosses", UpdateGameStats_TracksWinsAndLosses);

        Debug.Log("UserProfileTests: все проверки выполнены");
    }

    private static void Run(string name, System.Action body)
    {
        try
        {
            body();
            Debug.Log($"[PASS] {name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FAIL] {name}: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new System.Exception(message);
    }

    private static void AddDeposit_IncreasesBalanceAndWeeklyCounter()
    {
        var profile = new UserProfile { weeklyDepositLimit = 10_000 };
        bool success = profile.AddDeposit(1_500);

        AssertTrue(success, "Пополнение должно пройти успешно");
        AssertTrue(profile.chips == 2_500, "Баланс должен увеличиться");
        AssertTrue(profile.currentWeekDeposits == 1_500, "Счётчик недели должен увеличиться");
    }

    private static void AddDeposit_RespectsWeeklyLimit()
    {
        var profile = new UserProfile { weeklyDepositLimit = 1_000 };
        bool first = profile.AddDeposit(800);
        bool second = profile.AddDeposit(300);

        AssertTrue(first, "Первое пополнение должно пройти");
        AssertTrue(!second, "Пополнение сверх лимита должно быть отклонено");
        AssertTrue(profile.chips == 1_800, "Баланс не должен превышать лимит");
    }

    private static void GetRemainingWeeklyDeposit_UpdatesAfterDeposit()
    {
        var profile = new UserProfile { weeklyDepositLimit = 5_000 };
        profile.AddDeposit(1_750);

        int remaining = profile.GetRemainingWeeklyDeposit();
        AssertTrue(remaining == 3_250, "Остаток лимита рассчитывается неверно");
    }

    private static void WeeklyLimitResetsWhenNewWeekStarts()
    {
        var profile = new UserProfile { weeklyDepositLimit = 2_000 };
        profile.AddDeposit(1_900);
        profile.weekStartDate = profile.weekStartDate.AddDays(-7);

        bool success = profile.AddDeposit(2_000);
        AssertTrue(success, "После новой недели лимит должен сбрасываться");
        AssertTrue(profile.currentWeekDeposits == 2_000, "Счётчик недели должен обновляться");
    }

    private static void UpdateGameStats_TracksWinsAndLosses()
    {
        var profile = new UserProfile();
        profile.UpdateGameStats(true, chipsWon: 500, chipsLost: 0);
        profile.UpdateGameStats(false, chipsWon: 0, chipsLost: 300);

        AssertTrue(profile.totalGamesPlayed == 2, "Общее число игр неверно");
        AssertTrue(profile.gamesWon == 1 && profile.gamesLost == 1, "Победы/поражения считаются неверно");
        AssertTrue(profile.chips == 1_200, "Баланс после игр некорректен");
        AssertTrue(profile.biggestWin == 500 && profile.biggestLoss == 300, "Рекордные выигрыш/проигрыш рассчитаны неверно");
    }
}
#endif

