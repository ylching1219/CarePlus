namespace CarePlus.API.Extension;

public static class PatientAge
{
    public const int ChildPurchaseAge = 12;

    static PatientAge()
    {
        var today = new DateTime(2026, 11, 17);
        if (Years(new DateTime(2015, 11, 17), today) != 11) throw new InvalidOperationException("Age check failed.");
        if (Years(new DateTime(2014, 11, 18), today) != 11) throw new InvalidOperationException("Age check failed.");
        if (!UnderTwelve(new DateTime(2015, 1, 1), today) || UnderTwelve(new DateTime(2010, 1, 1), today))
        {
            throw new InvalidOperationException("Age check failed.");
        }
    }

    public static int? Years(DateTime? dateOfBirth, DateTime today)
    {
        if (!dateOfBirth.HasValue || dateOfBirth.Value.Date > today) return null;
        var age = today.Year - dateOfBirth.Value.Year;
        if (dateOfBirth.Value.Date > today.AddYears(-age)) age--;
        return age;
    }

    public static bool UnderTwelve(DateTime? dateOfBirth, DateTime today)
    {
        var age = Years(dateOfBirth, today);
        return age != null && age < ChildPurchaseAge;
    }
}
