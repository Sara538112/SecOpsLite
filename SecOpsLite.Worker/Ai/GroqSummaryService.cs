using System.Text;
using System.Text.Json;
using SecOpsLite.Worker.Analysis;

namespace SecOpsLite.Worker.Ai;

public class GroqSummaryService : IGroqSummaryService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public GroqSummaryService(HttpClient httpClient , IConfiguration configuration){
        _httpClient = httpClient;
        _configuration = configuration;

        var baseUrl = _configuration["Groq:BaseUrl"];
        var apiKey = _configuration["Groq:ApiKey"];

        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization" , $"Bearer {apiKey}");

    }

    public async Task<string> GenerateSummaryAsync(AnomalySummaryStats stats){   
        var model = _configuration["Groq:Model"];
        var topOffendersText = stats.TopOffenderIps.Any()
            ? string.Join(", ", stats.TopOffenderIps)
            : "yok";

        var prompt = $"""
            Aşağıdaki ağ güvenliği verilerine bakarak, Türkçe, kısa (en fazla 3 cümle)
            bir güvenlik özeti yaz. Bir güvenlik analistine rapor verir gibi yaz.

            Toplam tespit edilen anomali: {stats.TotalAnomalies}
            Brute-force denemesi sayısı: {stats.BruteForceCount}
            Anormal veri transferi sayısı: {stats.LargeTransferCount}
            En çok olay yaratan IP'ler: {topOffendersText}
            """;

        var requestBody = new{
            model ,
            messages= new[]{
                new { role= "user" , content = prompt}
            },
            temperature=0.7
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json , Encoding.UTF8 , "application/json");

        var response = await _httpClient.PostAsync("chat/completions" , content);

        if(!response.IsSuccessStatusCode){
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Groq API hatası ({response.StatusCode}): {errorBody}"); 
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseBody);
        var message = doc.RootElement
        .GetProperty("choices")[0]
        .GetProperty("message")
        .GetProperty("content")
        .GetString();
    
        return message ?? "ozet uretilmedi";
    }
}