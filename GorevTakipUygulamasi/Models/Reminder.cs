using System;
namespace GorevTakipUygulamasi.Models
{
    public class Reminder
    {
        // Hatırlatıcının benzersiz kimliği
        public string Id { get; set; } = Guid.NewGuid().ToString();

        // Hatırlatıcının başlığı
        public string Title { get; set; } = string.Empty;

        // Hatırlatıcının açıklaması
        public string Description { get; set; } = string.Empty;

        // Hatırlatıcı tarihi (sadece tarih kısmı önemlidir, saat genellikle kullanılmaz)
        public DateTime ReminderDate { get; set; } = DateTime.Today;

        // Hatırlatıcının gönderilip gönderilmediğini belirten bayrak
        public bool IsSent { get; set; } = false;

        // Azure Table Storage için PartitionKey ve RowKey özellikleri
        // Bu özellikler, Table Storage'da verileri düzenlemek için kullanılır.
        // PartitionKey genellikle bir kategori veya grup için kullanılır (örneğin "Reminders").
        // RowKey ise benzersiz bir kimlik veya sıralama anahtarıdır (örneğin tarih + Id).
        public string PartitionKey { get; set; } = "Reminders"; // Tüm hatırlatıcıları aynı PartitionKey altında tutabiliriz
        public string RowKey { get; set; } = Guid.NewGuid().ToString(); // Benzersiz kimlik olarak kullanabiliriz
    }
}
