using System.Collections;
using UnityEngine;

// 테스트용 자동 플레이어 — 로직 검증 후 삭제
public class BattleTest : MonoBehaviour
{
    private GameManager gameManager;

    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        gameManager.OnGameOver += result => Log($"=== 게임 종료: {result} ===");
        gameManager.OnTurnStarted += team => Log($"--- {team} 턴 시작 ---");

        gameManager.Board.OnCardRefilled += (card, team, slot) =>
            Log($"[리필] {team} 슬롯{slot}에 {card.Data.cardName} 배치");

        gameManager.BattleResolver.OnCardRemoved += (card, team) =>
            Log($"[사망] {team}의 {card.Data.cardName} 제거");

        gameManager.OnActionPhaseBegin += OnActionPhase;

        gameManager.StartGame();
        LogBoardState();
    }

    private void OnActionPhase(Team team)
    {
        if (team != Team.Player) return;
        StartCoroutine(AutoPlayerAction());
    }

    private IEnumerator AutoPlayerAction()
    {
        yield return new WaitForSeconds(0.1f);

        if (gameManager.IsGameOver) yield break;

        var actable = gameManager.Board.GetActableCards(Team.Player);
        if (actable.Count == 0) yield break;

        var actor = actable[Random.Range(0, actable.Count)];

        // 스킬 가능하면 스킬, 아니면 공격
        ActionType actionType = ActionType.BasicAttack;
        CardInstance target = null;

        if (actor.IsSkillReady && actor.SkillStrategy.CanUse(actor, gameManager.Board))
        {
            actionType = ActionType.Skill;

            if (actor.SkillStrategy.RequiresTarget)
            {
                var candidates = actor.SkillStrategy is SingleHealSkill
                    ? gameManager.Board.GetAliveCards(Team.Player)
                    : gameManager.Board.GetAliveCards(Team.Enemy);

                target = candidates.Count > 0
                    ? candidates[Random.Range(0, candidates.Count)]
                    : null;
            }
        }
        else
        {
            var forced = gameManager.Board.GetForcedTarget(Team.Player);
            var enemies = gameManager.Board.GetAliveCards(Team.Enemy);
            target = forced ?? (enemies.Count > 0 ? enemies[Random.Range(0, enemies.Count)] : null);
        }

        if (target == null && actionType == ActionType.Skill && actor.SkillStrategy.RequiresTarget)
        {
            actionType = ActionType.BasicAttack;
            var enemies = gameManager.Board.GetAliveCards(Team.Enemy);
            target = enemies.Count > 0 ? enemies[Random.Range(0, enemies.Count)] : null;
        }

        if (target == null && !actor.SkillStrategy.RequiresTarget && actionType == ActionType.Skill)
        {
            Log($"[행동] {actor.Data.cardName} → {actionType}");
            gameManager.OnPlayerAction(actor, actionType, null);
            yield break;
        }

        if (target == null) yield break;

        Log($"[행동] {actor.Data.cardName} → {actionType} → {target.Data.cardName}");
        gameManager.OnPlayerAction(actor, actionType, target);

        yield return new WaitForSeconds(0.2f);
        LogBoardState();
    }

    private void LogBoardState()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== 보드 상태 ===");

        sb.Append("[적] ");
        foreach (var card in gameManager.Board.GetField(Team.Enemy))
        {
            if (card == null) sb.Append("[비어있음] ");
            else sb.Append($"{card.Data.cardName}(HP:{card.CurrentHp}/{card.MaxHp}{(card.IsStunned ? " 기절" : "")}{(card.IsProvoking ? " 도발" : "")}) ");
        }

        sb.AppendLine();
        sb.Append("[아군] ");
        foreach (var card in gameManager.Board.GetField(Team.Player))
        {
            if (card == null) sb.Append("[비어있음] ");
            else sb.Append($"{card.Data.cardName}(HP:{card.CurrentHp}/{card.MaxHp}{(card.IsStunned ? " 기절" : "")}{(card.IsProvoking ? " 도발" : "")}) ");
        }

        Log(sb.ToString());
    }

    private void Log(string msg) => Debug.Log(msg);
}
