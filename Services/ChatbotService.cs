using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PawVerseAPI.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotService> _logger;
        private readonly string _cozeApiKey;
        private readonly string _cozeBotId;
        private readonly string _cozeApiEndpoint;
        
        private const int RESPONSE_TIMEOUT_MS = 30000;

        public ChatbotService(HttpClient httpClient, IConfiguration configuration, ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _cozeApiKey = _configuration["Coze:ApiKey"];
            _cozeBotId = _configuration["Coze:BotId"];
            _cozeApiEndpoint = _configuration["Coze:ApiEndpoint"];
            
            _logger.LogInformation($"Coze API Configuration: BotId={_cozeBotId}, Endpoint={_cozeApiEndpoint}");
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _cozeApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> SendMessageAsync(string userMessage)
        {
            try
            {
                var requestData = new CozeSendMessageRequest
                {
                    BotId = _cozeBotId,
                    UserId = "website_user_" + Guid.NewGuid().ToString(),
                    AdditionalMessages = new List<CozeMessage>
                    {
                        new CozeMessage
                        {
                            Role = "user",
                            Content = userMessage,
                            ContentType = "text"
                        }
                    },
                    Stream = true,
                    AutoSaveHistory = false
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    }),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation($"Sending streaming request to COZE API: {_cozeApiEndpoint}");
                
                var request = new HttpRequestMessage(HttpMethod.Post, _cozeApiEndpoint)
                {
                    Content = content
                };
                
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"COZE API Error: {response.StatusCode}, {errorContent}");
                    return $"Lỗi khi gọi API: {response.StatusCode}";
                }
                
                _logger.LogInformation("Starting to read stream response from COZE API");
                
                var rawResponseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Raw COZE response: {rawResponseContent.Substring(0, Math.Min(200, rawResponseContent.Length))}...");
                
                var newContent = new StringContent(rawResponseContent, Encoding.UTF8, "text/event-stream");
                using (var stream = await newContent.ReadAsStreamAsync())
                using (var reader = new StreamReader(stream))
                {
                    var assistantContent = new StringBuilder();
                    var isCompleted = false;
                    var startTime = DateTime.Now;
                    
                    while (!reader.EndOfStream && !isCompleted && (DateTime.Now - startTime).TotalMilliseconds < RESPONSE_TIMEOUT_MS)
                    {
                        string line = await reader.ReadLineAsync();
                        
                        if (string.IsNullOrEmpty(line))
                            continue;
                        
                        _logger.LogInformation($"SSE Stream line: {line}");
                        
                        if (line.StartsWith("event:"))
                        {
                            string eventType = line.Substring("event:".Length).Trim();
                            _logger.LogInformation($"SSE Event Type: {eventType}");
                            
                            if (eventType == "conversation.chat.completed")
                            {
                                isCompleted = true;
                                _logger.LogInformation("Detected completion event");
                            }
                        }
                        else if (line.StartsWith("data:"))
                        {
                            string data = line.Substring("data:".Length).Trim();
                            
                            try
                            {
                                using (JsonDocument doc = JsonDocument.Parse(data))
                                {
                                    // Parse response với type="answer" và role="assistant"
                                    if (doc.RootElement.TryGetProperty("type", out var typeProperty) &&
                                        doc.RootElement.TryGetProperty("role", out var roleProperty) &&
                                        typeProperty.GetString() == "answer" &&
                                        roleProperty.GetString() == "assistant" &&
                                        doc.RootElement.TryGetProperty("content", out var contentProperty))
                                    {
                                        string chunkContent = contentProperty.GetString();
                                        if (!string.IsNullOrEmpty(chunkContent))
                                        {
                                            assistantContent.Append(chunkContent);
                                            _logger.LogInformation($"Accumulated content: {assistantContent}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Exception parsing SSE data: {ex.Message}");
                            }
                        }
                    }
                    
                    var finalResponse = assistantContent.ToString();
                    
                    if (!string.IsNullOrEmpty(finalResponse))
                    {
                        // Apply duplicate removal before returning
                        var cleanedResponse = RemoveDuplicatedContent(finalResponse);
                        _logger.LogInformation($"Returning cleaned assistant content: {cleanedResponse}");
                        return cleanedResponse;
                    }
                    else
                    {
                        _logger.LogWarning("No assistant content found in stream");
                        return "Không nhận được phản hồi từ chatbot.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception when calling COZE API: {ex}");
                return $"Đã xảy ra lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Loại bỏ nội dung lặp lại trong phản hồi của chatbot
        /// </summary>
        private string RemoveDuplicatedContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return content;
                
            _logger.LogInformation($"Removing duplicated content from string of length: {content.Length}");
            
            // Xử lý trường hợp đặc biệt khi có hai câu trả lời trùng nhau
            int potentialDuplicateIndex = FindPotentialDuplicateStart(content);
            if (potentialDuplicateIndex > 0)
            {
                string firstPart = content.Substring(0, potentialDuplicateIndex);
                _logger.LogInformation($"Found potential duplicate at position {potentialDuplicateIndex}, returning first part");
                return firstPart;
            }
            
            // Xử lý trường hợp cụ thể khi nội dung bị lặp lại hoàn toàn
            if (content.Length % 2 == 0)
            {
                int halfLength = content.Length / 2;
                string firstHalf = content.Substring(0, halfLength);
                string secondHalf = content.Substring(halfLength);
                
                if (firstHalf == secondHalf)
                {
                    _logger.LogInformation("Detected exact duplicate content, returning first half");
                    return firstHalf;
                }
            }
            
            // Tìm và loại bỏ những câu hoàn chỉnh bị lặp lại
            var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 5)
                .ToList();
                
            if (sentences.Count <= 1)
                return content;
                
            List<string> uniqueSentences = new List<string>();
            for (int i = 0; i < sentences.Count; i++)
            {
                string current = sentences[i];
                bool isDuplicate = false;
                
                foreach (var unique in uniqueSentences.ToList())
                {
                    if (unique.Contains(current) || current.Contains(unique))
                    {
                        isDuplicate = true;
                        break;
                    }
                    
                    double similarity = CalculateSimilarity(NormalizeText(unique), NormalizeText(current));
                    
                    if (similarity > 0.7) // Ngưỡng 70%
                    {
                        _logger.LogInformation($"Found similar sentences with similarity {similarity}. Treating as duplicate.");
                        isDuplicate = true;
                        
                        if (current.Length > unique.Length)
                        {
                            uniqueSentences.Remove(unique);
                            uniqueSentences.Add(current);
                            _logger.LogInformation($"Replaced shorter sentence with longer one");
                        }
                        
                        break;
                    }
                }
                
                if (!isDuplicate)
                {
                    uniqueSentences.Add(current);
                }
            }
            
            var result = string.Join(". ", uniqueSentences) + ".";
            _logger.LogInformation($"Removed duplicates: Original had {sentences.Count} sentences, result has {uniqueSentences.Count}");
            
            return result;
        }
        
        /// <summary>
        /// Chuẩn hóa văn bản để so sánh
        /// </summary>
        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;
                
            return new string(text.ToLowerInvariant()
                .Where(c => !char.IsWhiteSpace(c) && char.IsLetterOrDigit(c))
                .ToArray());
        }
        
        /// <summary>
        /// Tính độ tương đồng giữa hai chuỗi (Jaccard similarity)
        /// </summary>
        private double CalculateSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0;
                
            var set1 = new HashSet<char>(s1);
            var set2 = new HashSet<char>(s2);
            
            var union = new HashSet<char>(set1);
            union.UnionWith(set2);
            
            var intersection = new HashSet<char>(set1);
            intersection.IntersectWith(set2);
            
            if (union.Count == 0)
                return 0;
                
            return (double)intersection.Count / union.Count;
        }
        
        /// <summary>
        /// Tìm vị trí tiềm năng bắt đầu của phần trùng lặp trong chuỗi
        /// </summary>
        private int FindPotentialDuplicateStart(string content)
        {
            if (string.IsNullOrEmpty(content) || content.Length < 20)
                return -1;
                
            string[] keywords = new[] { "Xin chào", "Bạn có cần", "Tôi rất", "hỗ trợ bạn", "về sản phẩm" };
            
            foreach (var keyword in keywords)
            {
                int firstIndex = content.IndexOf(keyword);
                if (firstIndex >= 0)
                {
                    int secondIndex = content.IndexOf(keyword, firstIndex + keyword.Length);
                    if (secondIndex > 0)
                    {
                        int position = Math.Max(0, secondIndex - 5);
                        int sentenceStart = content.LastIndexOf('.', position);
                        if (sentenceStart >= 0)
                        {
                            return sentenceStart + 1;
                        }
                        return position;
                    }
                }
            }
            
            return -1;
        }
    }

    // DTOs for COZE API
    public class CozeSendMessageRequest
    {
        [JsonPropertyName("bot_id")]
        public string BotId { get; set; }
        
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }
        
        [JsonPropertyName("additional_messages")]
        public List<CozeMessage> AdditionalMessages { get; set; }
        
        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
        
        [JsonPropertyName("auto_save_history")]
        public bool AutoSaveHistory { get; set; }
    }

    public class CozeMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        [JsonPropertyName("content_type")]
        public string ContentType { get; set; } = "text";
    }
}
