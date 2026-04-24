using Godot;
using System;

public partial class RoomManager : Node2D
{
    [Export] public TileMapLayer BaseGridLayer;
    [Export] public Node2D ObjectsContainer;
    [Export] public Vector2I CellSize = new(16, 16);
    [Export] public Godot.Collections.Dictionary<string, PackedScene> ObjectPrefabs = new();
    [Export] public string ActivePlayerId = "player_1";
    [Export] public string ActiveObjectType = "TestObject";

    private Vector2I _selectedSizeInTiles = Vector2I.One;

    public override void _Ready()
    {
        if (ObjectsContainer == null)
        {
            ObjectsContainer = GetNodeOrNull<Node2D>("ObjectsContainer");
            if (ObjectsContainer == null)
            {
                ObjectsContainer = new Node2D { Name = "ObjectsContainer" };
                AddChild(ObjectsContainer);
            }
        }

        if (BaseGridLayer == null)
        {
            BaseGridLayer = GetNodeOrNull<TileMapLayer>("TileMapLayer");
        }

        // TileMapLayer uses a TileSet. Enforce the requested 16x16 base cell size when available.
        if (BaseGridLayer?.TileSet != null)
        {
            BaseGridLayer.TileSet.TileSize = CellSize;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            HandleHotkeys(keyEvent.Keycode);
        }

        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.Pressed &&
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            PlaceTestObjectAtMouse();
        }
    }

    public Vector2 GetSnappedPosition(Vector2 globalPos, Vector2I sizeInTiles)
    {
        var sizePixels = new Vector2I(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y);
        var snappedTopLeft = new Vector2(
            Mathf.Round(globalPos.X / CellSize.X) * CellSize.X,
            Mathf.Round(globalPos.Y / CellSize.Y) * CellSize.Y
        );

        // Most Node2D content (Sprite2D) has centered pivot. Shift to the center of occupied grid area.
        return snappedTopLeft + sizePixels / 2.0f;
    }

    public void PlaceObject(Node2D instance, string objectType, Vector2 globalPos, Vector2I sizeInTiles, string playerId)
    {
        if (instance == null)
        {
            GD.PushError("PlaceObject called with null instance.");
            return;
        }

        var snappedPosition = GetSnappedPosition(globalPos, sizeInTiles);
        var gridPosition = GetGridPositionFromSnapped(snappedPosition, sizeInTiles);

        instance.GlobalPosition = snappedPosition;
        instance.SetMeta("ObjectType", objectType);
        instance.SetMeta("GridPosition", gridPosition);
        instance.SetMeta("SizeInTiles", sizeInTiles);
        instance.SetMeta("PlayerID", playerId);

        if (instance.GetParent() != ObjectsContainer)
        {
            ObjectsContainer.AddChild(instance);
        }
    }

    public void SaveRoom(string playerId)
    {
        if (ObjectsContainer == null)
        {
            GD.PushError("ObjectsContainer is missing. Cannot save room.");
            return;
        }

        var roomData = new RoomData { PlayerID = playerId };

        foreach (var child in ObjectsContainer.GetChildren())
        {
            if (child is not Node2D node)
            {
                continue;
            }

            var objectType = node.HasMeta("ObjectType") ? node.GetMeta("ObjectType").AsString() : node.Name;
            var gridPosition = node.HasMeta("GridPosition")
                ? (Vector2I)node.GetMeta("GridPosition")
                : WorldToGrid(node.GlobalPosition);

            roomData.Objects.Add(new PlacedObjectData
            {
                PlayerID = playerId,
                ObjectType = objectType,
                GridPosition = gridPosition
            });
        }

        var roomDictionary = roomData.ToDictionary();
        var jsonText = Json.Stringify(roomDictionary, "\t");
        var savePath = GetRoomFilePath(playerId);

        using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Write);
        file.StoreString(jsonText);
    }

    public void LoadRoom(string playerId)
    {
        if (ObjectsContainer == null)
        {
            GD.PushError("ObjectsContainer is missing. Cannot load room.");
            return;
        }

        ClearObjectsContainer();

        var savePath = GetRoomFilePath(playerId);
        if (!FileAccess.FileExists(savePath))
        {
            GD.Print($"No saved room for player '{playerId}'.");
            return;
        }

        using var file = FileAccess.Open(savePath, FileAccess.ModeFlags.Read);
        var jsonText = file.GetAsText();

        var parseResult = Json.ParseString(jsonText);
        if (parseResult.VariantType != Variant.Type.Dictionary)
        {
            GD.PushError($"Invalid room save data for player '{playerId}'.");
            return;
        }

        var roomData = RoomData.FromDictionary(parseResult.AsGodotDictionary());

        foreach (var entry in roomData.Objects)
        {
            var node = InstantiateObjectForType(entry.ObjectType);
            if (node == null)
            {
                continue;
            }

            var position = GridToWorld(entry.GridPosition, Vector2I.One);
            node.GlobalPosition = position;
            node.SetMeta("ObjectType", entry.ObjectType);
            node.SetMeta("GridPosition", entry.GridPosition);
            node.SetMeta("SizeInTiles", Vector2I.One);
            node.SetMeta("PlayerID", entry.PlayerID);
            ObjectsContainer.AddChild(node);
        }
    }

    private void HandleHotkeys(Key keycode)
    {
        switch (keycode)
        {
            case Key.Key1:
                _selectedSizeInTiles = new Vector2I(1, 1);
                GD.Print("Selected size: 1x1 (16x16)");
                break;
            case Key.Key2:
                _selectedSizeInTiles = new Vector2I(2, 2);
                GD.Print("Selected size: 2x2 (32x32)");
                break;
            case Key.Key3:
                _selectedSizeInTiles = new Vector2I(4, 4);
                GD.Print("Selected size: 4x4 (64x64)");
                break;
            case Key.Key4:
                _selectedSizeInTiles = new Vector2I(8, 8);
                GD.Print("Selected size: 8x8 (128x128)");
                break;
            case Key.F5:
                SaveRoom(ActivePlayerId);
                GD.Print($"Room saved for '{ActivePlayerId}'.");
                break;
            case Key.F9:
                LoadRoom(ActivePlayerId);
                GD.Print($"Room loaded for '{ActivePlayerId}'.");
                break;
        }
    }

    private void PlaceTestObjectAtMouse()
    {
        var mousePosition = GetGlobalMousePosition();
        var node = InstantiateObjectForType(ActiveObjectType);
        if (node == null)
        {
            GD.PushError($"Could not instantiate object type '{ActiveObjectType}'.");
            return;
        }

        // When no prefab exists, create a simple colored square marker for quick visual testing.
        if (node is Node2D node2D && node2D.GetChildCount() == 0 && !ObjectPrefabs.ContainsKey(ActiveObjectType))
        {
            var marker = new ColorRect
            {
                Color = new Color(0.2f, 0.75f, 1.0f, 0.65f),
                Size = new Vector2(_selectedSizeInTiles.X * CellSize.X, _selectedSizeInTiles.Y * CellSize.Y),
                Position = new Vector2(
                    -(_selectedSizeInTiles.X * CellSize.X) / 2.0f,
                    -(_selectedSizeInTiles.Y * CellSize.Y) / 2.0f
                )
            };
            node2D.AddChild(marker);
        }

        PlaceObject(node, ActiveObjectType, mousePosition, _selectedSizeInTiles, ActivePlayerId);
    }

    private Node2D InstantiateObjectForType(string objectType)
    {
        if (ObjectPrefabs.TryGetValue(objectType, out var packedScene) && packedScene != null)
        {
            return packedScene.Instantiate<Node2D>();
        }

        // Fallback marker to keep room layout loading functional even without configured prefabs.
        return new Node2D { Name = objectType };
    }

    private Vector2I GetGridPositionFromSnapped(Vector2 snappedPosition, Vector2I sizeInTiles)
    {
        var halfSize = new Vector2(sizeInTiles.X * CellSize.X / 2.0f, sizeInTiles.Y * CellSize.Y / 2.0f);
        var topLeft = snappedPosition - halfSize;
        return new Vector2I(
            Mathf.RoundToInt(topLeft.X / CellSize.X),
            Mathf.RoundToInt(topLeft.Y / CellSize.Y)
        );
    }

    private Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(
            Mathf.RoundToInt(worldPosition.X / CellSize.X),
            Mathf.RoundToInt(worldPosition.Y / CellSize.Y)
        );
    }

    private Vector2 GridToWorld(Vector2I gridPosition, Vector2I sizeInTiles)
    {
        var topLeft = new Vector2(gridPosition.X * CellSize.X, gridPosition.Y * CellSize.Y);
        var sizePixels = new Vector2(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y);
        return topLeft + sizePixels / 2.0f;
    }

    private string GetRoomFilePath(string playerId)
    {
        return $"user://rooms_{playerId}.json";
    }

    private void ClearObjectsContainer()
    {
        foreach (var child in ObjectsContainer.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }
    }
}
