// SoldierType.cs
using System.Collections.Generic;
using System;
using UnityEngine;
using static MapGeneration;

[System.Serializable]
public class GameData
{
    public MapData mapMetadata;
    public SoldiersDataWrapper soldiersData;
    public List<GameRound> gameRounds;
}
[System.Serializable]
public class MapData
{
    public string mapName;
    public string mapDescription;
    public int mapWidth;
    public float cubeSize;
    public List<MapRow> rows; // ʹ���б�洢������
}
// SoldierData.cs
[System.Serializable]
public class SoldierData
{
    public int ID;
    public string soldierType;
    public string camp;
    public Vector3 position;
    public SoldierStats stats;

    public void output()
    {
        Debug.Log(ID);
        Debug.Log(soldierType);

    }
}

[System.Serializable]
public class SoldierStats
{
    public int health;
    public int strength;
    public int mana;
}

// JSON��װ��
[System.Serializable]
public class SoldiersDataWrapper
{
    //public SoldierData[] soldiers;
    public List<SoldierData> soldiers;
}

[System.Serializable]
public struct SoldierConfig
{
    public string type;
    public GameObject prefab;

}

// ��������ݵİ�װ��
[System.Serializable]
public class MapRow
{
    public List<int> row; // ÿ�е�����
}

[System.Serializable]
public class InitialState
{
    public List<SoldierData> soldiers;
}

[System.Serializable]
public class GameRound
{
    public int roundNumber;
    public InitialState initialState;
    public List<BattleAction> actions;
}

[System.Serializable]
public class BattleAction
{
    public string actionType;
    public int soldierId;

    // Movement
    public List<Vector3> path;
    public int remainingMovement;

    // Attack
    public int targetId;
    public int damageDealt;
    public SoldierStats newStats;

    // Ability
    public string ability;
    public Vector3 targetPosition;
    public int manaCost;
}