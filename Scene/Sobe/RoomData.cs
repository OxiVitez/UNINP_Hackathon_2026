using System;
using System.Collections.Generic;
using Godot;

[Serializable]
public partial class PlacedObjectData
{
    public string PlayerID { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public Vector2I GridPosition { get; set; } = Vector2I.Zero;
    public Vector2I SizeInTiles { get; set; } = Vector2I.One;

    public Godot.Collections.Dictionary ToDictionary()
    {
        return new Godot.Collections.Dictionary
        {
            { "player_id", PlayerID },
            { "object_type", ObjectType },
            { "grid_x", GridPosition.X },
            { "grid_y", GridPosition.Y },
            { "size_x", SizeInTiles.X },
            { "size_y", SizeInTiles.Y }
        };
    }

    public static PlacedObjectData FromDictionary(Godot.Collections.Dictionary data)
    {
        var playerId = data.ContainsKey("player_id") ? data["player_id"].AsString() : string.Empty;
        var objectType = data.ContainsKey("object_type") ? data["object_type"].AsString() : string.Empty;
        var gridX = data.ContainsKey("grid_x") ? data["grid_x"].AsInt32() : 0;
        var gridY = data.ContainsKey("grid_y") ? data["grid_y"].AsInt32() : 0;
        var sizeX = data.ContainsKey("size_x") ? data["size_x"].AsInt32() : 1;
        var sizeY = data.ContainsKey("size_y") ? data["size_y"].AsInt32() : 1;

        return new PlacedObjectData
        {
            PlayerID = playerId,
            ObjectType = objectType,
            GridPosition = new Vector2I(gridX, gridY),
            SizeInTiles = new Vector2I(sizeX, sizeY)
        };
    }
}

[Serializable]
public partial class RoomData
{
    public string PlayerID { get; set; } = string.Empty;
    public List<PlacedObjectData> Objects { get; set; } = new();

    public Godot.Collections.Dictionary ToDictionary()
    {
        var objectsArray = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var placedObject in Objects)
        {
            objectsArray.Add(placedObject.ToDictionary());
        }

        return new Godot.Collections.Dictionary
        {
            { "player_id", PlayerID },
            { "objects", objectsArray }
        };
    }

    public static RoomData FromDictionary(Godot.Collections.Dictionary data)
    {
        var roomData = new RoomData
        {
            PlayerID = data.ContainsKey("player_id") ? data["player_id"].AsString() : string.Empty
        };

        var objectsArray = data.ContainsKey("objects")
            ? data["objects"].AsGodotArray()
            : new Godot.Collections.Array();

        foreach (var item in objectsArray)
        {
            var objectDictionary = item.AsGodotDictionary();
            roomData.Objects.Add(PlacedObjectData.FromDictionary(objectDictionary));
        }

        return roomData;
    }
}
