using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ProfessorAI : CharacterBody2D
{
    public RichTextLabel textProfesor;
    public LineEdit playerInput;
    public Control chatPanel;

    private static System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
    private string apiKey = "###";

    private List<object> conversationHistory = new List<object>();

    public override void _Ready()
    {
        textProfesor = GetNode<RichTextLabel>("CanvasLayer/ChatPanel/textProfesor");
        playerInput  = GetNode<LineEdit>("CanvasLayer/ChatPanel/playerInput");
        chatPanel    = GetNode<Control>("CanvasLayer/ChatPanel");

        var sendButton  = GetNode<Button>("CanvasLayer/ChatPanel/SendButton");
        var closeButton = GetNode<Button>("CanvasLayer/ChatPanel/closeButton");

        sendButton.Pressed      += OnSendPressed;
        closeButton.Pressed     += OnClosePressed;
        playerInput.TextSubmitted += (_) => OnSendPressed();

        chatPanel.Visible = false;
    }

    public void OpenChat()
    {
        chatPanel.Visible = true;
        playerInput.GrabFocus();
    }

    private void OnClosePressed()
    {
        chatPanel.Visible = false;
    }

    public async void OnSendPressed()
    {
        string pitanje = playerInput.Text.Trim();
        if (string.IsNullOrEmpty(pitanje)) return;

        playerInput.Text = "";

        textProfesor.Text += $"\n[b]Ti:[/b] {pitanje}\n";
        textProfesor.Text += "[i]Profesor razmišlja...[/i]\n";

        string odgovor = await AskAI(pitanje);

        // Remove the "razmišlja" line and append the real answer
        int idx = textProfesor.Text.LastIndexOf("[i]Profesor razmišlja...[/i]\n");
        if (idx >= 0)
            textProfesor.Text = textProfesor.Text.Remove(idx, "[i]Profesor razmišlja...[/i]\n".Length);

        textProfesor.Text += $"[b]Profesor:[/b] {odgovor}\n";
    }

    async Task<string> AskAI(string pitanje)
    {
        try
        {
            conversationHistory.Add(new { role = "user", content = pitanje });

            var systemMessage = new { role = "system", content = "Ti si profesor u edukativnoj igri. Odgovaraj kratko i jasno na srpskom jeziku." };

            var allMessages = new List<object> { systemMessage };
            allMessages.AddRange(conversationHistory);

            var bodyObj = new
            {
                model = "gpt-4o-mini",
                messages = allMessages
            };

            string body = JsonSerializer.Serialize(bodyObj);
            var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
            {
                Content = content
            };
            request.Headers.Add("Authorization", "Bearer " + apiKey);

            var response = await httpClient.SendAsync(request);
            var result   = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(result);

            if (json.RootElement.TryGetProperty("error", out var error))
            {
                conversationHistory.RemoveAt(conversationHistory.Count - 1);
                return "API greška: " + error.GetProperty("message").GetString();
            }

            string odgovor = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            conversationHistory.Add(new { role = "assistant", content = odgovor });

            return odgovor;
        }
        catch (Exception e)
        {
            conversationHistory.RemoveAt(conversationHistory.Count - 1);
            return "Greška: " + e.Message;
        }
    }
}