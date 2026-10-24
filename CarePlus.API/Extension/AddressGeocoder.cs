using System.Globalization;
using System.Net.Http.Json;

namespace CarePlus.API.Extension
{
    public class AddressGeocoder
    {
        private readonly IHttpClientFactory _http;

        public AddressGeocoder(IHttpClientFactory http)
        {
            _http = http;
        }

        public async Task<(double Latitude, double Longitude)?> LocateAsync(string? address)
        {
            var query = MapQuery(address);
            if (query.Length == 0)
            {
                return null;
            }

            try
            {
                var client = _http.CreateClient("nominatim");
                var hits = await client.GetFromJsonAsync<List<NominatimHit>>(
                    "search?format=jsonv2&limit=1&countrycodes=my&q=" + Uri.EscapeDataString(query));
                var hit = hits?.FirstOrDefault();
                if (hit == null
                    || !double.TryParse(hit.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                    || !double.TryParse(hit.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
                {
                    return null;
                }

                return (lat, lng);
            }
            catch
            {
                return null;
            }
        }

        public static string StreetLine(string? unit, string? address)
        {
            var house = unit?.Trim() ?? "";
            var street = address?.Trim() ?? "";
            if (house.Length > 0 && street.Length > 0)
            {
                return $"{house}, {street}";
            }

            return street.Length > 0 ? street : house;
        }

        public static string MapQuery(string? address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return "";
            }

            var parts = address.Split('·', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 1 ? string.Join(", ", parts.Skip(1)) : parts[0];
        }

        private sealed class NominatimHit
        {
            public string Lat { get; set; } = "";
            public string Lon { get; set; } = "";
        }
    }
}
