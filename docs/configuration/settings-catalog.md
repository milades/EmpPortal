# فهرست تنظیمات EmpPortal

این سند نام دقیق کلیدها، محل نگه‌داری و اثر تغییر را مشخص می‌کند. Secret، Password،
Private Key، Connection String و Token هرگز در جدول Runtime Settings یا رابط مدیریتی
ذخیره و نمایش داده نمی‌شوند.

## تنظیمات Bootstrap و عملیات

این موارد فقط از `appsettings.Production.json` خارج از Git، Environment Variableهای
IIS یا Secret Store سازمانی خوانده می‌شوند.

| کلید | مالک | Restart | توضیح |
|---|---|---:|---|
| `ConnectionStrings:PortalDatabase` | DBA/Operations | بله | حساب Runtime فاقد مجوز Migration باشد |
| `AllowedHosts` | Operations | بله | FQDN نهایی پرتال |
| `BootstrapAdministrator:Upn` | Security | بله | UPN حساب اولیه؛ Password در برنامه ذخیره نمی‌شود |
| `DataProtection:KeyRingPath` | Operations | بله | مسیر پایدار با ACL محدود به App Pool |
| `DataProtection:CertificateThumbprint` | Security | بله | Certificate دارای Private Key در LocalMachine/My |
| `Jwt:Issuer` | Security | بله | ترجیحاً URL نهایی HTTPS |
| `Jwt:Audience` | Security | بله | شناسه API |
| `Jwt:SigningCertificateThumbprint` | Security | بله | Certificate امضای JWT، جدا از TLS |
| `Forms:Pdf:License` | Legal/Operations | بله | `Community` فقط در صورت احراز شرایط رسمی؛ در غیر این صورت مجوز خریداری‌شده |
| `Forms:Pdf:RegularFontPath` | Operations | بله | مسیر فونت عادی محلی PDF نسبت به Content Root یا مسیر مطلق |
| `Forms:Pdf:BoldFontPath` | Operations | بله | مسیر فونت Bold محلی PDF نسبت به Content Root یا مسیر مطلق |
| `DevelopmentIdentity:*` | Development | بله | فقط Fake AD؛ در Production ثبت نمی‌شود |

## Runtime Settings قابل مدیریت

تنها نقش `SystemAdministrator` به صفحه `/admin/settings` دسترسی دارد. همه تغییرها Audit
می‌شوند و در نسخه فعلی برای اعمال نیازمند Restart کنترل‌شده هستند.

| کلید | مقدار اولیه | محدودیت |
|---|---|---|
| `ActiveDirectory:DomainFqdn` | تعیین توسط AD Team | نام کامل DNS دامنه |
| `ActiveDirectory:BaseDn` | تعیین توسط AD Team | Base DN معتبر |
| `ActiveDirectory:LdapsPort` | `636` | ۱ تا ۶۵۵۳۵ |
| `ActiveDirectory:OperationTimeoutSeconds` | `10` | ۱ تا ۶۰ ثانیه |
| `Authentication:SsoEnabled` | `true` | Boolean |
| `Authentication:ManualLoginEnabled` | `true` | حداقل یکی از دو روش باید فعال بماند |
| `Session:AbsoluteMinutes` | `180` | ۳۰ تا ۷۲۰ دقیقه |
| `Session:IdleMinutes` | `30` | ۵ تا ۱۸۰ و کوچک‌تر از عمر مطلق |
| `Session:MaxConcurrentPerUser` | `3` | ۱ تا ۱۰ نشست |
| `Session:AdRevalidationSeconds` | `15` | ۱۵ تا ۶۰ ثانیه برای Circuit باز |
| `Jwt:AccessTokenMinutes` | `5` | ۱ تا ۱۵ دقیقه و حداکثر تا پایان Session |
| `Portal:Title` | `پرتال کارمندی` | حداکثر ۱۲۰ نویسه |

`ActiveDirectory:DomainControllers` عمداً از UI فاز اول خارج است؛ تا زمان مشخص شدن
توپولوژی DC، این مقدار فقط توسط Operations در فایل محیطی تعیین می‌شود. ترتیب آرایه، ترتیب
Failover است و اگر خالی باشد `DomainFqdn` به‌عنوان endpoint استفاده می‌شود.

## قواعد امنیتی

- تنظیمات Authentication و Session هنگام Startup به‌صورت cross-field اعتبارسنجی می‌شوند؛
  پیکربندی نامعتبر باعث Fail-fast شدن برنامه است.
- وضعیت Disabled حساب AD در هر درخواست جدید Cookie/JWT کنترل و همان لحظه Sessionها باطل
  می‌شود. Circuit فعال Blazor حداکثر در بازه `AdRevalidationSeconds` قطع اعتبار می‌شود.
- تغییر Domain/DC/LDAPS باید ابتدا در محیط Integration واقعی آزموده و سپس همراه Rollback
  Plan اعمال شود.
- تغییر `BootstrapAdministrator:Upn` نقش مدیرهای قبلی را حذف نمی‌کند؛ مدیریت Role پس از
  Bootstrap از SQL و پنل مدیریتی انجام می‌شود.
- مجوز PDF یک اعلام حقوقی/عملیاتی است و از پنل Runtime قابل تغییر نیست. برنامه در Production با
  مقدار خالی یا `Evaluation` شروع نمی‌شود.
