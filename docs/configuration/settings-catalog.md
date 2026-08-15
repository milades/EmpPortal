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
| `ExternalData:*:ConnectionString` | DBA/Operations | بله | اتصال منابع مزایا و اموال |
| `Payslip:Report:LicenseKey` | Legal/Operations | بله | لایسنس موجود Stimulsoft؛ طبق تصمیم پروژه از پنل قابل تغییر نیست |
| `DevelopmentIdentity:*` | Development | بله | فقط Fake AD؛ در Production ثبت نمی‌شود |

## Runtime Settings قابل مدیریت

تنها نقش `SystemAdministrator` به صفحه `/admin/settings` دسترسی دارد. همه تغییرها Audit
می‌شوند. اگر حداقل یک مقدار وابسته به Startup تغییر کند، برنامه Restart کنترل‌شده را زمان‌بندی
می‌کند و IIS آن را با مقادیر جدید اجرا می‌کند.

| کلید | مقدار اولیه | محدودیت |
|---|---|---|
| `ActiveDirectory:DomainFqdn` | تعیین توسط AD Team | نام کامل DNS دامنه |
| `ActiveDirectory:BaseDn` | تعیین توسط AD Team | Base DN معتبر |
| `ActiveDirectory:DomainControllers:0` | DC اصلی | FQDN سرور LDAPS اول |
| `ActiveDirectory:DomainControllers:1` | DC جایگزین | FQDN سرور LDAPS دوم |
| `ActiveDirectory:LdapsPort` | `636` | ۱ تا ۶۵۵۳۵ |
| `ActiveDirectory:OperationTimeoutSeconds` | `10` | ۱ تا ۶۰ ثانیه |
| `Authentication:SsoEnabled` | `true` | Boolean |
| `Authentication:ManualLoginEnabled` | `true` | حداقل یکی از دو روش باید فعال بماند |
| `Login:AttemptLimit` | `5` | ۱ تا ۲۰ تلاش |
| `Login:AttemptWindowMinutes` | `15` | ۱ تا ۶۰ دقیقه |
| `Session:AbsoluteMinutes` | `180` | ۳۰ تا ۷۲۰ دقیقه |
| `Session:IdleMinutes` | `30` | ۵ تا ۱۸۰ و کوچک‌تر از عمر مطلق |
| `Session:MaxConcurrentPerUser` | `3` | ۱ تا ۱۰ نشست |
| `Session:AdRevalidationSeconds` | `15` | ۱۵ تا ۶۰ ثانیه برای Circuit باز |
| `Jwt:AccessTokenMinutes` | `5` | ۱ تا ۱۵ دقیقه و حداکثر تا پایان Session |
| `Portal:Title` | `پرتال کارمندی` | حداکثر ۱۲۰ نویسه |
| `Portal:FoodReservationExternalUrl` | خالی | URL اختیاری HTTP/HTTPS؛ اعمال فوری |
| `Forms:Pdf:License` | `Professional` | Community، Professional یا Enterprise |
| `Forms:Pdf:RegularFontPath` | فونت Vazirmatn | مسیر فونت معمولی |
| `Forms:Pdf:BoldFontPath` | فونت Vazirmatn | مسیر فونت ضخیم |
| `Payslip:Report:TemplateRelativePath` | `Reports/Payslip.mrt` | مسیر قالب گزارش |
| `Payslip:Report:*Variable` | نام‌های پیش‌فرض | نام متغیرهای قالب؛ لایسنس را شامل نمی‌شود |
| `ExternalData:*:ViewName` | Viewهای نمونه | نام View منبع خارجی |
| `ExternalData:*:PersonnelCodeColumn` | `PersonnelCode` | نام ستون کد پرسنلی |
| `Logging:LogLevel:*` | Information/Warning | سطح استاندارد Microsoft Logging |

## قواعد امنیتی

- تنظیمات Authentication و Session هنگام Startup به‌صورت cross-field اعتبارسنجی می‌شوند؛
  پیکربندی نامعتبر باعث Fail-fast شدن برنامه است.
- وضعیت Disabled حساب AD در هر درخواست جدید Cookie/JWT کنترل و همان لحظه Sessionها باطل
  می‌شود. Circuit فعال Blazor حداکثر در بازه `AdRevalidationSeconds` قطع اعتبار می‌شود.
- تغییر Domain/DC/LDAPS باید ابتدا در محیط Integration واقعی آزموده و سپس همراه Rollback
  Plan اعمال شود.
- تغییر `BootstrapAdministrator:Upn` نقش مدیرهای قبلی را حذف نمی‌کند؛ مدیریت Role پس از
  Bootstrap از SQL و پنل مدیریتی انجام می‌شود.
- مجوز QuestPDF از پنل قابل تغییر است، اما برنامه در Production با مقدار خالی یا `Evaluation`
  شروع نمی‌شود. این تنظیم با لایسنس Stimulsoft فیش حقوقی مستقل است.
