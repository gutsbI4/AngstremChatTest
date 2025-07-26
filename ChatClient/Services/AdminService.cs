using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ChatClient.Models;

namespace ChatClient.Services
{
    public class AdminService : IDisposable
    {
        private readonly HttpClient _httpClient;

        public AdminService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:3001/")
            };
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/admin/users");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<User>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<User>();
                }
            }
            catch
            {
                // Можно добавить логирование
            }
            return new List<User>();
        }

        public async Task<UserStatistics> GetStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/admin/statistics");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserStatistics>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new UserStatistics();
                }
            }
            catch
            {
                // Можно добавить логирование
            }
            return new UserStatistics();
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/admin/users/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Message>> GetUserConversationAsync(int userId, int partnerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/admin/users/{userId}/conversation/{partnerId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Message>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? new List<Message>();
                }
            }
            catch
            {
                // Можно добавить логирование
            }
            return new List<Message>();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
