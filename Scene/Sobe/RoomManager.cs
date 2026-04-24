using Godot;
using System;
using System.Collections.Generic;

public partial class RoomManager : Node2D
{
    [Export] public TileMapLayer BaseGridLayer;
    [Export] public Node2D ObjectsContainer;
    [Export] public Vector2I CellSize = new(16, 16);
    [Export] public Godot.Collections.Dictionary<string, PackedScene> ObjectPrefabs = new();
    [Export] public string ActivePlayerId = "player_1";

    private readonly HashSet<Vector2I> _occupiedTiles = new();
    private InventoryPanel _inventoryPanel;
    private Button _inventoryButton;
    private TextureRect _dragPreview;
    private bool _isDraggingFromInventory;
    private string _dragObjectType = string.Empty;
    private string _dragTexturePath = string.Empty;
    private Vector2I _dragSizeInTiles = Vector2I.One;
    private Node2D _draggedPlacedNode;
    private Vector2I _draggedOriginalGrid;
    private Vector2I _draggedSize = Vector2I.One;

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

        if (BaseGridLayer?.TileSet != null)
        {
            BaseGridLayer.TileSet.TileSize = CellSize;
        }

        _inventoryPanel = GetNodeOrNull<InventoryPanel>("Inventar");
        if (_inventoryPanel != null)
        {
            _inventoryPanel.SetPlayer(ActivePlayerId);
            _inventoryPanel.DragRequested += StartInventoryDrag;
        }

        _inventoryButton = GetNodeOrNull<Button>("InventoryButton");
        if (_inventoryButton != null)
        {
            _inventoryButton.Pressed += () => _inventoryPanel?.ToggleVisibility();
        }

        _dragPreview = new TextureRect
        {
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Modulate = new Color(1f, 1f, 1f, 0.75f),
            ZIndex = 1000
        };
        AddChild(_dragPreview);

        RebuildOccupiedTiles();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton pressedEvent &&
            pressedEvent.Pressed &&
            pressedEvent.ButtonIndex == MouseButton.Left &&
            !_isDraggingFromInventory)
        {
            TryStartPlacedItemDrag();
        }

        if (@event is InputEventMouseButton mouseEvent &&
            !mouseEvent.Pressed &&
            mouseEvent.ButtonIndex == MouseButton.Left &&
            _isDraggingFromInventory)
        {
            TryPlaceDraggedItemAtMouse();
            EndInventoryDrag();
        }

        if (@event is InputEventMouseButton releaseEvent &&
            !releaseEvent.Pressed &&
            releaseEvent.ButtonIndex == MouseButton.Left &&
            _draggedPlacedNode != null)
        {
            EndPlacedItemDrag();
        }
    }

    public override void _Process(double delta)
    {
        if (_isDraggingFromInventory && _dragPreview != null)
        {
            var mousePos = GetViewport().GetMousePosition();
            _dragPreview.Position = mousePos - (_dragPreview.Size / 2.0f);
        }

        if (_draggedPlacedNode != null)
        {
            _draggedPlacedNode.GlobalPosition = GetSnappedPosition(GetGlobalMousePosition(), _draggedSize);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.I:
                    _inventoryPanel?.ToggleVisibility();
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

        if (@event is InputEventMouseButton mouseEvent &&
            mouseEvent.Pressed &&
            mouseEvent.ButtonIndex == MouseButton.Right)
        {
            TryRemoveObjectAtMouse();
        }
    }

    public Vector2 GetSnappedPosition(Vector2 globalPos, Vector2I sizeInTiles)
    {
        var sizePixels = new Vector2I(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y);
        var snappedTopLeft = new Vector2(
            Mathf.Round(globalPos.X / CellSize.X) * CellSize.X,
            Mathf.Round(globalPos.Y / CellSize.Y) * CellSize.Y
        );
        var halfSize = new Vector2(sizePixels.X / 2.0f, sizePixels.Y / 2.0f);
        return snappedTopLeft + halfSize;
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
        if (!CanPlaceAt(gridPosition, sizeInTiles))
        {
            instance.QueueFree();
            GD.Print("Cannot place item: target tiles are already occupied.");
            return;
        }

        instance.GlobalPosition = snappedPosition;
        instance.SetMeta("ObjectType", objectType);
        instance.SetMeta("GridPosition", gridPosition);
        instance.SetMeta("SizeInTiles", sizeInTiles);
        instance.SetMeta("PlayerID", playerId);

        if (instance.GetParent() != ObjectsContainer)
        {
            ObjectsContainer.AddChild(instance);
        }

        MarkOccupied(gridPosition, sizeInTiles);
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

            var objectType = node.HasMeta("ObjectType") ? node.GetMeta("ObjectType").AsString() : node.Name.ToString();
            var gridPosition = node.HasMeta("GridPosition")
                ? (Vector2I)node.GetMeta("GridPosition")
                : WorldToGrid(node.GlobalPosition);
            var sizeInTiles = node.HasMeta("SizeInTiles")
                ? (Vector2I)node.GetMeta("SizeInTiles")
                : Vector2I.One;

            roomData.Objects.Add(new PlacedObjectData
            {
                PlayerID = playerId,
                ObjectType = objectType,
                GridPosition = gridPosition,
                SizeInTiles = sizeInTiles
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
        _occupiedTiles.Clear();
        _inventoryPanel?.SetPlayer(playerId);

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
            var node = InstantiateObjectForType(entry.ObjectType, entry.SizeInTiles);
            if (node == null)
            {
                continue;
            }

            node.GlobalPosition = GridToWorld(entry.GridPosition, entry.SizeInTiles);
            node.SetMeta("ObjectType", entry.ObjectType);
            node.SetMeta("GridPosition", entry.GridPosition);
            node.SetMeta("SizeInTiles", entry.SizeInTiles);
            node.SetMeta("PlayerID", entry.PlayerID);
            ObjectsContainer.AddChild(node);
            MarkOccupied(entry.GridPosition, entry.SizeInTiles);
        }
    }

    private void StartInventoryDrag(string objectType, Vector2I sizeInTiles, string texturePath)
    {
        if (_dragPreview == null)
        {
            return;
        }

        _dragObjectType = objectType;
        _dragSizeInTiles = sizeInTiles;
        _dragTexturePath = texturePath;

        var texture = GD.Load<Texture2D>(_dragTexturePath);
        if (texture == null)
        {
            GD.PushError($"Inventory texture missing: {_dragTexturePath}");
            return;
        }

        _dragPreview.Texture = texture;
        _dragPreview.Size = new Vector2(_dragSizeInTiles.X * CellSize.X, _dragSizeInTiles.Y * CellSize.Y);
        _dragPreview.Visible = true;
        _isDraggingFromInventory = true;
    }

    private void EndInventoryDrag()
    {
        _isDraggingFromInventory = false;
        if (_dragPreview != null)
        {
            _dragPreview.Visible = false;
            _dragPreview.Texture = null;
        }
        _dragObjectType = string.Empty;
        _dragTexturePath = string.Empty;
        _dragSizeInTiles = Vector2I.One;
    }

    private void TryPlaceDraggedItemAtMouse()
    {
        if (string.IsNullOrWhiteSpace(_dragObjectType))
        {
            return;
        }

        var node = InstantiateObjectForType(_dragObjectType, _dragSizeInTiles);
        if (node == null)
        {
            return;
        }

        PlaceObject(node, _dragObjectType, GetGlobalMousePosition(), _dragSizeInTiles, ActivePlayerId);
    }

    private void TryStartPlacedItemDrag()
    {
        var mouseGrid = WorldToGrid(GetGlobalMousePosition());
        var placedNode = FindPlacedNodeAtGrid(mouseGrid);
        if (placedNode == null)
        {
            return;
        }

        _draggedPlacedNode = placedNode;
        _draggedOriginalGrid = placedNode.HasMeta("GridPosition")
            ? (Vector2I)placedNode.GetMeta("GridPosition")
            : WorldToGrid(placedNode.GlobalPosition);
        _draggedSize = placedNode.HasMeta("SizeInTiles")
            ? (Vector2I)placedNode.GetMeta("SizeInTiles")
            : Vector2I.One;
        UnmarkOccupied(_draggedOriginalGrid, _draggedSize);
    }

    private void EndPlacedItemDrag()
    {
        if (_draggedPlacedNode == null)
        {
            return;
        }

        var snappedPosition = GetSnappedPosition(GetGlobalMousePosition(), _draggedSize);
        var newGrid = GetGridPositionFromSnapped(snappedPosition, _draggedSize);
        if (CanPlaceAt(newGrid, _draggedSize))
        {
            _draggedPlacedNode.GlobalPosition = snappedPosition;
            _draggedPlacedNode.SetMeta("GridPosition", newGrid);
            MarkOccupied(newGrid, _draggedSize);
        }
        else
        {
            _draggedPlacedNode.GlobalPosition = GridToWorld(_draggedOriginalGrid, _draggedSize);
            _draggedPlacedNode.SetMeta("GridPosition", _draggedOriginalGrid);
            MarkOccupied(_draggedOriginalGrid, _draggedSize);
        }

        _draggedPlacedNode = null;
        _draggedSize = Vector2I.One;
        _draggedOriginalGrid = Vector2I.Zero;
    }

    private Node2D InstantiateObjectForType(string objectType, Vector2I sizeInTiles)
    {
        if (ObjectPrefabs.TryGetValue(objectType, out var packedScene) && packedScene != null)
        {
            return packedScene.Instantiate<Node2D>();
        }

        var node = new Node2D { Name = objectType };
        var sprite = BuildItemSprite(objectType, sizeInTiles);
        if (sprite != null)
        {
            node.AddChild(sprite);
            return node;
        }

        return node;
    }

    private Sprite2D BuildItemSprite(string objectType, Vector2I sizeInTiles)
    {
        var texturePath = GetTexturePathForObjectType(objectType);
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        var texture = GD.Load<Texture2D>(texturePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = new Sprite2D { Texture = texture, Centered = true };
        var targetSize = new Vector2(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y);
        if (texture.GetSize().X > 0 && texture.GetSize().Y > 0)
        {
            sprite.Scale = new Vector2(targetSize.X / texture.GetSize().X, targetSize.Y / texture.GetSize().Y);
        }
        return sprite;
    }

    private string GetTexturePathForObjectType(string objectType)
    {
        if (_inventoryPanel != null && _inventoryPanel.TryGetTexturePath(objectType, out var mappedPath))
        {
            return mappedPath;
        }
        return string.Empty;
    }

    private Vector2I GetGridPositionFromSnapped(Vector2 snappedPosition, Vector2I sizeInTiles)
    {
        var halfSize = new Vector2(sizeInTiles.X * CellSize.X / 2.0f, sizeInTiles.Y * CellSize.Y / 2.0f);
        var topLeft = snappedPosition - halfSize;
        return new Vector2I(Mathf.RoundToInt(topLeft.X / CellSize.X), Mathf.RoundToInt(topLeft.Y / CellSize.Y));
    }

    private Vector2I WorldToGrid(Vector2 worldPosition)
    {
        return new Vector2I(Mathf.RoundToInt(worldPosition.X / CellSize.X), Mathf.RoundToInt(worldPosition.Y / CellSize.Y));
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

    private bool CanPlaceAt(Vector2I gridPosition, Vector2I sizeInTiles)
    {
        for (var x = 0; x < sizeInTiles.X; x++)
        {
            for (var y = 0; y < sizeInTiles.Y; y++)
            {
                if (_occupiedTiles.Contains(new Vector2I(gridPosition.X + x, gridPosition.Y + y)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void MarkOccupied(Vector2I gridPosition, Vector2I sizeInTiles)
    {
        for (var x = 0; x < sizeInTiles.X; x++)
        {
            for (var y = 0; y < sizeInTiles.Y; y++)
            {
                _occupiedTiles.Add(new Vector2I(gridPosition.X + x, gridPosition.Y + y));
            }
        }
    }

    private void UnmarkOccupied(Vector2I gridPosition, Vector2I sizeInTiles)
    {
        for (var x = 0; x < sizeInTiles.X; x++)
        {
            for (var y = 0; y < sizeInTiles.Y; y++)
            {
                _occupiedTiles.Remove(new Vector2I(gridPosition.X + x, gridPosition.Y + y));
            }
        }
    }

    private void RebuildOccupiedTiles()
    {
        _occupiedTiles.Clear();
        foreach (var child in ObjectsContainer.GetChildren())
        {
            if (child is not Node2D node)
            {
                continue;
            }

            var gridPosition = node.HasMeta("GridPosition") ? (Vector2I)node.GetMeta("GridPosition") : WorldToGrid(node.GlobalPosition);
            var sizeInTiles = node.HasMeta("SizeInTiles") ? (Vector2I)node.GetMeta("SizeInTiles") : Vector2I.One;
            MarkOccupied(gridPosition, sizeInTiles);
        }
    }

    private void TryRemoveObjectAtMouse()
    {
        var mouseGrid = WorldToGrid(GetGlobalMousePosition());
        var placedNode = FindPlacedNodeAtGrid(mouseGrid);
        if (placedNode == null)
        {
            return;
        }

        var gridPosition = placedNode.HasMeta("GridPosition") ? (Vector2I)placedNode.GetMeta("GridPosition") : WorldToGrid(placedNode.GlobalPosition);
        var sizeInTiles = placedNode.HasMeta("SizeInTiles") ? (Vector2I)placedNode.GetMeta("SizeInTiles") : Vector2I.One;
        UnmarkOccupied(gridPosition, sizeInTiles);
        placedNode.QueueFree();
    }

    private Node2D FindPlacedNodeAtGrid(Vector2I grid)
    {
        foreach (var child in ObjectsContainer.GetChildren())
        {
            if (child is not Node2D node)
            {
                continue;
            }

            var gridPosition = node.HasMeta("GridPosition") ? (Vector2I)node.GetMeta("GridPosition") : WorldToGrid(node.GlobalPosition);
            var sizeInTiles = node.HasMeta("SizeInTiles") ? (Vector2I)node.GetMeta("SizeInTiles") : Vector2I.One;
            var insideX = grid.X >= gridPosition.X && grid.X < gridPosition.X + sizeInTiles.X;
            var insideY = grid.Y >= gridPosition.Y && grid.Y < gridPosition.Y + sizeInTiles.Y;
            if (insideX && insideY)
            {
                return node;
            }
        }
        return null;
    }
}
