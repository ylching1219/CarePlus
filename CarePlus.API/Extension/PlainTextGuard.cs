using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

namespace CarePlus.API.Extension
{
    // ponytail: one scan for HTML/script junk on every action argument; passwords/secrets skipped
    public static class PlainTextGuard
    {
        private static readonly Regex ControlChars = new(@"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]", RegexOptions.Compiled);

        public static bool ContainsDangerousMarkup(string? value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.IndexOfAny(['<', '>', '`']) >= 0) return true;
            if (value.Contains("javascript:", StringComparison.OrdinalIgnoreCase)) return true;
            return ControlChars.IsMatch(value);
        }

        public static string? FindDangerousValue(object? root)
        {
            if (root is null) return null;

            var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
            return Walk(root, seen);
        }

        private static string? Walk(object value, HashSet<object> seen)
        {
            if (value is string text)
            {
                return ContainsDangerousMarkup(text) ? text : null;
            }

            var type = value.GetType();
            if (type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || value is Enum)
            {
                return null;
            }

            if (!type.IsValueType && !seen.Add(value))
            {
                return null;
            }

            if (value is IDictionary dictionary)
            {
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is string key && ShouldSkip(key)) continue;
                    var hit = Walk(entry.Value!, seen);
                    if (hit != null) return hit;
                }

                return null;
            }

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                {
                    if (item is null) continue;
                    var hit = Walk(item, seen);
                    if (hit != null) return hit;
                }

                return null;
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;
                if (ShouldSkip(property.Name)) continue;

                object? child;
                try
                {
                    child = property.GetValue(value);
                }
                catch
                {
                    continue;
                }

                if (child is null) continue;
                var hit = Walk(child, seen);
                if (hit != null) return hit;
            }

            return null;
        }

        public static bool ShouldSkip(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var n = name.Trim().ToLowerInvariant();
            return n.Contains("password", StringComparison.Ordinal)
                || n.Contains("hash", StringComparison.Ordinal)
                || n.Contains("token", StringComparison.Ordinal)
                || n.Contains("secret", StringComparison.Ordinal)
                || n.Contains("apikey", StringComparison.Ordinal)
                || n is "cvv" or "number"; // card number / cvv
        }
    }
}
