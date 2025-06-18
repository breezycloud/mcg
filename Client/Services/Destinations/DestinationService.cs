using Shared.Dtos;
using Shared.Helpers;
using Shared.Models.Trips;
using Shared.Interfaces.Destinations;
using System.Net.Http.Json;

namespace Client.Services.Destinations;


    public class DestinationService(IHttpClientFactory _httpClient) : IDestinationService
    {
        public async Task<bool> AddAsync(Destination model, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync("Destinations", model, cancellationToken);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (System.Exception)
            {

                throw;
            }
        }
        public async Task<bool> UpdateAsync(Destination model, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.CreateClient("AppUrl").PutAsJsonAsync($"Destinations/{model.Id}", model, cancellationToken);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        
        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.CreateClient("AppUrl").DeleteAsync($"Destinations/{id}", cancellationToken);
                response.EnsureSuccessStatusCode();
                return response.IsSuccessStatusCode;
            }
            catch (System.Exception)
            {

                throw;
            }
        }
        public async Task<Destination?> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.CreateClient("AppUrl").GetAsync($"Destinations/{id}", cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Destination?>();
            }
            catch (System.Exception)
            {

                throw;
            }
        }

        
        public async Task<GridDataResponse<Destination>?> GetPagedAsync(GridDataRequest request, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _httpClient.CreateClient("AppUrl").PostAsJsonAsync($"Destinations/paged", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<GridDataResponse<Destination>?>();
            }
            catch (System.Exception)
            {

                throw;
            }
        }
    }