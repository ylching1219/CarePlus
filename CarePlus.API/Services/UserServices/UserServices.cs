using CarePlus.API.DTO;
using CarePlus.API.DTO.DeliveryAddressDTO;
using CarePlus.API.DTO.PaymentCardDTO;
using CarePlus.API.DTO.UserDTO;
using CarePlus.API.Data;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.Payment;
using CarePlus.API.Extension;
using CarePlus.API.Models.User;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.JWTServices.Interface;
using CarePlus.API.Services.UserServices.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


namespace CarePlus.API.Services.UserServices
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepo;
        private readonly PasswordHasher<User> _passwordHasher = new();
        private readonly IPatientRepository _patientRepo;
        private readonly IStaffRepository _staffRepo;
        private readonly IJWTServices _jwtService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _environment;
        private readonly AddressGeocoder _geocoder;
        private readonly AppDbContext _db;
        public UserServices(IUserRepository userRepo,IPatientRepository patientRepo , IStaffRepository staffRepo , IJWTServices jwtService, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, AddressGeocoder geocoder, AppDbContext db)
        {
            _userRepo = userRepo;
            _patientRepo = patientRepo;
            _staffRepo = staffRepo;
            _jwtService = jwtService;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            _geocoder = geocoder;
            _db = db;
        }

        public async Task<ApiResponse<object>> CheckUsernameAvailabilityAsync(string username)
        {
            var normalizedUsername = username?.Trim() ?? string.Empty;

            if (normalizedUsername.Length == 0)
            {
                return ApiResponse<object>.Ok(new { available = false });
            }

            var normalizedUsernameKey = normalizedUsername.ToUpperInvariant();
            var existingUsers = await _userRepo.GetAllUsersByAsync(user =>
                user.Username.ToUpper() == normalizedUsernameKey);

            return ApiResponse<object>.Ok(new { available = existingUsers.Count == 0 });
        }

        public async Task<List<string>> ValidateUserAsync(UserRegisterRequest request)
        {
            var errors = new List<string>();

            var checkEmail = await _userRepo.GetAllUsersByAsync(u => u.Email == request.Email);
            if (checkEmail != null && checkEmail.Count > 0)
            {
                errors.Add("Email already exists");
            }

            var normalizedUsername = request.Username?.Trim() ?? string.Empty;
            var normalizedUsernameKey = normalizedUsername.ToUpperInvariant();
            var checkUsername = await _userRepo.GetAllUsersByAsync(u =>
                u.Username.ToUpper() == normalizedUsernameKey);
            if (checkUsername != null && checkUsername.Count > 0)
            {
                errors.Add("Username already exists");
            }

            return errors;
        }

        public async Task<ApiResponse<object>> RegisterUserAsync(UserRegisterRequest request, bool emailVerified = true)
        {
            try
            {
                var errors = await ValidateUserAsync( request);

            if (errors.Count > 0)
            {
                return ApiResponse<object>.Fail(errors[0], 400, errors);
            }

            var createdUser = new User
            {
                Username = request.Username.Trim(),
                Name = request.Name,
                Email = request.Email,
                Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
                EmailVerified = emailVerified,
            };

                createdUser.PasswordHash = HashPassword(createdUser, request.Password);
                
                long userId = await _userRepo.CreateUserAsync(createdUser);
                await EnsureAppSettingAsync(userId, false);

                if (userId > 0)
                {
                    return ApiResponse<object>.Ok(new UserRegisterResponse
                    {
                        UserId = userId,
                        Name = request.Name
                    });
                }

                return ApiResponse<object>.Fail("User registration failed", 500);

            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public Task<ApiResponse<object>> PatientLoginAsync(UserLoginRequest request)
        {
            return LoginAsync(request, "Patient");
        }

        public Task<ApiResponse<object>> StaffLoginAsync(UserLoginRequest request)
        {
            return LoginAsync(request, "Staff");
        }

        private async Task<ApiResponse<object>> LoginAsync(UserLoginRequest request, string expectedAccountType)
        {
            try
            {
                var users = await _userRepo.GetAllUsersByAsync(u => u.Username == request.Identifier || u.Email == request.Identifier);      
                var user = users.FirstOrDefault();
                if (user == null)
                {
                    return ApiResponse<object>.Fail("Incorrect username/email or password.", 401);
                }

                bool passwordMatch = VerifyPassword(user,request.Password);

                if (!passwordMatch) {
                    return ApiResponse<object>.Fail("Incorrect username/email or password.", 401);
                }

                if (user.Status != "Active")
                {
                    return ApiResponse<object>.Fail("This account is not active.", 403);
                }

                if (expectedAccountType == "Patient" && !user.EmailVerified)
                {
                    return ApiResponse<object>.Fail("Please verify your email address before logging in.", 403);
                }

                var patient =await _patientRepo.GetPatientByUserIdAsync(user.UserId);

                var staff =await _staffRepo.GetStaffByUserIdAsync(user.UserId);

                if (patient == null && staff == null)
                {
                    return ApiResponse<object>.Fail(
                        "User profile was not found.", 404);
                }

                if (patient != null && staff != null)
                {
                    return ApiResponse<object>.Fail(
                        "This account has multiple profiles.", 409);
                }

                var accountType = staff != null ? "Staff" : "Patient";

                if (accountType != expectedAccountType)
                {
                    return ApiResponse<object>.Fail(
                        $"This account is not a {expectedAccountType.ToLowerInvariant()} account.", 403);
                }

                var role = staff?.Role ?? "Patient";

                var token = _jwtService.GenerateToken(
                    user.UserId,
                    user.Username,
                    accountType,
                    role,
                    patient?.PatientId,
                    staff?.StaffId);

                await RecordLoginAsync(user.UserId);

                return ApiResponse<object>.Ok(new
                {
                    token
                });


            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }
        public async Task<ApiResponse<object>> RetrieveAllUserAsync()
        {
            try
            {
                List<User> users = await _userRepo.GetAllUsersAsync();

                if (users.Count > 0)
                {
                    var result = users.Select(u => new UserRetrieveResponse
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Name = u.Name!,
                        Email = u.Email,
                        Phone = u.Phone!,
                        Status = u.Status
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No user found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveUserByIdAsync(long id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);

                if (user == null)
                {
                    return ApiResponse<object>.Fail(
                        $"No user found with id {id}", 404);
                }

                var result = new UserRetrieveResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Name = user.Name!,
                    Email = user.Email,
                    Phone = user.Phone!,
                    Status = user.Status
                };

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail( $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveCurrentUserAsync(long id)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(id);

                if (user == null)
                {
                    return ApiResponse<object>.Fail("Current user was not found", 404);
                }

                var patient = await _patientRepo.GetPatientByUserIdAsync(user.UserId);
                var staff = await _staffRepo.GetStaffByUserIdAsync(user.UserId);

                if (patient == null && staff == null)
                {
                    return ApiResponse<object>.Fail("User profile was not found", 404);
                }

                if (patient != null && staff != null)
                {
                    return ApiResponse<object>.Fail("This account has multiple profiles", 409);
                }

                var result = new CurrentUserResponse
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Name = user.Name ?? string.Empty,
                    Email = user.Email,
                    Phone = user.Phone ?? string.Empty,
                    Status = user.Status,
                    AccountType = staff != null ? "Staff" : "Patient",
                    Role = staff?.Role ?? "Patient",
                    PatientId = patient?.PatientId,
                    StaffId = staff?.StaffId,
                    IdentityCardNumber = patient?.IdentityCardNumber,
                    Gender = patient == null ? null : patient.Gender.ToString(),
                    Address = patient?.Address,
                    ProfilePicture = patient?.ProfilePicture
                };

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveLoginActivityAsync(long userId)
        {
            try
            {
                var rows = await _userRepo.GetRecentLoginActivityAsync(userId);
                var result = rows.Select(row => new LoginActivityResponse
                {
                    Id = row.Id,
                    Device = row.Device,
                    IpAddress = row.IpAddress,
                    CreatedAt = row.CreatedAt
                }).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllActiveUserAsync()
        {
            try
            {
                List<User> users = await _userRepo.GetAllUsersByAsync(u => u.Status == "Active");

                if (users != null && users.Count > 0)
                {
                    var result = users.Select(u => new UserRetrieveResponse
                    {
                        UserId = u.UserId,
                        Username = u.Username,
                        Name = u.Name!,
                        Email = u.Email,
                        Phone = u.Phone!,
                        Status = u.Status
                    }).ToList();

                    return ApiResponse<object>.Ok(result);
                }

                return ApiResponse<object>.Fail("No active user found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateUserAsync(UserUpdateRequest request)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(request.Id);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.Status))
                    {
                        var status = request.Status.Trim();

                        if (status != "Active" &&
                            status != "Inactive" &&
                            status != "Suspended")
                        {
                            return ApiResponse<object>.Fail("Invalid status input", 400);
                        }

                        user.Status = status;
                    }

                    if (!string.IsNullOrWhiteSpace(request.Name))
                    {
                        user.Name = request.Name.Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(request.Phone))
                    {
                        user.Phone = request.Phone.Trim();
                    }

                    bool success = await _userRepo.UpdateUserAsync(user);
                    if (!success)
                    {
                        return ApiResponse<object>.Fail("Failed to update user", 500);
                    }
                    return ApiResponse<object>.Ok(null);
                }

                return ApiResponse<object>.Fail("User not found", 404);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> ChangePasswordAsync(long userId, PasswordChangeRequest request)
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<object>.Fail("User not found.", 404);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !VerifyPassword(user, request.CurrentPassword))
            {
                return ApiResponse<object>.Fail("Current password is incorrect.", 400);
            }

            var ruleError = PasswordRuleError(request.NewPassword);
            if (ruleError != null)
            {
                return ApiResponse<object>.Fail(ruleError, 400);
            }

            user.PasswordHash = HashPassword(user, request.NewPassword);
            await _userRepo.UpdateUserAsync(user);
            return ApiResponse<object>.Ok(new { updated = true }, "Password updated.");
        }

        public async Task<ApiResponse<object>> DeleteCurrentAccountAsync(long userId, AccountDeleteRequest request)
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null || user.Status == "Deleted")
            {
                return ApiResponse<object>.Fail("Account was not found.", 404);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) || !VerifyPassword(user, request.CurrentPassword))
            {
                return ApiResponse<object>.Fail("Current password is incorrect.", 400);
            }

            var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
            if (patient == null)
            {
                return ApiResponse<object>.Fail("Only patient accounts can be deleted here.", 400);
            }

            var ownedGroup = await _db.FamilyGroups.FirstOrDefaultAsync(group => group.CreatedBy == patient.PatientId);
            if (ownedGroup != null)
            {
                var activeMemberCount = await _db.FamilyMembers.CountAsync(member =>
                    member.FamilyGroupId == ownedGroup.FamilyGroupId && member.Status == "Active");
                if (activeMemberCount > 1)
                {
                    return ApiResponse<object>.Fail("Transfer family ownership before deleting your account.", 409);
                }
            }

            var oldProfilePicture = patient.ProfilePicture;
            await using var transaction = await _db.Database.BeginTransactionAsync();

            if (ownedGroup != null)
            {
                _db.FamilyGroups.Remove(ownedGroup);
            }
            else
            {
                var memberships = await _db.FamilyMembers
                    .Include(member => member.FamilyGroup)
                    .Where(member => member.PatientId == patient.PatientId)
                    .ToListAsync();

                foreach (var membership in memberships)
                {
                    if (membership.Status == "Active")
                    {
                        membership.FamilyGroup.MemberCount = Math.Max(0, membership.FamilyGroup.MemberCount - 1);
                    }
                    _db.FamilyMembers.Remove(membership);
                }
            }

            var accessRequests = await _db.FamilyUseRequests
                .Where(row => row.FromPatientId == patient.PatientId || row.TargetPatientId == patient.PatientId)
                .ToListAsync();
            _db.FamilyUseRequests.RemoveRange(accessRequests);

            var cart = await _db.Carts.FirstOrDefaultAsync(row => row.PatientId == patient.PatientId);
            if (cart != null)
            {
                _db.Carts.Remove(cart);
            }

            _db.DeliveryAddresses.RemoveRange(_db.DeliveryAddresses.Where(row => row.PatientId == patient.PatientId));
            _db.PaymentCards.RemoveRange(_db.PaymentCards.Where(row => row.UserId == userId));
            _db.LoginActivities.RemoveRange(_db.LoginActivities.Where(row => row.UserId == userId));
            _db.Notifications.RemoveRange(_db.Notifications.Where(row => row.UserId == userId));
            _db.TwoFactorChallenges.RemoveRange(_db.TwoFactorChallenges.Where(row => row.UserId == userId));
            _db.AppSettings.RemoveRange(_db.AppSettings.Where(row => row.UserId == userId));

            var deletedKey = $"deleted-{userId}-{Guid.NewGuid():N}";
            user.Username = deletedKey;
            user.Email = $"{deletedKey}@deleted.careplus.local";
            user.Name = "Deleted account";
            user.Phone = null;
            user.Status = "Deleted";
            user.EmailVerified = false;
            user.PasswordHash = HashPassword(user, Guid.NewGuid().ToString("N"));

            patient.Address = null;
            patient.ProfilePicture = null;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            DeleteProfilePictureFile(oldProfilePicture);

            return ApiResponse<object>.Ok(new { deleted = true }, "Account deleted.");
        }

        private void DeleteProfilePictureFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            try
            {
                var webRoot = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var profileRoot = Path.GetFullPath(Path.Combine(webRoot, "profiles"));
                var candidate = Path.GetFullPath(Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
                if (!candidate.StartsWith(profileRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
                if (File.Exists(candidate)) File.Delete(candidate);
            }
            catch
            {
                // Account deletion must not fail because an old profile image cannot be removed.
            }
        }

        public async Task<ApiResponse<object>> UpdateCurrentProfileAsync(long userId, ProfileUpdateRequest request)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<object>.Fail("Current user was not found", 404);
                }

                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                if (request.Name != null)
                {
                    var name = request.Name.Trim();
                    if (name.Length < 2 || name.Length > 100)
                    {
                        return ApiResponse<object>.Fail("Name must be between 2 and 100 characters.", 400);
                    }

                    user.Name = name;
                }

                if (request.Email != null)
                {
                    var email = request.Email.Trim().ToLowerInvariant();
                    if (email.Length > 254 || !email.Contains('@') || !email.Contains('.'))
                    {
                        return ApiResponse<object>.Fail("Invalid email format.", 400);
                    }

                    var taken = await _userRepo.GetAllUsersByAsync(match => match.Email == email);
                    if (taken.Any(match => match.UserId != user.UserId))
                    {
                        return ApiResponse<object>.Fail("Email already exists", 400);
                    }

                    user.Email = email;
                }

                if (request.Phone != null)
                {
                    var phone = NormalizePhone(request.Phone);
                    if (phone == null)
                    {
                        return ApiResponse<object>.Fail("Invalid phone number.", 400);
                    }

                    user.Phone = phone;
                }

                var derivedGender = GenderFromIdentityCard(patient.IdentityCardNumber);
                if (derivedGender != null)
                {
                    patient.Gender = derivedGender.Value;
                }

                if (request.Address != null)
                {
                    var address = request.Address.Trim();
                    if (address.Length < 5 || address.Length > 200)
                    {
                        return ApiResponse<object>.Fail("Address must be between 5 and 200 characters.", 400);
                    }

                    patient.Address = address;
                }

                await _userRepo.UpdateUserAsync(user);
                await _patientRepo.UpdatePatientAsync(patient);
                return await RetrieveCurrentUserAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public Task<ApiResponse<object>> RetrieveDeliveryAddressesAsync(long userId)
        {
            return DeliveryAddressListAsync(userId);
        }

        public async Task<ApiResponse<object>> AddDeliveryAddressAsync(long userId, DeliveryAddressSaveRequest request)
        {
            try
            {
                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                var saved = ReadDeliveryAddress(request.Label, request.Unit, request.Address);
                if (saved == null)
                {
                    return ApiResponse<object>.Fail("Enter a delivery label and an address with a 5-digit postcode.", 400);
                }

                var point = await _geocoder.LocateAsync(AddressGeocoder.StreetLine(saved.Value.Unit, saved.Value.Address));
                await _patientRepo.AddDeliveryAddressAsync(new DeliveryAddress
                {
                    PatientId = patient.PatientId,
                    Label = saved.Value.Label,
                    Unit = saved.Value.Unit,
                    Address = saved.Value.Address,
                    IsDefault = request.IsDefault,
                    Latitude = point?.Latitude,
                    Longitude = point?.Longitude,
                });

                return await DeliveryAddressListAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateDeliveryAddressAsync(long userId, long addressId, DeliveryAddressSaveRequest request)
        {
            try
            {
                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                var row = await _patientRepo.GetDeliveryAddressAsync(addressId, patient.PatientId);
                if (row == null)
                {
                    return ApiResponse<object>.Fail("Delivery address was not found", 404);
                }

                var saved = ReadDeliveryAddress(request.Label, request.Unit, request.Address);
                if (saved == null)
                {
                    return ApiResponse<object>.Fail("Enter a delivery label and an address with a 5-digit postcode.", 400);
                }

                row.Label = saved.Value.Label;
                row.Unit = saved.Value.Unit;
                row.Address = saved.Value.Address;
                var point = await _geocoder.LocateAsync(AddressGeocoder.StreetLine(saved.Value.Unit, saved.Value.Address));
                row.Latitude = point?.Latitude;
                row.Longitude = point?.Longitude;
                await _patientRepo.UpdateDeliveryAddressAsync(row);
                return await DeliveryAddressListAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> DeleteDeliveryAddressAsync(long userId, long addressId)
        {
            try
            {
                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                var row = await _patientRepo.GetDeliveryAddressAsync(addressId, patient.PatientId);
                if (row == null)
                {
                    return ApiResponse<object>.Fail("Delivery address was not found", 404);
                }

                await _patientRepo.DeleteDeliveryAddressAsync(row);
                return await DeliveryAddressListAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> SetDefaultDeliveryAddressAsync(long userId, long? addressId)
        {
            try
            {
                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                var updated = await _patientRepo.SetDefaultDeliveryAddressAsync(patient.PatientId, addressId);
                if (!updated)
                {
                    return ApiResponse<object>.Fail("Delivery address was not found", 404);
                }

                return await DeliveryAddressListAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        private async Task<ApiResponse<object>> DeliveryAddressListAsync(long userId)
        {
            var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
            if (patient == null)
            {
                return ApiResponse<object>.Fail("This account is not a patient account.", 400);
            }

            var rows = await _patientRepo.GetDeliveryAddressesAsync(patient.PatientId);
            var result = rows.Select(row => new DeliveryAddressResponse
            {
                Id = row.Id,
                Label = row.Label,
                Unit = row.Unit,
                Address = row.Address,
                IsDefault = row.IsDefault,
            }).ToList();

            return ApiResponse<object>.Ok(result);
        }

        private static (string Label, string Unit, string Address)? ReadDeliveryAddress(string label, string unit, string address)
        {
            var savedLabel = label.Trim().ToLowerInvariant();
            if (savedLabel is not ("home" or "office" or "other"))
            {
                return null;
            }

            var savedUnit = unit.Trim();
            if (savedUnit.Length < 1 || savedUnit.Length > 30)
            {
                return null;
            }

            var savedAddress = address.Trim();
            if (savedAddress.Length < 5 || savedAddress.Length > 200 || !System.Text.RegularExpressions.Regex.IsMatch(savedAddress, @"\d{5}"))
            {
                return null;
            }

            return (savedLabel, savedUnit, savedAddress);
        }

        private static string? NormalizePhone(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("60"))
            {
                digits = digits[2..];
            }

            digits = digits.TrimStart('0');
            var valid = (digits.Length == 10 && digits.StartsWith("11"))
                || (digits.Length is 9 or 10 && (digits.StartsWith("10") || digits.StartsWith("14")))
                || (digits.Length == 9 && digits[0] == '1' && "236789".Contains(digits[1]));
            return valid ? "+60" + digits : null;
        }

        private static char? GenderFromIdentityCard(string? identityCard)
        {
            if (string.IsNullOrWhiteSpace(identityCard))
            {
                return null;
            }

            var digits = new string(identityCard.Where(char.IsDigit).ToArray());
            if (digits.Length != 12 || !char.IsDigit(digits[^1]))
            {
                return null;
            }

            return (digits[^1] - '0') % 2 == 1 ? 'M' : 'F';
        }

        private static char? ReadGender(string value)
        {
            var text = value.Trim().ToUpperInvariant();
            if (text is "M" or "MALE")
            {
                return 'M';
            }

            if (text is "F" or "FEMALE")
            {
                return 'F';
            }

            return null;
        }

        public async Task<ApiResponse<object>> SaveProfilePictureAsync(long userId, IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0 || file.Length > 2 * 1024 * 1024)
                {
                    return ApiResponse<object>.Fail("Use a JPG, PNG, or WebP photo under 2 MB.", 400);
                }

                var extension = file.ContentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/webp" => ".webp",
                    _ => null
                };
                if (extension == null)
                {
                    return ApiResponse<object>.Fail("Use a JPG, PNG, or WebP photo under 2 MB.", 400);
                }

                var patient = await _patientRepo.GetPatientByUserIdAsync(userId);
                if (patient == null)
                {
                    return ApiResponse<object>.Fail("This account is not a patient account.", 400);
                }

                var root = string.IsNullOrEmpty(_environment.WebRootPath)
                    ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                    : _environment.WebRootPath;
                var folder = Path.Combine(root, "profiles");
                Directory.CreateDirectory(folder);

                foreach (var old in Directory.EnumerateFiles(folder, patient.PatientId + "*"))
                {
                    File.Delete(old);
                }

                var fileName = $"{patient.PatientId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}{extension}";
                await using (var stream = File.Create(Path.Combine(folder, fileName)))
                {
                    await file.CopyToAsync(stream);
                }

                patient.ProfilePicture = $"profiles/{fileName}";
                await _patientRepo.UpdatePatientAsync(patient);
                return await RetrieveCurrentUserAsync(userId);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail($"An error occurred: {ex.Message}", 500);
            }
        }

        private async Task RecordLoginAsync(long userId)
        {
            var http = _httpContextAccessor.HttpContext;
            var address = http?.Connection.RemoteIpAddress;
            if (address is { IsIPv4MappedToIPv6: true })
            {
                address = address.MapToIPv4();
            }

            var device = DescribeDevice(http?.Request.Headers.UserAgent.ToString());
            var ipAddress = address?.ToString() ?? "";
            var createdAt = DateTimeHelper.MalaysiaNow();
            var updated = await _userRepo.TouchLoginActivityAsync(userId, device, ipAddress, createdAt);
            if (!updated)
            {
                await _userRepo.AddLoginActivityAsync(new LoginActivity
                {
                    UserId = userId,
                    Device = device,
                    IpAddress = ipAddress,
                    CreatedAt = createdAt,
                });
            }
        }

        private static string DescribeDevice(string? userAgent)
        {
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                return "Unknown browser";
            }

            var browser = userAgent.Contains("Edg/") ? "Edge"
                : userAgent.Contains("Chrome/") ? "Chrome"
                : userAgent.Contains("Firefox/") ? "Firefox"
                : userAgent.Contains("Safari/") ? "Safari"
                : "Browser";
            var system = userAgent.Contains("iPhone") ? "iPhone"
                : userAgent.Contains("iPad") ? "iPad"
                : userAgent.Contains("Android") ? "Android"
                : userAgent.Contains("Windows") ? "Windows"
                : userAgent.Contains("Mac OS") ? "Mac"
                : userAgent.Contains("Linux") ? "Linux"
                : "";

            return system.Length == 0 ? browser : $"{browser} · {system}";
        }

        public async Task EnsureAppSettingAsync(long userId, bool staff)
        {
            var setting = await _userRepo.GetAppSettingAsync(userId);
            if (setting == null)
            {
                await _userRepo.AddAppSettingAsync(new AppSetting
                {
                    UserId = userId,
                    Appearance = "light",
                    Language = staff ? null : "en",
                    Notification = "all",
                    TwoFactorEnabled = false,
                    HasCompletedTutorial = false,
                });
                return;
            }

            var changed = false;
            if (staff && setting.Language != null)
            {
                setting.Language = null;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(setting.Notification))
            {
                setting.Notification = "all";
                changed = true;
            }

            if (changed)
            {
                await _userRepo.UpdateAppSettingAsync(setting);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAppSettingAsync(long userId)
        {
            var staff = await _staffRepo.GetStaffByUserIdAsync(userId);
            await EnsureAppSettingAsync(userId, staff != null);
            var setting = await _userRepo.GetAppSettingAsync(userId);
            return ApiResponse<object>.Ok(setting == null ? null : ToAppSettingResponse(setting));
        }

        public async Task<ApiResponse<object>> SaveAppSettingAsync(long userId, AppSettingSaveRequest request)
        {
            var staff = await _staffRepo.GetStaffByUserIdAsync(userId);
            var setting = await _userRepo.GetAppSettingAsync(userId);
            var isNew = setting == null;
            setting ??= new AppSetting
            {
                UserId = userId,
                Appearance = "light",
                Language = staff == null ? "en" : null,
                Notification = "all",
                TwoFactorEnabled = false,
                HasCompletedTutorial = false,
            };

            var error = ApplyAppSetting(setting, request, staff != null);
            if (error != null)
            {
                return ApiResponse<object>.Fail(error, 400);
            }

            try
            {
                if (isNew)
                {
                    await _userRepo.AddAppSettingAsync(setting);
                }
                else
                {
                    await _userRepo.UpdateAppSettingAsync(setting);
                }
            }
            catch (DbUpdateException)
            {
                var existing = await _userRepo.GetAppSettingAsync(userId);
                if (existing == null)
                {
                    throw;
                }

                error = ApplyAppSetting(existing, request, staff != null);
                if (error != null)
                {
                    return ApiResponse<object>.Fail(error, 400);
                }

                await _userRepo.UpdateAppSettingAsync(existing);
                setting = existing;
            }

            return ApiResponse<object>.Ok(ToAppSettingResponse(setting));
        }

        private static string? ApplyAppSetting(AppSetting setting, AppSettingSaveRequest request, bool staff)
        {
            if (!string.IsNullOrWhiteSpace(request.Appearance))
            {
                var appearance = request.Appearance.Trim().ToLowerInvariant();
                if (appearance != "light" && appearance != "dark")
                {
                    return "Appearance must be light or dark.";
                }

                setting.Appearance = appearance;
            }

            if (staff)
            {
                setting.Language = null;
            }
            else if (!string.IsNullOrWhiteSpace(request.Language))
            {
                var language = request.Language.Trim().ToLowerInvariant();
                if (language != "en" && language != "zh")
                {
                    return "Language must be en or zh.";
                }

                setting.Language = language;
            }

            if (!string.IsNullOrWhiteSpace(request.Notification))
            {
                var notification = request.Notification.Trim().ToLowerInvariant();
                if (notification != "all" && notification != "reminder" && notification != "none")
                {
                    return "Notification must be all, reminder, or none.";
                }

                setting.Notification = notification;
            }

            if (request.HasCompletedTutorial.HasValue)
            {
                setting.HasCompletedTutorial = request.HasCompletedTutorial.Value;
            }

            return null;
        }

        private static AppSettingResponse ToAppSettingResponse(AppSetting setting)
        {
            return new AppSettingResponse
            {
                Appearance = setting.Appearance,
                Language = setting.Language,
                Notification = setting.Notification,
                TwoFactorEnabled = setting.TwoFactorEnabled,
                HasCompletedTutorial = setting.HasCompletedTutorial,
            };
        }

        public async Task<ApiResponse<object>> RetrievePaymentCardsAsync(long userId)
        {
            var cards = await _userRepo.GetPaymentCardsAsync(userId);
            return ApiResponse<object>.Ok(cards.Select(ToPaymentCardResponse).ToList());
        }

        public async Task<ApiResponse<object>> AddPaymentCardAsync(long userId, PaymentCardSaveRequest request)
        {
            var parsed = ReadPaymentCard(request);
            if (parsed.Error != null)
            {
                return ApiResponse<object>.Fail(parsed.Error, 400);
            }

            var existing = await _userRepo.GetPaymentCardsAsync(userId);
            if (existing.Any(card => SecretMatches(card.NumberHash, parsed.Number)))
            {
                return ApiResponse<object>.Fail("This card is already saved.", 400);
            }

            await _userRepo.AddPaymentCardAsync(new PaymentCard
            {
                UserId = userId,
                Brand = parsed.Brand,
                Last4 = parsed.Last4,
                Expiry = parsed.Expiry,
                HolderName = parsed.Holder,
                NumberHash = HashSecret(parsed.Number),
                CvvHash = HashSecret(parsed.Cvv),
                IsDefault = existing.Count == 0,
            });

            return await RetrievePaymentCardsAsync(userId);
        }

        public async Task<ApiResponse<object>> UpdatePaymentCardAsync(long userId, long cardId, PaymentCardSaveRequest request)
        {
            var card = await _userRepo.GetPaymentCardAsync(cardId, userId);
            if (card == null)
            {
                return ApiResponse<object>.Fail("Card not found.", 404);
            }

            var parsed = ReadPaymentCard(request);
            if (parsed.Error != null)
            {
                return ApiResponse<object>.Fail(parsed.Error, 400);
            }

            var existing = await _userRepo.GetPaymentCardsAsync(userId);
            if (existing.Any(other => other.Id != cardId && SecretMatches(other.NumberHash, parsed.Number)))
            {
                return ApiResponse<object>.Fail("This card is already saved.", 400);
            }

            card.Brand = parsed.Brand;
            card.Last4 = parsed.Last4;
            card.Expiry = parsed.Expiry;
            card.HolderName = parsed.Holder;
            card.NumberHash = HashSecret(parsed.Number);
            card.CvvHash = HashSecret(parsed.Cvv);
            await _userRepo.SavePaymentCardsAsync();
            return await RetrievePaymentCardsAsync(userId);
        }

        public async Task<ApiResponse<object>> DeletePaymentCardAsync(long userId, long cardId)
        {
            var cards = await _userRepo.GetPaymentCardsAsync(userId);
            var card = cards.FirstOrDefault(item => item.Id == cardId);
            if (card == null)
            {
                return ApiResponse<object>.Fail("Card not found.", 404);
            }

            var wasDefault = card.IsDefault;
            await _userRepo.DeletePaymentCardAsync(card);
            if (wasDefault)
            {
                var remaining = await _userRepo.GetPaymentCardsAsync(userId);
                if (remaining.Count > 0 && remaining.All(item => !item.IsDefault))
                {
                    remaining[0].IsDefault = true;
                    await _userRepo.SavePaymentCardsAsync();
                }
            }

            return await RetrievePaymentCardsAsync(userId);
        }

        public async Task<ApiResponse<object>> SetDefaultPaymentCardAsync(long userId, long cardId)
        {
            var cards = await _userRepo.GetPaymentCardsAsync(userId);
            if (cards.All(card => card.Id != cardId))
            {
                return ApiResponse<object>.Fail("Card not found.", 404);
            }

            foreach (var card in cards)
            {
                card.IsDefault = card.Id == cardId;
            }

            await _userRepo.SavePaymentCardsAsync();
            return await RetrievePaymentCardsAsync(userId);
        }

        private string HashSecret(string value)
        {
            return _passwordHasher.HashPassword(new User(), value);
        }

        private bool SecretMatches(string hash, string value)
        {
            return _passwordHasher.VerifyHashedPassword(new User(), hash, value) == PasswordVerificationResult.Success;
        }

        private static PaymentCardParsed ReadPaymentCard(PaymentCardSaveRequest request)
        {
            var number = new string((request.Number ?? "").Where(char.IsDigit).ToArray());
            if (number.Length == 0)
            {
                return PaymentCardParsed.Invalid("Enter the card number.");
            }

            if (number.Length < 13 || number.Length > 19 || !PassesLuhn(number))
            {
                return PaymentCardParsed.Invalid("That card number doesn't look right. Check the digits.");
            }

            var expiry = (request.Expiry ?? "").Trim();
            var match = System.Text.RegularExpressions.Regex.Match(expiry, @"^(\d{2})/(\d{2})$");
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var month) || month < 1 || month > 12)
            {
                return PaymentCardParsed.Invalid("Enter an expiry date as MM/YY.");
            }

            var year = 2000 + int.Parse(match.Groups[2].Value);
            var end = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            if (end.Date < DateTimeHelper.MalaysiaNow().Date)
            {
                return PaymentCardParsed.Invalid("This card is expired.");
            }

            var holder = (request.HolderName ?? "").Trim().ToUpperInvariant();
            if (holder.Length == 0)
            {
                return PaymentCardParsed.Invalid("Enter the name printed on the card.");
            }

            if (holder.Length < 2 || holder.Length > 26 || !System.Text.RegularExpressions.Regex.IsMatch(holder, @"^[A-Z\s'.-]+$"))
            {
                return PaymentCardParsed.Invalid("Use letters only, the way the name appears on the card.");
            }

            var cvv = new string((request.Cvv ?? "").Where(char.IsDigit).ToArray());
            var amex = number.StartsWith("34") || number.StartsWith("37");
            if (cvv.Length == 0)
            {
                return PaymentCardParsed.Invalid("Enter the security code.");
            }

            if (amex && cvv.Length != 4)
            {
                return PaymentCardParsed.Invalid("American Express uses the 4-digit code on the front of the card.");
            }

            if (!amex && cvv.Length != 3)
            {
                return PaymentCardParsed.Invalid("Enter the 3-digit code on the back of the card.");
            }

            return new PaymentCardParsed(number, $"{match.Groups[1].Value}/{match.Groups[2].Value}", holder, cvv, number.StartsWith('4') ? "Visa" : "Mastercard");
        }

        private static bool PassesLuhn(string digits)
        {
            var sum = 0;
            var alternate = false;
            for (var index = digits.Length - 1; index >= 0; index--)
            {
                var value = digits[index] - '0';
                if (alternate)
                {
                    value *= 2;
                    if (value > 9)
                    {
                        value -= 9;
                    }
                }

                sum += value;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        private static PaymentCardResponse ToPaymentCardResponse(PaymentCard card)
        {
            return new PaymentCardResponse
            {
                Id = card.Id,
                Brand = card.Brand,
                Last4 = card.Last4,
                Expiry = card.Expiry,
                HolderName = card.HolderName,
                IsDefault = card.IsDefault,
            };
        }

        private readonly record struct PaymentCardParsed(string Number, string Expiry, string Holder, string Cvv, string Brand, string? Error = null)
        {
            public string Last4 => Number.Length >= 4 ? Number[^4..] : Number;

            public static PaymentCardParsed Invalid(string error) => new("", "", "", "", "", error);
        }

        private static string? PasswordRuleError(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8 || password.Length > 72)
            {
                return "Use 8 to 72 characters.";
            }

            if (!password.Any(char.IsLower)) return "Add a lowercase letter.";
            if (!password.Any(char.IsUpper)) return "Add a capital letter.";
            if (!password.Any(char.IsDigit)) return "Add a number.";
            if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return "Add a symbol.";
            return null;
        }

        private string HashPassword(User user, string password)
        {
            return _passwordHasher.HashPassword(user, password);
        }

        private bool VerifyPassword(User user, string password)
        {
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, password);
            return result == PasswordVerificationResult.Success;
        }
    }
}
