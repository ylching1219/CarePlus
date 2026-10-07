using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.Common;
using CarePlus.API.Models.DispenseRec;
using CarePlus.API.Models.Leave;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Models.Medicine;
using CarePlus.API.Models.MedicineBatch;
using CarePlus.API.Models.PersonalHealthRec;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Models.Queue;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.User;
using CarePlus.API.Models.User.Patient;
using CarePlus.API.Models.User.Staff;
using CarePlus.API.Models.User.Staff.Admin;
using CarePlus.API.Models.User.Staff.Doctor;
using CarePlus.API.Models.User.Staff.Pharmacist;
using CarePlus.API.Models.WorkSch;
using Microsoft.EntityFrameworkCore;
using System.Threading;

namespace CarePlus.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Staff> Staffs { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Pharmacist> Pharmacists { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Leave> LeaveApplications { get; set; } = null!;
        public DbSet<WorkSchedule> WorkSchedules { get; set; } = null!;
        public DbSet<ClinicRoom> ClinicRooms { get; set; } = null!;
        public DbSet<RoomAssignment> RoomAssignments { get; set; } = null!;
        public DbSet<Medicine> Medicines { get; set; } = null!;
        public DbSet<MedicineBatch> MedicineBatches { get; set; } = null!;  
        public DbSet<MedRecord> MedicalRecords { get; set; } = null!;
        public DbSet<Prescription> Prescriptions { get; set; } = null!;
        public DbSet<PrescriptionItem> PrescriptionItems { get; set; } = null!;
        public DbSet<DispenseRec> DispenseRecords { get; set; } = null!;



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Staff>().ToTable("Staffs");
            modelBuilder.Entity<Doctor>().ToTable("Doctors");
            modelBuilder.Entity<Pharmacist>().ToTable("Pharmacists");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId);

            modelBuilder.Entity<Staff>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Staff>(s => s.UserId);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId);

            modelBuilder.Entity<DispenseRec>()
                .HasOne(d => d.Pharmacist)
                .WithMany()
                .HasForeignKey(d => d.PharmacistId);

            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Staff)
                .WithMany()
                .HasForeignKey(l => l.StaffId);

            modelBuilder.Entity<MedRecord>()
                .HasOne(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId);

            modelBuilder.Entity<MedRecord>()
                .HasOne(m => m.Doctor)
                .WithMany()
                .HasForeignKey(m => m.DoctorId);

            modelBuilder.Entity<PersonalHealthRec>()
                .HasOne(h => h.Patient)
                .WithMany()
                .HasForeignKey(h => h.PatientId);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId);

            modelBuilder.Entity<Prescription>()
                .HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId);

            modelBuilder.Entity<QueueEntry>()
                .HasOne(q => q.Patient)
                .WithMany()
                .HasForeignKey(q => q.PatientId);

            modelBuilder.Entity<RoomAssignment>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId);

            modelBuilder.Entity<WorkSchedule>()
                .HasOne(w => w.Staff)
                .WithMany()
                .HasForeignKey(w => w.StaffId);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditTrail();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            UpdateAuditTrail();
            return base.SaveChanges();
        }

        private void UpdateAuditTrail()
        {
            var entries = ChangeTracker.Entries<AuditTrail>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreateDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = "System"; 
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateDate = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = "System"; 
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.Entity.DeleteDate = DateTime.UtcNow;
                    entry.Entity.DeletedBy = "System";
                }
            }
        }
    }
}
