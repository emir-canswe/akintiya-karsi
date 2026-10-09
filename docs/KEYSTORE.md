# Keystore (imzalama anahtarı) — nasıl oluşturulur

Google Play'e yüklenen her AAB, senin **yükleme anahtarınla (upload key)** imzalanmalı.
Bu anahtarı kaybedersen uygulamayı güncelleyemezsin (Play App Signing açıksa Google'dan sıfırlama
isteyebilirsin ama uzun sürer). O yüzden:

- `.keystore` / `.jks` dosyasını **asla git'e koyma** (`.gitignore` zaten engelliyor).
- Dosyayı ve şifreleri bir şifre yöneticisinde + bir yedekte (USB / bulut kasası) sakla.

## 1. Anahtarı oluştur (bir kere)

Unity'nin içindeki JDK'da `keytool` var. PowerShell'de:

```powershell
& "C:\Users\Msi\Unity\Hub\Editor\6000.3.25f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe" `
  -genkeypair -v `
  -keystore "C:\Users\Msi\keys\upstream-upload.keystore" `
  -alias upstream `
  -keyalg RSA -keysize 2048 -validity 10000
```

Şifre ve isim/şehir gibi bilgileri soracak. Keystore'u **proje klasörünün dışında** tut
(örnekte `C:\Users\Msi\keys\`).

## 2. Build sırasında anahtarı göster

Build script'i anahtarı ortam değişkenlerinden okur; şifreler hiçbir dosyaya yazılmaz
(build bitince Unity ayarlarından da silinir):

```powershell
$env:UPSTREAM_KEYSTORE_PATH = "C:\Users\Msi\keys\upstream-upload.keystore"
$env:UPSTREAM_KEYSTORE_PASS = "keystore-şifren"
$env:UPSTREAM_KEY_ALIAS     = "upstream"
$env:UPSTREAM_KEY_PASS      = "anahtar-şifren"   # keystore şifresiyle aynıysa boş bırakabilirsin
```

Sonra (aynı PowerShell penceresinde) Unity menüsünden **Upstream → 4. Build Android AAB** ya da:

```powershell
& "C:\Users\Msi\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\Users\Msi\UpstreamBuild" -buildTarget Android `
  -executeMethod Upstream.EditorTools.UpstreamSceneBuilder.BuildAndroidAab
```

- **APK (telefon testi):** anahtar yoksa debug anahtarıyla imzalanır, sorun değil.
- **AAB (Play Store):** anahtar yoksa build bilerek durur ve ne yapman gerektiğini yazar.

> Not: Proje yolu `akıntiya karşi` Türkçe karakter içeriyor; Unity'nin Android araçları buna izin
> vermiyor. Build'leri `C:\Users\Msi\UpstreamBuild` (aynı klasöre bağlı bir junction) üzerinden al
> ya da klasörü `akintiya-karsi` gibi ASCII bir isme çevir.

## 3. Play Console'da

İlk yüklemede **Play App Signing**'i kabul et: Google uygulamayı kendi anahtarıyla imzalar,
seninki sadece "yükleme anahtarı" olur. Kaybedersen Console'dan yükleme anahtarını sıfırlatabilirsin.

## 4. Sürüm numarası

`Assets/Editor/ReleaseSettings.cs` içinde:

```csharp
public const string Version = "0.1.0";   // kullanıcının gördüğü
public const int VersionCode = 1;        // her yüklemede 1 artmalı
```

Her Play Store yüklemesinde `VersionCode`'u artır; aynı numara ikinci kez kabul edilmez.
