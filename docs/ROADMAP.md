# نقشه راه توسعه EmpPortal از صفر تا استقرار

## هدف و خط مبنا

EmpPortal یک پرتال کارمندی فارسی و راست‌چین بر پایه .NET 10، Blazor Web App،
Clean Architecture، IIS و SQL Server 2022 است. مدیریت User، Role، Claim، Cookie،
Security Stamp و Sign-in توسط ASP.NET Core Identity انجام می‌شود. Active Directory
فقط منبع اثبات هویت SSO/LDAPS است و JWT فقط در مرز API استفاده خواهد شد.

محدوده فاز اول شامل صفحه انتخاب ورود، SSO، ورود دستی با UPN و LDAPS، خروج مستقل
از Windows Session، داشبورد، مدیریت نقش‌ها و تنظیمات پایه، Audit و بسته استقرار
آفلاین است.

فاز محصولی دوم، یعنی فرم‌ساز داخلی، اکنون در کد تکمیل شده است: طراحی پویا، نسخه‌بندی، زمان‌بندی،
ACL، ثبت داده، گزارش جدولی، Excel، چاپ و PDF. Workflow تأیید در فاز بعد و فایل/امضا و نمودارهای
آماری نیز طبق backlog مصوب در فازهای بعدی اضافه می‌شوند. جزئیات تحویل در
`docs/PHASE2-FORM-BUILDER.md` است. شماره‌گذاری فازهای زیر، نقشه اولیه ساخت زیرساخت فاز اول است.

## اصول تحویل

- هر فاز فقط پس از عبور از Gate همان فاز بسته می‌شود.
- هیچ Secret، Password، Private Key یا Token وارد Git نمی‌شود.
- Migration دیتابیس در Startup برنامه اجرا نمی‌شود و جزئی از فرایند Deployment است.
- Static SSR برای Account/Auth و Interactive Server برای ناحیه داخلی پرتال استفاده می‌شود.
- پیاده‌سازی احراز هویت خارج از `UserManager`، `SignInManager` و Storeهای Identity ممنوع است.
- Componentها باید قواعد Render Mode، Form Binding و Lifecycle مخصوص Blazor SSR را رعایت کنند.
- تمام دارایی‌های UI، فونت‌ها و Packageهای زمان اجرا باید در اینترانت قابل استفاده باشند.
- وضعیت تجاری و امنیتی در Circuit نگه‌داری نمی‌شود.

## فاز ۰ ـ کشف، تصمیم‌گیری و آماده‌سازی (۰ تا ۸ درصد)

### خروجی‌ها

- ثبت ADRهای Rendering، Authentication/Session و Active Directory.
- تعیین Domain FQDN، UPN suffixها، DCها، Forest/Trust، SPN و Chrome GPO.
- تعیین RPO/RTO، توپولوژی SQL و سیاست Backup/Restore.
- تعیین URL نهایی، TLS Certificate، حساب gMSA و مالکیت عملیات.
- ایجاد Risk Register، Settings Catalog و Deployment Checklist.
- آماده‌سازی Node.js LTS یا Tailwind CLI در محیط توسعه/CI؛ Node روی سرور عملیاتی لازم نیست.
- الزام Hosting Bundle و Packageهای .NET 10 روی آخرین Patch امنیتی پشتیبانی‌شده؛
  نسخه 10.0.9 به‌دلیل Advisoryهای High قابل استفاده نیست.

### Gate خروج

- تصمیم‌های معماری بدون ابهام بحرانی ثبت شده باشند.
- مالک هر وابستگی بیرونی مشخص باشد.
- راه‌اندازی محیط Integration دارای AD آزمایشگاهی برنامه‌ریزی شده باشد.

## فاز ۱ ـ اسکلت مهندسی و Clean Architecture (۸ تا ۱۸ درصد)

### خروجی‌ها

- Solution و پروژه‌های Domain، Application، Infrastructure و Web.
- پروژه‌های Unit، Integration، Architecture و End-to-End Test.
- Nullable، TreatWarningsAsErrors، Analyzerها و Central Package Management.
- تنظیمات محیطی، Health Checks اولیه، Error Handling و Correlation ID.
- ASP.NET Core Identity با کلید Guid، Role Store و EF Core/SQL Server.
- قراردادهای اجباری Static SSR Form و Interactive Server Event Handling.
- Bootstrap RTL، فونت فارسی محلی و Pipeline ساخت Tailwind.
- CI داخلی برای Restore، Build، Test، Format و Publish.

### Gate خروج

- Clone/Restore/Build/Test آفلاین یا از Package Feed داخلی موفق باشد.
- وابستگی لایه‌ها توسط Architecture Test کنترل شود.
- برنامه بدون دسترسی اینترنت اجرا شود.

## فاز ۲ ـ Vertical Slice احراز هویت در Development (۱۸ تا ۳۲ درصد)

### خروجی‌ها

- `IEnterpriseIdentityProvider` و Development/Fake Provider.
- صفحه انتخاب SSO یا ورود دستی.
- ورود دستی Development با UPN فرضی و Secret خارج از Git.
- `ApplicationUser`، `ApplicationRole` و ApplicationSession در SQL.
- ورود از مسیر `SignInManager` و Cookie استاندارد و شخصی‌سازی‌شده Identity.
- Cookie امن، Logout، Session سه‌ساعته و Idle سی‌دقیقه‌ای.
- Bootstrap اولین SystemAdministrator.
- داشبورد اولیه فارسی و RTL.

### Gate خروج

- مسیر Login → Dashboard → Logout به‌صورت End-to-End موفق باشد.
- ورود خودکار بعد از Logout رخ ندهد.
- Fake Provider در Production با Fail-fast متوقف شود.

## فاز ۳ ـ اتصال واقعی Active Directory (۳۲ تا ۴۷ درصد)

### خروجی‌ها

- SSO با IIS Windows Authentication و Challenge محدود به `/auth/sso`.
- ورود دستی با LDAPS، UPN و اعتبارسنجی کامل Certificate؛ نتیجه Bind فقط به
  `UserManager`/`SignInManager` تحویل می‌شود و Cookie دستی تولید نمی‌شود.
- کشف DC با DNS/DC Locator و Failover کنترل‌شده.
- نگاشت UPN به SID و objectGUID و JIT Provisioning کاربر.
- خطاهای عمومی، Rate Limit و جلوگیری از ثبت Credential در Log.
- تست Chrome GPO، Kerberos/SPN و جلوگیری از NTLM ناخواسته.

### Gate خروج

- SSO و Manual Login روی IIS واقعی و کلاینت Domain-joined تأیید شوند.
- سناریوهای Disabled، Locked، Password Expired و AD Unavailable تست شوند.
- Wireshark/LDP یا ابزار مصوب سازمان امن بودن LDAPS را تأیید کند.

## فاز ۴ ـ Session، JWT و ابطال امنیتی (۴۷ تا ۶۰ درصد)

### خروجی‌ها

- Cookie مرجع برای مرورگر و JWT Bearer پنج‌دقیقه‌ای برای API.
- Refresh Token چرخشی و Hash‌شده در صورت نیاز API مستقل.
- اعتبارسنجی `sid` روی هر درخواست محافظت‌شده.
- Revalidation حساب‌های فعال AD با SLA حداکثر ۶۰ ثانیه.
- ابطال همه Sessionها هنگام Disabled شدن حساب.
- Data Protection Key Ring پایدار و Signing Certificate قابل Rotation.
- Security Stamp Validator برای Requestهای HTTP و Identity Revalidating
  AuthenticationStateProvider با بازه حداکثر ۶۰ ثانیه برای Circuitها.
- Antiforgery، CSP، Security Headers و تست Session Fixation.

### Gate خروج

- JWT باطل‌شده حتی پیش از `exp` رد شود.
- Recycle شدن IIS باعث خرابی کنترل‌نشده Sessionها نشود.
- Disabled شدن کاربر در بازه SLA تمام دسترسی‌ها را متوقف کند.

## فاز ۵ ـ RBAC، تنظیمات و Audit (۶۰ تا ۷۲ درصد)

### خروجی‌ها

- Role/Permissionهای SQL و `AuthorizationVersion`.
- نقش‌های اولیه `SystemAdministrator` و `Employee`.
- جلوگیری از حذف آخرین مدیر فعال.
- پنل تنظیمات Runtime همراه Validate/Test/Apply/Rollback.
- ثبت Audit برای Login، Logout، تغییر Role، تنظیمات و Session Revoke.
- جداسازی Bootstrap Settings، Runtime Settings و Secretها.

### Gate خروج

- تغییر Permission بدون انتظار برای انقضای JWT مؤثر شود.
- تغییرات حساس دارای قبل/بعد، عامل، زمان و Correlation ID باشند.
- هیچ Secret از UI یا API قابل بازیابی نباشد.

## فاز ۶ ـ UX/UI و داشبورد فاز اول (۷۲ تا ۸۲ درصد)

### خروجی‌ها

- Design Tokens و قرارداد استفاده هم‌زمان Bootstrap RTL و Tailwind.
- فونت فارسی WOFF2 محلی، `dir=rtl` و `lang=fa`.
- Layout ریسپانسیو، داشبورد، منوی کاربر و تجربه خطا/عدم دسترسی.
- Loading، Empty، Error و Offline-friendly Stateها.
- Accessibility، Keyboard Navigation، Focus و Contrast.

### Gate خروج

- تست Chrome در Mobile/Tablet/Desktop موفق باشد.
- هیچ درخواست CDN، Google Fonts یا اینترنتی وجود نداشته باشد.
- معیارهای UX، RTL و Accessibility تأیید شوند.

## فاز ۷ ـ QA، Hardening و Performance (۸۲ تا ۹۰ درصد)

### خروجی‌ها

- Unit، Integration، Architecture، E2E و Security Test Suite.
- Load Test برای تعداد Circuit و Session هم‌زمان هدف.
- Threat Model و بررسی OWASP ASVS متناسب با سامانه.
- تست CSRF، XSS، Brute Force، Replay، Token Theft و Privilege Escalation.
- Logging، Health Monitoring، Alerting و Runbook رخداد.

### Gate خروج

- هیچ Finding بحرانی یا High حل‌نشده وجود نداشته باشد.
- ظرفیت و زمان پاسخ با معیار غیرعملکردی مصوب سازگار باشد.
- Restore دیتابیس، Key Ring و Certificate در محیط آزمایش موفق باشد.

## فاز ۸ ـ بسته استقرار آفلاین و UAT (۹۰ تا ۹۷ درصد)

### خروجی‌ها

- Publish نوع Framework-dependent برای `win-x64` و Hosting Bundle مصوب.
- Artifact نسخه‌دار، Hash، SBOM، Migration Script و Release Notes.
- Runbook نصب IIS، Certificate، SPN، App Pool و File ACL.
- Full/Differential/Log Backup Job و تست Restore.
- استقرار UAT روی زیرساخت مشابه Production.
- آموزش مدیر سیستم و تحویل راهنمای عملیات.

### Gate خروج

- Artifact بدون Build مجدد در UAT نصب شود.
- Rollback برنامه و دیتابیس تمرین شده باشد.
- صورت‌جلسه UAT و مجوز Release صادر شود.

## فاز ۹ ـ Cutover، Go-Live و تثبیت (۹۷ تا ۱۰۰ درصد)

### خروجی‌ها

- Backup نهایی، اعمال Migration، استقرار و Smoke Test.
- کنترل SSO، Manual Login، Logout، Dashboard، Audit و Revoke.
- پایش فشرده پس از انتشار و مسیر Escalation.
- بازبینی ۲۴ ساعت، ۷ روز و ۳۰ روز پس از Go-Live.
- انتقال Backlog فاز دوم بر اساس داده واقعی استفاده.

### Gate نهایی

- شاخص‌های امنیت، دسترس‌پذیری و پشتیبان‌گیری سبز باشند.
- مالکیت Support، Patch و Incident Response تحویل شده باشد.

## وابستگی‌های باز پیش از Production

- اطلاعات دقیق Domain/DC/Forest/Trust و SPN.
- RPO، RTO، HA و محل ثانویه Backup SQL Server.
- تعداد کاربران و Peak Concurrent Users برای Capacity Test.
- نسخه سازمانی Node.js/Tailwind CLI و Package Feed داخلی.
- نام نهایی DNS و Certificateهای HTTPS/LDAPS.
