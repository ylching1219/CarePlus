using CarePlus.API.DTO.MedicineOrderDTO;
using CarePlus.API.Services.MedicineOrderServices.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarePlus.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Pharmacist")]
[Route("api/MedicineOrder/Staff")]
public class StaffMedicineOrderController(IMedicineOrderServices orders) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List()
    {
        return Ok(await orders.ListForStaffAsync());
    }

    [HttpPatch("{orderId}/Status")]
    public async Task<IActionResult> UpdateStatus(string orderId, MedicineOrderStatusUpdateRequest request)
    {
        return Ok(await orders.UpdateStatusAsync(orderId, request.Status));
    }
}
