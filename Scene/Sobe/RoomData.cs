using System;
using Godot;

[Serializable]
public partial class PlacedObjectData
{
    public string PlayerID { get; set; } = string.Empty;
    public string ObjectType { get; set; } = string.Empty;
    public Vector2I GridPosition { get; set; } = Vector2I.Zero;

    public Dictionary ToDictionary()
    {
        return new Dictionary
        {
            { "player_id", PlayerID },
            { "object_type", ObjectType },
            { "grid_x", GridPosition.X },
            { "grid_y", GridPosition.Y }
        };
    }

    public static PlacedObjectData FromDictionary(Dictionary data)
    {
        return new PlacedObjectData
        {
            PlayerID = data.GetValueOrDefault("player_id", string.Empty).AsString(),
            ObjectType = data.GetValueOrDefault("object_type", string.Empty).AsString(),
            GridPosition = new Vector2I(
                data.GetValueOrDefault("grid_x", 0).AsInt32(),
                data.GetValueOrDefault("grid_y", 0).AsInt32()
            )
        };
    }
}

[Serializable]
public partial class RoomData
{
    public string PlayerID { get; set; } = string.Empty;
    public Godot.Collections.Array<PlacedObjectData> Objects { get; set; } = new();

    public Dictionary ToDictionary()
    {
        var objectsArray = new Godot.Collections.Array<Dictionary>();
        foreach (var placedObject in Objects)
        {
            objectsArray.Add(placedObject.ToDictionary());
        }

        return new Dictionary
        {
            { "player_id", PlayerID },
            { "objects", objectsArray }
        };
    }

    public static RoomData FromDictionary(Dictionary data)
    {
        var roomData = new RoomData
        {
            PlayerID = data.GetValueOrDefault("player_id", string.Empty).AsString()
        };

        var objectsVariant = data.GetValueOrDefault("objects", new Godot.Collections.Array());
        var objectsArray = objectsVariant.AsGodotArray();

        foreach (var item in objectsArray)
        {
            var objectDictionary = item.AsGodotDictionary();
            roomData.Objects.Add(PlacedObjectData.FromDictionary(objectDictionary));
        }

        return roomData;
    }
}
