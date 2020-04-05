
using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

public static class Loader
{
    // Load the player's progress for this level.
    public static StageData LoadStageProgress(StageID stageId)
    {
        var path = Path.Combine(Environment.CurrentDirectory + "/Assets/StageData", stageId + ".xml");
        StageData stageData;

        try
        {
            stageData = Load<StageData>(path);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to load Stage Data:" + stageId);
            stageData = new StageData(stageId);
        }

        return stageData;
    }
    
    public static GameData LoadGameProgress()
    {
        var path = Path.Combine(Environment.CurrentDirectory + "/Assets/GameData", "game.xml");
        GameData gameData;
        try
        {
            gameData = Load<GameData>(path);
        }
        catch (Exception e)
        {
            Debug.Log("Failed to load Game Data: " + e);
            gameData = new GameData();
            Game.WriteGameData(gameData);
        }

        return gameData;
    }

    private static T Load<T>(string path)
    {
        var dataOut = default(T);
        var xmlDocument = new XmlDocument();
        xmlDocument.Load(path);
        var xmlString = xmlDocument.OuterXml;

        using (var read = new StringReader(xmlString))
        {
            var outType = typeof(T);
            var serializer = new XmlSerializer(outType);
            using (var reader = new XmlTextReader(read))
            {
                dataOut = (T) serializer.Deserialize(reader);
            }
        }

        return dataOut;
    }
}
