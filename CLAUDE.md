# Durum & Proje Özeti — Ödeme Ağ Geçidi Simülasyonu

> Bu belge tek kaynak. Claude Code'da context olarak yapıştır, Notion'da takip et, ve her kavramda önce "Öğren" kısmını oku/izle, sonra kodla.

---

## 0. Bu doküman nasıl kullanılır

- **Claude Code:** Yeni bir sohbet aç, bu belgeyi context olarak ver. Aşağıdaki "Claude Code çalışma kuralları" bölümü, ona nasıl yardım etmesi gerektiğini söylüyor.
- **Notion:** "Fazlı plan" bölümündeki checklist'i kopyala, anlık takip et. v1 bitiş çizgisi net işaretli.
- **Öğren-önce yaklaşımı:** Bir fazın koduna dalmadan önce, o fazın "Öğren" kaynaklarını oku/izle. Mantığı anlamadan kod yazma — zaten senin tarzın bu, belge buna göre kurulu.

---

## 1. Ben kimim (developer profili)

- **Seviye/odak:** Junior, backend-ağırlıklı full-stack.
- **Stack:** .NET / C# (ana), Java / Spring Boot, React, Firebase.
- **Gerçek deneyim:** Traick stajında (sağlık AI) `.NET` ile multi-tenant bir DICOM REST API kurdum (TraickMiniDicom): EF Core + PostgreSQL, JWT auth, global query filter ile organizasyon bazlı veri izolasyonu, katmanlı mimari (Controller/Service/DTO ayrımı), `ServiceResult<T>` ↔ `ApiResponse<T>` ayrımı, BaseEntity ile otomatik audit alanları, Mapster.
- **Bilinen kaslarım:** katmanlı API tasarımı, EF Core, JWT auth, multi-tenant izolasyon, DTO/servis ayrımı, dependency injection, Postgres.
- **Hedefim:** Fintech / banka backend rolleri (ödeme, POS, bankacılık sistemleri). Bu proje o hedefe birebir hizmet ediyor.
- **İş ilanlarında görüp henüz yapmadığım şeyler:** Docker, CI/CD, Redis, cloud (Azure/AWS), mesaj kuyrukları (Kafka/RabbitMQ). Bu proje bunları *doğal ihtiyaçtan* öğretiyor.
- **Çalışma tarzım:** Mantığı önce öğrenmek → tek hatta odaklanmak → mükemmelleştirmek yerine **bitirmek**. Beni en çok yoran şey emek verip sonuç alamamak; bu yüzden bu proje "bitmiş, deploy edilmiş, gösterilebilir sonuç" verecek şekilde kurgulandı.

---

## 2. Proje özeti

- **Ne:** iyzico / Stripe gibi bir ödeme sağlayıcının *mantığını* taklit eden bir .NET Web API.
- **Neden:** (1) fintech hedefiyle birebir örtüşen portföy sinyali, (2) tüm altyapı araçlarını gerçek bir ihtiyaç üzerinden öğrenme.
- **KAPSAM DIŞI (kritik):** Gerçek kart verisiyle ASLA çalışılmaz. Gerçek PAN = PCI-DSS = bu projenin işi değil. Sadece standart test kartları kullanılır. Amaç mantığı simüle etmek.

---

## 3. Teknik kararlar & mimari

- **Stack:** .NET Web API, EF Core, PostgreSQL. Katmanlı yapı (TraickMiniDicom deseninin aynısı).
- **Çekirdek varlıklar:** `Merchant` (üye işyeri), `Payment` (işlem). Opsiyonel ileri: `Ledger` (çift girişli defter).
- **Durum makinesi (state machine):**
  - `Pending → Authorized → Captured → Refunded`
  - `Authorized → Voided`
  - `Pending → Failed`
  - Geçersiz geçişler engellenir (ör. `Refunded` bir işlem tekrar refund edilemez).
- **Auth:** Üye işyeri API key ile (`Authorization: Bearer sk_test_...`) — gerçek gateway'ler gibi.
- **Test kartları:**
  - `4242 4242 4242 4242` → başarılı (Authorized)
  - `4000 0000 0000 0002` → reddedildi (Failed)
  - Luhn geçersiz → doğrulama hatası
- **Idempotency:** `Idempotency-Key` header ile. İlk isteğin cevabı saklanır; aynı key ile ikinci istek gelirse yeni işlem yaratılmaz, saklanan cevap döner.
- **Webhook:** İşlem durumu değişince üye işyerinin URL'sine `POST`. HMAC imzası + başarısızlıkta exponential backoff ile retry.

---

## 4. Öğren-önce rehberi (her kavram: Ne / Neden / Kaynak)

> Kod yazmadan önce ilgili kavramın kaynağını oku. Doğrulanmış linkler doğrudan verildi; olmayan yerlerde arama terimi verildi.

### Idempotency (Faz 2 — projenin tacı)
- **Ne:** Aynı isteğin yanlışlıkla iki kez işlenmesini engellemek. Müşteri "öde"ye iki kez basarsa tek kez çekilir.
- **Neden:** Ödeme sistemlerinin kalbi ve fintech mülakatının klasik sorusu.
- **Oku:**
  - Stripe resmi: https://docs.stripe.com/api/idempotent_requests
  - Derin mantık (Postgres ile): https://brandur.org/idempotency-keys
  - Sistem tasarımı anlatımı: https://newsletter.systemdesign.one/p/idempotent-api

### Webhook + HMAC imza (Faz 3)
- **Ne:** Bir olay olunca karşı tarafın sunucusuna anlık bildirim göndermek; imzayla bildirimin gerçekliğini doğrulatmak.
- **Neden:** Asenkron event bildirimi + güvenlik. Event-driven mimarinin girişi.
- **Oku:**
  - Stripe webhooks: https://docs.stripe.com/webhooks
  - İmza doğrulama: https://docs.stripe.com/webhooks/signature

### Durum makinesi (state machine) (Faz 1)
- **Ne:** Bir işlemin hangi durumdan hangisine geçebileceğini tanımlayan kurallar.
- **Neden:** Ödeme akışının omurgası; geçersiz geçişleri engeller.
- **Ara:** "finite state machine payment transaction states"

### Luhn algoritması (Faz 1)
- **Ne:** Bir kart numarasının matematiksel olarak geçerli olup olmadığını kontrol eden basit algoritma.
- **Neden:** Sahte kart doğrulamanın ilk adımı.
- **Oku:** Wikipedia — "Luhn algorithm" (ara: `Luhn algorithm wikipedia`)

### BackgroundService / Worker (Faz 3 ve 7)
- **Ne:** Arka planda sürekli/periyodik çalışan .NET servisi (webhook retry, settlement işleri için).
- **Neden:** Zamanlanmış ve asenkron işlerin motoru.
- **Oku:**
  - Worker Services: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
  - Hosted services (ASP.NET Core): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services
  - Scope yönetimi: https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service

### Docker (Faz 4)
- **Ne:** Uygulamayı, çalışması için gereken her şeyle bir "kutuya" koyup her ortamda aynı çalıştırmak.
- **Neden:** Deploy'un standardı; diğer araçların (Redis, Postgres, kuyruk) kapısı.
- **Oku:** Microsoft Learn — "Containerize a .NET app" (ara: `containerize a .NET app microsoft learn`) + Docker resmi "Get started".

### CI/CD — GitHub Actions (Faz 5)
- **Ne:** Kod push edilince otomatik build + test + deploy eden hat.
- **Neden:** Hem sektör standardı hem de otomasyonun kendisi.
- **Oku:** GitHub Docs — "Building and testing .NET" (ara: `github actions building and testing .net`)

### Cloud deploy (Faz 6)
- **Ne:** Container'ı 7/24 çalışan kiralık sunucuya çıkarmak.
- **Neden:** Uygulamanın canlı ve erişilebilir olması; "deploy ettim" diyebilmek.
- **Oku:** Azure App Service quickstart / "Deploy a Worker Service to Azure" (ara: `deploy .net container azure app service quickstart`). Başlangıç için Railway/Render daha basit alternatif.

### Redis (Faz 7 — opsiyonel)
- **Ne:** Çok hızlı, RAM tabanlı geçici hafıza (cache, rate limit).
- **Neden:** Idempotency cache + üye işyeri istek limiti için. Kişisel ölçekte şart değil ama bir kez öğrenmeye değer.
- **Ara:** `StackExchange.Redis getting started` + `redis caching aspnet core`

### Mesaj kuyruğu — RabbitMQ / Kafka (Faz 7 — opsiyonel, ileri)
- **Ne:** Olayları bir kuyruğa atıp asenkron işlemek (publish/subscribe).
- **Neden:** Webhook gönderimi ve settlement gibi işlerin doğal evi. **RabbitMQ ile başla, Kafka'ya sonra.** Kişisel ölçekte zorlama — bunu bilinçli olarak sona bıraktık.
- **Oku:** .NET Queue Service: https://learn.microsoft.com/en-us/dotnet/core/extensions/queue-service + (ara: `rabbitmq .net client tutorial`)

---

## 5. Fazlı plan (Notion checklist)

> **v1 = Faz 0–3 + deploy.** Orası bitince mülakata hazır bir şeyin var. Faz 4 sonrası bonus. Her fazda: kod → commit → kısa README notu.

**Faz 0 — İskelet & tasarım**
- [ ] Katmanlı proje yapısı (TraickMiniDicom deseni)
- [ ] Durum makinesini kâğıda çiz
- [ ] `Merchant`, `Payment` modelleri
- [ ] Postgres + EF Core bağlantısı

**Faz 1 — Çekirdek ödeme akışı**
- [ ] Üye işyeri API key auth
- [ ] `POST /v1/payments` (authorize)
- [ ] `POST /v1/payments/{id}/capture`
- [ ] `POST /v1/payments/{id}/refund`
- [ ] `POST /v1/payments/{id}/void`
- [ ] `GET /v1/payments/{id}`
- [ ] Luhn kontrolü + test kartı kuralları
- [ ] Geçersiz durum geçişlerini engelle

**Faz 2 — Idempotency ⭐**
- [ ] `Idempotency-Key` header'ını oku
- [ ] Aynı key → saklanan cevabı dön
- [ ] Idempotency kayıtlarını sakla

**Faz 3 — Webhook (v1 biter)**
- [ ] Durum değişince üye işyeri URL'sine POST
- [ ] HMAC imza
- [ ] Retry + exponential backoff
- [ ] Birim testler (idempotency + durum geçişi)
- [ ] ✅ **v1 tamam**

**Faz 4 — Dockerize**
- [ ] Dockerfile
- [ ] docker-compose (API + Postgres)

**Faz 5 — CI/CD**
- [ ] GitHub Actions: build + test
- [ ] Test geçmezse deploy engelle

**Faz 6 — Cloud deploy**
- [ ] Container'ı cloud'a çıkar
- [ ] Managed Postgres
- [ ] Canlı URL

**Faz 7 — Event-driven & kuyruk (opsiyonel)**
- [ ] Webhook/settlement'ı kuyruğa taşı (RabbitMQ)
- [ ] Redis: idempotency cache + rate limit

**Faz 8 — Ledger & mutabakat (opsiyonel)**
- [ ] Çift girişli defter
- [ ] Üye işyeri bakiyesi
- [ ] Gün sonu mutabakat raporu

---

## 6. Claude Code çalışma kuralları

> Bunları Claude Code'a talimat olarak ver:

- **Önce mantığı anlat, sonra kodla.** Bir parçayı yazmadan önce ne yaptığını ve neden öyle yaptığını kısaca açıkla. Ben öğren-önce çalışıyorum; kod dökümü değil, anlayış istiyorum.
- **Küçük adımlarla ilerle.** Tek seferde tüm projeyi scaffold etme. Faz faz, endpoint endpoint gidelim.
- **Mevcut konvansiyonlarıma uy:** interface + implementation ayrımı, DI (scoped), `ServiceResult<T>` ↔ `ApiResponse<T>` ayrımı, `BaseEntity` audit alanları — TraickMiniDicom'daki gibi.
- **Kod yazınca test edilebilirliği gözet.** Faz 3'te birim test yazacağız; ona uygun tasarla.
- **Gerçek kart verisi / PCI kapsamı önerme.** Sadece test kartları ve simülasyon.
- **Beni ezme.** Bir seferde bir sonraki adıma odaklan; "hepsini şimdi yapalım" deme.

---

## 7. Kişisel guardrail'ler (yön kaybetmemek için)

- **İki hat, o kadar:** Birincil = bu ödeme projesi (iş/gelir). Yan = DJ aracı (tutku/moral). Üçüncü bir şeye dalma.
- **Bitir > mükemmelleştir.** v1'de (Faz 3) dur, cilalama tuzağına düşme.
- **Öğrenirken başvurmaya devam et.** İlanların tamamını bitirmeyi bekleme.
- **Kafka'yı zorlama.** Ne zaman *kullanmayacağını* bilmek de mühendislik. Doğal evi Faz 7.
- **Sonuç odağı:** Her faz bitmiş, gösterilebilir bir kazanım. Motivasyonu besleyen bu.

---

## 8. Git, CI/CD & günlük takip iş akışı

**Git / GitHub (ilk günden itibaren):**
- Proje baştan bir GitHub reposuyla senkron çalışır. Faz 0'da repo oluşturulur (`git init` + ilk push).
- Anlamlı her adımda commit atılır — küçük, sık ve açıklayıcı mesajlarla (ör. "authorize endpoint eklendi", "idempotency tamam").
- Her yeni faz/özellik için ayrı branch açılır (ör. `feature/idempotency`), bitince `main`'e merge edilir. Küçük işler doğrudan `main`'e gidebilir; kural: iş mantıklı bir bütünse branch.

**CI/CD:** Faz 5'te öğrenilecek (GitHub Actions ile build + test hattı). Repo GitHub'da olduğu için doğal olarak üstüne biner.

**Günlük Notion takibi:**
- Her gün, o gün yapılan görevler ve tamamlananlar Notion'a gün gün not edilir.
- Gün sonunda aşağıdaki "gün sonu" komutuyla Claude Code'dan hazır not bloğu alınır, Notion'a yapıştırılır.

**Faz başı görev listesi (Notion'a yapıştırmak için):**
- Her yeni faza başlarken, o fazın görevlerini Notion'a kopyala-yapıştır olacak şekilde çıkar ve bana ver (checklist formatında, tek blok).
- Notion'u ben yönetiyorum; sen sadece yapıştırılmaya hazır metni üretiyorsun. Notion'a doğrudan erişimin/yazma yetkin yok.
- Faz ilerledikçe "şu madde bitti" diye bana söyle; kutuları Notion'da ben işaretlerim.

**Claude Code'a talimat:**
- Gerektiğinde branch aç ve commit at; ne yaptığını bana söyle.
- Ama `push`, `merge`, force veya geri alınması zor git işlemlerini yapmadan önce **onayımı bekle**.

### "Gün sonu" komutu
Ben "gün sonu" ya da "bugünü kapat" dediğimde, bu oturumdaki işlere bakarak Notion'a
yapıştıracağım hazır bir not üret. Şu formatta, kopyala-yapıştır olacak tek blok halinde ver:

- **Tarih & faz:** (bugünün tarihi + hangi fazdayız)
- **Bugün ne yaptık:** (madde madde, kısa)
- **Ne öğrendim:** (yeni kavramlar / "aha" anları — kısa açıklamayla)
- **Neyle boğuştuk & nasıl çözdük:** (bug + çözüm, ileride arayınca bulayım diye)
- **Yarın ilk iş:** (bir sonraki somut adım, boş sayfa korkusu olmasın diye)
- **CLAUDE.md'ye taşınmalı mı?** (bugün kalıcı bir mimari karar/kural çıktıysa ayrıca belirt)

Not: Bu özeti Notion için üretiyorsun; CLAUDE.md'yi sen güncelleme, sadece bana metni ver.

---

## 9. Bu hafta tek hedef

Faz 0 + Faz 1'in ilk endpoint'i: `POST /v1/payments` çalışsın ve `4242...` (başarılı) ile `4000...0002` (başarısız) test kartlarını ayırt etsin. O kadar. Gerisi sıradaki hafta.