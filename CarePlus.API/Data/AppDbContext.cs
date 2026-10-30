using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.Announcement;
using CarePlus.API.Models.Cart;
using CarePlus.API.Models.CartItem;
using CarePlus.API.Models.Common;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.DispenseRec;
using CarePlus.API.Models.FamilyGroup;
using CarePlus.API.Models.FamilyMember;
using CarePlus.API.Models.FamilyUseRequest;
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
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Security.Claims;

namespace CarePlus.API.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private bool _isSavingWithAudit;

        private static readonly HashSet<Type> AuditExcludedTypes = new()
        {
            typeof(AuditLog),
            typeof(Counter),
            typeof(AppSetting),
            typeof(Cart),
            typeof(CartItem),
            typeof(Notification),
            typeof(LoginActivity)
        };

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            IHttpContextAccessor? httpContextAccessor = null) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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
        public DbSet<VisitType> VisitTypes { get; set; } = null!;
        public DbSet<PersonalHealthRec> PersonalHealthRecords { get; set; } = null!;
        public DbSet<QueueEntry> QueueEntries { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<ClinicAnnouncement> ClinicAnnouncements { get; set; } = null!;
        public DbSet<FamilyGroup> FamilyGroups { get; set; } = null!;
        public DbSet<FamilyMember> FamilyMembers { get; set; } = null!;
        public DbSet<FamilyUseRequest> FamilyUseRequests { get; set; } = null!;
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
        public DbSet<TwoFactorChallenge> TwoFactorChallenges { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue("Active");

            modelBuilder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne()
                .HasForeignKey<Patient>(p => p.UserId);
            modelBuilder.Entity<Patient>()
                .Property(p => p.DateOfBirth)
                .HasColumnType("date");

            modelBuilder.Entity<Staff>().ToTable("Staffs");
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Staff>(s => s.UserId);
            modelBuilder.Entity<Doctor>().ToTable("Doctors");
            modelBuilder.Entity<Pharmacist>().ToTable("Pharmacists");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany()
                .HasForeignKey(a => a.PatientId);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany()
                .HasForeignKey(a => a.DoctorId);
            modelBuilder.Entity<Appointment>()
                .Property(a => a.AppointmentDate)
                .HasColumnType("date");
            modelBuilder.Entity<Appointment>()
                .Property(a => a.AppointmentTime)
                .HasColumnType("time");
            modelBuilder.Entity<Appointment>().Property(a => a.Status).HasDefaultValueSql("('Scheduled')");
            modelBuilder.Entity<Appointment>().Property(a => a.CheckInStatus).HasDefaultValueSql("('Pending')");

            modelBuilder.Entity<DispenseRec>()
                .HasOne(d => d.Pharmacist)
                .WithMany()
                .HasForeignKey(d => d.PharmacistId);
            modelBuilder.Entity<DispenseRec>().Property(d => d.Status).HasDefaultValueSql("('Dispensed')");

            modelBuilder.Entity<Leave>()
                .HasOne(l => l.Staff)
                .WithMany()
                .HasForeignKey(l => l.StaffId);
            modelBuilder.Entity<Leave>().Property(l => l.SupportDocument).HasMaxLength(500);
            modelBuilder.Entity<Leave>().Property(l => l.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
            modelBuilder.Entity<Leave>().Property(l => l.Status).HasDefaultValueSql("('Pending')");

            modelBuilder.Entity<ClinicRoom>().Property(r => r.Status).HasDefaultValueSql("('Available')");

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
            modelBuilder.Entity<Prescription>().Property(p => p.Status).HasDefaultValueSql("('Pending')");

            modelBuilder.Entity<QueueEntry>()
                .HasOne(q => q.Patient)
                .WithMany()
                .HasForeignKey(q => q.PatientId);
            modelBuilder.Entity<QueueEntry>().Property(q => q.Status).HasDefaultValueSql("('Waiting')");

            modelBuilder.Entity<RoomAssignment>()
                .HasOne(r => r.Doctor)
                .WithMany()
                .HasForeignKey(r => r.DoctorId);
            modelBuilder.Entity<RoomAssignment>()
                .Property(r => r.AssignmentDate)
                .HasConversion(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    value => DateOnly.FromDateTime(value))
                .HasColumnType("date");
            modelBuilder.Entity<RoomAssignment>()
                .Property(r => r.StartTime)
                .HasConversion(
                    time => time.ToTimeSpan(),
                    value => TimeOnly.FromTimeSpan(value))
                .HasColumnType("time");
            modelBuilder.Entity<RoomAssignment>()
                .Property(r => r.EndTime)
                .HasConversion(
                    time => time.ToTimeSpan(),
                    value => TimeOnly.FromTimeSpan(value))
                .HasColumnType("time");

            modelBuilder.Entity<WorkSchedule>()
                .HasOne(w => w.Staff)
                .WithMany()
                .HasForeignKey(w => w.StaffId);
            modelBuilder.Entity<WorkSchedule>()
                .Property(w => w.WorkingDate)
                .HasConversion(
                    date => date.ToDateTime(TimeOnly.MinValue),
                    value => DateOnly.FromDateTime(value))
                .HasColumnType("date");
            modelBuilder.Entity<WorkSchedule>().Property(w => w.Shift).HasMaxLength(20).HasDefaultValue("Morning");
            modelBuilder.Entity<WorkSchedule>().Property(w => w.Status).HasDefaultValueSql("('Active')");

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId);
            modelBuilder.Entity<Notification>().Property(n => n.Status).HasMaxLength(20).HasDefaultValueSql("('Unread')");
            modelBuilder.Entity<Notification>().Property(n => n.Actor).HasMaxLength(255).HasDefaultValue("CarePlus Clinic");
            modelBuilder.Entity<Notification>().Property(n => n.Initials).HasMaxLength(8).HasDefaultValue("CC");
            modelBuilder.Entity<Notification>().Property(n => n.Href).HasMaxLength(500).HasDefaultValue("/notifications");

            modelBuilder.Entity<ClinicAnnouncement>().Property(row => row.AnnouncementId).HasMaxLength(80);
            modelBuilder.Entity<ClinicAnnouncement>().Property(row => row.IsPublished).HasDefaultValue(true);
            modelBuilder.Entity<ClinicAnnouncement>().HasIndex(row => new { row.IsPublished, row.PublishedAt });

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
            modelBuilder.Entity<Medicine>().Property(m => m.Status).HasDefaultValueSql("('Available')");
            MedicineSeed.Apply(modelBuilder);

            modelBuilder.Entity<MedicineBatch>()
                .HasOne(batch => batch.Medicine)
                .WithMany()
                .HasForeignKey(batch => batch.MedicineId);

            modelBuilder.Entity<FamilyGroup>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.CreatedBy);
            modelBuilder.Entity<FamilyGroup>().Property(f => f.MemberCount).HasDefaultValue(1);

            modelBuilder.Entity<FamilyMember>()
                .HasOne(f=> f.FamilyGroup)
                .WithMany()
                .HasForeignKey(f => f.FamilyGroupId);

            modelBuilder.Entity<FamilyMember>()
                .HasOne(f => f.Patient)
                .WithMany()
                .HasForeignKey(f => f.PatientId);
            modelBuilder.Entity<FamilyMember>()
                .HasIndex(f => new { f.FamilyGroupId, f.PatientId })
                .IsUnique();
            modelBuilder.Entity<FamilyMember>()
                .Property(f => f.DisplayName)
                .HasMaxLength(100);

            modelBuilder.Entity<FamilyUseRequest>()
                .HasOne(request => request.FamilyGroup)
                .WithMany()
                .HasForeignKey(request => request.FamilyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FamilyUseRequest>()
                .HasOne(request => request.FromPatient)
                .WithMany()
                .HasForeignKey(request => request.FromPatientId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FamilyUseRequest>()
                .HasOne(request => request.TargetPatient)
                .WithMany()
                .HasForeignKey(request => request.TargetPatientId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<FamilyUseRequest>()
                .HasIndex(request => new { request.FamilyGroupId, request.FromPatientId, request.TargetPatientId, request.Status });
            modelBuilder.Entity<FamilyUseRequest>().Property(request => request.Status).HasDefaultValue("Pending");
            modelBuilder.Entity<FamilyMember>().Property(f => f.AccessLevel).HasDefaultValueSql("('View')");
            modelBuilder.Entity<FamilyMember>().Property(f => f.Status).HasDefaultValueSql("('Pending')");

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
            modelBuilder.Entity<MedicineOrder>().Property(o => o.Status).HasDefaultValueSql("('Pending')");

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
            modelBuilder.Entity<Patient>()
                .Property(p => p.IdentityCardNumber)
                .HasMaxLength(20);
            modelBuilder.Entity<Invoice>().Property(i => i.PaymentStatus).HasDefaultValueSql("('Pending')");
            modelBuilder.Entity<Invoice>().Property(i => i.DueDate).HasColumnType("date");
            modelBuilder.Entity<Invoice>().Property(i => i.DoctorName).HasMaxLength(120);
            modelBuilder.Entity<Invoice>().Property(i => i.VisitType).HasMaxLength(80);
            modelBuilder.Entity<Invoice>().Property(i => i.RecordRef).HasMaxLength(40);

            modelBuilder.Entity<LoginActivity>()
                .HasOne(activity => activity.User)
                .WithMany()
                .HasForeignKey(activity => activity.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
            modelBuilder.Entity<AppSetting>()
                .Property(setting => setting.TwoFactorEnabled)
                .HasDefaultValue(false);

            modelBuilder.Entity<AppSetting>()
                .Property(setting => setting.HasCompletedTutorial)
                .HasDefaultValue(false);

            modelBuilder.Entity<TwoFactorChallenge>()
                .HasOne(challenge => challenge.User)
                .WithMany()
                .HasForeignKey(challenge => challenge.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TwoFactorChallenge>()
                .HasIndex(challenge => new { challenge.UserId, challenge.Purpose, challenge.CreatedAt });

            modelBuilder.Entity<PaymentCard>()
                .HasOne(card => card.User)
                .WithMany()
                .HasForeignKey(card => card.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PaymentCard>()
                .HasIndex(card => card.UserId);

            modelBuilder.Entity<User>().Property(u => u.Status).HasDefaultValue("Active");
            modelBuilder.Entity<User>().Property(u => u.EmailVerified).HasDefaultValue(true);
            modelBuilder.Entity<Appointment>().Property(a => a.AppointmentDate).HasColumnType("date");
            modelBuilder.Entity<Appointment>().Property(a => a.AppointmentTime).HasColumnType("time");
            modelBuilder.Entity<Appointment>().Property(a => a.ForName).HasMaxLength(100);
            modelBuilder.Entity<Appointment>().Property(a => a.VisitType).HasMaxLength(80);
            modelBuilder.Entity<Appointment>().Property(a => a.Notes).HasMaxLength(500);
            modelBuilder.Entity<Appointment>().Property(a => a.LastRescheduledAt).HasColumnType("datetime(6)");
            modelBuilder.Entity<VisitType>().Property(type => type.IsActive).HasDefaultValue(true);
            modelBuilder.Entity<VisitType>().Property(type => type.Price).HasPrecision(10, 2);
            modelBuilder.Entity<VisitType>().HasData(
                new VisitType { VisitTypeId = "general", Name = "General consultation", Description = "Cough, fever, skin, or a usual GP visit.", Price = 50m, IsActive = true },
                new VisitType { VisitTypeId = "mental", Name = "Mental wellness", Description = "Stress, sleep, mood, or a quiet talk with a doctor.", Price = 60m, IsActive = true },
                new VisitType { VisitTypeId = "screening", Name = "Body check", Description = "Health screening and routine checks.", Price = 55m, IsActive = true }
            );
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
                    Id = 9,
                    Key = "StaffId",
                    Value = 1,
                    Step = 1,
                    Prefix = "STF",
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
                    Prefix = "MED",
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
                    Prefix = "CART",
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
                    Prefix = "CARTI",
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
                    Prefix = "ORD",
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
                    Prefix = "ORDI",
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
                    Prefix = "INV",
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
                    Prefix = "PAY",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 10,
                    Key = "AppointmentId",
                    Value = 0,
                    Step = 1,
                    Prefix = "Apt",
                    Length = 4,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                },
                new Counter
                {
                    Id = 11,
                    Key = "ScheduleId",
                    Value = 0,
                    Step = 1,
                    Prefix = "SCH",
                    Length = 6,
                    PaddingChar = '0',
                    IsActive = true,
                    Version = 0,
                });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(log => log.LogId);
                entity.Property(log => log.LogId).HasMaxLength(64);
                entity.Property(log => log.UserId).HasMaxLength(100).IsRequired();
                entity.Property(log => log.Action).HasMaxLength(20).IsRequired();
                entity.Property(log => log.Module).HasMaxLength(100).IsRequired();
                entity.Property(log => log.AffectedRecord).HasMaxLength(255).IsRequired();
                entity.Property(log => log.DateTime).HasColumnType("datetime(6)");
                entity.HasIndex(log => log.DateTime);
                entity.HasIndex(log => new { log.Module, log.Action });
            });

        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isSavingWithAudit)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            var pendingAudits = PrepareAuditEntries();
            _isSavingWithAudit = true;

            try
            {
                var result = await base.SaveChangesAsync(cancellationToken);
                AddAuditLogs(pendingAudits);

                if (pendingAudits.Count > 0)
                {
                    await base.SaveChangesAsync(cancellationToken);
                }

                return result;
            }
            finally
            {
                _isSavingWithAudit = false;
            }
        }

        public override int SaveChanges()
        {
            var pendingAudits = PrepareAuditEntries();
            _isSavingWithAudit = true;

            try
            {
                var result = base.SaveChanges();
                AddAuditLogs(pendingAudits);

                if (pendingAudits.Count > 0)
                {
                    base.SaveChanges();
                }

                return result;
            }
            finally
            {
                _isSavingWithAudit = false;
            }
        }

        private List<PendingAudit> PrepareAuditEntries()
        {
            ChangeTracker.DetectChanges();

            var userId = _httpContextAccessor?.HttpContext?.User
                ?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "SYSTEM";

            return ChangeTracker.Entries()
                .Where(entry => !AuditExcludedTypes.Contains(entry.Metadata.ClrType))
                .Where(entry => entry.State is EntityState.Added
                    or EntityState.Modified
                    or EntityState.Deleted)
                .Select(entry => new PendingAudit(
                    entry,
                    userId,
                    entry.State switch
                    {
                        EntityState.Added => "Create",
                        EntityState.Modified => "Update",
                        EntityState.Deleted => "Delete",
                        _ => string.Empty
                    },
                    entry.Metadata.GetTableName()
                        ?? entry.Metadata.ClrType.Name,
                    entry.Metadata.FindPrimaryKey()?.Properties
                        .Select(property => entry.Property(property.Name)
                            .CurrentValue?.ToString())
                        .ToArray() ?? Array.Empty<string?>()))
                .ToList();
        }

        private void AddAuditLogs(IEnumerable<PendingAudit> pendingAudits)
        {
            var occurredAt = DateTimeHelper.MalaysiaNow();
            var logs = pendingAudits.Select(pending => new AuditLog
            {
                LogId = CreateAuditLogId(pending, occurredAt),
                UserId = pending.UserId,
                Action = pending.Action,
                Module = pending.Module,
                AffectedRecord = GetAffectedRecord(pending),
                DateTime = occurredAt
            });

            AuditLogs.AddRange(logs);
        }

        private static string CreateAuditLogId(
            PendingAudit pending,
            DateTime occurredAt)
        {
            var actionCode = pending.Action switch
            {
                "Create" => "C",
                "Update" => "U",
                "Delete" => "D",
                _ => "A"
            };

            var module = new string(
                pending.Module
                    .Where(char.IsLetterOrDigit)
                    .ToArray())
                .ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(module))
            {
                module = "MODULE";
            }

            if (module.Length > 3)
            {
                module = module[..3];
            }

            var uniqueSuffix = Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant();

            return $"AUD-{occurredAt:yyMMdd}-{actionCode}-{module}-{uniqueSuffix}";
        }

        private static string GetAffectedRecord(PendingAudit pending)
        {
            var primaryKey = pending.Entry.Metadata.FindPrimaryKey();
            if (primaryKey == null)
            {
                return pending.Entry.Metadata.ClrType.Name;
            }

            var values = primaryKey.Properties
                .Select((property, index) =>
                {
                    var current = pending.Entry.State == EntityState.Detached
                        ? null
                        : pending.Entry.Property(property.Name)
                            .CurrentValue?.ToString();
                    var original = index < pending.OriginalKeyValues.Length
                        ? pending.OriginalKeyValues[index]
                        : null;
                    return $"{property.Name}={current ?? original ?? "?"}";
                });

            return string.Join(",", values);
        }

        private sealed record PendingAudit(
            EntityEntry Entry,
            string UserId,
            string Action,
            string Module,
            string?[] OriginalKeyValues);
    }
}
