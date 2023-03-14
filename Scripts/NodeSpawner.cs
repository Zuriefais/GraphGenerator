using Godot;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using ImGuiNET;

public partial class NodeSpawner: Node {
    [Export]
    public Camera2D camera;
    private string inputData;
    private PackedScene markerScene;
    private GraphData data;
    private PackedScene lineScene;
    private List<Vector2> graphOfPos = new();
    private Dictionary<List<int>, Vector2>dictPos = new();
    private Vector2 moveDirection = new();
    [Export]
    private float speed = 100;
    private bool firstClick = true;
    bool gameStarted;

    public override void _Ready()
    {
        markerScene = ResourceLoader.Load<PackedScene>("res://Scenes/marker.tscn");
        lineScene = ResourceLoader.Load<PackedScene>("res://Scenes/line_2d.tscn");
        using var file = FileAccess.Open("res://GraphData.json", FileAccess.ModeFlags.Read);
        inputData = file.GetAsText();
    }

    private void AddPointsFromData() {
        var rand = new Random();
        Vector2 rightUpCornerOfScreen = new Vector2(camera.GetScreenCenterPosition().X + camera.GetViewportRect().Size.X, camera.GetScreenCenterPosition().Y + camera.GetViewportRect().Size.Y);
        Vector2 leftBottomCornerOfScreen = new Vector2(camera.GetScreenCenterPosition().X - camera.GetViewportRect().Size.X, camera.GetScreenCenterPosition().Y - camera.GetViewportRect().Size.Y);
        foreach (var item in data.graphData)
        {
            dictPos.Add(item, new Vector2(rand.Next((int)leftBottomCornerOfScreen.X, (int)rightUpCornerOfScreen.X), rand.Next((int)leftBottomCornerOfScreen.Y, (int)rightUpCornerOfScreen.Y)));
            var newMarker = (Sprite2D)markerScene.Instantiate();
            newMarker.Position = dictPos[item];
            this.AddChild(newMarker);
            graphOfPos.Add(dictPos[item]);
        }
        GD.Print(data);
        CreateGraphFromData();
    }

    private async void CreateGraphFromData() 
    {
        foreach (var item in data.graphData)
        {
            foreach (var itemTwo in data.graphData)
            {
                if(item.Any(c => itemTwo.Contains(c))) 
                {
                    await Task.Delay(1000);
                    GD.Print("contains");
                    CreateLine(dictPos[item], dictPos[itemTwo]);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        var mousePos = GetViewport().GetCamera2D().GetGlobalMousePosition();
        if (Input.IsActionJustPressed("MouseClick") && gameStarted) {
            if (!firstClick)
            foreach (var item in graphOfPos)
            {
                CreateLine(mousePos, item);
            }
            else
            firstClick = false;
            var newMarker = (Sprite2D)markerScene.Instantiate();
            newMarker.Position = mousePos;
            this.AddChild(newMarker);
            graphOfPos.Add(mousePos);
        }
        Camera(delta);
    }


    private void CreateLine(Vector2 pointOne, Vector2 pointTwo) {
        var newLine = (Line2D)lineScene.Instantiate();
        AddChild(newLine);
        newLine.AddPoint(pointOne);
        newLine.AddPoint(pointTwo);
        GD.Print(graphOfPos.Count);
        firstClick = false;
    }

    private GraphData Load(string content)
    {
        GraphData data = JsonConvert.DeserializeObject<GraphData>(content);
        return data;
    }

    private void Camera(double delta) 
    {
        ImGui.Begin("Graph generator conf");
        ImGui.InputTextMultiline("Graph Data Json", ref inputData, 1000, new(600, 400));
        if (ImGui.Button("Generate")) {
            GD.Print("кнопка нажата");
            gameStarted = true;
            data = Load(inputData);
            AddPointsFromData();
        }
        
        if (Input.IsActionPressed("MouseDown"))
        {
            if(camera.Zoom != new Vector2(0.01f, 0.01f))
            camera.Zoom = camera.Zoom + new Vector2(-0.01f, -0.01f);
        }
        if (Input.IsActionPressed("MouseUp"))
        {
            camera.Zoom = camera.Zoom + new Vector2(0.01f, 0.01f);
        }
        moveDirection.X = 0;
        moveDirection.Y = 0;
        if (Input.IsActionPressed("MovementUp"))
        {
            moveDirection.Y -= 1;
            GD.Print("Up");
        }
        if (Input.IsActionPressed("MovementDown"))
        {
            moveDirection.Y += 1;
            GD.Print("Down");
        }
        if (Input.IsActionPressed("MovementLeft"))
        {
            moveDirection.X -= 1;
            GD.Print("Left");
        }
        if (Input.IsActionPressed("MovementRight"))
        {
            moveDirection.X += 1;
            GD.Print("Right");
        }
        moveDirection += moveDirection.Normalized() * speed * (float)delta;
        camera.Position = camera.Position + moveDirection;
    }
}

[Serializable]
public class GraphData 
{
    public List<List<int>> graphData;
}