using CarePlus.API.DTO.MedicineBatchDTO;
using CarePlus.API.Extension;
using CarePlus.API.Models.MedicineBatch;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.MedicineBatchServices.Interface;

namespace CarePlus.API.Services.MedicineBatchServices
{
    public class MedicineBatchServices : IMedicineBatchServices
    {
        private readonly IMedicineBatchRepository _medicineBatchRepo;
        private readonly IMedicineRepository _medicineRepo;

        public MedicineBatchServices(
            IMedicineBatchRepository medicineBatchRepo,
            IMedicineRepository medicineRepo)
        {
            _medicineBatchRepo = medicineBatchRepo;
            _medicineRepo = medicineRepo;
        }

        public async Task<ApiResponse<object>> CreateMedicineBatchAsync(
            MedicineBatchRegisterRequest request)
        {
            try
            {
                var medicineId = request.MedicineId.Trim();
                var medicine = await _medicineRepo.GetMedicineByIdAsync(medicineId);

                if (medicine == null)
                {
                    return ApiResponse<object>.Fail("Medicine not found", 404);
                }

                var medicineBatch = new MedicineBatch
                {
                    BatchId = $"BAT-{DateTimeHelper.MalaysiaNow():yyyyMMddHHmmss}-{medicineId}",
                    MedicineId = medicineId,
                    StockQuantity = request.StockQuantity,
                    ExpiryDate = request.ExpiryDate
                };

                await _medicineBatchRepo.CreateMedicineBatchAsync(medicineBatch);

                return ApiResponse<object>.Ok(new MedicineBatchRegisterResponse
                {
                    BatchId = medicineBatch.BatchId,
                    MedicineId = medicineBatch.MedicineId,
                    StockQuantity = medicineBatch.StockQuantity,
                    ExpiryDate = medicineBatch.ExpiryDate
                });
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveAllMedicineBatchesAsync()
        {
            try
            {
                var medicineBatches =
                    await _medicineBatchRepo.GetAllMedicineBatchesAsync();

                if (medicineBatches.Count == 0)
                {
                    return ApiResponse<object>.Fail(
                        "No medicine batch found", 404);
                }

                return ApiResponse<object>.Ok(
                    medicineBatches.Select(ToResponse).ToList());
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> RetrieveMedicineBatchByIdAsync(
            string batchId)
        {
            try
            {
                var medicineBatch = await _medicineBatchRepo
                    .GetMedicineBatchByIdAsync(batchId.Trim());

                if (medicineBatch == null)
                {
                    return ApiResponse<object>.Fail(
                        "Medicine batch not found", 404);
                }

                return ApiResponse<object>.Ok(ToResponse(medicineBatch));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        public async Task<ApiResponse<object>> UpdateMedicineBatchAsync(
            MedicineBatchUpdateRequest request)
        {
            try
            {
                var medicineBatch = await _medicineBatchRepo
                    .GetMedicineBatchByIdAsync(request.BatchId.Trim());

                if (medicineBatch == null)
                {
                    return ApiResponse<object>.Fail(
                        "Medicine batch not found", 404);
                }

                if (request.StockQuantity.HasValue)
                {
                    medicineBatch.StockQuantity = request.StockQuantity.Value;
                }

                if (request.ExpiryDate.HasValue)
                {
                    medicineBatch.ExpiryDate = request.ExpiryDate.Value;
                }

                var success = await _medicineBatchRepo
                    .UpdateMedicineBatchAsync(medicineBatch);

                if (!success)
                {
                    return ApiResponse<object>.Fail(
                        "Failed to update medicine batch", 500);
                }

                return ApiResponse<object>.Ok(ToResponse(medicineBatch));
            }
            catch (Exception ex)
            {
                return ApiResponse<object>.Fail(
                    $"An error occurred: {ex.Message}", 500);
            }
        }

        private static MedicineBatchRetrieveResponse ToResponse(
            MedicineBatch medicineBatch)
        {
            return new MedicineBatchRetrieveResponse
            {
                BatchId = medicineBatch.BatchId,
                MedicineId = medicineBatch.MedicineId,
                StockQuantity = medicineBatch.StockQuantity,
                ExpiryDate = medicineBatch.ExpiryDate
            };
        }
    }
}
