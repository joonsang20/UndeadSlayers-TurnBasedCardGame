using System;
using UnityEngine;

public enum Team { Player, Enemy }

public class CardInstance
{
    public CardData Data { get; }
    public Team Team { get; }

    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public int Atk { get; }

    public int CurrentCooldown { get; private set; }
    public bool IsSkillReady => CurrentCooldown == 0;

    public bool IsStunned { get; private set; }
    public int StunRemainingTurns { get; private set; }

    public bool IsProvoking { get; private set; }
    public int ProvokeRemainingTurns { get; private set; }

    public bool IsAlive => CurrentHp > 0;

    public IAttackStrategy AttackStrategy { get; set; }
    public ISkillStrategy SkillStrategy { get; set; }
    public IPassiveStrategy PassiveStrategy { get; set; }

    public event Action<int, int> OnHpChanged;    // currentHp, maxHp
    public event Action OnDied;
    public event Action<bool> OnStunChanged;
    public event Action<bool> OnProvokeChanged;

    public CardInstance(CardData data, Team team)
    {
        Data = data;
        Team = team;
        MaxHp = data.maxHp;
        CurrentHp = data.maxHp;
        Atk = data.atk;
        CurrentCooldown = 0;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive) return;
        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        if (CurrentHp <= 0)
            OnDied?.Invoke();
    }

    public void Heal(int amount)
    {
        if (!IsAlive) return;
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    public void ReduceCooldown()
    {
        if (CurrentCooldown > 0)
            CurrentCooldown--;
    }

    public void ResetCooldown()
    {
        CurrentCooldown = Data.skillCooldown;
    }

    // 기절 적용: 이미 기절 중이면 턴 연장
    public void ApplyStun()
    {
        if (IsStunned)
        {
            StunRemainingTurns++;
        }
        else
        {
            IsStunned = true;
            StunRemainingTurns = 1;
            OnStunChanged?.Invoke(true);
        }
    }

    // 턴 종료 시 카운터 감소 (현재 팀 턴 종료 시 호출)
    public void TickStun()
    {
        if (!IsStunned) return;
        StunRemainingTurns--;
    }

    // 턴 시작 시 카운터가 0이면 해제
    public void TryReleaseStun()
    {
        if (!IsStunned) return;
        if (StunRemainingTurns <= 0)
        {
            IsStunned = false;
            OnStunChanged?.Invoke(false);
        }
    }

    public void ActivateProvoke()
    {
        IsProvoking = true;
        ProvokeRemainingTurns = 2;
        OnProvokeChanged?.Invoke(true);
    }

    // 상대방 턴 종료 시 카운터 감소
    public void TickProvoke()
    {
        if (!IsProvoking) return;
        ProvokeRemainingTurns--;
        if (ProvokeRemainingTurns <= 0)
            ClearProvoke();
    }

    public void ClearProvoke()
    {
        if (!IsProvoking) return;
        IsProvoking = false;
        ProvokeRemainingTurns = 0;
        OnProvokeChanged?.Invoke(false);
    }
}
