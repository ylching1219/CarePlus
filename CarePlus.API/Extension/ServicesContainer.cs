using CarePlus.API.Data;
using CarePlus.API.Repository;
using CarePlus.API.Repository.Interface;
using CarePlus.API.Services.CounterServices;
using CarePlus.API.Services.CounterServices.Interface;
using CarePlus.API.Services.JwtServices;
using CarePlus.API.Services.JWTServices.Interface;
using CarePlus.API.Services.MedicineServices;
using CarePlus.API.Services.MedicineServices.Interface;
using CarePlus.API.Services.MedicineBatchServices;
using CarePlus.API.Services.MedicineBatchServices.Interface;
using CarePlus.API.Services.WorkScheduleServices;
using CarePlus.API.Services.WorkScheduleServices.Interface;
using CarePlus.API.Services.LeaveServices;
using CarePlus.API.Services.LeaveServices.Interface;
using CarePlus.API.Services.CartServices;
using CarePlus.API.Services.CartServices.Interface;
using CarePlus.API.Services.AppointmentServices;
using CarePlus.API.Services.AppointmentServices.Interface;
using CarePlus.API.Services.ClinicRecordServices;
using CarePlus.API.Services.ClinicRecordServices.Interface;
using CarePlus.API.Services.MedicalRecordServices;
using CarePlus.API.Services.MedicalRecordServices.Interface;
using CarePlus.API.Services.ClinicRoomServices;
using CarePlus.API.Services.ClinicRoomServices.Interface;
using CarePlus.API.Services.AuditLogServices;
using CarePlus.API.Services.AuditLogServices.Interface;
using CarePlus.API.Services.InvoiceServices;
using CarePlus.API.Services.InvoiceServices.Interface;
using CarePlus.API.Services.MedicineOrderServices;
using CarePlus.API.Services.MedicineOrderServices.Interface;
using CarePlus.API.Services.PatientServices;
using CarePlus.API.Services.PatientServices.Interface;
using CarePlus.API.Services.FamilyServices;
using CarePlus.API.Services.FamilyServices.Interface;
using CarePlus.API.Services.NotificationServices;
using CarePlus.API.Services.NotificationServices.Interface;
using CarePlus.API.Services.QueueServices;
using CarePlus.API.Services.QueueServices.Interface;
using CarePlus.API.Services.StaffService;
using CarePlus.API.Services.StaffService.Interface;
using CarePlus.API.Services.UserServices;
using CarePlus.API.Services.UserServices.Interface;
using CarePlus.API.Services.EmailServices;
using CarePlus.API.Services.EmailServices.Interface;
using CarePlus.API.Services.EmailVerificationServices;
using CarePlus.API.Services.EmailVerificationServices.Interface;
using CarePlus.API.Services.TwoFactorServices;
using CarePlus.API.Services.TwoFactorServices.Interface;
using CarePlus.API.Services.PasswordResetServices;
using CarePlus.API.Services.PasswordResetServices.Interface;
using CarePlus.API.Services.RegistrationEmailVerificationServices;
using CarePlus.API.Services.RegistrationEmailVerificationServices.Interface;
using CarePlus.API.Services.ChatServices;
using CarePlus.API.Services.ChatServices.Interface;
using CarePlus.API.Services.StripeServices;
using CarePlus.API.Services.StripeServices.Interface;
using CarePlus.API.Options;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Extension
{
    public static class ServicesContainer
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration config)
        {
            //db connection
            var connectString = config.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseMySQL(connectString!));

            //services
            services.AddScoped<IUserServices, UserServices>();
            services.Configure<EmailOptions>(config.GetSection("Email"));
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
            services.AddScoped<ITwoFactorServices, TwoFactorServices>();
            services.AddScoped<IEmailVerificationServices, EmailVerificationServices>();
            services.AddScoped<IPasswordResetServices, PasswordResetServices>();
            services.AddScoped<IRegistrationEmailVerificationServices, RegistrationEmailVerificationServices>();
            services.AddScoped<IPatientServices, PatientServices>();
            services.AddScoped<IFamilyServices, FamilyServices>();
            services.AddScoped<INotificationServices, NotificationServices>();
            services.AddScoped<IQueueServices, QueueServices>();
            services.AddHostedService<AppointmentCheckInMonitor>();
            services.AddScoped<ICounterServices, CounterServices>();
            services.AddScoped<IStaffServices, StaffServices>();
            services.AddScoped<IAdminServices, AdminServices>();
            services.AddScoped<IDoctorServices, DoctorServices>();
            services.AddScoped<IPharmacistServices, PharmacistServices>();
            services.AddScoped<IMedicineServices, MedicineServices>();
            services.AddScoped<IMedicineBatchServices, MedicineBatchServices>();
            services.AddScoped<IWorkScheduleServices, WorkScheduleServices>();
            services.AddScoped<ILeaveServices, LeaveServices>();
            services.AddScoped<IMedicineOrderServices, MedicineOrderServices>();
            services.AddScoped<IInvoiceServices, InvoiceServices>();
            services.AddScoped<IAppointmentServices, AppointmentServices>();
            services.AddScoped<IClinicRecordServices, ClinicRecordServices>();
            services.AddScoped<IMedicalRecordServices, MedicalRecordServices>();
            services.AddScoped<IClinicRoomServices, ClinicRoomServices>();
            services.AddScoped<ICartServices, CartServices>();
            services.AddScoped<IAuditLogServices, AuditLogServices>();
            services.Configure<GroqOptions>(config.GetSection(GroqOptions.SectionName));
            services.Configure<QdrantOptions>(config.GetSection(QdrantOptions.SectionName));
            services.Configure<ChatOptions>(config.GetSection(ChatOptions.SectionName));
            services.Configure<StripeOptions>(config.GetSection(StripeOptions.SectionName));
            services.AddScoped<IChatServices, ChatServices>();
            services.AddScoped<IChatHistoryServices, ChatHistoryServices>();
            services.AddScoped<IStripeCheckoutServices, StripeCheckoutServices>();
            services.AddHttpClient<IVectorKnowledgeStore, QdrantKnowledgeStore>(client =>
                client.Timeout = TimeSpan.FromSeconds(45));
            services.AddHttpClient<IGroqChatClient, GroqChatClient>(client =>
                client.Timeout = TimeSpan.FromSeconds(45));
            services.AddScoped<AddressGeocoder>();
            services.AddHttpClient("nominatim", client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
                client.Timeout = TimeSpan.FromSeconds(8);
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "CarePlus-FYP/1.0 (student clinic portal)");
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            });
            services.AddScoped<IJWTServices, JWTServices>();

            //repository
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IStaffRepository, StaffRepository>();
            services.AddScoped<IAdminRepository, AdminRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IPharmacistRepository, PharmacistRepository>();
            services.AddScoped<IMedicineRepository, MedicineRepository>();
            services.AddScoped<IMedicineBatchRepository, MedicineBatchRepository>();
            services.AddScoped<IWorkScheduleRepository, WorkScheduleRepository>();
            services.AddScoped<ILeaveRepository, LeaveRepository>();
            services.AddScoped<ICounterRepository, CounterRepository>();
            services.AddHttpContextAccessor();
            return services;
        }
    }
}
