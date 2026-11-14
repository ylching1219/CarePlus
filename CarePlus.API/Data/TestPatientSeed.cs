using CarePlus.API.Extension;
using CarePlus.API.Models.Appointment;
using CarePlus.API.Models.DeliveryAddress;
using CarePlus.API.Models.Invoice;
using CarePlus.API.Models.MedicalRecord;
using CarePlus.API.Models.MedicineOrder;
using CarePlus.API.Models.Notification;
using CarePlus.API.Models.OrderItem;
using CarePlus.API.Models.Patient;
using CarePlus.API.Models.Payment;
using CarePlus.API.Models.PersonalHealthRec;
using CarePlus.API.Models.Prescription;
using CarePlus.API.Models.PrescriptionItem;
using CarePlus.API.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Data;

/// <summary>
/// One-shot seed for patient username "test". Run with: dotnet run --project CarePlus.API -- --seed-test
/// Re-running clears previous TST-* rows for that patient, then inserts fresh samples.
/// </summary>
public static class TestPatientSeed
{
    private const string Username = "test";
    private const string DefaultPassword = "Test@12345";

    public static async Task RunAsync(AppDbContext db, TextWriter log)
    {
        var now = DateTimeHelper.MalaysiaNow();
        var today = now.Date;
        var hasher = new PasswordHasher<User>();

        var doctorId = await db.Doctors.AsNoTracking()
            .OrderBy(row => row.StaffId)
            .Select(row => row.StaffId)
            .FirstOrDefaultAsync();
        if (string.IsNullOrWhiteSpace(doctorId))
        {
            throw new InvalidOperationException("No doctor exists in the database. Seed staff users first.");
        }
        log.WriteLine($"Using doctor {doctorId}.");

        var medicineIds = await db.Medicines.AsNoTracking()
            .Where(row => row.Status == "Available")
            .OrderBy(row => row.MedicineId)
            .Select(row => row.MedicineId)
            .Take(2)
            .ToListAsync();
        if (medicineIds.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 available medicines to seed prescriptions/orders.");
        }

        var roomId = await db.ClinicRooms.AsNoTracking()
            .OrderBy(row => row.RoomId)
            .Select(row => row.RoomId)
            .FirstOrDefaultAsync();

        var user = await db.Users.FirstOrDefaultAsync(row => row.Username == Username);
        if (user == null)
        {
            user = new User
            {
                Username = Username,
                Name = "Test Patient",
                Email = "test@careplus.local",
                Phone = "0123456789",
                Status = "Active",
                EmailVerified = true,
            };
            user.PasswordHash = hasher.HashPassword(user, DefaultPassword);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            db.AppSettings.Add(new AppSetting
            {
                UserId = user.UserId,
                Appearance = "light",
                Language = "en",
                Notification = "all",
                TextSize = "normal",
                TwoFactorEnabled = false,
                HasCompletedTutorial = true,
            });
            await db.SaveChangesAsync();
            log.WriteLine($"Created user '{Username}' (password {DefaultPassword}).");
        }
        else
        {
            log.WriteLine($"Found user '{Username}' (UserId={user.UserId}).");
        }

        var patient = await db.Patients.FirstOrDefaultAsync(row => row.UserId == user.UserId);
        if (patient == null)
        {
            var next = await NextPatientIdAsync(db);
            patient = new Patient
            {
                PatientId = next,
                UserId = user.UserId,
                IdentityCardNumber = "900101145678",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = 'M',
                Address = "12 Jalan Test, 50000 Kuala Lumpur",
                AnnouncementStartAt = now.AddDays(-30),
            };
            db.Patients.Add(patient);
            await db.SaveChangesAsync();
            log.WriteLine($"Created patient {patient.PatientId}.");
        }
        else
        {
            log.WriteLine($"Found patient {patient.PatientId}.");
        }

        await ClearPreviousSeedAsync(db, patient.PatientId, user.UserId);
        await db.SaveChangesAsync();

        // Past completed visit + clinic record + prescription + paid invoice
        var pastDate = today.AddDays(-10);
        var pastApt = new Appointment
        {
            AppointmentId = "TST-APT-PAST",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            RoomId = roomId,
            AppointmentDate = pastDate,
            AppointmentTime = new TimeSpan(10, 0, 0),
            ForName = user.Name ?? "Test Patient",
            VisitType = "VT01",
            Notes = "Seeded past visit for testing.",
            Status = "Completed",
            CheckInStatus = "Completed",
        };
        db.Appointments.Add(pastApt);

        var record = new MedRecord
        {
            MedicalRecordId = "TST-MR-001",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            ConsultationDate = pastDate.AddHours(10),
            Diagnosis = "Upper respiratory tract infection",
            Notes = "Rest, fluids, and finish the antibiotic course.",
        };
        db.MedicalRecords.Add(record);

        var rx = new Prescription
        {
            PrescriptionId = "TST-RX-001",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            MedicalRecordId = record.MedicalRecordId,
            CreatedAt = pastDate.AddHours(10).AddMinutes(20),
            Status = "Dispensed",
        };
        db.Prescriptions.Add(rx);
        db.PrescriptionItems.AddRange(
            new PrescriptionItem
            {
                PrescriptionItemId = "TST-RXI-001",
                PrescriptionId = rx.PrescriptionId,
                MedicineId = medicineIds[0],
                Dosage = "1 capsule, 3 times a day",
                Quantity = 1,
                Instructions = "Finish the course",
            },
            new PrescriptionItem
            {
                PrescriptionItemId = "TST-RXI-002",
                PrescriptionId = rx.PrescriptionId,
                MedicineId = medicineIds[1],
                Dosage = "1 to 2 tablets when needed",
                Quantity = 1,
                Instructions = "For fever or pain",
            });

        var doctorName = await db.Doctors.AsNoTracking()
            .Where(row => row.StaffId == doctorId)
            .Select(row => row.User.Name)
            .FirstOrDefaultAsync() ?? "Clinic doctor";

        var paidInvoice = new Invoice
        {
            InvoiceId = "TST-INV-PAID",
            PatientId = patient.PatientId,
            MedicalRecordId = record.MedicalRecordId,
            TotalAmount = 71.40m,
            IssueAt = pastDate.AddHours(11),
            DueDate = pastDate.AddDays(7),
            DoctorName = doctorName,
            VisitType = "General consultation",
            RecordRef = record.MedicalRecordId,
            PaymentStatus = "Paid",
        };
        db.Invoices.Add(paidInvoice);
        db.Payments.Add(new Payment
        {
            PaymentId = "TST-PAY-001",
            InvoiceId = paidInvoice.InvoiceId,
            TotalAmount = 71.40m,
            PaymentMethod = "Online banking (FPX) · sandbox",
            TransactionDate = pastDate.AddDays(1).AddHours(15),
            DueDate = paidInvoice.DueDate,
            Status = "Paid",
        });

        // Outstanding invoice (due soon) without medical record link optional - use a second completed visit unpaid
        var midDate = today.AddDays(-3);
        var midApt = new Appointment
        {
            AppointmentId = "TST-APT-MID",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            RoomId = roomId,
            AppointmentDate = midDate,
            AppointmentTime = new TimeSpan(14, 30, 0),
            ForName = user.Name ?? "Test Patient",
            VisitType = "VT02",
            Notes = "Seeded unpaid visit.",
            Status = "Completed",
            CheckInStatus = "Completed",
        };
        db.Appointments.Add(midApt);

        var record2 = new MedRecord
        {
            MedicalRecordId = "TST-MR-002",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            ConsultationDate = midDate.AddHours(14).AddMinutes(30),
            Diagnosis = "Routine body check - all clear",
            Notes = "Blood pressure normal. Come back in 6 months.",
        };
        db.MedicalRecords.Add(record2);

        db.Invoices.Add(new Invoice
        {
            InvoiceId = "TST-INV-DUE",
            PatientId = patient.PatientId,
            MedicalRecordId = record2.MedicalRecordId,
            TotalAmount = 58.30m,
            IssueAt = midDate.AddHours(15),
            DueDate = today.AddDays(4),
            DoctorName = doctorName,
            VisitType = "Body check",
            RecordRef = record2.MedicalRecordId,
            PaymentStatus = "Pending",
        });

        // Overdue invoice for payment / lock testing
        db.Invoices.Add(new Invoice
        {
            InvoiceId = "TST-INV-OVER",
            PatientId = patient.PatientId,
            MedicalRecordId = null,
            TotalAmount = 50.00m,
            IssueAt = today.AddDays(-20),
            DueDate = today.AddDays(-8),
            DoctorName = doctorName,
            VisitType = "General consultation",
            RecordRef = null,
            PaymentStatus = "Pending",
        });

        // Upcoming booking
        var upcoming = today.AddDays(3);
        if (upcoming.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            upcoming = upcoming.AddDays(2);
        }
        db.Appointments.Add(new Appointment
        {
            AppointmentId = "TST-APT-NEXT",
            PatientId = patient.PatientId,
            DoctorId = doctorId,
            AppointmentDate = upcoming,
            AppointmentTime = new TimeSpan(9, 30, 0),
            ForName = user.Name ?? "Test Patient",
            VisitType = "VT01",
            Notes = "Seeded upcoming booking.",
            Status = "Scheduled",
            CheckInStatus = "Pending",
        });

        // Personal health record
        db.PersonalHealthRecords.Add(new PersonalHealthRec
        {
            HealthRecId = "TST-PHR-001",
            PatientId = patient.PatientId,
            RecordType = "vitals",
            BloodPressure = "118/76",
            Allergies = "Penicillin (mild rash)",
            HealthConditions = "None chronic",
            MedicalHistory = "Appendectomy 2015",
            Symptoms = null,
            HealthNotes = "Seeded PHR for user-side testing.",
            RecordedAt = today.AddDays(-5),
            CreatedAt = today.AddDays(-5),
        });

        // Delivery address
        db.DeliveryAddresses.Add(new DeliveryAddress
        {
            PatientId = patient.PatientId,
            Label = "Home",
            Unit = "12-3",
            Address = "12 Jalan Test, 50000 Kuala Lumpur",
            IsDefault = true,
            Latitude = 3.1390,
            Longitude = 101.6869,
        });

        // Medicine order (picked up)
        var order = new MedicineOrder
        {
            OrderId = "TST-ORD-001",
            PatientId = patient.PatientId,
            TotalAmount = 21.40m,
            DeliveryFee = 0m,
            OrderDate = pastDate.AddDays(1),
            Status = "picked_up",
            Fulfilment = "pickup",
            PaymentMethod = "Visa ···· 4242 · sandbox",
        };
        db.MedicineOrders.Add(order);
        var medRows = await db.Medicines.AsNoTracking()
            .Where(row => medicineIds.Contains(row.MedicineId))
            .ToDictionaryAsync(row => row.MedicineId);
        db.OrderItems.Add(new OrderItem
        {
            OrderItemId = "TST-ORDI-001",
            OrderId = order.OrderId,
            MedicineId = medicineIds[1],
            ItemName = medRows[medicineIds[1]].MedicineName,
            Detail = $"{medRows[medicineIds[1]].Pack} · Clinic pharmacy",
            UnitPrice = medRows[medicineIds[1]].Price,
            Quantity = 1,
            SubTotal = medRows[medicineIds[1]].Price,
        });
        db.OrderItems.Add(new OrderItem
        {
            OrderItemId = "TST-ORDI-002",
            OrderId = order.OrderId,
            MedicineId = medicineIds[0],
            ItemName = medRows[medicineIds[0]].MedicineName,
            Detail = $"{medRows[medicineIds[0]].Pack} · Clinic pharmacy",
            UnitPrice = medRows[medicineIds[0]].Price,
            Quantity = 1,
            SubTotal = medRows[medicineIds[0]].Price,
        });
        order.TotalAmount = medRows[medicineIds[0]].Price + medRows[medicineIds[1]].Price;
        db.Invoices.Add(new Invoice
        {
            InvoiceId = "TST-INV-ORD",
            PatientId = patient.PatientId,
            OrderId = order.OrderId,
            TotalAmount = order.TotalAmount,
            IssueAt = order.OrderDate,
            DueDate = null,
            DoctorName = null,
            VisitType = null,
            RecordRef = null,
            PaymentStatus = "Paid",
        });
        db.Payments.Add(new Payment
        {
            PaymentId = "TST-PAY-ORD",
            InvoiceId = "TST-INV-ORD",
            TotalAmount = order.TotalAmount,
            PaymentMethod = "Visa ···· 4242 · sandbox",
            TransactionDate = order.OrderDate,
            DueDate = null,
            Status = "Paid",
        });

        // Notifications
        db.Notifications.AddRange(
            new Notification
            {
                NotificationId = "TST-NOTIF-001",
                UserId = user.UserId,
                Title = "Upcoming visit reminder",
                Message = "You have a seeded appointment coming up. Open Visits to review the details.",
                TitleZh = "就诊提醒",
                MessageZh = "你有一个测试预约即将到来。打开“就诊”查看详情。",
                Type = "appointment_reminder",
                Actor = "CarePlus Clinic",
                Initials = "CC",
                Href = "/visits",
                Status = "Unread",
                CreatedAt = now.AddHours(-2),
            },
            new Notification
            {
                NotificationId = "TST-NOTIF-002",
                UserId = user.UserId,
                Title = "Invoice ready",
                Message = "A body-check invoice is waiting. Pay it from Outstanding bills.",
                TitleZh = "账单已就绪",
                MessageZh = "有一张身体检查账单待付款。请到未付账单支付。",
                Type = "payment",
                Actor = "CarePlus Clinic",
                Initials = "CC",
                Href = "/invoices",
                Status = "Unread",
                CreatedAt = now.AddHours(-6),
            });

        await db.SaveChangesAsync();
        log.WriteLine("Seeded appointments, clinic records, invoices, payments, PHR, order, address, and notifications.");
        log.WriteLine("Login: username 'test' (use existing password, or Test@12345 if just created).");
    }

    private static async Task ClearPreviousSeedAsync(AppDbContext db, string patientId, long userId)
    {
        var seedPayments = await db.Payments.Where(row => row.PaymentId.StartsWith("TST-")).ToListAsync();
        db.Payments.RemoveRange(seedPayments);

        var seedOrderItems = await db.OrderItems.Where(row => row.OrderItemId.StartsWith("TST-")).ToListAsync();
        db.OrderItems.RemoveRange(seedOrderItems);

        var seedInvoices = await db.Invoices.Where(row => row.InvoiceId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.Invoices.RemoveRange(seedInvoices);

        var seedOrders = await db.MedicineOrders.Where(row => row.OrderId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.MedicineOrders.RemoveRange(seedOrders);

        var seedRxItems = await db.PrescriptionItems.Where(row => row.PrescriptionItemId.StartsWith("TST-")).ToListAsync();
        db.PrescriptionItems.RemoveRange(seedRxItems);

        var seedRx = await db.Prescriptions.Where(row => row.PrescriptionId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.Prescriptions.RemoveRange(seedRx);

        var seedReviews = await db.DoctorReviews.Where(row => row.MedicalRecordId.StartsWith("TST-")).ToListAsync();
        db.DoctorReviews.RemoveRange(seedReviews);

        var seedRecords = await db.MedicalRecords.Where(row => row.MedicalRecordId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.MedicalRecords.RemoveRange(seedRecords);

        var seedApts = await db.Appointments.Where(row => row.AppointmentId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.Appointments.RemoveRange(seedApts);

        var seedPhr = await db.PersonalHealthRecords.Where(row => row.HealthRecId.StartsWith("TST-") && row.PatientId == patientId).ToListAsync();
        db.PersonalHealthRecords.RemoveRange(seedPhr);

        var seedNotices = await db.Notifications.Where(row => row.NotificationId.StartsWith("TST-") && row.UserId == userId).ToListAsync();
        db.Notifications.RemoveRange(seedNotices);

        // Keep only one seeded Home address: remove prior seed addresses with this unit/label pair.
        var seedAddresses = await db.DeliveryAddresses
            .Where(row => row.PatientId == patientId && row.Label == "Home" && row.Unit == "12-3")
            .ToListAsync();
        db.DeliveryAddresses.RemoveRange(seedAddresses);
    }

    private static async Task<string> NextPatientIdAsync(AppDbContext db)
    {
        var counter = await db.Counters.FirstOrDefaultAsync(row => row.Key == "PatientId");
        if (counter == null)
        {
            throw new InvalidOperationException("PatientId counter is missing.");
        }

        counter.Value += counter.Step;
        await db.SaveChangesAsync();
        return counter.FormattedValue;
    }
}
