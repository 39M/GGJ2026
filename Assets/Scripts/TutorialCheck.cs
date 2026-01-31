using System;
using System.Collections;// ← 添加这个!
using System.Collections.Generic;
using System.Linq;  // ← 添加这个(用于 Max和 All 方法)
using UnityEngine;
using UnityEngine.SceneManagement;// ← 添加这个!
using GGJ;

public class TutorialCheck : MonoBehaviour
{
    [Header("场景配置")]
    public string nextSceneName = "NextLevel";
    public float loadDelay = 2f;

    private bool enableTutorialCheck;
    private float MaxWaveTime;


    [Header("通关条件")]
    [Tooltip("关卡理想得分(每个玩家必须达到这个分数)")]
    public float requiredScore = 0f;

    void Start()
    {
        enableTutorialCheck = GameManager.Instance.enableTutorialCheck;
        MaxWaveTime = GameManager.Instance.MaxWaveTime;


        if (!enableTutorialCheck)
        {
            //禁用脚本
            enabled = false;
            Debug.Log("TutorialCheck 已禁用");
            return;
        }

        GameEventManager.Instance.OnWaveEnd += HandleWaveEnd;
        GameCfg.Instance.EventConfig.WaveDuration = MaxWaveTime;
    }

    void OnDestroy()
    {
        if (!enableTutorialCheck) return;
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnWaveEnd -= HandleWaveEnd;
        }
    }

    private void HandleWaveEnd(int waveNumber)
    {
        if (!enableTutorialCheck) return;

        bool allQualified = CheckIfAllPlayersQualified();
        if (allQualified)
        {
            Debug.Log($"[TutorialCheck] ✅ 所有玩家达标! 准备进入: {nextSceneName}");
            // 停止游戏事件系统
            GameEventManager.Instance.StopGameEventManager();
            // 开始加载下一关
            StartCoroutine(LoadNextScene());
        }
        else
        {
            Debug.Log($"[TutorialCheck] ❌ 有玩家未达标! 准备重新开始");
            // 停止游戏事件系统
            GameEventManager.Instance.StopGameEventManager();
            // 重新加载当前场景
            StartCoroutine(RestartCurrentScene());
        }
    }

    private IEnumerator LoadNextScene()
    {
        Debug.Log($"[TutorialCheck] 等待 {loadDelay} 秒后进入下一关...");
        yield return new WaitForSeconds(loadDelay);

        Debug.Log($"[TutorialCheck] 正在加载场景: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator RestartCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[TutorialCheck] 等待 {loadDelay} 秒后重新开始...");
        yield return new WaitForSeconds(loadDelay);

        Debug.Log($"[TutorialCheck] 正在重新加载场景: {currentScene}");
        SceneManager.LoadScene(currentScene);
    }




    private bool CheckIfAllPlayersQualified()
    {
        var players = GameManager.Instance.PlayerList;

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("没有玩家!");
            return false;
        }

        // 找出最高分
        float maxScore = players.Max(p => p.curScore);
        float halfMax = maxScore * 0.5f;

        Debug.Log($"=== 分数检查 ===");
        Debug.Log($"最高分: {maxScore}, 半数线: {halfMax}, 要求分数: {requiredScore}");

        // 检查每个玩家
        bool allQualified = true;
        foreach (var player in players)
        {
            bool meetsHalfMax = player.curScore >= halfMax;
            bool meetsRequired = player.curScore >= requiredScore;
            bool qualified = meetsHalfMax && meetsRequired;

            string status = qualified ? "✅" : "❌";
            Debug.Log($"{status} Player {player.PlayerIdx}: {player.curScore} " +
                      $"(>= {halfMax}? {meetsHalfMax}, >= {requiredScore}? {meetsRequired})");

            if (!qualified)
            {
                allQualified = false;
            }
        }

        Debug.Log($"总体结果: {(allQualified ? "✅ 全部达标" : "❌ 未全部达标")}");
        return allQualified;
    }

    
}
