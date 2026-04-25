using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ProfessorAI : CharacterBody2D
{
    [Export] public RichTextLabel textProfesor;
    [Export] public LineEdit      playerInput;
    [Export] public Control       chatPanel;

    private string apiKey = "AIzaSyD7iSdmNQd2hfhzwodFkL7R2MqrF1g8Qxo"; // mora pocinjati sa sk-

    private static System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
    private List<object> conversationHistory = new List<object>();

    public override void _Ready()
    {
        textProfesor = GetNode<RichTextLabel>("CanvasLayer/ChatPanel/Panel/TeksProfesora");
        playerInput  = GetNode<LineEdit>     ("CanvasLayer/ChatPanel/playerInput");
        chatPanel    = GetNode<Control>      ("CanvasLayer/ChatPanel");

        var closeButton = GetNode<Button>("CanvasLayer/ChatPanel/Panel/closeButton");
        var sendButton  = GetNode<Button>("CanvasLayer/ChatPanel/playerInput/SendButton");

        sendButton.Pressed        += OnSendPressed;
        closeButton.Pressed       += OnClosePressed;
        playerInput.TextSubmitted += (_) => OnSendPressed();

        textProfesor.BbcodeEnabled = true;
        textProfesor.Clear();
        //chatPanel.Visible = false;
    }

    public void OpenChat()
    {
        chatPanel.Visible = true;
        GetNode<Control>("CanvasLayer/ChatPanel/Panel").Visible = true;
        playerInput.GrabFocus();
    }

    private void OnClosePressed()
    {
        chatPanel.Visible = false;
        textProfesor.Clear();
        playerInput.Text = "";
        conversationHistory.Clear();
    }

    public async void OnSendPressed()
    {
        string pitanje = playerInput.Text.Trim();
        if (string.IsNullOrEmpty(pitanje)) return;

        playerInput.Text = "";
        textProfesor.AppendText($"\n[b]Ti:[/b] {pitanje}\n");
        textProfesor.AppendText("[i]Profesor razmišlja...[/i]\n");

        string odgovor = await AskAI(pitanje);

        // Ukloni "razmišlja" i dodaj odgovor
        string current  = textProfesor.Text;
        const string thinking = "Profesor razmišlja...\n";
        int idx = current.LastIndexOf(thinking, StringComparison.Ordinal);
        if (idx >= 0)
            textProfesor.Text = current.Remove(idx, thinking.Length);

        textProfesor.AppendText($"[b]Profesor:[/b] {odgovor}\n");
    }

    private async Task<string> AskAI(string pitanje)
{
    try
    {
        conversationHistory.Add(new { role = "user", parts = new[] { new { text = pitanje } } });

        var bodyObj = new
        {
            system_instruction = new
            {
                parts = new[] { new { text = "Ti si profesor u edukativnoj igri. Odgovaraj kratko i jasno na srpskom jeziku." } }
            },
            contents = conversationHistory
        };

        string bodyJson = JsonSerializer.Serialize(bodyObj);
        var httpContent = new System.Net.Http.StringContent(bodyJson, Encoding.UTF8, "application/json");

        var request = new System.Net.Http.HttpRequestMessage(
            System.Net.Http.HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}")
        {
            Content = httpContent
        };

        var response = await httpClient.SendAsync(request);
        var result   = await response.Content.ReadAsStringAsync();

        GD.Print("=== API RESPONSE ===");
        GD.Print(result);
        GD.Print("====================");

        var json = JsonDocument.Parse(result);

        if (json.RootElement.TryGetProperty("error", out var error))
        {
            conversationHistory.RemoveAt(conversationHistory.Count - 1);
            return "API greška: " + error.GetProperty("message").GetString();
        }

        string odgovor = json.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        conversationHistory.Add(new { role = "model", parts = new[] { new { text = odgovor } } });
        return odgovor;
    }
    catch (Exception e)
    {
        if (conversationHistory.Count > 0)
            conversationHistory.RemoveAt(conversationHistory.Count - 1);
        return "Greška: " + e.Message;
    }
}
}