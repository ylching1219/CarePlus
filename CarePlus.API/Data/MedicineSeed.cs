using CarePlus.API.Models.Medicine;
using Microsoft.EntityFrameworkCore;

namespace CarePlus.API.Data
{
    public static class MedicineSeed
    {
        public static void Apply(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Medicine>().HasData(
                Row("Med0001", "Amoxicillin 500 mg capsules", "阿莫西林 500 毫克胶囊",
                    "For a bacterial infection. Finish the course unless the doctor tells you to stop.",
                    "用于细菌感染。除非医生叫停，请把疗程吃完。",
                    "16 capsules", "16 粒", "capsule", "antibiotic",
                    "1 capsule, 3 times a day", "每日 3 次，每次 1 粒", "amox", 12.50m),
                Row("Med0002", "Paracetamol 500 mg tablets", "扑热息痛 500 毫克片",
                    "For fever or pain. Do not take more than 4 g in 24 hours.",
                    "用来退烧或止痛。24 小时内不要超过 4 克。",
                    "20 tablets", "20 片", "tablet", "analgesic",
                    "1 to 2 tablets, up to 4 times a day", "每次 1 至 2 片，每日最多 4 次", "para", 8.90m),
                Row("Med0003", "Loratadine 10 mg tablets", "氯雷他定 10 毫克片",
                    "For allergy symptoms. Take it at the same time each day.",
                    "针对过敏症状。尽量每天同一时间服用。",
                    "10 tablets", "10 片", "tablet", "antihistamine",
                    "1 tablet once a day", "每日 1 次，每次 1 片", "lora", 16.50m),
                Row("Med0004", "Ibuprofen 200 mg tablets", "布洛芬 200 毫克片",
                    "For pain or swelling after a visit. Take it with food.",
                    "用于就诊后的疼痛或肿。饭后服用。",
                    "24 tablets", "24 片", "tablet", "analgesic",
                    "1 tablet, up to 3 times a day", "每次 1 片，每日最多 3 次", "para", 9.50m),
                Row("Med0005", "Cetirizine 10 mg tablets", "西替利嗪 10 毫克片",
                    "For hay fever and itchy skin. It can make some people sleepy.",
                    "用于花粉过敏和皮肤发痒。有些人吃了会困。",
                    "10 tablets", "10 片", "tablet", "antihistamine",
                    "1 tablet once a day", "每日 1 次，每次 1 片", "lora", 11.00m),
                Row("Med0006", "Cloxacillin 500 mg capsules", "氯唑西林 500 毫克胶囊",
                    "For a skin infection. Take it on an empty stomach and finish the course.",
                    "用于皮肤感染。空腹服用，并把疗程吃完。",
                    "20 capsules", "20 粒", "capsule", "antibiotic",
                    "1 capsule, 4 times a day", "每日 4 次，每次 1 粒", "amox", 14.80m));
        }

        private static Medicine Row(
            string id, string name, string nameZh, string description, string descriptionZh,
            string pack, string packZh, string form, string kind, string method, string methodZh,
            string imageKey, decimal price) => new()
        {
            MedicineId = id,
            MedicineName = name,
            NameZh = nameZh,
            Description = description,
            DescriptionZh = descriptionZh,
            Pack = pack,
            PackZh = packZh,
            Form = form,
            Kind = kind,
            Method = method,
            MethodZh = methodZh,
            ImageKey = imageKey,
            Price = price,
            Status = "Available"
        };
    }
}
