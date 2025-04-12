// SoldierSpawner.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.Playables;

public class Manager : MonoBehaviour
{
    GameData gameData;

    private Dictionary<int, GameObject> currentSoldiers = new Dictionary<int, GameObject>();
    [Header("Configurations")]
    public TextAsset jsonFile;
    public SoldierConfig[] soldierConfigs;
    public SoldiersDataWrapper wrapper;
    public Color campColorRed;
    public Color campColorBlue;
    public Vector3 uiOffset = new Vector3(0, 2.5f, 0);

    private Dictionary<string, SoldierConfig> _configDict;

    void Start()
    {
        gameData = JsonUtility.FromJson<GameData>(jsonFile.text);
        wrapper = gameData.soldiersData;
        InitializeConfigDictionary();
        LoadAndSpawnSoldiers();
        ProcessRounds(gameData.gameRounds);
    }

    void InitializeConfigDictionary()
    {
        _configDict = new Dictionary<string, SoldierConfig>();
        foreach (var config in soldierConfigs)
        {
            _configDict[config.type] = config;
        }
    }

    void LoadAndSpawnSoldiers()
    {
        if (jsonFile == null) return;


        foreach (var data in wrapper.soldiers)
        {
            data.output();
            SpawnSoldier(data);
        }
    }


    void SpawnSoldier(SoldierData data)
    {
        if (!_configDict.TryGetValue(data.soldierType, out SoldierConfig config))
        {
            Debug.LogError($"Config for {data.soldierType} not found!");
            return;
        }
        Debug.Log(config.type);

        GameObject soldier = Instantiate(config.prefab, data.position, Quaternion.identity);
        soldier.name = "" + data.ID;
        currentSoldiers[data.ID] = soldier;
        SetupCampIndicator(soldier, data.camp, config);
        SetupStatusUI(data.ID);
    }

    void SetupCampIndicator(GameObject soldier, string camp, SoldierConfig config)
    {
        Transform indicator = soldier.transform.Find("CampIndicator");
        if (indicator && indicator.TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = camp == "Red" ?
                campColorRed :
                campColorBlue;
        }
    }

    void SetupStatusUI(int id)
    {
        GameObject soldier = currentSoldiers[id];
        SoldierData data = wrapper.soldiers[id];
        Transform uiRoot = soldier.transform.Find("StatusUI");
        if (!uiRoot) return;

        TextMeshProUGUI text = uiRoot.GetComponentInChildren<TextMeshProUGUI>();
        if (text)
        {
            text.text = $"HP: {data.stats.health}\nSTR: {data.stats.strength}\nMANA: {data.stats.mana}";
            uiRoot.position = soldier.transform.position + uiOffset;
        }
    }

    void ProcessRounds(List<GameRound> rounds)
    {
        foreach (var round in rounds)
        {
            if (round.roundNumber == 0) continue; // 初始回合已处理

            Debug.Log($"Processing Round {round.roundNumber}");
            foreach (var action in round.actions)
            {
                ProcessAction(action);
            }
        }
    }
    void ProcessAction(BattleAction action)
    {

        switch (action.actionType.ToLower())
        {
            case "movement":
                HandleMovement(action.soldierId, action);
                break;

            case "attack":
                HandleAttack(action.soldierId, action);
                break;

            case "ability":
                HandleAbility(action.soldierId, action);
                break;

            default:
                Debug.LogError($"Unknown action type: {action.actionType}");
                break;
        }

    }

    void HandleMovement(int id, BattleAction action)
    {
        if (action.path == null || action.path.Count == 0)
        {
            Debug.LogError("Invalid movement path");
            return;
        }
        GameObject soldier = currentSoldiers[id];
        SoldierData data = wrapper.soldiers[id];
        soldier.transform.position = action.path[action.path.Count - 1];//之后再写移动
        data.position = action.path[action.path.Count - 1];

        Debug.Log($"{data.ID} moved to {data.position}");
    }
    void HandleAttack(int id, BattleAction action)
    {
        if (!currentSoldiers.ContainsKey(action.targetId))
        {
            Debug.LogError($"Target {action.targetId} not found!");
            return;
        }

        var target = wrapper.soldiers[action.targetId];
        target.stats.health -= action.damageDealt;
        SetupStatusUI(target.ID);

        
        // 检查是否死亡
        if (target.stats.health <= 0)
        {
            currentSoldiers.Remove(target.ID);
            Debug.Log($"{target.ID} has been defeated!");
        }
        SetupStatusUI(id);

    }

    void HandleAbility(int id, BattleAction action)
    {

        //caster.stats.mana -= action.manaCost;
        Debug.Log($"{id} cast {action.ability} at {action.targetPosition}");
    }
}