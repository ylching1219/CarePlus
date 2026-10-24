using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.Cart;
using CarePlus.API.Models.CartItem;
using CarePlus.API.Models.Common;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.DispenseRec;
using CarePlus.API.Models.FamilyGroup;
using CarePlus.API.Models.FamilyMember;
using CarePlus.API.Models.Invoice;
using CarePlus.API.Models.Leave;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Models.Medicine;
using CarePlus.API.Models.MedicineBatch;
using CarePlus.API.Models.MedicineOrder;
using CarePlus.API.Models.Notification;
using CarePlus.API.Models.OrderItem;
using CarePlus.API.Models.Payment;
using CarePlus.API.Models.PersonalHealthRec;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Models.Queue;
using CarePlus.API.Models.Room;
using CarePlus.API.Models.RoomAssg;
using CarePlus.API.Models.User;
using CarePlus.API.Models.Patient;
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
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<PersonalHealthRec> PersonalHealthRecords { get; set; } = null!;
        public DbSet<QueueEntry> QueueEntries { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<FamilyGroup> FamilyGroups { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<Cart> Carts { get; set; } = null!;
        public DbSet<CartItem> CartItems { get; set; } = null!;
        public DbSet<MedicineOrder> MedicineOrders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<Counter> Counters { get; set; } = null!;
        public DbSet<LoginActivity> LoginActivities { get; set; } = null!;
        public DbSet<DeliveryAddress> DeliveryAddresses { get; set; } = null!;
        public DbSet<AppSetting> AppSettings { get; set; } = null!;
        public DbSet<PaymentCard> PaymentCards { get; set; } = null!;


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

            modelBuilder.Entity<DeliveryAddress>()
                .HasOne(address => address.Patient)
                .WithMany()
                .HasForeignKey(address => address.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Id).HasColumnOrder(0);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.PatientId).HasColumnOrder(1);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Label).HasMaxLength(20).HasColumnOrder(2);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Unit).HasMaxLength(30).HasColumnOrder(3);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Address).HasMaxLength(200).HasColumnOrder(4);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.IsDefault).HasColumnOrder(5);
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Latitude).HasColumnType("double");
            modelBuilder.Entity<DeliveryAddress>().Property(address => address.Longitude).HasColumnType("double");

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

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);

            modelBuilder.Entity<Medicine>().Property(m => m.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Medicine>().Property(m => m.Description).HasMaxLength(500);
            modelBuilder.Entity<Medicine>().Property(m => m.NameZh).HasMaxLength(100);
            modelBuilder.Entity<Medicine>().Property(m => m.DescriptionZh).HasMaxLength(500);
            modelBuilder.Entity<Medicine>().Property(m => m.Pack).HasMaxLength(40).HasDefaultValue("");
            modelBuilder.Entity<Medicine>().Property(m => m.PackZh).HasMaxLength(40);
            modelBuilder.Entity<Medicine>().Property(m => m.Form).HasMaxLength(20).HasDefaultValue("tablet");
            modelBuilder.Entity<Medicine>().Property(m => m.Kind).HasMaxLength(30).HasDefaultValue("analgesic");
            modelBuilder.Entity<Medicine>().Property(m => m.Method).HasMaxLength(160).HasDefaultValue("");
            modelBuilder.Entity<Medicine>().Property(m => m.MethodZh).HasMaxLength(160);
            modelBuilder.Entity<Medicine>().Property(m => m.ImageKey).HasMaxLength(10).HasDefaultValue("para");
            MedicineSeed.Apply(modelBuilder);

            modelBuilder.Entity<MedicineBatch>()
                .HasOne(batch => batch.Medicine)
                .WithMany()
                .HasForeignKey(batch => batch.MedicineId);

            modelBuilder.Entity<FamilyGroup>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.CreatedBy);

            modelBuilder.Entity<FamilyMember>()
                .HasOne(f=> f.FamilyGroup)
                .WithMany()
                .HasForeignKey(f => f.FamilyGroupId);

            modelBuilder.Entity<FamilyMember>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.PatientId);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Patient)
                .WithOne()
                .HasForeignKey<Cart>(c => c.PatientId);

            modelBuilder.Entity<CartItem>()
                .HasOne(c=> c.Cart)
                .WithMany()
                .HasForeignKey(c=> c.CartId);

            modelBuilder.Entity<CartItem>()
                .HasOne(c=> c.Medicine)
                .WithMany()
                .HasForeignKey(c=>c.MedicineId);

            modelBuilder.Entity<MedicineOrder>()
                .HasOne(m=>m.Patient)
                .WithMany()
                .HasForeignKey(m=>m.PatientId);
            modelBuilder.Entity<MedicineOrder>()
                .HasOne(m => m.ChosenAddress)
                .WithMany()
                .HasForeignKey(m => m.DeliveryAddressId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
            modelBuilder.Entity<MedicineOrder>().Property(o => o.Fulfilment).HasMaxLength(20).HasDefaultValue("pickup");
            modelBuilder.Entity<MedicineOrder>().Property(o => o.PaymentMethod).HasMaxLength(80).HasDefaultValue("");
            modelBuilder.Entity<MedicineOrder>().Property(o => o.DeliveryAddress).HasMaxLength(300);
            modelBuilder.Entity<MedicineOrder>().Property(o => o.DeliveryFee).HasPrecision(10, 2).HasDefaultValue(0m);
            modelBuilder.Entity<MedicineOrder>().Property(o => o.Latitude).HasColumnType("double");
            modelBuilder.Entity<MedicineOrder>().Property(o => o.Longitude).HasColumnType("double");

            modelBuilder.Entity<OrderItem>()
                .HasOne(m=>m.Order)
                .WithMany()
                .HasForeignKey(m=>m.OrderId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(m=>m.Medicine)
                .WithMany()
                .HasForeignKey(m=>m.MedicineId)
                .IsRequired(false);
            modelBuilder.Entity<OrderItem>().Property(o => o.ItemName).HasMaxLength(120).HasDefaultValue("");
            modelBuilder.Entity<OrderItem>().Property(o => o.Detail).HasMaxLength(200);
            modelBuilder.Entity<OrderItem>().Property(o => o.UnitPrice).HasPrecision(18, 2).HasDefaultValue(0m);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Patient)
                .WithMany()
                .HasForeignKey(i=>i.PatientId);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.MedicineOrder)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .IsRequired(false);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.MedicalRecord)
                .WithMany()
                .HasForeignKey(i => i.MedicalRecordId)
                .IsRequired(false);

            modelBuilder.Entity<Patient>()
                .Property(p => p.DateOfBirth)
                .HasColumnType("date");

            modelBuilder.Entity<LoginActivity>()
                .HasOne(activity => activity.User)
                .WithMany()
                .HasForeignKey(activity => activity.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppSetting>()
                .HasIndex(setting => setting.UserId)
                .IsUnique();

            modelBuilder.Entity<AppSetting>()
                .HasOne(setting => setting.User)
                .WithMany()
                .HasForeignKey(setting => setting.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AppSetting>()
                .Property(setting => setting.Appearance)
                .HasDefaultValue("light");

            modelBuilder.Entity<AppSetting>()
                .Property(setting => setting.Notification)
                .HasDefaultValue("all");

            modelBuilder.Entity<PaymentCard>()
                .HasOne(card => card.User)
                .WithMany()
                .HasForeignKey(card => card.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentCard>()
                .HasIndex(card => card.UserId);

            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue("Active");
            modelBuilder.Entity<Appointment>().Property(a => a.Status).HasDefaultValueSql("('Scheduled')");
            modelBuilder.Entity<Appointment>().Property(a => a.CheckInStatus).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<ClinicRoom>().Property(r => r.Status).HasDefaultValueSql("('Available')");
            modelBuilder.Entity<DispenseRec>().Property(d => d.Status).HasDefaultValueSql("('Dispensed')");
            modelBuilder.Entity<FamilyGroup>().Property(f => f.MemberCount).HasDefaultValue(1);
            modelBuilder.Entity<FamilyMember>().Property(f => f.AccessLevel).HasDefaultValueSql("('View')");
            modelBuilder.Entity<FamilyMember>().Property(f => f.Status).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Invoice>().Property(i => i.PaymentStatus).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Invoice>().Property(i => i.DueDate).HasColumnType("date");
            modelBuilder.Entity<Invoice>().Property(i => i.DoctorName).HasMaxLength(120);
            modelBuilder.Entity<Invoice>().Property(i => i.VisitType).HasMaxLength(80);
            modelBuilder.Entity<Invoice>().Property(i => i.RecordRef).HasMaxLength(40);
            modelBuilder.Entity<Leave>().Property(l => l.Status).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Medicine>().Property(m => m.Status).HasDefaultValueSql("('Available')");
            modelBuilder.Entity<MedicineOrder>().Property(o => o.Status).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Notification>().Property(n => n.Status).HasDefaultValueSql("('Unread')");
            modelBuilder.Entity<Payment>().Property(p => p.Status).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Payment>().Property(p => p.DueDate).HasColumnType("date");
            modelBuilder.Entity<Prescription>().Property(p => p.Status).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<QueueEntry>().Property(q => q.Status).HasDefaultValueSql("('Waiting')");
            modelBuilder.Entity<WorkSchedule>().Property(w => w.Status).HasDefaultValueSql("('Active')");
            modelBuilder.Entity<Counter>().Property(c => c.Prefix).HasDefaultValue("");
            modelBuilder.Entity<Counter>().Property(c => c.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<Counter>().HasData(
                new Counter
                {
                    Id = 1,
                    Key = "PatientId",
                    Value = 0,
                    Step = 1,
                    Prefix = "P",
                    Length = 6,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 2,
                    Key = "MedicineId",
                    Value = 6,
                    Step = 1,
                    Prefix = "Med",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 3,
                    Key = "CartId",
                    Value = 0,
                    Step = 1,
                    Prefix = "Cart",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 4,
                    Key = "CartItemId",
                    Value = 0,
                    Step = 1,
                    Prefix = "CartI",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 5,
                    Key = "OrderId",
                    Value = 0,
                    Step = 1,
                    Prefix = "Ord",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 6,
                    Key = "OrderItemId",
                    Value = 0,
                    Step = 1,
                    Prefix = "OrdI",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 7,
                    Key = "InvoiceId",
                    Value = 0,
                    Step = 1,
                    Prefix = "Inv",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 8,
                    Key = "PaymentId",
                    Value = 0,
                    Step = 1,
                    Prefix = "Pay",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                });

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
                    entry.Entity.CreateDate = DateTimeHelper.MalaysiaNow();
                    entry.Entity.CreatedBy = "System"; 
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdateDate = DateTimeHelper.MalaysiaNow();
                    entry.Entity.UpdatedBy = "System"; 
                }
                else if (entry.State == EntityState.Deleted)
                {
                    entry.Entity.DeleteDate = DateTimeHelper.MalaysiaNow();
                    entry.Entity.DeletedBy = "System";
                }
            }
        }
    }
}
