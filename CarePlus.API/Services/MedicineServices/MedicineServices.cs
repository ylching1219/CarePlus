using CarePlus.API.DTO.MedicineDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.Medicine;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.MedicineServices.Interface;

namespace CarePlus.API.Services.MedicineServices
{
    public class MedicineServices : IMedicineServices
    {
        private readonly IMedicineRepository _medicineRepo;
        private readonly ICounterServices _counterService;
        private readonly IWebHostEnvironment _environment;

        public MedicineServices(
            IMedicineRepository medicineRepo,
            ICounterServices counterService,
            IWebHostEnvironment environment)
        {
            _medicineRepo = medicineRepo;
            _counterService = counterService;
            _environment = environment;
        }

        public async Task<ApiResponse<object>> CreateMedicineAsync(
            MedicineRegisterRequest request)
        {
            try
            {
                var medicine = new Medicine
                {
                    MedicineId = await _counterService.GenerateNextIdAsync("MedicineId"),
                    MedicineName = request.MedicineName.Trim(),
                    NameZh = string.IsNullOrWhiteSpace(request.NameZh) ? null : request.NameZh.Trim(),
                    Description = request.Description.Trim(),
                    DescriptionZh = string.IsNullOrWhiteSpace(request.DescriptionZh) ? null : request.DescriptionZh.Trim(),
                    Price = request.Price,
                    RequiresPrescription = request.RequiresPrescription,
                    Status = "Available",
                    Pack = "",
                    Form = "tablet",
                    Kind = "analgesic",
                    Method = request.Method?.Trim() ?? "",
                    MethodZh = string.IsNullOrWhiteSpace(request.MethodZh) ? null : request.MethodZh.Trim(),
                    ImageKey = "para"
                };

                await _medicineRepo.CreateMedicineAsync(medicine);

                return ApiResponse<object>.Ok(Map(medicine));
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllMedicinesAsync()
        {
            try
            {
                var medicines = await _medicineRepo.GetAllMedicinesAsync();

                if (medicines.Count == 0)
                {
                    return ApiResponse<object>.Fail("No medicine found", 404);
                }

                var stock = await _medicineRepo.AvailableStockAsync();
                var result = medicines.Select(medicine => Map(medicine, stock.GetValueOrDefault(medicine.MedicineId))).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveMedicineByIdAsync(
            string medicineId)
        {
            try
            {
                var medicine = await _medicineRepo
                    .GetMedicineByIdAsync(medicineId);

                if (medicine == null)
                {
                    return ApiResponse<object>.Fail("Medicine not found", 404);
                }

                var stock = await _medicineRepo.AvailableStockAsync();
                return ApiResponse<object>.Ok(Map(medicine, stock.GetValueOrDefault(medicine.MedicineId)));
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateMedicineAsync(
            MedicineUpdateRequest request)
        {
            try
            {
                var medicine = await _medicineRepo
                    .GetMedicineByIdAsync(request.MedicineId);

                if (medicine == null)
                {
                    return ApiResponse<object>.Fail("Medicine not found", 404);
                }

                if (!string.IsNullOrWhiteSpace(request.MedicineName))
                {
                    medicine.MedicineName = request.MedicineName.Trim();
                }

                if (request.NameZh != null)
                {
                    medicine.NameZh = string.IsNullOrWhiteSpace(request.NameZh)
                        ? null
                        : request.NameZh.Trim();
                }

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    medicine.Description = request.Description.Trim();
                }

                if (request.DescriptionZh != null)
                {
                    medicine.DescriptionZh = string.IsNullOrWhiteSpace(request.DescriptionZh)
                        ? null
                        : request.DescriptionZh.Trim();
                }

                if (request.Method != null)
                {
                    medicine.Method = request.Method.Trim();
                }

                if (request.MethodZh != null)
                {
                    medicine.MethodZh = string.IsNullOrWhiteSpace(request.MethodZh)
                        ? null
                        : request.MethodZh.Trim();
                }

                if (request.Price != null)
                {
                    medicine.Price = request.Price.Value;
                }

                if (request.RequiresPrescription.HasValue)
                {
                    medicine.RequiresPrescription = request.RequiresPrescription.Value;
                }

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    medicine.Status = request.Status.Trim();
                }

                var success = await _medicineRepo.UpdateMedicineAsync(medicine);

                if (!success)
                {
                    return ApiResponse<object>.Fail(
                        "Failed to update medicine", 500);
                }

                return ApiResponse<object>.Ok(null);
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail(
                    "An unexpected error occurred.", 500);
            }
        }

        public async Task<ApiResponse<object>> SaveImageAsync(string medicineId, IFormFile? file)
        {
            try
            {
                if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
                {
                    return ApiResponse<object>.Fail("Use a JPG, PNG, or WebP photo under 5 MB.", 400);
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
                    return ApiResponse<object>.Fail("Use a JPG, PNG, or WebP photo under 5 MB.", 400);
                }

                var medicine = await _medicineRepo.GetMedicineByIdAsync(medicineId);
                if (medicine == null)
                {
                    return ApiResponse<object>.Fail("Medicine not found", 404);
                }

                var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                    ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                    : _environment.WebRootPath;
                var folder = Path.Combine(root, "medicines");
                Directory.CreateDirectory(folder);

                var oldPath = medicine.ImagePath;
                var fileName = UploadFileNameHelper.GenerateOpaque(extension);
                await using (var stream = File.Create(Path.Combine(folder, fileName)))
                {
                    await file.CopyToAsync(stream);
                }

                var relativePath = $"medicines/{fileName}";
                var storedFile = Path.Combine(folder, fileName);
                medicine.ImagePath = relativePath;
                var saved = await _medicineRepo.UpdateMedicineAsync(medicine);
                if (!saved)
                {
                    TryDelete(storedFile);
                    return ApiResponse<object>.Fail("Failed to save the medicine photo.", 500);
                }

                DeleteImageFile(oldPath);
                return ApiResponse<object>.Ok(new { imagePath = relativePath });
            }
            catch (Exception)
            {
                return ApiResponse<object>.Fail("An unexpected error occurred.", 500);
            }
        }

        public async Task<string?> ResolveStoredImageAsync(string medicineId)
        {
            var medicine = await _medicineRepo.GetMedicineByIdAsync(medicineId);
            if (string.IsNullOrWhiteSpace(medicine?.ImagePath)
                || !medicine.ImagePath.StartsWith("medicines/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return ResolveImageFile(Path.GetFileName(medicine.ImagePath));
        }

        private string? ResolveImageFile(string fileName)
        {
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
            {
                return null;
            }

            var root = string.IsNullOrWhiteSpace(_environment.WebRootPath)
                ? Path.Combine(_environment.ContentRootPath, "wwwroot")
                : _environment.WebRootPath;
            var folder = Path.GetFullPath(Path.Combine(root, "medicines"));
            var path = Path.GetFullPath(Path.Combine(folder, safeName));
            if (!path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                return null;
            }

            return path;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // A leftover file must not block the error response.
            }
        }

        private void DeleteImageFile(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("medicines/", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var fileName = Path.GetFileName(relativePath);
                var path = ResolveImageFile(fileName);
                if (path != null)
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // A leftover file must not block saving the new photo.
            }
        }

        private static MedicineRetrieveResponse Map(Medicine medicine, int stock = 0) => new()
        {
            MedicineId = medicine.MedicineId,
            MedicineName = medicine.MedicineName,
            NameZh = medicine.NameZh,
            Description = medicine.Description,
            DescriptionZh = medicine.DescriptionZh,
            Pack = medicine.Pack,
            PackZh = medicine.PackZh,
            Form = medicine.Form,
            Kind = medicine.Kind,
            Method = medicine.Method,
            MethodZh = medicine.MethodZh,
            ImageKey = medicine.ImageKey,
            ImagePath = medicine.ImagePath,
            Price = medicine.Price,
            RequiresPrescription = medicine.RequiresPrescription,
            Status = medicine.Status,
            Stock = stock
        };
    }
}
