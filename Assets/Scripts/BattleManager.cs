﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;


public class BattleManager : MonoBehaviour
{

    public static BattleManager instance;
    private bool battleActive;
    public GameObject battleScene;
    public Transform[] playerPositions;
    public Transform[] enemyPositions;
    public BattleChar[] playerPrefabs;
    public BattleChar[] enemyPrefabs;
    public List<BattleChar> activeBattlers = new List<BattleChar>();
    public int currentTurn;
    public bool turnWaiting;
    public GameObject uiButtonsHolder;
    public BattleMove[] movesList;
    public GameObject enemyAttackEffect;
    public DamageNumber theDamageNumber;
    public Text[] playerName, playerHP, playerMP;
    public GameObject targetMenu;
    public BattleTargetButton[] targetButtons;
    public GameObject magicMenu;
    public BattleMagicSelect[] magicButtons;
    public BattleNotification battleNotice;
    public int chanceToFlee = 35;
    private bool fleeing;
    public string gameOverScene;
    public int rewardXP;
    public string[] rewardItems;
    public bool cannotFlee;





    // Use this for initialization
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
       
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            BattleStart(new string[] {"Drigan"}, false);
        }

        if (battleActive)
        {
            if (turnWaiting)
            {
                if (activeBattlers[currentTurn].isPlayer)
                {
                    uiButtonsHolder.SetActive(true);
                }
                else
                {
                    uiButtonsHolder.SetActive(false);

                    //enemy should attack
                    StartCoroutine(EnemyMoveCo());
                }
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                NextTurn();
            }
        }
    }

    public void BattleStart(string[] enemiesToSpawn, bool setCannotFlee)
    {
        PlayerController.instance.ActivateJoystick(false);
        if (!battleActive)
        {
            cannotFlee = setCannotFlee;

            battleActive = true;

            GameManager.instance.battleActive = true;

            transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, transform.position.z);
            battleScene.SetActive(true);

            AudioManager.instance.PlayBGM(0);

            for (int i = 0; i < playerPositions.Length; i++)//for no of players to spawn
            {
                if (GameManager.instance.playerStats[i].gameObject.activeInHierarchy)//if the playerstats available/active
                {
                    for (int j = 0; j < playerPrefabs.Length; j++)//for all characters available for player
                    {
                        if (playerPrefabs[j].charName == GameManager.instance.playerStats[i].charName)//when the desired character prefab is found
                        {
                            Debug.Log("Spawning :" + playerPrefabs[j].charName);

                            BattleChar newPlayer = Instantiate(playerPrefabs[j], playerPositions[i].position, playerPositions[i].rotation);
                            newPlayer.transform.parent = playerPositions[i];
                            newPlayer.GetComponent<SpriteRenderer>().sortingOrder = playerPositions[i].GetChild(0).GetComponent<SpriteRenderer>().sortingOrder;
                            if (playerPrefabs[j].charName != "Tim")
                            {
                                activeBattlers.Add(newPlayer);
                                int noOfBattlers = activeBattlers.Count - 1;

                                CharStats thePlayer = GameManager.instance.playerStats[i];
                                activeBattlers[noOfBattlers].currentHp = thePlayer.currentHP;
                                activeBattlers[noOfBattlers].maxHP = thePlayer.maxHP;
                                activeBattlers[noOfBattlers].currentMP = thePlayer.currentMP;
                                activeBattlers[noOfBattlers].maxMP = thePlayer.maxMP;
                                activeBattlers[noOfBattlers].strength = thePlayer.strength;
                                activeBattlers[noOfBattlers].defence = thePlayer.defence;
                                activeBattlers[noOfBattlers].wpnPower = thePlayer.wpnPwr;
                                activeBattlers[noOfBattlers].armrPower = thePlayer.armrPwr;
                                //activeBattlers[noOfBattlers].isPlayer = true;
                            }
                        }
                    }
                }
            }
            //spawning enemies
            for (int i = 0; i < enemiesToSpawn.Length; i++)
            {
                if (enemiesToSpawn[i] != "")
                {
                    for (int j = 0; j < enemyPrefabs.Length; j++)
                    {
                        if (enemyPrefabs[j].charName == enemiesToSpawn[i])
                        {
                            BattleChar newEnemy = Instantiate(enemyPrefabs[j], enemyPositions[i].position, enemyPositions[i].rotation);
                            newEnemy.transform.parent = enemyPositions[i];
                            newEnemy.GetComponent<SpriteRenderer>().sortingOrder = enemyPositions[i].GetChild(0).GetComponent<SpriteRenderer>().sortingOrder;

                            activeBattlers.Add(newEnemy);
                            Debug.Log("Enemy Spawned");
                        }
                    }
                }
            }

            turnWaiting = true;
            currentTurn = 0;//Random.Range(0, activeBattlers.Count);

            UpdateUIStats();
        }
    }

    public void NextTurn()
    {
        currentTurn++;
        if (currentTurn >= activeBattlers.Count)
        {
            currentTurn = 0;
        }

        turnWaiting = true;

        UpdateBattle();
        UpdateUIStats();
    }

    public void UpdateBattle()
    {
        bool allEnemiesDead = true;
        bool allPlayersDead = true;

        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (activeBattlers[i].currentHp < 0)
            {
                activeBattlers[i].currentHp = 0;
            }

            if (activeBattlers[i].currentHp == 0)
            {
                //Handle dead battler
                if (activeBattlers[i].isPlayer)
                {
                    activeBattlers[i].theSprite.sprite = activeBattlers[i].deadSprite;
                }
                else
                {
                    activeBattlers[i].EnemyFade();
                }

            }
            else
            {
                if (activeBattlers[i].isPlayer)
                {
                    allPlayersDead = false;
                    activeBattlers[i].theSprite.sprite = activeBattlers[i].aliveSprite;
                }
                else
                {
                    allEnemiesDead = false;
                }
            }
        }

        if (allEnemiesDead || allPlayersDead)
        {
            if (allEnemiesDead)
            {
                //end battle in victory
                StartCoroutine(EndBattleCo());
            }
            else
            {
                //end battle in failure
                StartCoroutine(GameOverCo());
            }

            /* battleScene.SetActive(false);
            GameManager.instance.battleActive = false;
            battleActive = false; */
        }
        else
        {
            while (activeBattlers[currentTurn].currentHp == 0)
            {
                currentTurn++;
                if (currentTurn >= activeBattlers.Count)
                {
                    currentTurn = 0;
                }
            }
        }
    }

    public IEnumerator EnemyMoveCo()
    {
        turnWaiting = false;
        yield return new WaitForSeconds(2f);
        EnemyAttack();
        yield return new WaitForSeconds(1f);
        NextTurn();
    }

    public void EnemyAttack()
    {
        List<int> players = new List<int>();
        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (activeBattlers[i].isPlayer && activeBattlers[i].currentHp > 0)
            {
                players.Add(i);
            }
        }
        int selectedTarget = players[Random.Range(0, players.Count)];

        //activeBattlers[selectedTarget].currentHp -= 30;

        int selectAttack = Random.Range(0, activeBattlers[currentTurn].movesAvailable.Length);
        int movePower = 0;
        for (int i = 0; i < movesList.Length; i++)
        {
            if (movesList[i].moveName == activeBattlers[currentTurn].movesAvailable[selectAttack])
            {
                Instantiate(movesList[i].theEffect, activeBattlers[selectedTarget].transform.position, activeBattlers[selectedTarget].transform.rotation);
                movePower = movesList[i].movePower;
            }
        }

        Instantiate(enemyAttackEffect, activeBattlers[currentTurn].transform.position, activeBattlers[currentTurn].transform.rotation);

        DealDamage(selectedTarget, movePower);
    }

    public void DealDamage(int target, int movePower)
    {
        float atkPwr = activeBattlers[currentTurn].strength + activeBattlers[currentTurn].wpnPower;
        float defPwr = activeBattlers[target].defence + activeBattlers[target].armrPower;

        float damageCalc = (atkPwr / defPwr) * movePower * Random.Range(.9f, 1.1f);
        int damageToGive = Mathf.RoundToInt(damageCalc);

        Debug.Log(activeBattlers[currentTurn].charName + " is dealing " + damageCalc + "(" + damageToGive + ") damage to " + activeBattlers[target].charName);

        activeBattlers[target].currentHp -= damageToGive;

        Instantiate(theDamageNumber, activeBattlers[target].transform.position, activeBattlers[target].transform.rotation).SetDamage(damageToGive);

        UpdateUIStats();
    }

    public void UpdateUIStats()
    {
        int j = 0; 

        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (activeBattlers[i].isPlayer && activeBattlers[i].charName != "Tim")
            {
                BattleChar playerData = activeBattlers[i];

                playerName[j].gameObject.SetActive(true);
                playerName[j].text = playerData.charName;
                playerHP[j].text = Mathf.Clamp(playerData.currentHp, 0, int.MaxValue) + "/" + playerData.maxHP;
                playerMP[j].text = Mathf.Clamp(playerData.currentMP, 0, int.MaxValue) + "/" + playerData.maxMP;
                j++;
            }
        }

        int enemyIndex = j; 

        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (!activeBattlers[i].isPlayer && enemyIndex < playerHP.Length)
            {
                BattleChar enemyData = activeBattlers[i];

                playerName[enemyIndex].gameObject.SetActive(true);  
                playerName[enemyIndex].text = enemyData.charName;  
                playerHP[enemyIndex].text = Mathf.Clamp(enemyData.currentHp, 0, int.MaxValue) + "/" + enemyData.maxHP; 
                playerMP[enemyIndex].text = "";  // In case if enemies have MP

                enemyIndex++;
            }
        }

        for (int k = enemyIndex; k < playerName.Length; k++)
        {
            playerName[k].gameObject.SetActive(false); 
            playerHP[k].text = "-";  
            //playerMP[k].text = "-";  //commented because enemies don't have MP
        }
    }


    public void PlayerAttack(string moveName, int selectedTarget)
    {

        int movePower = 0;
        for (int i = 0; i < movesList.Length; i++)
        {
            if (movesList[i].moveName == moveName)
            {
                Instantiate(movesList[i].theEffect, activeBattlers[selectedTarget].transform.position, activeBattlers[selectedTarget].transform.rotation);
                movePower = movesList[i].movePower;
            }
        }

        Instantiate(enemyAttackEffect, activeBattlers[currentTurn].transform.position, activeBattlers[currentTurn].transform.rotation);

        DealDamage(selectedTarget, movePower);

        uiButtonsHolder.SetActive(false);
        targetMenu.SetActive(false);

        NextTurn();

    }

    public void OpenTargetMenu(string moveName)
    {
        targetMenu.SetActive(true);

        List<int> Enemies = new List<int>();
        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (!activeBattlers[i].isPlayer)
            {
                Enemies.Add(i);
            }
        }

        for (int i = 0; i < targetButtons.Length; i++)
        {
            if (Enemies.Count > i && activeBattlers[Enemies[i]].currentHp > 0)
            {
                targetButtons[i].gameObject.SetActive(true);

                targetButtons[i].moveName = moveName;
                targetButtons[i].activeBattlerTarget = Enemies[i];
                targetButtons[i].targetName.text = activeBattlers[Enemies[i]].charName;
            }
            else
            {
                targetButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void OpenMagicMenu()
    {
        magicMenu.SetActive(true);

        for (int i = 0; i < magicButtons.Length; i++)
        {
            if (activeBattlers[currentTurn].movesAvailable.Length > i)
            {
                magicButtons[i].gameObject.SetActive(true);

                magicButtons[i].spellName = activeBattlers[currentTurn].movesAvailable[i];
                magicButtons[i].nameText.text = magicButtons[i].spellName;

                for (int j = 0; j < movesList.Length; j++)
                {
                    if (movesList[j].moveName == magicButtons[i].spellName)
                    {
                        magicButtons[i].spellCost = movesList[j].moveCost;
                        magicButtons[i].costText.text = magicButtons[i].spellCost.ToString();
                    }
                }

            }
            else
            {
                magicButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void Flee()
    {
        if (cannotFlee)
        {
            battleNotice.theText.text = "Can not flee this battle!";
            battleNotice.Activate();
        }
        else
        {
            int fleeSuccess = Random.Range(0, 100);
            if (fleeSuccess < chanceToFlee)
            {
                //end the battle
                //battleActive = false;
                //battleScene.SetActive(false);
                fleeing = true;
                StartCoroutine(EndBattleCo());
            }
            else
            {
                NextTurn();
                battleNotice.theText.text = "Couldn't escape!";
                battleNotice.Activate();
            }
        }

    }

    public IEnumerator EndBattleCo()
    {
        //Debug.Log("Saving Players");
    
        //for(int i=0; i< activeBattlers.Count; i++)
        //{
        //    if (activeBattlers[i].isPlayer)
        //    {
        //        Debug.Log("Player Found!");
        //        for (int k = 0; k < GameManager.instance.playerStats.Length; k++)
        //        {
        //            if (activeBattlers[i].charName == GameManager.instance.playerStats[k].charName)
        //            {
        //                Debug.Log("Stats Saved!" + k);
        //                GameManager.instance.playerStats[k].currentHP = activeBattlers[i].currentHp;
        //                GameManager.instance.playerStats[k].currentMP = activeBattlers[i].currentMP;
        //            }
        //        }
        //    }
        //}





        Debug.Log("Ending battle");
        PlayerController.instance.ActivateJoystick(true);
        battleActive = false;
        uiButtonsHolder.SetActive(false);
        targetMenu.SetActive(false);
        magicMenu.SetActive(false);

        yield return new WaitForSeconds(.5f);

        UIFade.instance.FadeToBlack();

        yield return new WaitForSeconds(1.5f);

        for (int i = 0; i < activeBattlers.Count; i++)
        {
            if (activeBattlers[i].isPlayer)
            {
                for (int j = 0; j < GameManager.instance.playerStats.Length; j++)
                {
                    if (activeBattlers[i].charName == GameManager.instance.playerStats[j].charName)
                    {
                        GameManager.instance.playerStats[j].currentHP = activeBattlers[i].currentHp;
                        GameManager.instance.playerStats[j].currentMP = activeBattlers[i].currentMP;
                    }
                }
            }

            Destroy(activeBattlers[i].gameObject);
        }

        UIFade.instance.FadeFromBlack();
        battleScene.SetActive(false);
        activeBattlers.Clear();
        currentTurn = 0;
        //GameManager.instance.battleActive = false;
        if (fleeing)
        {
            GameManager.instance.battleActive = false;
            fleeing = false;
        }
        else
        {
            BattleReward.instance.OpenRewardScreen(rewardXP, rewardItems);
        }

        AudioManager.instance.PlayBGM(FindObjectOfType<CameraController>().musicToPlay);
    }

    public IEnumerator GameOverCo()
    {
        battleActive = false;
        UIFade.instance.FadeToBlack();
        yield return new WaitForSeconds(1.5f);
        battleScene.SetActive(false);
        SceneManager.LoadScene(gameOverScene);
    }


}
