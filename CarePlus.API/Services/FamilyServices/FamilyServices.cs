using CarePlus.API.Data;
using CarePlus.API.DTO;
using CarePlus.API.DTO.FamilyDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.FamilyGroup;
using CarePlus.API.Models.FamilyMember;
using CarePlus.API.Models.Notification;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Services.FamilyServices;

public class FamilyServices : Interface.IFamilyServices
{
    private static readonly HashSet<string> AllowedRelations = new(StringComparer.OrdinalIgnoreCase) { "parent", "child" };
    private readonly AppDbContext _db;

    public FamilyServices(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> GetOverviewAsync(string patientId)
    {
        var overview = await BuildOverviewAsync(patientId);
        return ApiResponse<FamilyOverviewResponse>.Ok(overview);
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> CreateAsync(string patientId, FamilyCreateRequest request)
    {
        var name = request.Name.Trim();
        if (name.Length < 2 || name.Length > 100)
        {
            return Fail("Family name must be between 2 and 100 characters.", 400);
        }

        if (await HasAnyFamilyMembershipAsync(patientId))
        {
            return Fail("You already belong to a family.", 409);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var group = new FamilyGroup
        {
            GroupName = name,
            CreatedBy = patientId,
            MemberCount = 1,
            CreatedAt = DateTimeHelper.MalaysiaNow()
        };
        _db.FamilyGroups.Add(group);
        await _db.SaveChangesAsync();

        _db.FamilyMembers.Add(new FamilyMember
        {
            FamilyGroupId = group.FamilyGroupId,
            PatientId = patientId,
            Relationship = "parent",
            AccessLevel = "Parent",
            Status = "Active",
            JoinAt = DateTimeHelper.MalaysiaNow()
        });
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await SuccessOverviewAsync(patientId, "Family created.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> RenameAsync(string patientId, FamilyRenameRequest request)
    {
        var group = await OwnerGroupAsync(patientId);
        if (group == null) return Fail("Only the family owner can rename the family.", 403);

        var name = request.Name.Trim();
        if (name.Length < 2 || name.Length > 100) return Fail("Family name must be between 2 and 100 characters.", 400);
        group.GroupName = name;
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "Family renamed.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> InviteAsync(string patientId, FamilyInviteRequest request)
    {
        var group = await OwnerGroupAsync(patientId);
        if (group == null) return Fail("Only the family owner can invite members.", 403);

        var relation = NormalizeRelation(request.Relation);
        if (relation == null) return Fail("Choose either parent or child.", 400);

        var email = request.Email.Trim().ToLowerInvariant();
        if (email.Length > 254 || !email.Contains('@')) return Fail("Enter a valid email address.", 400);

        var targetPatient = await _db.Patients.Include(patient => patient.User)
            .FirstOrDefaultAsync(patient => patient.User.Email.ToLower() == email && patient.User.Status == "Active");
        if (targetPatient == null) return Fail("No active patient account was found for this email.", 404);
        if (targetPatient.PatientId == patientId) return Fail("You cannot invite yourself.", 400);

        if (await HasAnyFamilyMembershipAsync(targetPatient.PatientId)) return Fail("This patient already belongs to a family.", 409);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        var invitation = new FamilyMember
        {
            FamilyGroupId = group.FamilyGroupId,
            PatientId = targetPatient.PatientId,
            Relationship = relation,
            AccessLevel = ToAccessLevel(relation),
            Status = "Pending",
            JoinAt = DateTimeHelper.MalaysiaNow()
        };
        _db.FamilyMembers.Add(invitation);
        await _db.SaveChangesAsync();

        var ownerName = await _db.Patients
            .Where(row => row.PatientId == patientId)
            .Select(row => row.User.Name)
            .FirstAsync();
        await UpsertNotificationAsync(
            targetPatient.UserId,
            $"family-invite:{invitation.FamilyMemberId}",
            "Family invitation",
            $"{ownerName} invited you to join {group.GroupName}.",
            "家庭邀请",
            $"{ownerName}邀请您加入{group.GroupName}。",
            ownerName,
            "family_invitation",
            "/family",
            invitation.JoinAt);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await SuccessOverviewAsync(patientId, "Invitation sent.");
    }

    public Task<ApiResponse<FamilyOverviewResponse>> AcceptInviteAsync(string patientId, long familyMemberId) =>
        DecideInviteAsync(patientId, familyMemberId, true);

    public Task<ApiResponse<FamilyOverviewResponse>> DeclineInviteAsync(string patientId, long familyMemberId) =>
        DecideInviteAsync(patientId, familyMemberId, false);

    public async Task<ApiResponse<FamilyOverviewResponse>> CancelInviteAsync(string patientId, long familyMemberId)
    {
        var member = await _db.FamilyMembers.Include(row => row.FamilyGroup)
            .FirstOrDefaultAsync(row => row.FamilyMemberId == familyMemberId && row.Status == "Pending");
        if (member == null) return Fail("Invitation was not found.", 404);
        if (member.FamilyGroup.CreatedBy != patientId) return Fail("Only the family owner can cancel an invitation.", 403);

        _db.FamilyMembers.Remove(member);
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "Invitation cancelled.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> UpdateAccessAsync(string patientId, long familyMemberId, FamilyAccessUpdateRequest request)
    {
        var group = await OwnerGroupAsync(patientId);
        if (group == null) return Fail("Only the family owner can update permissions.", 403);

        var relation = NormalizeRelation(request.Relation);
        if (relation == null) return Fail("Choose either parent or child.", 400);

        var member = await _db.FamilyMembers.FirstOrDefaultAsync(row => row.FamilyGroupId == group.FamilyGroupId && row.FamilyMemberId == familyMemberId);
        if (member == null) return Fail("Family member was not found.", 404);
        if (member.PatientId == patientId) return Fail("The owner must remain a parent.", 400);

        member.Relationship = relation;
        member.AccessLevel = ToAccessLevel(relation);
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "Family permission updated.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> RenameSelfAsync(string patientId, FamilyMemberDisplayNameRequest request)
    {
        var member = await ActiveMemberAsync(patientId);
        if (member == null) return Fail("You do not belong to an active family.", 404);

        var name = request.Name.Trim();
        if (name.Length < 2 || name.Length > 100)
        {
            return Fail("Your family display name must be between 2 and 100 characters.", 400);
        }

        member.DisplayName = name;
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "Your family display name was updated.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> RemoveMemberAsync(string patientId, long familyMemberId)
    {
        var group = await OwnerGroupAsync(patientId);
        if (group == null) return Fail("Only the family owner can remove a member.", 403);

        var member = await _db.FamilyMembers.FirstOrDefaultAsync(row => row.FamilyGroupId == group.FamilyGroupId && row.FamilyMemberId == familyMemberId && row.PatientId != patientId);
        if (member == null) return Fail("Family member was not found.", 404);

        _db.FamilyMembers.Remove(member);
        group.MemberCount = await _db.FamilyMembers.CountAsync(row => row.FamilyGroupId == group.FamilyGroupId && row.Status == "Active" && row.FamilyMemberId != familyMemberId);
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "Family member removed.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> LeaveAsync(string patientId, FamilyLeaveRequest request)
    {
        var member = await ActiveMemberAsync(patientId);
        if (member == null) return Fail("You do not belong to an active family.", 404);

        var group = await _db.FamilyGroups.FirstAsync(row => row.FamilyGroupId == member.FamilyGroupId);
        var activeMembers = await _db.FamilyMembers.Where(row => row.FamilyGroupId == group.FamilyGroupId && row.Status == "Active").ToListAsync();

        if (group.CreatedBy == patientId)
        {
            if (activeMembers.Count == 1)
            {
                _db.FamilyGroups.Remove(group);
                await _db.SaveChangesAsync();
                return ApiResponse<FamilyOverviewResponse>.Ok(new FamilyOverviewResponse { CurrentPatientId = patientId }, "Family deleted.");
            }

            if (!request.TransferToMemberId.HasValue) return Fail("Transfer ownership before leaving the family.", 400);
            var transfer = activeMembers.FirstOrDefault(row => row.FamilyMemberId == request.TransferToMemberId.Value && row.PatientId != patientId);
            if (transfer == null) return Fail("Choose an active family member for ownership transfer.", 400);

            group.CreatedBy = transfer.PatientId;
            transfer.AccessLevel = "Parent";
            transfer.Relationship = "parent";
        }

        _db.FamilyMembers.Remove(member);
        group.MemberCount = activeMembers.Count - 1;
        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, "You left the family.");
    }

    public async Task<ApiResponse<FamilyOverviewResponse>> DeleteAsync(string patientId)
    {
        var group = await OwnerGroupAsync(patientId);
        if (group == null) return Fail("Only the family owner can delete the family.", 403);
        _db.FamilyGroups.Remove(group);
        await _db.SaveChangesAsync();
        return ApiResponse<FamilyOverviewResponse>.Ok(new FamilyOverviewResponse { CurrentPatientId = patientId }, "Family deleted.");
    }

    private async Task<ApiResponse<FamilyOverviewResponse>> DecideInviteAsync(string patientId, long familyMemberId, bool accept)
    {
        var member = await _db.FamilyMembers
            .Include(row => row.Patient).ThenInclude(row => row.User)
            .Include(row => row.FamilyGroup).ThenInclude(row => row.Patient).ThenInclude(row => row.User)
            .FirstOrDefaultAsync(row => row.FamilyMemberId == familyMemberId && row.PatientId == patientId && row.Status == "Pending");
        if (member == null) return Fail("Invitation was not found.", 404);

        var invitationNotification = await _db.Notifications
            .FirstOrDefaultAsync(row => row.NotificationId == $"family-invite:{familyMemberId}" && row.UserId == member.Patient.UserId);
        if (invitationNotification != null)
        {
            invitationNotification.Status = "Read";
        }

        if (accept)
        {
            if (await HasAnyActiveFamilyMembershipAsync(patientId)) return Fail("You already belong to an active family.", 409);
            member.Status = "Active";
            member.JoinAt = DateTimeHelper.MalaysiaNow();
            member.FamilyGroup.MemberCount = await _db.FamilyMembers.CountAsync(row => row.FamilyGroupId == member.FamilyGroupId && row.Status == "Active") + 1;
        }
        else
        {
            var inviteeName = member.Patient.User.Name;
            await UpsertNotificationAsync(
                member.FamilyGroup.Patient.UserId,
                $"family-invite-declined:{familyMemberId}",
                "Family invitation declined",
                $"{inviteeName} declined your family invitation.",
                "家庭邀请已拒绝",
                $"{inviteeName}拒绝了您的家庭邀请。",
                inviteeName,
                "family_invitation_declined",
                "/family",
                DateTimeHelper.MalaysiaNow());
            _db.FamilyMembers.Remove(member);
        }

        await _db.SaveChangesAsync();
        return await SuccessOverviewAsync(patientId, accept ? "Invitation accepted." : "Invitation declined.");
    }

    private async Task<FamilyOverviewResponse> BuildOverviewAsync(string patientId)
    {
        var memberships = await _db.FamilyMembers
            .Include(row => row.FamilyGroup)
            .Include(row => row.Patient).ThenInclude(patient => patient.User)
            .Where(row => row.PatientId == patientId)
            .ToListAsync();

        var active = memberships.FirstOrDefault(row => row.Status == "Active");
        FamilyResponse? family = null;
        if (active != null)
        {
            var allMembers = await _db.FamilyMembers
                .Include(row => row.FamilyGroup)
                .Include(row => row.Patient).ThenInclude(patient => patient.User)
                .Where(row => row.FamilyGroupId == active.FamilyGroupId)
                .OrderBy(row => row.FamilyMemberId)
                .ToListAsync();
            family = new FamilyResponse
            {
                FamilyGroupId = active.FamilyGroupId,
                Name = active.FamilyGroup.GroupName,
                OwnerPatientId = active.FamilyGroup.CreatedBy,
                Members = allMembers.Select(ToMemberResponse).ToList()
            };
        }

        var ownerIds = memberships.Where(row => row.Status == "Pending").Select(row => row.FamilyGroup.CreatedBy).Distinct().ToList();
        var ownerNames = await _db.Patients
            .Include(row => row.User)
            .Where(row => ownerIds.Contains(row.PatientId))
            .ToDictionaryAsync(row => row.PatientId, row => row.User.Name);
        var invites = memberships.Where(row => row.Status == "Pending").Select(row => new FamilyInviteResponse
        {
            FamilyMemberId = row.FamilyMemberId,
            FamilyGroupId = row.FamilyGroupId,
            FamilyName = row.FamilyGroup.GroupName,
            InvitedByName = ownerNames.GetValueOrDefault(row.FamilyGroup.CreatedBy, "Your family owner"),
            Relation = row.Relationship.ToLowerInvariant(),
            Permission = row.AccessLevel.ToLowerInvariant()
        }).ToList();

        return new FamilyOverviewResponse
        {
            CurrentPatientId = patientId,
            Family = family,
            Invites = invites
        };
    }

    private async Task<FamilyGroup?> OwnerGroupAsync(string patientId)
    {
        var member = await ActiveMemberAsync(patientId);
        if (member == null) return null;
        return member.FamilyGroup.CreatedBy == patientId ? member.FamilyGroup : null;
    }

    private async Task<FamilyMember?> ActiveMemberAsync(string patientId)
    {
        return await _db.FamilyMembers.Include(row => row.FamilyGroup)
            .FirstOrDefaultAsync(row => row.PatientId == patientId && row.Status == "Active");
    }

    private Task<bool> HasAnyFamilyMembershipAsync(string patientId) => _db.FamilyMembers.AnyAsync(row => row.PatientId == patientId && (row.Status == "Active" || row.Status == "Pending"));
    private Task<bool> HasAnyActiveFamilyMembershipAsync(string patientId) => _db.FamilyMembers.AnyAsync(row => row.PatientId == patientId && row.Status == "Active");

    private static string? NormalizeRelation(string value)
    {
        var relation = value.Trim().ToLowerInvariant();
        return AllowedRelations.Contains(relation) ? relation : null;
    }

    private static string ToAccessLevel(string relation) => relation.Equals("parent", StringComparison.OrdinalIgnoreCase) ? "Parent" : "Child";

    private async Task UpsertNotificationAsync(
        long userId,
        string notificationId,
        string title,
        string message,
        string titleZh,
        string messageZh,
        string actor,
        string type,
        string href,
        DateTime createdAt)
    {
        var notification = await _db.Notifications.FirstOrDefaultAsync(row => row.NotificationId == notificationId);
        if (notification == null)
        {
            notification = new Notification
            {
                NotificationId = notificationId,
                UserId = userId,
                CreatedAt = createdAt,
            };
            _db.Notifications.Add(notification);
        }

        var preference = await _db.AppSettings
            .AsNoTracking()
            .Where(row => row.UserId == userId)
            .Select(row => row.Notification)
            .FirstOrDefaultAsync() ?? "all";

        notification.Title = title;
        notification.Message = message;
        notification.TitleZh = titleZh;
        notification.MessageZh = messageZh;
        notification.Actor = actor;
        notification.Initials = Initials(actor);
        notification.Type = type;
        notification.Href = href;
        notification.Status = preference.Equals("all", StringComparison.OrdinalIgnoreCase) ? "Unread" : "Read";
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Take(2).Select(part => char.ToUpperInvariant(part[0])));
    }

    private static FamilyMemberResponse ToMemberResponse(FamilyMember row) => new()
    {
        FamilyMemberId = row.FamilyMemberId,
        PatientId = row.PatientId,
        Name = string.IsNullOrWhiteSpace(row.DisplayName) ? row.Patient.User.Name : row.DisplayName,
        Email = row.Patient.User.Email,
        Relation = row.Relationship.ToLowerInvariant(),
        Permission = row.AccessLevel.ToLowerInvariant(),
        Status = row.Status.ToLowerInvariant(),
        IsOwner = row.FamilyGroup?.CreatedBy == row.PatientId
    };

    private async Task<ApiResponse<FamilyOverviewResponse>> SuccessOverviewAsync(string patientId, string message)
    {
        return ApiResponse<FamilyOverviewResponse>.Ok(await BuildOverviewAsync(patientId), message);
    }

    private static ApiResponse<FamilyOverviewResponse> Fail(string message, int code) => ApiResponse<FamilyOverviewResponse>.Fail(message, code);
}
