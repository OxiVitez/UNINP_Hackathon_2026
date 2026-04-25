using Godot;
using System;
using System.Collections.Generic;

public partial class InventoryPanel : CanvasLayer
{
    private sealed class InventoryItem
    {
        public string ObjectType { get; set; } = string.Empty;
        public string TexturePath { get; set; } = string.Empty;
        public Vector2I SizeInTiles { get; set; } = Vector2I.One;
    }

    public event Action<string, Vector2I, string> DragRequested;

    private readonly Dictionary<string, List<InventoryItem>> _playerItems = new();
    private readonly Dictionary<string, string> _textureByType = new();
    private readonly List<InventoryItem> _activeItems = new();

    private PanelContainer _panel;
    private Label _titleLabel;
    private HBoxContainer _slotsContainer;
    private string _activePlayerId = "player_1";

    public override void _Ready()
    {
        _panel = GetNodeOrNull<PanelContainer>("Panel");
        _titleLabel = GetNodeOrNull<Label>("Panel/VBox/Title");
        _slotsContainer = GetNodeOrNull<HBoxContainer>("Panel/VBox/SlotsContainer/Slots");

        BuildPlayerInventories();
        SetPlayer(_activePlayerId);
        SetVisible(false);
    }

    public void SetPlayer(string playerId)
    {
        _activePlayerId = string.IsNullOrWhiteSpace(playerId) ? "player_1" : playerId;
        RebuildSlotsForPlayer(_activePlayerId);
    }

    public void ToggleVisibility()
    {
        Visible = !Visible;
    }

    public void SetVisible(bool visible)
    {
        Visible = visible;
    }

    public bool TryGetTexturePath(string objectType, out string texturePath)
    {
        if (_textureByType.TryGetValue(objectType, out var mappedPath))
        {
            texturePath = mappedPath;
            return true;
        }

        texturePath = string.Empty;
        return false;
    }

    private void BuildPlayerInventories()
    {
        _playerItems.Clear();
        _textureByType.Clear();

        var firstTenTextures = new string[]
        {
            "res://Asseti/trofeji/Gemini_Generated_Image_omay7zomay7zomay.png",
            "res://Asseti/trofeji/cash.png",
            "res://Asseti/trofeji/program.png",
            "res://Asseti/trofeji/ukflagg.png"
        };

        List<InventoryItem> BuildItemsForPlayer(string playerPrefix)
        {
            var items = new List<InventoryItem>();
            for (var i = 0; i < firstTenTextures.Length; i++)
            {
                items.Add(new InventoryItem
                {
                    ObjectType = $"{playerPrefix}_Item_{i + 1}",
                    TexturePath = firstTenTextures[i],
                    SizeInTiles = new Vector2I(4, 4) // 64x64 px on 16x16 grid
                });
            }

            return items;
        }

        _playerItems["player_1"] = BuildItemsForPlayer("P1");
        _playerItems["player_2"] = BuildItemsForPlayer("P2");
    }

    private void RebuildSlotsForPlayer(string playerId)
    {
        if (_slotsContainer == null)
        {
            return;
        }

        foreach (var child in _slotsContainer.GetChildren())
        {
            if (child is Node node)
            {
                node.QueueFree();
            }
        }

        _activeItems.Clear();
        if (_playerItems.TryGetValue(playerId, out var itemsForPlayer))
        {
            _activeItems.AddRange(itemsForPlayer);
        }
        else if (_playerItems.TryGetValue("player_1", out var fallback))
        {
            _activeItems.AddRange(fallback);
        }

        if (_titleLabel != null)
        {
            _titleLabel.Text = $"Inventory - {_activePlayerId}";
        }

        for (var i = 0; i < _activeItems.Count; i++)
        {
            var item = _activeItems[i];
            _textureByType[item.ObjectType] = item.TexturePath;
            AddSlot(item);
        }
    }

    private void AddSlot(InventoryItem item)
    {
        var slotPanel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(88, 88)
        };

        var textureRect = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(80, 80),
            MouseFilter = Control.MouseFilterEnum.Stop,
            TooltipText = $"{item.ObjectType} ({item.SizeInTiles.X}x{item.SizeInTiles.Y})"
        };
        textureRect.Texture = GD.Load<Texture2D>(item.TexturePath);
        textureRect.GuiInput += (InputEvent inputEvent) =>
        {
            if (inputEvent is InputEventMouseButton mouse &&
                mouse.ButtonIndex == MouseButton.Left &&
                mouse.Pressed)
            {
                DragRequested?.Invoke(item.ObjectType, item.SizeInTiles, item.TexturePath);
            }
        };

        slotPanel.AddChild(textureRect);
        _slotsContainer.AddChild(slotPanel);
    }
}
