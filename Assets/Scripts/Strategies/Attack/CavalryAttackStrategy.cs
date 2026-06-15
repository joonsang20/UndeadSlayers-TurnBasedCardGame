using System.Collections.Generic;
using UnityEngine;

public class CavalryAttackStrategy : IAttackStrategy
{
    public void Execute(CardInstance attacker, CardInstance target, BoardManager board)
    {
        target.TakeDamage(attacker.Atk);
        // 무쌍: 반격 없음

        var (left, right) = board.GetAdjacentCards(target, target.Team);

        var adjacents = new List<CardInstance>();
        if (left != null) adjacents.Add(left);
        if (right != null) adjacents.Add(right);

        if (adjacents.Count == 0) return;

        var splashTarget = adjacents[Random.Range(0, adjacents.Count)];
        int splashDamage = Mathf.CeilToInt(attacker.Atk * 0.5f);
        splashTarget.TakeDamage(splashDamage);
    }
}
