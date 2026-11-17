namespace CarePlus.API.Extension
{
    public static class UploadFileNameHelper
    {
        public static string Generate(string module, string username, string extension)
        {
            var safeModule = new string((module ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');

            if (string.IsNullOrWhiteSpace(safeModule))
            {
                safeModule = "upload";
            }

            var safeUsername = new string((username ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');

            if (string.IsNullOrWhiteSpace(safeUsername))
            {
                safeUsername = "user";
            }

            var extensionBody = new string((extension ?? string.Empty)
                .Trim()
                .TrimStart('.')
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToLowerInvariant();
            var safeExtension = string.IsNullOrWhiteSpace(extensionBody)
                ? ".bin"
                : $".{extensionBody}";

            return $"{safeModule}-{safeUsername}{safeExtension}";
        }

        public static string GenerateForIdentifier(string identifier, string extension)
        {
            var safeIdentifier = new string((identifier ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray())
                .Trim('-');

            if (string.IsNullOrWhiteSpace(safeIdentifier))
            {
                safeIdentifier = "upload";
            }

            var extensionBody = new string((extension ?? string.Empty)
                .Trim()
                .TrimStart('.')
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToLowerInvariant();
            var safeExtension = string.IsNullOrWhiteSpace(extensionBody)
                ? ".bin"
                : $".{extensionBody}";

            return $"{safeIdentifier}{safeExtension}";
        }

        // ponytail: opaque names so leaked URLs are not guessable from username/id
        public static string GenerateOpaque(string extension)
        {
            var extensionBody = new string((extension ?? string.Empty)
                .Trim()
                .TrimStart('.')
                .Where(char.IsLetterOrDigit)
                .ToArray())
                .ToLowerInvariant();
            var safeExtension = string.IsNullOrWhiteSpace(extensionBody)
                ? ".bin"
                : $".{extensionBody}";

            return $"{Guid.NewGuid():N}{safeExtension}";
        }
    }
}
