# 🛠️ Active Directory Destek Konsolu

Windows Active Directory ortamlarında günlük yardım masası (Helpdesk) ve sistem yönetimi operasyonlarını hızlandırmak için geliştirilmiş, hafif ve bağımsız bir yönetim aracıdır.

---

## 🚀 Öne Çıkan Özellikler

- **Canlı Otomatik Tamamlama (Auto-Complete):** Kullanıcı ve grup adlarını yazarken Active Directory üzerinden asenkron arama (Debounce mekanizmalı).
- **Kapsamlı Hesap Analizi:** Hesap durumu (Aktif/Pasif), kilit durumu (Lockout), şifre değişim ve bitiş tarihi, son hatalı parola denemesi ve kurumsal bilgiler (Departman, Unvan, Yönetici).
- **Hiyerarşik OU Gösterimi:** Kullanıcının hangi Organizational Unit (OU) altında olduğunu anlaşılır bir ağaç yapısıyla listeleme.
- **Kullanıcı Dostu Geçici Parola Üretimi:** Active Directory şifre ilkelerine tam uyan ancak telefonda ve klavyede hatasız aktarılabilen (*Mavi384!* formatında) parola üretimi ve ilk oturumda şifre değiştirmeye zorlama.
- **Dinamik Grup Yönetimi:** 
  - Tek tıkla güvenlik grubuna ekleme ve çıkarma.
  - Grubun üzerine çift tıklayarak tüm üyeleri ayrı bir pencerede canlı filtreleme ile listeleme.
  - Grup üyeleri listesindeki bir kullanıcıya çift tıklayarak doğrudan o kullanıcının profiline geçiş yapma.

---

## 💻 Kurulum ve Çalıştırma

Bu araç herhangi bir kurulum sihirbazı (setup/installer) gerektirmez; **taşınabilir (portable)** olarak çalışır.

### Yöntem 1: Hazır EXE'yi Çalıştırma (Tavsiye Edilen)

1. [Releases](https://github.com/muratcancatmak52/ActiveDirectory-Helpdesk-Console/releases) sayfasından en son sürüm olan `ADBilgi.exe` dosyasını indirin.
2. Masaüstünüze veya istediğiniz bir klasöre kopyalayın.
3. Çift tıklayarak doğrudan çalıştırın.

> **Not:** Uygulamanın Active Directory ile iletişim kurabilmesi için bilgisayarınızın şirket ağına (Domain veya VPN) bağlı olması gerekir.

---

### Yöntem 2: Kaynak Koddan Derleme (Visual Studio Gerekmez)

Projeyi doğrudan kaynak koddan kendiniz derlemek isterseniz:

1. Depoyu klonlayın:
   ```powershell
   git clone [https://github.com/muratcancatmak52/ActiveDirectory-Helpdesk-Console.git](https://github.com/muratcancatmak52/ActiveDirectory-Helpdesk-Console.git)
   cd ActiveDirectory-Helpdesk-Console
