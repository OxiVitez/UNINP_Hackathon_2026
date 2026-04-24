using Godot;
using System;
using System.Collections.Generic;

public partial class RoomManager : Node2D
{
    private sealed class InventoryItem
    {
        public string ObjectType { get; set; } = string.Empty;
        public string TexturePath { get; set; } = string.Empty;
        public Vector2I SizeInTiles { get; set; } = Vector2I.One;
    }

    [Export] public TileMapLayer BaseGridLayer;
    [Export] public Node2D ObjectsContainer;
    [Export] public Vector2I CellSize = new(16, 16);
    [Export] public Godot.Collections.Dictionary<string, PackedScene> ObjectPrefabs = new();
    [Export] public string ActivePlayerId = "player_1";

    private readonly HashSet<Vector2I> _occupiedTiles = new();
    private readonly List<InventoryItem> _inventoryItems = new();
    private int _selectedItemIndex;
    private Label _inventoryLabel;

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

        SetupInventory();
        BuildInventoryUi();
        RebuildOccupiedTiles();
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

            var position = GridToWorld(entry.GridPosition, entry.SizeInTiles);
            node.GlobalPosition = position;
            node.SetMeta("ObjectType", entry.ObjectType);
            node.SetMeta("GridPosition", entry.GridPosition);
            node.SetMeta("SizeInTiles", entry.SizeInTiles);
            node.SetMeta("PlayerID", entry.PlayerID);
            ObjectsContainer.AddChild(node);
            MarkOccupied(entry.GridPosition, entry.SizeInTiles);
        }
    }

    private void HandleHotkeys(Key keycode)
    {
        switch (keycode)
        {
            case Key.Key1:
                SelectInventoryItem(0);
                break;
            case Key.Key2:
                SelectInventoryItem(1);
                break;
            case Key.Key3:
                SelectInventoryItem(2);
                break;
            case Key.Key4:
                SelectInventoryItem(3);
                break;
            case Key.Tab:
                SelectInventoryItem((_selectedItemIndex + 1) % _inventoryItems.Count);
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
        if (_inventoryItems.Count == 0)
        {
            GD.PushError("Inventory is empty.");
            return;
        }

        var selectedItem = _inventoryItems[_selectedItemIndex];
        var mousePosition = GetGlobalMousePosition();
        var node = InstantiateObjectForType(selectedItem.ObjectType, selectedItem.SizeInTiles);
        if (node == null)
        {
            GD.PushError($"Could not instantiate object type '{selectedItem.ObjectType}'.");
            return;
        }

        PlaceObject(node, selectedItem.ObjectType, mousePosition, selectedItem.SizeInTiles, ActivePlayerId);
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

        var marker = new ColorRect
        {
            Color = new Color(0.2f, 0.75f, 1.0f, 0.65f),
            Size = new Vector2(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y),
            Position = new Vector2(
                -(sizeInTiles.X * CellSize.X) / 2.0f,
                -(sizeInTiles.Y * CellSize.Y) / 2.0f
            )
        };
        node.AddChild(marker);
        return node;
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

    private bool CanPlaceAt(Vector2I gridPosition, Vector2I sizeInTiles)
    {
        for (var x = 0; x < sizeInTiles.X; x++)
        {
            for (var y = 0; y < sizeInTiles.Y; y++)
            {
                var tile = new Vector2I(gridPosition.X + x, gridPosition.Y + y);
                if (_occupiedTiles.Contains(tile))
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

    private void RebuildOccupiedTiles()
    {
        _occupiedTiles.Clear();
        foreach (var child in ObjectsContainer.GetChildren())
        {
            if (child is not Node2D node)
            {
                continue;
            }

            var gridPosition = node.HasMeta("GridPosition")
                ? (Vector2I)node.GetMeta("GridPosition")
                : WorldToGrid(node.GlobalPosition);
            var sizeInTiles = node.HasMeta("SizeInTiles")
                ? (Vector2I)node.GetMeta("SizeInTiles")
                : Vector2I.One;

            MarkOccupied(gridPosition, sizeInTiles);
        }
    }

    private void SetupInventory()
    {
        _inventoryItems.Clear();
        _inventoryItems.Add(new InventoryItem
        {
            ObjectType = "Test_1x1",
            TexturePath = "res://Asseti/sOBNI/test.png",
            SizeInTiles = new Vector2I(1, 1)
        });
        _inventoryItems.Add(new InventoryItem
        {
            ObjectType = "Test_2x2",
            TexturePath = "res://Asseti/sOBNI/test.png",
            SizeInTiles = new Vector2I(2, 2)
        });
        _inventoryItems.Add(new InventoryItem
        {
            ObjectType = "Test_4x4",
            TexturePath = "res://Asseti/sOBNI/test.png",
            SizeInTiles = new Vector2I(4, 4)
        });
        _inventoryItems.Add(new InventoryItem
        {
            ObjectType = "Test_8x8",
            TexturePath = "res://Asseti/sOBNI/test.png",
            SizeInTiles = new Vector2I(8, 8)
        });

        _selectedItemIndex = 0;
    }

    private void BuildInventoryUi()
    {
        var canvasLayer = new CanvasLayer { Name = "InventoryUI" };
        AddChild(canvasLayer);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(12, 12);
        panel.Size = new Vector2(320, 140);
        canvasLayer.AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _inventoryLabel = new Label();
        vbox.AddChild(_inventoryLabel);

        var hintLabel = new Label
        {
            Text = "Click: place | 1-4/Tab: select | F5 save | F9 load"
        };
        vbox.AddChild(hintLabel);

        var buttons = new HBoxContainer();
        vbox.AddChild(buttons);

        for (var i = 0; i < _inventoryItems.Count; i++)
        {
            var localIndex = i;
            var item = _inventoryItems[i];
            var button = new Button
            {
                Text = $"{i + 1}:{item.SizeInTiles.X}x{item.SizeInTiles.Y}",
                CustomMinimumSize = new Vector2(70, 28)
            };
            button.Pressed += () => SelectInventoryItem(localIndex);
            buttons.AddChild(button);
        }

        UpdateInventoryLabel();
    }

    private void SelectInventoryItem(int index)
    {
        if (index < 0 || index >= _inventoryItems.Count)
        {
            return;
        }

        _selectedItemIndex = index;
        UpdateInventoryLabel();
        var item = _inventoryItems[_selectedItemIndex];
        GD.Print($"Selected item: {item.ObjectType} ({item.SizeInTiles.X}x{item.SizeInTiles.Y} tiles)");
    }

    private void UpdateInventoryLabel()
    {
        if (_inventoryLabel == null || _inventoryItems.Count == 0)
        {
            return;
        }

        var item = _inventoryItems[_selectedItemIndex];
        _inventoryLabel.Text = $"Selected: {item.ObjectType} ({item.SizeInTiles.X}x{item.SizeInTiles.Y})";
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

        var sprite = new Sprite2D
        {
            Texture = texture,
            Centered = true
        };

        var targetSize = new Vector2(sizeInTiles.X * CellSize.X, sizeInTiles.Y * CellSize.Y);
        if (texture.GetSize().X > 0 && texture.GetSize().Y > 0)
        {
            sprite.Scale = new Vector2(
                targetSize.X / texture.GetSize().X,
                targetSize.Y / texture.GetSize().Y
            );
        }

        return sprite;
    }

    private string GetTexturePathForObjectType(string objectType)
    {
        foreach (var item in _inventoryItems)
        {
            if (item.ObjectType == objectType)
            {
                return item.TexturePath;
            }
        }

        return string.Empty;
    }
}
