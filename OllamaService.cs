using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FinanceGPT
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _model;

        public OllamaService(string baseUrl = "http://localhost:11434", string model = "llama2")
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
            _baseUrl = baseUrl;
            _model = model;
        }

        /// <summary>
        /// Check if Ollama is running
        /// </summary>
        public async Task<bool> IsOllamaRunningAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate a response from Ollama
        /// </summary>
        public async Task<string> GenerateResponseAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = prompt,
                    stream = false
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseContent);
                    return jsonResponse["response"]?.ToString() ?? "No response received.";
                }
                else
                {
                    return $"Error: {response.StatusCode} - {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                return $"Error communicating with Ollama: {ex.Message}";
            }
        }

        /// <summary>
        /// Generate a response with streaming support
        /// </summary>
        public async Task GenerateResponseStreamAsync(string prompt, Action<string> onChunk)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    prompt = prompt,
                    stream = true
                };

                var json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);

                if (response.IsSuccessStatusCode)
                {
                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var reader = new System.IO.StreamReader(stream))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                var jsonResponse = JObject.Parse(line);
                                var chunk = jsonResponse["response"]?.ToString();
                                if (!string.IsNullOrEmpty(chunk))
                                {
                                    onChunk(chunk);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                onChunk($"\n\nError: {ex.Message}");
            }
        }
    }
}