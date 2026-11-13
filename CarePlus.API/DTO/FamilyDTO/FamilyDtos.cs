namespace CarePlus.API.DTO.FamilyDTO;

public class FamilyCreateRequest
{
    public string Name { get; set; } = "";
}

public class FamilyRenameRequest
{
    public string Name { get; set; } = "";
}

public class FamilyMemberDisplayNameRequest
{
    public string Name { get; set; } = "";
}

public class FamilyInviteRequest
{
    public string Email { get; set; } = "";
    public string Relation { get; set; } = "child";
}

public class FamilyLeaveRequest
{
    public long? TransferToMemberId { get; set; }
}

public class FamilyAccessUpdateRequest
{
    public string Relation { get; set; } = "child";
}

public class FamilyMemberResponse
{
    public long FamilyMemberId { get; set; }
    public string PatientId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Relation { get; set; } = "child";
    public string Permission { get; set; } = "child";
    public string Status { get; set; } = "pending";
    public bool IsOwner { get; set; }
}

public class FamilyResponse
{
    public long FamilyGroupId { get; set; }
    public string Name { get; set; } = "";
    public string OwnerPatientId { get; set; } = "";
    public List<FamilyMemberResponse> Members { get; set; } = [];
}

public class FamilyInviteResponse
{
    public long FamilyMemberId { get; set; }
    public long FamilyGroupId { get; set; }
    public string FamilyName { get; set; } = "";
    public string InvitedByName { get; set; } = "";
    public string Relation { get; set; } = "child";
    public string Permission { get; set; } = "child";
}

public class FamilyOverviewResponse
{
    public string CurrentPatientId { get; set; } = "";
    public FamilyResponse? Family { get; set; }
    public List<FamilyInviteResponse> Invites { get; set; } = [];
}

