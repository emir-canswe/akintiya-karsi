# Google Play mağaza metinleri (taslak)

Limitler: uygulama adı ≤ 30, kısa açıklama ≤ 80, tam açıklama ≤ 4000 karakter.

## İsim

Oyunun kahramanı **Blue** adında küçük mavi bir balık. Ekim 2026'da yapılan aramada Google Play'de
"Blue's River" / "Blue'nun Nehri" adında bir oyun çıkmadı. "Swim Upstream" ve "Upstream" isimlerinden
vazgeçildi, çünkü Play'de "Swimming Upstream" adında çok benzer bir nehir oyunu var (com.Unity.Upstream).

| | TR | EN |
|---|---|---|
| Mağaza başlığı (≤30) | `Blue'nun Nehri: Akıntıya Karşı` | `Blue's River: Swim & Grow` |
| Telefondaki ad | Blue’s River | Blue’s River |
| Menü başlığı | BLUE'NUN NEHRİ | BLUE'S RIVER |

Paket adı `com.emircan.upstream` olarak kalıyor (oyuncular görmez; yayından sonra değiştirilemez).

---

## English (en-US)

**App name (≤30):** `Blue's River: Swim & Grow`

**Short description (≤80):**
`Help Blue swim against the current, eat to grow and turn the sharks into snacks!`

**Full description:**

```
The river is fast. The sharks are hungry. Blue is tiny… for now.

Blue's River is a one-finger arcade game about a little blue fish with big
plans. Drag to steer, gobble shrimp and grow — until the sharks that chased
Blue become the next meal.

★ GROW FROM FRY TO TITAN
Every shrimp makes Blue bigger. Five growth stages, each one changing how
you play: bigger fish hit harder, but have less room to dodge.

★ TURN THE TABLES
Fill your power bar for a TURBO dash that smashes rocks and logs.
Get big enough and the sharks stop being the hunters.

★ PIRANHAS, PIKES, CATFISH… AND BIG WAVES
New predators join the further you swim. Past 2000 m the river roars and
giant waves roll in.

★ 30 FISH TO COLLECT
Pick up coins and unlock 30 fish, each with its own ability: magnets,
shields, rock breakers, wave riders and more.

• One-finger controls
• Plays offline
• Optional ads: watch one to keep swimming — never forced mid-run

How far up the river can Blue go?
```

---

## Türkçe (tr-TR)

**Uygulama adı (≤30):** `Blue'nun Nehri: Akıntıya Karşı`

**Kısa açıklama (≤80):**
`Blue ile akıntıya karşı yüz, yedikçe büyü, köpekbalıklarını sen avla!`

**Tam açıklama:**

```
Nehir hızlı. Köpekbalıkları aç. Blue küçük… şimdilik.

Blue'nun Nehri, büyük hayalleri olan küçük mavi bir balığın tek parmakla
oynanan arcade oyunu. Parmağını sürükle, karidesleri ye ve büyü —
ta ki Blue'yu kovalayan köpekbalıkları onun yemeği olana kadar.

★ YAVRUDAN DEV TİTANA
Her karides Blue'yu büyütür. Beş büyüme kademesi, her biri oyunu değiştirir:
büyük balık daha güçlüdür ama kaçacak yeri de azalır.

★ AV AVCIYI
Güç barını doldur, TURBO ile kayaları ve kütükleri parçala.
Yeterince büyüdüğünde köpekbalıkları artık avcı değil.

★ PİRANA, TURNA, YAYIN BALIĞI… VE DEV DALGALAR
Yüzdükçe yeni avcılar katılır. 2000 metreden sonra nehir coşar,
dev dalgalar gelir.

★ TOPLANACAK 30 BALIK
Paraları topla, her birinin kendi yeteneği olan 30 balığın kilidini aç:
mıknatıs, kalkan, kaya kıran, dalga sörfçüsü ve daha fazlası.

• Tek parmakla kontrol
• İnternetsiz oynanır
• İsteğe bağlı reklam: devam etmek için izle — oyunun ortasında zorla reklam yok

Blue nehrin ne kadar yukarısına yüzebilir?
```

---

## Ekran görüntüsü önerileri (en az 2, ideal 6-8; 9:16 dikey, 1080×1920)

Her görüntünün üstüne kısa, büyük bir başlık koy (EN/TR ayrı setler):

1. **Köpekbalığını yeme anı** (zoom + yavaşlama karesi) — "EAT THE SHARKS" / "KÖPEKBALIKLARINI YUT"
2. **Yavru balık köpekbalığından kaçıyor** — "START TINY" / "KÜÇÜK BAŞLA"
3. **Turbo ile kütüğü parçalama** — "TURBO SMASH"
4. **Olta tuzağı yakın plan** — "NOT EVERY SNACK IS SAFE" / "HER YEM YEM DEĞİL"
5. **Dev Titan nehri dolduruyor** — "GROW TO TITAN" / "DEV TİTANA DÖNÜŞ"
6. **Oyun sonu + YENİ REKOR rozeti** — "ONE MORE RUN?" / "BİR TUR DAHA?"

İpucu: Play Console bir **tanıtım videosu** (YouTube linki) da kabul ediyor. 15 saniyelik
"yavru → kaç → büyü → köpekbalığını ye" videosu reklam materyalinle aynı olabilir.

**Feature graphic (1024×500):** Blue + arkasında kocaman açık ağızlı köpekbalığı silüeti,
sağda oyun adı (Blue's River / Blue'nun Nehri).

---

## Play Console → "Veri güvenliği" formu için cevaplar

(AdMob kullanıldığı için; Google'ın AdMob veri güvenliği rehberiyle karşılaştır.)

- **Veri toplanıyor mu?** Evet (AdMob SDK üzerinden).
- **Toplanan veri türleri:** Cihaz veya diğer kimlikler (reklam kimliği), Uygulama etkinliği
  (uygulama etkileşimleri), Uygulama bilgileri ve performansı (kilitlenme/tanılama), yaklaşık konum (IP'den).
- **Amaç:** Reklam veya pazarlama, Analiz, Dolandırıcılığın önlenmesi.
- **Paylaşılıyor mu?** Evet, Google ile (reklam ağı).
- **Aktarım sırasında şifreleniyor mu?** Evet.
- **Kullanıcı verilerinin silinmesini isteyebilir mi?** Uygulama kendi sunucusunda veri tutmuyor;
  reklam kimliği cihaz ayarlarından sıfırlanabilir.
- **Reklam içeriyor mu?** Evet.
- **Hedef yaş:** 13+ öner (çocuklara yönelik seçersen "Families" politikaları ve sertifikalı reklam ayarları gerekir).
