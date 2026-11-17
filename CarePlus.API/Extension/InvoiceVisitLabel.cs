namespace CarePlus.API.Extension;

public static class InvoiceVisitLabel
{
    public const string PrescriptionMedicines = "Prescription medicines";

    public static bool IsPrescription(string? value) => Read(value).Visit == PrescriptionMedicines;

    static InvoiceVisitLabel()
    {
        var (visit, name) = Read(Pack("General consultation", "Aina Tan"));
        var (plain, none) = Read("Body check");
        if (visit != "General consultation" || name != "Aina Tan" || plain != "Body check" || none != null)
        {
            throw new InvalidOperationException("Invoice family label check failed.");
        }
    }

    public static string Pack(string visitName, string? billedFor)
    {
        var visit = string.IsNullOrWhiteSpace(visitName) ? "General consultation" : visitName.Trim();
        if (visit.Length > 80) visit = visit[..80];
        if (string.IsNullOrWhiteSpace(billedFor)) return visit;
        var room = 80 - visit.Length - 1;
        if (room < 1) return visit;
        var name = billedFor.Trim();
        if (name.Length > room) name = name[..room];
        return $"{visit}\u001f{name}";
    }

    public static (string Visit, string? BilledFor) Read(string? stored)
    {
        var value = stored ?? "";
        var cut = value.IndexOf('\u001f');
        if (cut < 0) return (value, null);
        var name = value[(cut + 1)..].Trim();
        return (value[..cut], name.Length == 0 ? null : name);
    }
}
