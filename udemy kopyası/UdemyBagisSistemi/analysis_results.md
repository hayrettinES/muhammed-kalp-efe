# Udemy Clone - Kimlik Doğrulama ve Yönlendirme Analizi

## 1. Kayıt Olma (Registration) İşleyişi
Sistemde Öğrenci ve Eğitmen olmak üzere iki ana kayıt akışı mevcut. (Admin dışarıdan kayıt olamıyor).

*   **View & Controller:** Öğrenci için `KayitOgrenci`, Eğitmen için `KayitEgitmen` aksiyonları ayrı View'lar döndürüyor. İkisi de form gönderimini (POST) `HesapController.Kayit` aksiyonuna yapıyor.
*   **Servis Katmanı:** Kayıt işlemi `PlatformServisi.KayitOlAsync` içinde yapılıyor.
    *   Sistem aynı e-posta ile ikinci bir kayda izin vermiyor.
    *   Kullanıcıya rastgele bir onay kodu (`Guid`) atanıyor.
    *   Veritabanına `EpostaOnaylandiMi = 0` (False) olarak kaydediliyor.
    *   Kullanıcıya `localhost:5003/Hesap/Dogrula` adresli bir onay linki mail olarak gönderiliyor. (Not: localhost portu şu an kod içine gömülü durumda, canlı ortam için dinamik hale getirilmeli).
*   **Kayıt Sonrası:** Başarılı kayıt sonrası sistem kullanıcıyı doğrudan sisteme almıyor. Giriş sayfasına (`Giris`) yönlendiriyor ve mailindeki linke tıklamasını bekliyor.

## 2. Mail Doğrulama (Email Verification)
*   **Akış:** Kullanıcı mailindeki `http://localhost:5003/Hesap/Dogrula?eposta=...&kod=...` linkine tıklar.
*   **İşlem:** `PlatformServisi.EpostaDogrula` metodu çalışır. Veritabanındaki `EpostaOnayKodu` ile linkteki kod eşleşirse `EpostaOnaylandiMi` 1 yapılır ve onay kodu sıfırlanır.
*   **Sonuç:** Kullanıcı tekrar giriş ekranına yönlendirilir ve artık şifresiyle sisteme giriş yapabilir.

## 3. Giriş Yapma (Login) İşleyişi
Normal e-posta ve şifre ile giriş işlemi `HesapController.Giris` üzerinden yapılıyor.
*   **Kontroller:**
    *   `PlatformServisi.GirisYap` e-posta ve şifrenin doğruluğunu kontrol eder (Şifre `SifrelemeServisi` ile hash doğrulamasına tabi tutulur).
    *   Eğer kullanıcının `EpostaOnaylandiMi` değeri false (0) ise, sisteme alınmaz ve e-postasını doğrulaması istenir.
*   **Cookie Oturumu:** Doğrulama başarılıysa `.AspNetCore.Cookies` üzerinden Claim'ler (Id, AdSoyad, Eposta, Rol) eklenerek oturum açılır.
*   **Profil Eksiklik Kontrolü:** Tam bu noktada `PlatformServisi.ProfilEksikMi` devreye girer.
    *   Eğer öğrenciyse: `EgitimSeviyesi`, `IlgiAlanlari`, `Hedef` boş ise;
    *   Eğer eğitmense: `Unvan` (varsayılan ise), `UzmanlikAlanlari`, `DeneyimYili`, `KursFormati`, `FiyatlandirmaTercihi` boş ise,
    *   Kullanıcı ilgili panele değil, `Hesap/ProfilTamamla` sayfasına atılır.
*   **Yönlendirme:** Profil tamamsa, kullanıcının `Rol` bilgisine göre;
    *   `Admin` -> `/Admin/Panel`
    *   `Egitmen` -> `/Egitmen/Panel`
    *   `Ogrenci` (veya varsayılan) -> `/Ogrenci/Panel` adresine yönlendirilir.

## 4. Google ile Devam Et (Google OAuth)
Sisteme Google ile entegre bir giriş ve kayıt mekanizması kurulmuş. Hem kayıt hem de giriş için `Hesap/GoogleLogin` aksiyonu çalışıyor.
*   **Mekanizma:** Tıklanan butona göre (öğrenci/eğitmen) bir `rol` parametresi alınır ve Google tarafına gönderilir.
*   **Google'dan Dönüş (`GoogleResponse`):**
    *   Kullanıcının E-posta ve Ad Soyad bilgisi alınır. E-postaya göre sistemde bu kullanıcı var mı diye bakılır.
    *   **Kullanıcı Yoksa (İlk Defa Geliyorsa):** Arka planda `KayitOlAsync` kullanılarak rastgele güçlü bir şifre ile sisteme kaydedilir. Öğrenci veya Eğitmen (hangi butonla geldiyse) o role sahip olur.
    *   **Mail Onayı Bypass:** Google'dan geldiği için e-postası zaten doğrulanmış kabul edilir (`EpostaDogrulaByGoogle` metodu ile `EpostaOnaylandiMi` 1 yapılır).
    *   **Kullanıcı Varsa:** Daha önceden normal form ile üye olmuş ama e-postasını doğrulamamış birisi gelirse, sırf Google ile girdiği için e-postası otomatik doğrulanmış olur.
*   **Oturum ve Yönlendirme:** Aynen normal girişte olduğu gibi Cookie oturumu başlatılır, `ProfilEksikMi` kontrolü yapılır ve role göre panele (`Admin`, `Egitmen`, `Ogrenci`) yönlendirilir.

---

## 🔍 Tespitler ve Sorular

Projenizi detaylıca inceledim. Kodlama yapısı (katmanlı servis mimarisi, DTO/ViewModel kullanımı) gayet düzenli oturtulmuş. Geliştirmelere başlamadan önce birkaç noktada onayınızı/görüşünüzü almak isterim:

1.  **Profil Tamamlama Zorunluluğu:** Şu an hem Google ile hem de normal kayıt olan birisi, kayıt olur olmaz (veya giriş yapınca) zorunlu olarak `ProfilTamamla` ekranına yönlendiriliyor. Bu kullanıcı deneyimini biraz yavaşlatabilir. Bazı Udemy benzeri sistemlerde bu sonraya bırakılabilir (örneğin kurs satın alana veya kurs yayınlayana kadar). Bunu bu şekilde zorunlu tutmaya devam edelim mi, yoksa atlanabilir/ertelenenebilir mi yapalım?
2.  **Hardcoded Localhost Linkleri:** `PlatformServisi.cs` içerisindeki mail atma kısmında `http://localhost:5003` statik olarak yazılmış. Gelecekte canlıya çıkılacağı için bunu Dinamik (o anki domaini alacak şekilde) yapalım mı?
3.  **Google ile Kayıt Olurken Rol Seçimi:** Google ile giriş yapan biri sisteme ilk kez geliyorsa `rol` parametresiyle "Öğrenci" veya "Eğitmen" oluyor. Eğer kullanıcı yanlış butona basıp Eğitmen yerine Öğrenci olursa (veya tam tersi) içeriden rolünü değiştirme hakkı verecek miyiz, yoksa bu sabit mi kalacak?
4.  **Admin Girişi Akışı:** Şu an ayrı bir `/Hesap/AdminGiris` rotası var. Bu gayet güvenli. Ama Adminler profil tamamlama veya Google girişine dahil edilmemiş (güvenlik açısından mantıklı). Admin için ekstra iki faktörlü (2FA) vb. bir şey düşündünüz mü yoksa klasik Eposta/Şifre devam mı edelim?

Bu akış analizim tamamen sizin kurduğunuz mantıkla birebir örtüşüyor mu? Soruları yanıtladıktan veya ekstra eklemek istediklerinizi belirttikten sonra fonksiyonel düzenlemelere geçebiliriz.
