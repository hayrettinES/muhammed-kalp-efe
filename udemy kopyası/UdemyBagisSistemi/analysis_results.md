# Proje Analizi: Udemy Bağış Sistemi

## 1. Genel Bilgiler
* **Uygulama Tipi:** ASP.NET Core Web Application (MVC Mimarisi - .NET 10)
* **Veritabanı:** SQLite (`Veriler/udemy.db`). Toplam boyutu yaklaşıp 60KB.
* **Proje Amacı:** Öğrenci, Öğretmen ve Bağışçılardan oluşan 3 rollü bir eğitim ekosistemi kurmak. 

## 2. Dizin Yapısı ve Katmanlar
* **Controllers:** `Ogrenci`, `Egitmen`, `Admin`, `Hesap`, `Profil` ve `Home` Controller'ları mevcuttur. İstek ve yönlendirme işlemleri büyük oranda bu dosyalarda yönetilir. 
* **Models:** `HataGorunumModeli.cs` ve `VarlikModelleri.cs`. Veritabanı ve View'ler için gereken model yapıları burada tasarlanmıştır. Model validasyonları ya da temel domain nesneleri buradan gelir. 
* **Servisler:** İş mantığı ve soyutlama burada yer almaktadır. `PlatformServisi` ana business akışlarını yönetir. `SifrelemeServisi` parola süreçlerini yönetir. Veritabanı etkileşimi `SqliteKomutServisi` kullanılarak çiğ SQL komutlarıyla sağlanmış gibi görünmektedir. `VeritabaniHazirlayici` servisi DB dosyası/tabloları hazır etme işlemlerini görür.
* **Views:** Controller'larla eşleşen birden fazla klasör ve paylaşılan bileşenler için `Shared` klasörü mevcut. Ayrıca özel statik dosyalar için ayrı bir `basic` klasörü konulmuştur. `_ViewStart` kullanılarak MVC'nin layout mantığı entegre durumdadır.

## 3. Çalışma Mantığı
* `Program.cs` içerisinde Cookie tabanlı kimlik doğrulama ayarlanmış, dependency injection ile gerekli servisler scope ve singleton tipli olarak sisteme kaydedilmiştir.
* Varsayılan ayar olarak uygulamanın başlangıç ekranı `{controller=Home}/{action=Index}` şeklinde yapılandırılmıştır. 
* Yetkisiz kullanıcılar `/Hesap/Giris` adresine yönlendirilmektedir.

## 4. Yapılan Değişiklikler
* Özel hazırlanmış Landing Page (Karşılama Ekranı) olan `Views/basic/index.cshtml` dosyası, `HomeController.cs` içerisindeki `Index` metodu yeniden yapılandırılarak varsayılan ekran olarak ayarlanmıştır.
* `Views/basic/index.cshtml` bir layout içerisine gömülmesin ve orijinal tasarımı/fonksiyonalliği korunsun diye ilgili dosyanın en üstüne MVC `Layout = null` komutu entegre edilmiştir. 
