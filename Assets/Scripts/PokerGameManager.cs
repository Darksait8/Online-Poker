using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PokerGameManager : MonoBehaviour
{
    public class Player
    {
        public string Name;
        public int Stack;
        public bool IsBot;
        public bool Folded;
        public int Bet;
        public NewBehaviourScript UI;
    }

    public int initialStack = 1000;
    public int smallBlind = 10;
    public int bigBlind = 20;
    public int minPlayers = 2;
    public int maxPlayers = 6;
    public List<Player> players = new List<Player>();
    private int pot = 0;
    private int dealer = 0;
    private int currentPlayer = 0;
    private int bettingRound = 0; // 0=preflop, 1=flop, 2=turn, 3=river
    private List<bool> hasActed = new List<bool>();
    private int currentBet = 0;
    private int raises = 0;

    void Start()
    {
        InitSeats();
        StartCoroutine(NewHand());
    }

    void InitSeats()
    {
    players.Clear();
    hasActed.Clear();
        var parent = GameObject.Find("Canvas/Seats");
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            var s = parent.transform.GetChild(i).GetComponent<NewBehaviourScript>();
            if (s != null)
                players.Add(new Player { Name = "Игрок " + (i+1), Stack = initialStack, IsBot = (i > 1), Folded = false, Bet = 0, UI = s });
                hasActed.Add(false);
        }
    }

    IEnumerator NewHand()
    {
        pot = 0;
        currentBet = 0;
        raises = 0;
    for (int i = 0; i < players.Count; i++)
        {
            players[i].Folded = false;
            players[i].Bet = 0;
            players[i].UI.SetPlayer(players[i].Name, players[i].Stack);
            players[i].UI.HideHoles();
            players[i].UI.SetDealer(i == dealer);
            hasActed[i] = false;
        }
        // Collect blinds
        int sb = (dealer + 1) % players.Count, bb = (dealer + 2) % players.Count;
        players[sb].Bet = smallBlind;   players[sb].Stack -= smallBlind;
        players[bb].Bet = bigBlind;     players[bb].Stack -= bigBlind;
        pot = smallBlind + bigBlind;
        currentBet = bigBlind;
        players[sb].UI.ShowBet(smallBlind);
        players[bb].UI.ShowBet(bigBlind);
    currentPlayer = (bb + 1) % players.Count;
    bettingRound = 0;
    for (int i = 0; i < hasActed.Count; i++) hasActed[i] = false;

        yield return new WaitForSeconds(1);
        NextTurn();
    }

    void NextTurn()
    {
        // Найти следующего активного игрока, который не фолднул и не сделал действие
        for (int i = 0; i < players.Count; i++)
        {
            int idx = (currentPlayer + i) % players.Count;
            if (!players[idx].Folded && players[idx].Stack > 0 && !hasActed[idx])
            {
                currentPlayer = idx;
                if (players[idx].IsBot)
                    StartCoroutine(BotAction(players[idx]));
                else
                    EnableUserUI(players[idx]);
                return;
            }
        }
        // Если все активные игроки сделали действие — завершить раунд ставок
        EndBettingRound();
    }

    void EndBettingRound()
    {
        // Сбросить hasActed и Folded для следующего раунда, если новая раздача
        if (bettingRound == 0)
        {
            for (int i = 0; i < players.Count; i++)
            {
                players[i].Folded = false;
                hasActed[i] = false;
            }
        }
        else
        {
            for (int i = 0; i < hasActed.Count; i++)
                hasActed[i] = false;
        }
        bettingRound++;
        if (bettingRound == 1)
        {
            Debug.Log("Переход к флопу");
            var gsm = FindObjectOfType<GameStateMachine>();
            if (gsm != null) gsm.RevealFlop();
        }
        else if (bettingRound == 2)
        {
            Debug.Log("Переход к терну");
            var gsm = FindObjectOfType<GameStateMachine>();
            if (gsm != null) gsm.RevealTurn();
        }
        else if (bettingRound == 3)
        {
            Debug.Log("Переход к риверу");
            var gsm = FindObjectOfType<GameStateMachine>();
            if (gsm != null) gsm.RevealRiver();
        }
        else
        {
            Debug.Log("Переход к шоудауну");
            StartCoroutine(Showdown());
            return;
        }
        // После перехода — начать новый круг ставок
        currentPlayer = dealer;
        NextTurn();
    }

    void EnableUserUI(Player p)
    {
        Debug.Log($"Твой ход: {p.Name} (фишек: {p.Stack}) — выбери действие через панель");
    }

    IEnumerator BotAction(Player bot)
    {
        yield return new WaitForSeconds(1f);
        if (bot.Bet == currentBet) PublicCheck();
        else if (bot.Stack > (currentBet - bot.Bet)) PublicCall();
        else PublicFold();
    }

    // === Публичные методы для Button OnClick ===
    // Добавляй в кнопки Fold: PublicFold(), Call: PublicCall(), Check: PublicCheck(), Raise: PublicRaise()

    public void PublicFold() { DoAction("fold"); }
    public void PublicCall() { DoAction("call"); }
    public void PublicCheck() { DoAction("check"); }
    // Для Raise (если нужно поле для ввода) — смотри RaiseButtonHelper
    public void PublicRaiseWithAmount(int amount) { DoAction("raise", amount); }
    public void DoAction(string action) { DoAction(action, 0); }
    public void DoAction(string action, int raiseAmt)
    {
        var p = players[currentPlayer];
        if (action == "fold")
        {
            p.Folded = true;
            p.UI.SetPlayer("Фолд", p.Stack);
            p.UI.ShowBet(0);
            hasActed[currentPlayer] = true;
            NextTurn();
            return;
        }
        else if (action == "call" || action == "check")
        {
            int diff = currentBet - p.Bet;
            if (diff > 0 && p.Stack >= diff)
            {
                p.Stack -= diff;
                p.Bet += diff;
                pot += diff;
            }
            p.UI.ShowBet(p.Bet);
            hasActed[currentPlayer] = true;
            NextTurn();
            return;
        }
        else if (action == "raise")
        {
            int toBet = raiseAmt - p.Bet;
            if (toBet > 0 && p.Stack >= toBet)
            {
                p.Stack -= toBet;
                p.Bet += toBet;
                currentBet = p.Bet;
                pot += toBet;
                raises++;
                p.UI.ShowBet(p.Bet);
                UpdateAllStacksUI();
                // Сбросить hasActed для всех, кроме текущего игрока
                for (int i = 0; i < hasActed.Count; i++)
                    hasActed[i] = (i == currentPlayer);
                NextTurn();
                return;
            }
        }
        UpdateAllStacksUI();
        hasActed[currentPlayer] = true;
        NextTurn();
    }

    IEnumerator Showdown()
    {
        int winner = UnityEngine.Random.Range(0, players.Count);
        players[winner].Stack += pot;
        Debug.Log($"Вышел {players[winner].Name}, банк +{pot}");
        yield return new WaitForSeconds(2);
        dealer = (dealer + 1) % players.Count;
        StartCoroutine(NewHand());
    }

    void UpdateAllStacksUI()
    {
        foreach(var p in players)
            p.UI.SetPlayer(p.Folded ? "Фолд" : p.Name, p.Stack);
    }
}
