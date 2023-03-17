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
    private PackedScene labelScene;
    private GraphData data;
    private PackedScene lineScene;
    private List<Vector2> graphOfPos = new();
    private Dictionary<List<int>, Vector2>dictPos = new();
    private Vector2 moveDirection = new();
    [Export]
    private float speed = 100;
    private bool firstClick = true;
    bool canCreateLineByClick;
    int iterationWaitTime = 200;
    bool menuIsOpen = true;

    public override void _Ready()
    {
        markerScene = ResourceLoader.Load<PackedScene>("res://Scenes/marker.tscn");
        lineScene = ResourceLoader.Load<PackedScene>("res://Scenes/line_2d.tscn");
        labelScene = ResourceLoader.Load<PackedScene>("res://Scenes/label.tscn");
        using var file = FileAccess.Open("res://GraphData.json", FileAccess.ModeFlags.Read);
        inputData = file.GetAsText();
    }

    private void AddPointsFromData() {
        int j = 0;
        var rand = new Random();
        Vector2 rightUpCornerOfScreen = new Vector2(camera.GetScreenCenterPosition().X + camera.GetViewportRect().Size.X/2, camera.GetScreenCenterPosition().Y + camera.GetViewportRect().Size.Y/2);
        Vector2 leftBottomCornerOfScreen = new Vector2(camera.GetScreenCenterPosition().X - camera.GetViewportRect().Size.X/2, camera.GetScreenCenterPosition().Y - camera.GetViewportRect().Size.Y/2);
        foreach (var item in data.graphData)
        {
            dictPos.Add(item, new Vector2(rand.Next((int)leftBottomCornerOfScreen.X, (int)rightUpCornerOfScreen.X), rand.Next((int)leftBottomCornerOfScreen.Y, (int)rightUpCornerOfScreen.Y)));
            var newMarker = (Sprite2D)markerScene.Instantiate();
            newMarker.Position = dictPos[item];
            this.AddChild(newMarker);
            graphOfPos.Add(dictPos[item]);
            var label = (Label)labelScene.Instantiate();
            label.Text = Alphabet.alphabet[j].ToString();
            newMarker.AddChild(label);
            j++;
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
                    await Task.Delay(iterationWaitTime);
                    GD.Print("contains");
                    CreateLine(dictPos[item], dictPos[itemTwo]);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if(menuIsOpen)
        Gui();
        var mousePos = GetViewport().GetCamera2D().GetGlobalMousePosition();
        if (Input.IsActionJustPressed("MouseClick") && canCreateLineByClick) {
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


    private void Gui() 
    {
        ImGui.Begin("Настройки генератора графа");
        ImGui.InputTextMultiline("Входные данные для генерации ", ref inputData, 1000, new(600, 400));
        ImGui.InputInt("Время ожидания перед следующей итерацией в миллисекундах", ref iterationWaitTime);
        ImGui.InputFloat("Скорость камеры", ref speed);
        if (ImGui.Button("Сгенерировать")) {
            GD.Print("кнопка нажата");
            data = Load(inputData);
            AddPointsFromData();
        }
        if(ImGui.Button("Перезапуск"))
        GetTree().ReloadCurrentScene();
        ImGui.Checkbox("Создавать точку при клике", ref canCreateLineByClick);
        ImGui.StyleColorsClassic();
    }

    private void Camera(double delta) 
    {   
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
        if (Input.IsActionJustPressed("X"))
        {
            canCreateLineByClick = !canCreateLineByClick;
        }
        if (Input.IsActionJustPressed("Tab"))
        {
            menuIsOpen = !menuIsOpen;
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

public static class Alphabet {
    public static char[] alphabet = {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'};
}

