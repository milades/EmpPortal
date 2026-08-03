# وضعیت پیاده‌سازی فاز اول

## تکمیل‌شده و قابل آزمون محلی

- Clean Architecture روی .NET 10 با Domain، Application، Infrastructure، Web و چهار پروژه تست.
- Blazor Web App با Static SSR برای Account و Interactive Server برای ناحیه داخلی.
- ASP.NET Core Identity/EF Core/SQL Server برای User، Role، Security Stamp و Sign-in Cookie.
- Fake AD توسعه، SSO توسعه و Manual Login با UPN فرضی.
- Windows SSO endpoint و Provider واقعی LDAPS با Certificate Validation و DC Failover.
- نشست SQL با عمر مطلق ۱۸۰ دقیقه، Idle سی دقیقه و سقف سه نشست هم‌زمان.
- JWT کوتاه‌عمر برای API با `sid`، Authorization Version و ابطال سمت سرور.
- خروج مستقل از Windows، ابطال Cookie/JWT، Rate Limit، Antiforgery و Security Headers.
- Roleهای SQL، Bootstrap مدیر اولیه، صفحه تنظیمات Runtime و Audit امنیتی.
- Bootstrap RTL، Tailwind pipeline، فونت Vazirmatn و تمام دارایی‌های Runtime به‌صورت محلی.
- Health endpoint، Migration idempotent، Publish Profile و اسکریپت IIS/ACL/HTTPS.

## Gateهای وابسته به زیرساخت سازمان

موارد زیر کدنویسی ناقص نیستند و فقط روی زیرساخت واقعی قابل نهایی‌شدن‌اند:

- FQDN نهایی، دو یا چند DC، Base DN، وضعیت Forest/Trust و UPN suffixها.
- gMSA، SPN و Chrome GPO برای تأیید Kerberos و سیاست NTLM.
- Certificateهای Production و محل پایدار Data Protection Key Ring.
- SQL HA/Backup، RPO/RTO، حساب Migration و حساب Runtime.
- تعداد کاربر و Peak Concurrent Circuit برای Load/Capacity Test.
- feed داخلی npm برای تولید و Commit اولین `pnpm-lock.yaml`؛ خروجی CSS لازم برای Runtime
  در Repository وجود دارد.
- UAT سناریوهای Disabled/Locked/Expired و قطع دسترسی DC روی IIS Domain-joined.

تا زمان عبور این Gateها، Artifact برای Production «Release Approved» محسوب نمی‌شود؛ با این
حال مسیر توسعه محلی و اجرای کامل جریان فاز اول مستقل از AD واقعی فراهم است.
