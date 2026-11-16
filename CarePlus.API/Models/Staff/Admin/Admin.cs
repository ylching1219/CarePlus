using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.User.Staff.Admin
{
    [Table("Admins")]
    public class Admin : Staff
    {
    }
}
