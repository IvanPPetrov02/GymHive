using MembershipService.BLL.DTOs;
using MembershipService.BLL.RepositoryInterfaces;
using System.Text.Json;

namespace MembershipService.Services;

public class GymServiceClient : IGymServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GymServiceClient> _logger;

    public GymServiceClient(HttpClient httpClient, ILogger<GymServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GymDTO?> GetGymByIdAsync(int gymId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/Gyms/{gymId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning($"Failed to fetch gym {gymId} from GymService. Status: {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var gym = JsonSerializer.Deserialize<GymDTO>(content, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
            
            return gym;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error fetching gym {gymId} from GymService");
            return null;
        }
    }
}
