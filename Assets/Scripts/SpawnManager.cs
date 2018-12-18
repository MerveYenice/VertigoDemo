﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using System.Linq;

public enum PlayerTeam
{
    None,
    BlueTeam,

    RedTeam
}

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private List<SpawnPoint> _sharedSpawnPoints = new List<SpawnPoint>();
    System.Random _random = new System.Random();
	float _closestDistance;
    [Tooltip("This will be used to calculate the second filter where algorithm looks for closest friends, if the friends are away from this value, they will be ignored")]
    [SerializeField] private float _maxDistanceToClosestFriend = 30;
    [Tooltip("This will be used to calculate the first filter where algorithm looks for enemies that are far away from this value. Only enemies which are away from this value will be calculated.")]
    [SerializeField] private float _minDistanceToClosestEnemy = 10;
    [Tooltip("This value is to prevent friendly player spawning on top of eachothers. If a player is within the range of this value to a spawn point, that spawn point will be ignored")]
    [SerializeField] private float _minMemberDistance = 2;


    public DummyPlayer PlayerToBeSpawned;
    public DummyPlayer[] DummyPlayers;

    private void Awake()
    {
		_sharedSpawnPoints.AddRange(FindObjectsOfType<SpawnPoint>());

		DummyPlayers = FindObjectsOfType<DummyPlayer>();
    }

    #region SPAWN ALGORITHM
    public SpawnPoint GetSharedSpawnPoint(PlayerTeam team)
    {
     
        List<SpawnPoint> spawnPoints = new List<SpawnPoint>(_sharedSpawnPoints.Count);
        CalculateDistancesForSpawnPoints(team);
        // once uzakliga gore spawn etmeye calis
        GetSpawnPointsByDistanceSpawning(ref spawnPoints);
        // uygun uzaklikta spawn poin bulamadiysan
        // takim uzakligina gore spawn etmeye calis
        if (spawnPoints.Count <= 0)
        {
            GetSpawnPointsBySquadSpawning(team, ref spawnPoints);
            Debug.Log("uygun uzaklıkta spawnPoint bulunamadı, squadSpawninge göre bulundu.");
        }
        // sonuclar icinden rastgele spawn point sec
        SpawnPoint spawnPoint = spawnPoints.Count <= 1 ? spawnPoints[0] : spawnPoints[_random.Next(0, (int)((float)spawnPoints.Count * .5f))];
        
        // bu spawnpoint icin timer baslat
        spawnPoint.StartTimer();
        
        return spawnPoint;
    }

	// GetSpawnPointsByDistanceSpawning metodu Suitable spawn noktaları arasından en yakın düşmana olan uzaklığı 
    // _minDistanceToClosestEnemy'den büyük olan ve 
    // en yakın dosta olan uzaklığı _minMemberDistance'dan büyük olan ve
    // SpawnTimer'ı 2 den büyük spawn noktalarını seçer.
    private void GetSpawnPointsByDistanceSpawning(ref List<SpawnPoint> suitableSpawnPoints)
    {
       Debug.Log("------------GET_SPAWN_BY_DIST------"); 
       List<SpawnPoint> filteredPoints = new List<SpawnPoint>();
       for (int i = 0; i< _sharedSpawnPoints.Count;i++){    
            SpawnPoint sp = _sharedSpawnPoints[i];
            if (sp.DistanceToClosestEnemy > _minDistanceToClosestEnemy){
                if (sp.DistanceToClosestFriend > _minMemberDistance && sp.DistanceToClosestEnemy > _minMemberDistance){
                        if (sp.SpawnTimer <= 0){
                            filteredPoints.Add(sp);
                            Debug.Log("suitable spawn point:" + sp);
                        } else {
                            Debug.Log(sp+" filtered out since it was recently choosen");    
                        }
                    } else {
                        Debug.Log(sp+" filtered out since it was too close to a member");                            
                    }
                } else {
                    Debug.Log(sp+" filtered out since it was too close to an enemy");
                }
       }
       Debug.Log("Found "+filteredPoints.Count+" suitable points");
        filteredPoints.Sort((x, y) => x.DistanceToClosestFriend.CompareTo(y.DistanceToClosestFriend));
        suitableSpawnPoints = filteredPoints;  
    }

    // takim üyelerinin uzakligina gore spawn etmeye calis
    private void GetSpawnPointsBySquadSpawning(PlayerTeam team, ref List<SpawnPoint> suitableSpawnPoints)
    {
        if (suitableSpawnPoints == null)
        {
            suitableSpawnPoints = new List<SpawnPoint>();
        }
        suitableSpawnPoints.Clear();
        // takim arkadaslarina olan uzakliklarina gore sort et
        _sharedSpawnPoints.Sort(delegate (SpawnPoint a, SpawnPoint b)
        {
            if (a.DistanceToClosestFriend == b.DistanceToClosestFriend)
            {
                return 0;
            }
            if (a.DistanceToClosestFriend > b.DistanceToClosestFriend)
            {
                return 1;
            }
            return -1;
        });
        //herhangi bir takim arkadasindan maxDistanceToClasestFriend'ten daha uzak olmasin ve
        //current span point etrafında minDistance'tan daha yakın dost-düsman bulunmasın ve
        // SpawnPoint'in spawn timer'i bitmis olsun
        for (int i = 0; i < _sharedSpawnPoints.Count && _sharedSpawnPoints[i].DistanceToClosestFriend <= _maxDistanceToClosestFriend; i++)
        {
            if (!(_sharedSpawnPoints[i].DistanceToClosestFriend <= _minMemberDistance) && 
            !(_sharedSpawnPoints[i].DistanceToClosestEnemy <= _minMemberDistance) && _sharedSpawnPoints[i].SpawnTimer <= 0)
            {
                suitableSpawnPoints.Add(_sharedSpawnPoints[i]);
            }
        }
        if (suitableSpawnPoints.Count <= 0)
        {
            suitableSpawnPoints.Add(_sharedSpawnPoints[0]);
        }
    }

    //CalculateDistancesForSpawnPoints metodu _sharedSpawnPoints içindeki herbir spawnpoint için enyakın dost/düsman mesafesini hesaplar
    private void CalculateDistancesForSpawnPoints(PlayerTeam playerTeam)
    {
        for (int i = 0; i < _sharedSpawnPoints.Count; i++)
        {        
            _sharedSpawnPoints[i].DistanceToClosestFriend = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam);
            _sharedSpawnPoints[i].DistanceToClosestEnemy = GetDistanceToClosestMember(_sharedSpawnPoints[i].PointTransform.position, playerTeam == PlayerTeam.BlueTeam ? PlayerTeam.RedTeam : playerTeam == PlayerTeam.RedTeam ? PlayerTeam.BlueTeam : PlayerTeam.None);
           
        }
    }

    // GetDistanceToClosestMember methodu player disabled değilse ve playerın takımı parametre olarak alınan playerTeam değerine eşitse
    // verilen pozisyona göre en yakın takım üyesinin mesafesini return eder.
    private float GetDistanceToClosestMember(Vector3 position, PlayerTeam playerTeam)
    {
        // başlangıcta _closestDistance maxValue olmalı
        _closestDistance = float.MaxValue;
        foreach (var player in DummyPlayers)
        {
            if (!player.Disabled && player.PlayerTeamValue != PlayerTeam.None && player.PlayerTeamValue == playerTeam && !player.IsDead())
            {
                float playerDistanceToSpawnPoint = Vector3.Distance(position, player.Transform.position);
                if (playerDistanceToSpawnPoint < _closestDistance)
                {
                    _closestDistance = playerDistanceToSpawnPoint;
                }
            }
        }
        return _closestDistance;
    }

    #endregion
	/// <summary>
	/// Test için paylaşımlı spawn noktalarından en uygun olanını seçer.
	/// Test oyuncusunun pozisyonunu seçilen spawn noktasına atar.
	/// </summary>
    public void TestGetSpawnPoint()
    {
    	    SpawnPoint spawnPoint = GetSharedSpawnPoint(PlayerToBeSpawned.PlayerTeamValue);
            PlayerToBeSpawned.Transform.position = spawnPoint.PointTransform.position;
    }

}