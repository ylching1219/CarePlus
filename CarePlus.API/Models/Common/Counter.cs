using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarePlus.API.Models.Common
{
    [Table("Counters")]
    [Index(nameof(Key),IsUnique = true)]
    public class Counter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = null!;

        public long Value { get; set; }

        public int Step { get; set; }

        [MaxLength(20)]
        public string Prefix { get; set; } = string.Empty;

        public int Length { get; set; }

        public char PaddingChar { get; set; }

        public bool IsActive { get; set; } = true;

        [ConcurrencyCheck]
        public long Version { get; set; } = 0;

        [NotMapped]
        public string FormattedValue => $"{Prefix}{Value.ToString().PadLeft(Length, PaddingChar)}";

    }
}
