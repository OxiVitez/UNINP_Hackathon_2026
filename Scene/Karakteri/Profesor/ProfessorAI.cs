using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ProfessorAI : CharacterBody2D  // bio Node, a scena je CharacterBody2D
{
    public RichTextLabel textProfesor;
    public LineEdit playerInput;
    public Control chatPanel;

    private System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
    private string apiKey = "AIzaSyD7iSdmNQd2hfhzwodFkL7R2MqrF1g8Qxo";

    public override void _Ready()
    {
        textProfesor = GetNode<RichTextLabel>("CanvasLayer/ChatPanel/textProfesor");
        playerInput  = GetNode<LineEdit>("CanvasLayer/ChatPanel/playerInput");
        chatPanel    = GetNode<Control>("CanvasLayer/ChatPanel");

        var sendButton  = GetNode<Button>("CanvasLayer/ChatPanel/SendButton");
        var closeButton = GetNode<Button>("CanvasLayer/ChatPanel/closeButton");

        sendButton.Pressed  += OnSendPressed;
        closeButton.Pressed += OnClosePressed;

        chatPanel.Visible = false; // zatvoreno na startu
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
        textProfesor.Text = "Razmišljam...";

        string odgovor = await AskAI(pitanje);

        textProfesor.Text = odgovor;
    }

    async Task<string> AskAI(string pitanje)
    {
        try
        {
            var url = "https://api.openai.com/v1/chat/completions";

            // Koristimo JsonSerializer umjesto ručnog stringa - sigurnije za specijalne karaktere
            var bodyObj = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "Ti si profesor u edukativnoj igri. Odgovaraj kratko i jasno na srpskom jeziku." },
                    new { role = "user",   content = pitanje }
                }
            };

            string body = JsonSerializer.Serialize(bodyObj);
            var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", "Bearer " + apiKey);

            var response = await httpClient.SendAsync(request);
            var result   = await response.Content.ReadAsStringAsync();

            var json = JsonDocument.Parse(result);

            // Provjeri da li API vratio grešku
            if (json.RootElement.TryGetProperty("error", out var error))
            {
                return "API greška: " + error.GetProperty("message").GetString();
            }

            return json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
        }
        catch (Exception e)
        {
            return "Greška: " + e.Message;
        }
    }
}