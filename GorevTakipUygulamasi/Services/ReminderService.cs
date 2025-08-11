// Services/ReminderService.cs
using Azure; // Azure için gerekli NuGet paketi
using Azure.Data.Tables; // Azure Table Storage için gerekli NuGet paketi
using Microsoft.Extensions.Configuration; // appsettings.json'dan okumak için gerekli
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GorevTakipUygulamasi.Models; // Hatırlatıcı modelinizin namespace'ini buraya ekleyin

namespace GorevTakipUygulamasi.Services
{
    public class ReminderService
    {
        // Azure Table Storage için bağlantı dizesi
        // Bu değer appsettings.json'dan IConfiguration aracılığıyla yüklenecektir.
        private readonly string _connectionString;
        // Table Storage'da kullanacağımız tablonun adı
        private readonly string _tableName = "RemindersTable";

        // Azure Table Storage istemcisi
        private readonly TableClient _tableClient;

        /// <summary>
        /// ReminderService sınıfının yapıcı metodu.
        /// IConfiguration bağımlılık enjeksiyonu ile appsettings.json'dan bağlantı dizesini alır.
        /// </summary>
        /// <param name="configuration">Uygulama yapılandırma arayüzü.</param>
        public ReminderService(IConfiguration configuration)
        {
            // appsettings.json dosyasından "AzureStorage:ConnectionString" anahtarını okuyoruz.
            // Eğer bulunamazsa veya boşsa bir ArgumentNullException fırlatırız.
            _connectionString = configuration.GetValue<string>("AzureStorage:ConnectionString") ??
                                throw new ArgumentNullException("Azure Storage ConnectionString not found in configuration. Please check appsettings.json.");

            // TableClient'ı bağlantı dizesi ve tablo adıyla başlatın.
            _tableClient = new TableClient(_connectionString, _tableName);

            // Tabloyu oluşturun (eğer yoksa). Bu işlem idempotenttir, yani tablo varsa tekrar oluşturmaz.
            _tableClient.CreateIfNotExists();
        }

        /// <summary>
        /// Yeni bir hatırlatıcıyı Azure Table Storage'a ekler.
        /// </summary>
        /// <param name="reminder">Eklenecek hatırlatıcı nesnesi.</param>
        public async Task AddReminderAsync(Reminder reminder)
        {
            // Hatırlatıcı tarihini sadece tarih kısmını alarak (saat 00:00:00) ayarlarız.
            // Bu, Logic App'in sadece güne göre filtreleme yapmasını kolaylaştırır.
            reminder.ReminderDate = reminder.ReminderDate.Date;

            // RowKey'i YYYYMMDD formatında tarih ve ardından benzersiz bir GUID ile oluştururuz.
            // Bu, aynı güne birden fazla hatırlatıcı eklemeyi mümkün kılar ve Logic App'in sorgulamasını optimize eder.
            reminder.RowKey = $"{reminder.ReminderDate:yyyyMMdd}-{Guid.NewGuid()}";

            // Tüm hatırlatıcıları aynı PartitionKey altında tutuyoruz.
            // Eğer kullanıcı bazlı hatırlatıcılar olsaydı, burası kullanıcının ID'si olabilirdi.
            reminder.PartitionKey = "Reminders";

            // Hatırlatıcıyı tabloya ekler.
            await _tableClient.AddEntityAsync(reminder);
            Console.WriteLine($"Hatırlatıcı başarıyla eklendi: {reminder.Title} - {reminder.ReminderDate.ToShortDateString()}");
        }

        /// <summary>
        /// Belirli bir tarih için hatırlatıcıları Azure Table Storage'dan getirir.
        /// Blazor uygulamasının kendi içinde hatırlatıcıları göstermesi için kullanılır.
        /// Logic App doğrudan Table Storage'ı sorgulayacağı için bu metot Logic App için değildir.
        /// </summary>
        /// <param name="date">Hatırlatıcıların getirileceği tarih.</param>
        /// <returns>Belirtilen tarihe ait hatırlatıcıların listesi.</returns>
        public async Task<List<Reminder>> GetRemindersForDateAsync(DateTime date)
        {
            var reminders = new List<Reminder>();
            // Sadece tarih kısmını kullanarak filtreleme yaparız.
            string dateFilter = date.Date.ToString("yyyyMMdd");

            // PartitionKey ve RowKey'in başlangıcı ile filtreleme yaparak ilgili günün hatırlatıcılarını getiririz.
            // 'ge' (greater than or equal) ve 'lt' (less than) operatörleri ile tarih aralığını kapsarız.
            // Logic App tarafında da benzer bir filtreleme kullanılacaktır.
            await foreach (var entity in _tableClient.QueryAsync<Reminder>(
                filter: $"PartitionKey eq 'Reminders' and RowKey ge '{dateFilter}-' and RowKey lt '{dateFilter}~'"
            ))
            {
                reminders.Add(entity);
            }
            return reminders;
        }

        /// <summary>
        /// Bir hatırlatıcının durumunu "gönderildi" olarak günceller.
        /// Bu metot genellikle Logic App tarafından, e-posta gönderildikten sonra çağrılır.
        /// </summary>
        /// <param name="partitionKey">Hatırlatıcının PartitionKey'i.</param>
        /// <param name="rowKey">Hatırlatıcının RowKey'i.</param>
        public async Task MarkReminderAsSentAsync(string partitionKey, string rowKey)
        {
            // Belirtilen PartitionKey ve RowKey ile hatırlatıcıyı alır.
            var response = await _tableClient.GetEntityAsync<Reminder>(partitionKey, rowKey);
            var reminder = response.Value;

            if (reminder != null)
            {
                // Hatırlatıcının IsSent özelliğini true olarak ayarlar.
                reminder.IsSent = true;
                // Hatırlatıcıyı tabloya günceller. ETag.All ile iyimser eşzamanlılık kontrolü yapılır.
                await _tableClient.UpdateEntityAsync(reminder, ETag.All, TableUpdateMode.Replace);
                Console.WriteLine($"Hatırlatıcı gönderildi olarak işaretlendi: {reminder.Title}");
            }
            else
            {
                Console.WriteLine($"Hatırlatıcı bulunamadı: PartitionKey={partitionKey}, RowKey={rowKey}");
            }
        }
    }
}
