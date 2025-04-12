using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    GameData gameData;

    private Dictionary<int, GameObject> currentSoldiers = new Dictionary<int, GameObject>();
    [Header("Configurations")]
    public TextAsset jsonFile;
    public SoldierConfig[] soldierConfigs;
    public GameObject[] Effectprefab;

    public SoldiersDataWrapper wrapper;
    public Color campColorRed;
    public Color campColorBlue;
    public Vector3 uiOffset = new Vector3(0, 2.5f, 0);

    private Dictionary<string, SoldierConfig> _configDict;

    [Header("Playback Controls")]
    public Button prevButton;
    public Button nextButton;
    public Button autoPlayButton;
    public float roundInterval = 2f;

    private int currentRoundIndex = 0;
    private List<List<SoldierData>> snapshots = new List<List<SoldierData>>();
    private bool isAutoPlaying = false;

    void Start()
    {

        gameData = JsonUtility.FromJson<GameData>(jsonFile.text);
        wrapper = gameData.soldiersData;
        InitializeConfigDictionary();
        LoadAndSpawnSoldiers();
        
        SaveSnapshot();
        UpdateButtons();
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

   
    void SaveSnapshot()
    {
        var snapshot = new List<SoldierData>();
        foreach (var soldier in wrapper.soldiers)
        {
            var copy = JsonUtility.FromJson<SoldierData>(JsonUtility.ToJson(soldier));
            snapshot.Add(copy);
        }
        snapshots.Add(snapshot);
    }

    void RestoreSnapshot(int index)
    {
        // 销毁当前士兵
        foreach (var soldier in currentSoldiers.Values)
            Destroy(soldier);
        currentSoldiers.Clear();

        // 恢复数据
        wrapper.soldiers = new List<SoldierData>(snapshots[index]);
        LoadAndSpawnSoldiers();
    }

    public void NextRound()
    {
        if (currentRoundIndex >= gameData.gameRounds.Count) return;

        ProcessRound(gameData.gameRounds[currentRoundIndex]);
        currentRoundIndex++;
        SaveSnapshot();
        UpdateButtons();
    }

    public void PreviousRound()
    {
        if (currentRoundIndex <= 0) return;

        currentRoundIndex--;
        RestoreSnapshot(currentRoundIndex);
        UpdateButtons();
    }

    public void ToggleAutoPlay()
    {
        Debug.Log(11111);

        isAutoPlaying = !isAutoPlaying;

        if (isAutoPlaying) StartCoroutine(AutoPlay());
        else
        {
            if (AutoPlay() != null)
                StopCoroutine(AutoPlay());
        }

        prevButton.interactable = !isAutoPlaying;
        nextButton.interactable = !isAutoPlaying;
    }

    IEnumerator AutoPlay()
    {
        while (isAutoPlaying && currentRoundIndex < gameData.gameRounds.Count)
        {
            Debug.Log(11111);
            NextRound();
            yield return new WaitForSeconds(roundInterval);
        }
        isAutoPlaying = false;
    }

    void ProcessRound(GameRound round)
    {
        Debug.Log($"Processing Round {round.roundNumber}");
        foreach (var action in round.actions)
            ProcessAction(action);
    }

    void UpdateButtons()
    {
        prevButton.interactable = currentRoundIndex > 0;
        nextButton.interactable = currentRoundIndex < gameData.gameRounds.Count;
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

        // 添加伤害特效
        StartCoroutine(ShowDamageEffect(target.ID));

    }

    IEnumerator ShowDamageEffect(int id)
    {
        GameObject effect = Instantiate(Effectprefab[0],
            currentSoldiers[id].transform.position,
            Quaternion.identity);
        yield return new WaitForSeconds(1f);
        Destroy(effect);
    }

    void HandleAbility(int id, BattleAction action)
    {

        //caster.stats.mana -= action.manaCost;
        Debug.Log($"{id} cast {action.ability} at {action.targetPosition}");
    }
}