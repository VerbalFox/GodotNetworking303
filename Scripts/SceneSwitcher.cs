using Godot;
using System;

public class SceneSwitcher : Node
{
    private PackedScene menu;
    private PackedScene game;
    private Node currentLevel;
    private Node previousLevel;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        menu = GD.Load("res://Scenes/Menu.tscn") as PackedScene;
        game = GD.Load("res://Scenes/Game.tscn") as PackedScene;

        currentLevel = menu.Instance();
        AddChild(currentLevel);
    }

    //Unload scene then load new instance of scene.
    public void LoadMenu() {
        GetChild(0).QueueFree();

        currentLevel = menu.Instance();
        AddChild(currentLevel);
    }
    public void LoadGame() {
        GetChild(0).QueueFree();
        
        currentLevel = game.Instance();
        AddChild(currentLevel);
    }
}
