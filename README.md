# EmpPortal

پرتال کارمندی فارسی و راست‌چین بر پایه .NET 10، Blazor Web App، Clean Architecture،
ASP.NET Core Identity، Active Directory، IIS و SQL Server 2022.

## تصمیم‌های اصلی

- صفحات Login/Logout با Static SSR و فرم استاندارد POST/Antiforgery پیاده شده‌اند.
- ناحیه داخلی از Interactive Server استفاده می‌کند؛ event و binding فقط در Componentهای
  interactive اجرا می‌شوند.
- Active Directory فقط هویت SSO یا UPN/Password را اثبات می‌کند. ایجاد User، Role، Cookie،
  Security Stamp و Sign-in کاملاً توسط Microsoft ASP.NET Core Identity انجام می‌شود.
- Cookie مرجع Session مرورگر است؛ JWT کوتاه‌عمر فقط برای API صادر می‌شود و هر دو به Session
  سمت سرور متصل‌اند.
- هیچ CDN یا فونت اینترنتی در Runtime استفاده نمی‌شود.

## اجرای محیط توسعه

پیش‌نیازها: .NET SDK 10، SQL Server 2022 و یک Certificate توسعه معتبر برای HTTPS.

```powershell
dotnet restore --locked-mode
dotnet tool restore
dotnet ef database update `
  --project src\EmpPortal.Infrastructure `
  --startup-project src\EmpPortal.Web
dotnet run --project src\EmpPortal.Web
```

کاربر پیش‌فرض Fake AD برابر `admin@empportal.test` است و در Development هر Password غیرخالی
پذیرفته می‌شود. این Provider خارج از Development ثبت نمی‌شود و Production بدون تنظیمات AD
واقعی Fail-fast خواهد شد.

## ساخت CSS

Bootstrap RTL و Vazirmatn داخل `wwwroot` نگه‌داری می‌شوند. Tailwind فقط هنگام توسعه/CI لازم
است و خروجی minified آن نیز در Repository قرار دارد:

```powershell
pnpm install
pnpm css:build
```

در شبکه سازمانی، registry مربوط به pnpm باید به feed داخلی npm اشاره کند. Node یا pnpm روی
Windows Server عملیاتی لازم نیست. پس از اولین restore موفق از feed داخلی، فایل
`pnpm-lock.yaml` تولیدشده باید Commit شود و CI از `pnpm install --frozen-lockfile` استفاده کند.

## کنترل کیفیت و انتشار

```powershell
dotnet build EmpPortal.sln --no-restore
dotnet test EmpPortal.sln --no-build
.\deploy\Publish-Intranet.ps1
```

اسناد مرجع:

- [نقشه راه](docs/ROADMAP.md)
- [استقرار ۰ تا ۱۰۰ آفلاین (آموزشی)](docs/PRODUCTION-OFFLINE-ROLLOUT.md)
- [فرم تکمیل‌شونده Go/No-Go عملیاتی](docs/PRODUCTION-FILLABLE-CHECKLIST.md)
- [Workflow توسعه](docs/DEVELOPMENT-WORKFLOW.md)
- [وضعیت پیاده‌سازی](docs/IMPLEMENTATION-STATUS.md)
- [تنظیمات](docs/configuration/settings-catalog.md)
- [استقرار IIS](docs/DEPLOYMENT-IIS.md)
