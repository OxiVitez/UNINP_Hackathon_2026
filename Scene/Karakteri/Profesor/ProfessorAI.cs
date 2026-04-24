using Godot;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public partial class ProfessorAI : Node
{
    public Label textProfesor;
    public LineEdit playerInput;

    private System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

    private string apiKey = "UBACI_SVOJ_API_KEY";

    public override void _Ready()
    {
        textProfesor = GetNode<Label>("Panel/textProfesor");
        playerInput = GetNode<LineEdit>("Panel/playerInput");
    }

    public async void OnSendPressed()
    {
        string pitanje = playerInput.Text;
        textProfesor.Text = "Razmišljam...";
        string odgovor = await AskAI(pitanje);
        textProfesor.Text = odgovor;
    }

    async Task<string> AskAI(string pitanje)
    {
        var url = "https://api.openai.com/v1/chat/completions";

        var body = @"
        {
            ""model"": ""gpt-4o-mini"",
            ""messages"": [
                {""role"": ""system"", ""content"": ""Ti si profesor u edukativnoj igri. Odgovaraj kratko i jasno."" },
                {""role"": ""user"", ""content"": """ + pitanje + @"""}
            ]
        }";

        var content = new System.Net.Http.StringContent(body, Encoding.UTF8, "application/json");

        var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("Authorization", "Bearer " + apiKey);

        var response = await httpClient.SendAsync(request);
        var result = await response.Content.ReadAsStringAsync();

        var json = JsonDocument.Parse(result);

        string odgovor = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return odgovor;
    }
}