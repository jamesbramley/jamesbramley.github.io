
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TowerManager : MonoBehaviour
{
    private readonly Generator towerGenerator = new Generator();
    private readonly PowerUpSelector powerUpSelector = new PowerUpSelector();
    private BombDropper bombDropper;
    
    private System.Random rand = new System.Random();

    private Tower currentTower;
    private List<Player> players;

    public float LeftStartingPosition { get; set; }
    public float roomObjectWidth = 10f;
    private const float RoomObjectHeight = 7.5f;
    private const int MaxFloorsAboveAndBelow = 3;
    private MenuSystem menuSystem;

    private bool canGenerate;

    public List<RoomObject> instantiatedRooms;

    private void Awake()
    {
        menuSystem = FindObjectOfType<MenuSystem>();
    }

    private void Start()
    {
        players = FindObjectsOfType<Player>().ToList();
        bombDropper = GetComponent<BombDropper>();
        instantiatedRooms = new List<RoomObject>();
        LeftStartingPosition = transform.position.x;
        ResetTower();
    }

    private void Update()
    {
        var lowestPlayerY = players.Min(p => p.transform.position.y);
        var player = players.Find(p => p.transform.position.y <= lowestPlayerY);
        canGenerate = player.CurrentFloor > currentTower.height - MaxFloorsAboveAndBelow;
        HandleFloors();
    }

    public void SetPlayers(List<Player> players)
    {
        this.players = players;
    }

    public float GetTowerCentrePointX()
    {
        return LeftStartingPosition + roomObjectWidth * (GetTowerWidth() / 2f);
    }

    public float GetTopOfTowerY()
    {
        var highestFloorY = instantiatedRooms.Max(f => f.transform.position.y);
        return highestFloorY - RoomObjectHeight;
    }

    public int GetTowerWidth()
    {
        return towerGenerator.TowerWidth;
    }

    public Tower GetTower()
    {
        return currentTower;
    }

    public void ResetTower()
    {
        foreach (var instantiatedRoom in instantiatedRooms)
        {
            Destroy(instantiatedRoom.gameObject);
        }

        foreach(var bomb in GameObject.FindGameObjectsWithTag("Bomb")) {
            Destroy(bomb.gameObject);
        }

        instantiatedRooms = new List<RoomObject>();

        bombDropper.Reset();

        GenerateTower();
        InstantiateTower();
    }

    public void StartDropping(bool multiplayer=false)
    {
        bombDropper.StartDropping(multiplayer);
    }

    private void HandleFloors()
    {
        var towerOriginalHeight = currentTower.height;
        if (canGenerate)
        {
            Debug.Log("Generating " + MaxFloorsAboveAndBelow + " additional floors.");
            currentTower = towerGenerator.GenerateAdditionalFloors(currentTower, MaxFloorsAboveAndBelow);
            InstantiateNewFloors(currentTower, towerOriginalHeight);
            canGenerate = false;
        }
        
        DeleteOldFloors();
    }

    private void DeleteOldFloors()
    {
        var temporaryList = new List<RoomObject>(instantiatedRooms);
        var highestPlayerY = players.Max(p => p.transform.position.y);
        var player = players.Find(p => p.transform.position.y >= highestPlayerY);
        foreach (var instantiatedRoom in temporaryList)
        {
            if (instantiatedRoom.GetFloorNumber() < player.CurrentFloor - MaxFloorsAboveAndBelow)
            {
                Destroy(instantiatedRoom.gameObject);
                instantiatedRooms.Remove(instantiatedRoom);
            }
        }
    }

    private void InstantiateNewFloors(Tower tower, int towerOriginalHeight)
    {
        var newFloors = tower.floors.Where(floor => floor.FloorNumber >= towerOriginalHeight);

        foreach (var newFloor in newFloors)
        {
            InstantiateFloor(newFloor);
        }
    }

    private void GenerateTower()
    {
        currentTower = towerGenerator.GenerateInitialTower();
    }

    private void InstantiateTower()
    {
        foreach (var floor in currentTower.floors)
        {
            InstantiateFloor(floor);            
        }
    }

    private void InstantiateFloor(Floor floor)
    {
        var isTargetFloor = floor.FloorNumber == menuSystem.MultiplayerTargetFloor &&
                            menuSystem.GetActivePlayers().Count > 1;
        foreach (var room in floor.rooms)
        {
            InstantiateRoom(room, isTargetFloor);
        }
    }

    private void InstantiateRoom(Room room, bool isTargetFloor=false)
    {
        var roomObject = !isTargetFloor ? Instantiate(Resources.Load<RoomObject>("prefabs/Room")) : Instantiate(Resources.Load<RoomObject>("prefabs/TargetRoom"));
        roomObject.SetRoom(room);
        roomObject.transform.SetParent(gameObject.transform);
        roomObject.transform.position = new Vector3(LeftStartingPosition + roomObjectWidth*room.RoomNumber, transform.position.y - RoomObjectHeight*room.FloorNumber);
        instantiatedRooms.Add(roomObject);
        
        InstantiateRandomPowerUps(roomObject);
        InstantiateRandomCoins(roomObject);
    }

    private void InstantiateRandomPowerUps(RoomObject roomObject)
    {
        var numberOfPowerUps = powerUpSelector.GetRandomNumberOfPowerUps();

        for (int i = 0; i < numberOfPowerUps; i++)
        {
            var powerUpObject = GetPowerUpObject();
            var randomPowerUpType = powerUpSelector.GetRandomPowerUp();
            var randomPowerUp = PowerUps.GetPowerUp(randomPowerUpType);
            var instantiatedObject = Instantiate(powerUpObject);
            instantiatedObject.transform.SetParent(roomObject.transform);
            instantiatedObject.transform.localPosition = GetRandomRoomPosition();
            instantiatedObject.PowerUp = randomPowerUp;
        }
        
    }

    private void InstantiateRandomCoins(RoomObject roomObject)
    {
        var numberOfCoins = powerUpSelector.GetRandomNumberOfCoins();

        for (int i = 0; i < numberOfCoins; i++)
        {
            var coin = Resources.Load<Coin>("prefabs/coin");
            var instantiatedObject = Instantiate(coin);
            instantiatedObject.transform.SetParent(roomObject.transform);
            instantiatedObject.transform.localPosition = GetRandomRoomPosition();
        }
    }

    private PowerUpObject GetPowerUpObject()
    {
        var powerUpObject = Resources.Load<PowerUpObject>("prefabs/PowerUp");

        return powerUpObject;
    }

    private Vector2 GetRandomRoomPosition()
    {
        var xPos = roomObjectWidth/4f - (float)rand.NextDouble()*roomObjectWidth*0.5f;
        var yPos = RoomObjectHeight/2f - (float) rand.NextDouble() * RoomObjectHeight;
        
        return new Vector2(xPos, yPos);
    }
}
