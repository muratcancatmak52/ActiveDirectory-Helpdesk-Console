# 🛠️ Active Directory Destek Konsolu

Windows Active Directory ortamlarında günlük yardım masası (Helpdesk) ve sistem yönetimi operasyonlarını hızlandırmak için geliştirilmiş, hafif ve bağımsız bir yönetim aracıdır.

## 🚀 Özellikler

- **Canlı Otomatik Tamamlama (Auto-Complete):** Kullanıcı ve grup adlarını yazarken Active Directory üzerinden gecikmeli (debounce) asenkron arama.
- **Kapsamlı Hesap Analizi:** Hesap durumu (Aktif/Pasif), hesap kilit durumu (Lockout), şifre son değişim ve bitiş tarihi, son hatalı parola denemesi ve kurumsal bilgiler (Departman, Unvan, Yönetici).
- **Hiyerarşik OU Gösterimi:** Kullanıcının hangi Organizational Unit (OU) altında olduğunu anlaşılır bir ağaç yapısıyla listeleme.
- **Kullanıcı Dostu Şifre Sıfırlama:** Active Directory şifre karmaşıklık ilkelerine tam uyan, ancak telefonda ve klavyede hatasız aktarılabilen (*Mavi384!* formatında) parola üretimi ve ilk oturumda parola değiştirmeye zorlama.
- **Dinamik Grup Yönetimi:** 
  - Tek tıkla güvenlik grubuna ekleme ve çıkarma.
  - Grubun üzerine çift tıklayarak tüm üyeleri ayrı bir pencerede canlı filtreleme ile listeleme.
  - Grup üyeleri listesindeki bir kullanıcıya çift tıklayarak doğrudan o kullanıcının profiline geçiş yapma.

## 📦 Kurulum ve Derleme

Bu uygulama, harici bir **RSAT** modülü veya Visual Studio kurulumu gerektirmez. Windows'un yerleşik C# derleyicisi (`csc.exe`) ile saniyeler içinde derlenebilir:

```powershell
# PowerShell üzerinden derleme:
$csc = Join-Path ([System.Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()) "csc.exe"
& $csc /target:winexe /out:"ADBilgi.exe" /reference:System.Windows.Forms.dll,System.Drawing.dll,System.DirectoryServices.dll,System.DirectoryServices.AccountManagement.dll Program.cs
🔐 Gereksinimler & Yetkiler
Domain'e bağlı bir Windows istemci.

Okuma işlemleri için standart domain kullanıcısı yetkisi yeterlidir.

Kilit açma, parola sıfırlama veya grup üyeliği değiştirme işlemleri için ilgili OU üzerinde yetkilendirilmiş (delegated) hesap gereklidir.


<ElicitationsGroup message="GitHub sürecinde takıldığınız bir adım olursa:">
  <Elicitation label="GitHub hesabı oluşturma" query="GitHub hesabı nasıl açılır ve ayarları nasıl yapılır?"/>
  <Elicitation label="Exe dosyasını release ekleme" query="Derlenmiş hazır .exe dosyasını GitHub Release olarak nasıl paylaşırım?"/>
  <Elicitation label="Git kimlik doğrulama hatası" query="Git push yaparken token veya kimlik doğrulama hatası alıyorum."/>
</ElicitationsGroup>
