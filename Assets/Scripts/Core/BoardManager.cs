using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardManager
{
    private readonly CardInstance[] playerField = new CardInstance[3];
    private readonly CardInstance[] enemyField = new CardInstance[3];
    private readonly List<CardInstance> playerReserve = new List<CardInstance>();
    private readonly List<CardInstance> enemyReserve = new List<CardInstance>();

    public CardInstance ActiveProvoker { get; private set; }

    public event Action<CardInstance, Team, int> OnCardRefilled; // card, team, slotIndex

    public CardInstance[] GetField(Team team) =>
        team == Team.Player ? playerField : enemyField;

    private List<CardInstance> GetReserve(Team team) =>
        team == Team.Player ? playerReserve : enemyReserve;

    public void InitializeField(Team team, List<CardInstance> fieldCards, List<CardInstance> reserveCards)
    {
        var field = GetField(team);
        var reserve = GetReserve(team);

        for (int i = 0; i < 3; i++)
            field[i] = i < fieldCards.Count ? fieldCards[i] : null;

        reserve.Clear();
        reserve.AddRange(reserveCards);
    }

    public List<CardInstance> GetAliveCards(Team team) =>
        GetField(team).Where(c => c != null && c.IsAlive).ToList();

    public List<CardInstance> GetActableCards(Team team) =>
        GetAliveCards(team).Where(c => !c.IsStunned).ToList();

    public bool HasAnyCards(Team team) =>
        GetAliveCards(team).Count > 0 || GetReserve(team).Count > 0;

    public int GetFieldIndex(CardInstance card, Team team)
    {
        var field = GetField(team);
        for (int i = 0; i < field.Length; i++)
            if (field[i] == card) return i;
        return -1;
    }

    public (CardInstance left, CardInstance right) GetAdjacentCards(CardInstance target, Team team)
    {
        int idx = GetFieldIndex(target, team);
        if (idx < 0) return (null, null);

        var field = GetField(team);
        var left  = idx > 0 && field[idx - 1] != null && field[idx - 1].IsAlive ? field[idx - 1] : null;
        var right = idx < 2 && field[idx + 1] != null && field[idx + 1].IsAlive ? field[idx + 1] : null;

        return (left, right);
    }

    public void RemoveCard(CardInstance card, Team team)
    {
        var field = GetField(team);
        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] == card)
            {
                field[i] = null;
                break;
            }
        }

        if (ActiveProvoker == card)
            ActiveProvoker = null;
    }

    public void TryRefill(Team team)
    {
        var field = GetField(team);
        var reserve = GetReserve(team);

        for (int i = 0; i < field.Length; i++)
        {
            if (field[i] != null || reserve.Count == 0) continue;

            int randIdx = UnityEngine.Random.Range(0, reserve.Count);
            field[i] = reserve[randIdx];
            reserve.RemoveAt(randIdx);
            OnCardRefilled?.Invoke(field[i], team, i);
        }
    }

    public void SetProvoke(CardInstance provoker)
    {
        ActiveProvoker = provoker;
        provoker.ActivateProvoke();
    }

    public void ClearActiveProvoker()
    {
        ActiveProvoker = null;
    }

    // 공격자 팀 기준으로 상대방 도발이 활성 중인지 확인
    public CardInstance GetForcedTarget(Team attackerTeam)
    {
        if (ActiveProvoker == null) return null;
        if (ActiveProvoker.Team == attackerTeam) return null;
        if (!ActiveProvoker.IsAlive) return null;
        return ActiveProvoker;
    }
}
