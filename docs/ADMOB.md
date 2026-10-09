# Gerçek AdMob'u açmak

Reklam kodu hazır ve test edildi (`AdsManager`, `AdMobService`, `UmpConsentService`,
`InterstitialPolicy`). Şu an oyun **`MockAdsService`** ile çalışıyor: reklam yerine ekranda
"TEST REKLAMI" yazısı çıkıyor, akış (ödül, geçiş reklamı sıklığı) birebir aynı.

Gerçek (test) reklamlara geçmek 4 adım. Bunu **telefonda ilk başarılı build'den sonra** yap:
eklenti Gradle'a yeni bağımlılıklar ekliyor, sorun çıkarsa neyin bozduğu net olsun.

## 1. Google Mobile Ads Unity eklentisini kur

`Packages/manifest.json` dosyasına OpenUPM registry'si ve paket eklenir (Unity içinden
*Window → Package Manager → + → Add package by name* de olur):

```json
"scopedRegistries": [
  {
    "name": "OpenUPM",
    "url": "https://package.openupm.com",
    "scopes": ["com.google"]
  }
],
"dependencies": {
  "com.google.ads.mobile": "10.x.x",
  ...
}
```

Bu, External Dependency Manager'ı (EDM4U) da getirir. Kurulumdan sonra:
*Assets → External Dependency Manager → Android Resolver → Force Resolve*.

## 2. AdMob uygulama kimliğini gir

*Assets → Google Mobile Ads → Settings*:

- **Android App ID** (test): `ca-app-pub-3940256099942544~3347511713`
- **iOS App ID** (test): `ca-app-pub-3940256099942544~1458002511`

> Bu alan boşsa uygulama **açılışta çöker**. Gerçek App ID'yi yayından hemen önce
> kendi AdMob hesabından alıp buraya yazarsın.

## 3. `USE_ADMOB` sembolünü aç

*Project Settings → Player → Android → Other Settings → Scripting Define Symbols* → `USE_ADMOB`
ekle (iOS için de aynı). Bu olmadan AdMob kodu derlenmez, oyun Mock'la devam eder.

Editörde her zaman Mock kullanılır; gerçek reklam sadece telefonda çıkar.

## 4. Gerçek reklam birimi kimlikleri (yayından önce)

`Assets/_Project/Config/ad_config.example.json` dosyasını
`Assets/Resources/ad_config.json` olarak kopyala ve kendi kimliklerini yaz.
Bu dosya `.gitignore`'da; **git'e asla girmez**. Koddaki varsayılanlar Google'ın resmi
test kimlikleri.

## Akış (ne zaman ne olur)

| An | Ne olur |
|---|---|
| Uygulama açılışı | AB/İngiltere/Türkiye'deyse UMP onay formu → sonra reklamlar yüklenir |
| Ölünce, skor ≥ 15 m | "Reklam İzle, Devam Et" (oyun başına bir kere). Ödül **reklam kapanınca** verilir. |
| "Tekrar Oyna" | İlk 3 oyunda asla; sonra her 3 oyunda bir geçiş reklamı. O tur ödüllü reklam izlendiyse gösterilmez. |
| Reklamları kaldır (ileride IAP) | `SaveData.adsRemoved = true` → geçiş reklamları tamamen kapanır |
| Banner | Yok (bilerek) |
