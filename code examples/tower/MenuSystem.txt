
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuSystem : MonoBehaviour
{
    private readonly Dictionary<MenuID, Menu> menus = new Dictionary<MenuID, Menu>();
    private MenuID openMenu;
    private List<Player> activePlayers = new List<Player>();
    private TowerManager towerManager;
    private CameraController camController;
    
    public int MultiplayerTargetFloor { get; set; }
    
    [SerializeField] private Image background;
    [SerializeField] private GameObject mainMenuPlatform;
    private UIUpdater overlayUi;
    private bool readyToPause;

    private AudioSource menuSfx;
    private float timeScaleAtLastPause = 1f;

    private void Awake()
    {
        activePlayers = FindObjectsOfType<Player>().ToList();
        towerManager = FindObjectOfType<TowerManager>();
        camController = FindObjectOfType<CameraController>();
        overlayUi = FindObjectOfType<UIUpdater>();
        background.gameObject.SetActive(false);
        mainMenuPlatform.SetActive(false);
        menuSfx = GetComponent<AudioSource>();
        InitialiseMenus();
    }

    private void Start()
    {
        DeactivateMenus();
        OpenMenu(MenuID.Main);
    }

    private void Update()
    {
        CheckForInput();
    }

    public MenuID GetOpenMenu()
    {
        return openMenu;
    }

    private void CheckForInput()
    {
        if (Input.GetButtonDown("Pause") && openMenu == MenuID.None && readyToPause)
        {
            PlaySfx("pause");
            OpenMenu(MenuID.Pause);
        }

        // So pause isnt pressed immediately upon starting multiplayer.
        if (!readyToPause && openMenu == MenuID.None)
        {
            readyToPause = true;
        }
    }

    public void EndMultiplayerGame()
    {
        Player winner = null;
        foreach (var activePlayer in activePlayers)
        {
            if (winner == null)
            {
                winner = activePlayer;
            }
            else if (activePlayer.CurrentScore > winner.CurrentScore)
            {
                winner = activePlayer;
            }
            
            activePlayer.Die(endGame:true);
        }
        
        var playersSorted = new List<Player>(activePlayers);
        playersSorted = playersSorted.OrderBy(p => p.CurrentScore).Reverse().ToList();
        var rankingsPrefab = Resources.Load<MultiplayerRankings>("prefabs/rankings");
        var rankingsObject = Instantiate(rankingsPrefab);
        rankingsObject.MenuSystem = this;
        rankingsObject.transform.SetParent(overlayUi.transform);
        rankingsObject.transform.position = overlayUi.transform.position;
        rankingsObject.DisplayRankings(playersSorted);
        PlaySfx("success");
        
    }

    private void InitialiseMenus()
    {
        var menuList = GetComponentsInChildren<Menu>();
        foreach (var menu in menuList)
        {
            menus[menu.menuId] = menu;
        }
    }

    private void DeactivateMenus()
    {
        var menuList = GetComponentsInChildren<Menu>();
        foreach (var menu in menuList)
        {
            menu.Close();
        }
    }

    private void PauseGame()
    {
        timeScaleAtLastPause = Time.timeScale;
        Time.timeScale = 0f;
        Game.IsPaused = true;
    }

    public List<Player> GetActivePlayers()
    {
        return activePlayers;
    }

    public void PlaySfx(string fileName)
    {
        menuSfx.PlayOneShot(Resources.Load<AudioClip>("sfx/"+ fileName));
    }

    // Here is where we update all of the players.
    public void UpdateActivePlayers(List<Player> players)
    {
        activePlayers = players;
        var deathZones = FindObjectsOfType<DeathZone>();
        foreach (var deathZone in deathZones)
        {
            deathZone.SetPlayers(players);
        }
        overlayUi.SetPlayers(players);
        towerManager.SetPlayers(players);
        camController.SetPlayers(players);
    }

    private void ResetPlayers()
    {
        foreach (var activePlayer in activePlayers)
        {
            activePlayer.ResetPlayer();
        }
    }

    private void SetPlayersHasControl(bool hasControl)
    {
        foreach (var activePlayer in activePlayers)
        {
            activePlayer.PlayerInput.HasControl = hasControl;
        }
    }

    #region buttonFunctions

    public void OpenMenu(MenuID newMenuId)
    {
        if (openMenu != MenuID.None)
        {
            menus[openMenu].Close(); // Close current menu.
        }

        var prevMenu = openMenu;
        openMenu = newMenuId;
        background.gameObject.SetActive(true);

        switch (newMenuId)
        {
            case MenuID.None:
                Game.IsPaused = false;
                Time.timeScale = timeScaleAtLastPause;
                SetPlayersHasControl(true);
                background.gameObject.SetActive(false);
                mainMenuPlatform.SetActive(false);
                overlayUi.contents.gameObject.SetActive(true);
                return; // Opening MenuID.None closes the current menu and opens no others.
            case MenuID.Pause:
                PauseGame();
                break;
            case MenuID.Main:
                overlayUi.contents.gameObject.SetActive(false);
                towerManager.ResetTower();
                ResetPlayers();
                timeScaleAtLastPause = 1;
                Time.timeScale = 1;
                background.gameObject.SetActive(false);
                mainMenuPlatform.SetActive(true);
                readyToPause = false;
                break;
        }

        SetPlayersHasControl(false);
        menus[newMenuId].Open(prevMenu);
    }

    public void StartGame(bool multiplayer=false)
    {
        camController.FreeCamera();
        towerManager.StartDropping(multiplayer);
        ResumeGame();
        SetPlayersHasControl(true);
    }

    public void ResumeGame()
    {
        OpenMenu(MenuID.None);
    }

    public void RestartGame()
    {
        timeScaleAtLastPause = 1;
        towerManager.ResetTower();
        towerManager.StartDropping();
        ResetPlayers();
        camController.FreeCamera();
        ResumeGame();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    #endregion


}
