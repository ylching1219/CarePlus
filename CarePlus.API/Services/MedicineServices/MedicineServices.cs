using CarePlus.API.DTO.MedicineDTO;
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

        public MedicineServices(
            IMedicineRepository medicineRepo,
            ICounterServices counterService)
        {
            _medicineRepo = medicineRepo;
            _counterService = counterService;
        }

        public async Task<ApiResponse<object>> CreateMedicineAsync(
            MedicineRegisterRequest request)
        {
            try
            {
                var medicine = new Medicine
                {
                    MedicineId = await _counterService.GenerateNextIdAsync("MedicineId"),
                    MedicineName = request.MedicineName,
                    Description = request.Description,
                    Price = request.Price,
                    Status = "Available",
                    Pack = "",
                    Form = "tablet",
                    Kind = "analgesic",
                    Method = "",
                    ImageKey = "para"
                };

                await _medicineRepo.CreateMedicineAsync(medicine);

                return ApiResponse<object>.Ok(Map(medicine));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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

                var result = medicines.Select(Map).ToList();

                return ApiResponse<object>.Ok(result);
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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

                return ApiResponse<object>.Ok(Map(medicine));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
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

                if (!string.IsNullOrWhiteSpace(request.Description))
                {
                    medicine.Description = request.Description.Trim();
                }

                if (request.Price != null)
                {
                    medicine.Price = request.Price.Value;
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
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        private static MedicineRetrieveResponse Map(Medicine medicine) => new()
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
            Price = medicine.Price,
            Status = medicine.Status
        };
    }
}
