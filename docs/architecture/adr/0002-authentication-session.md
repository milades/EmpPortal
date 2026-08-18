# ADR-0002: احراز هویت، Session و JWT

- وضعیت: پذیرفته‌شده
- تاریخ: 2026-08-03

## تصمیم

- ASP.NET Core Identity مرجع User، Role، Claim، Security Stamp، Cookie و Sign-in است.
- `ApplicationUser : IdentityUser<Guid>` و `ApplicationRole : IdentityRole<Guid>`
  در SQL Server توسط EF Core Identity Store نگه‌داری می‌شوند.
- کد برنامه مجاز به ساخت دستی Authentication Cookie یا ClaimsPrincipal نیست.
- AD Provider فقط SSO/Password را Verify می‌کند؛ JIT Provisioning با `UserManager`
  و ورود برنامه با `SignInManager` انجام می‌شود.
- `WindowsSso` فقط برای Endpoint انتخابی SSO است.
- ورود دستی با UPN و LDAPS انجام می‌شود.
- مرورگر از Application Cookie استاندارد Identity با تنظیمات HttpOnly/Secure استفاده می‌کند.
- API از JWT Bearer کوتاه‌عمر استفاده می‌کند؛ مرورگر آن را فقط در `localStorage` نگه می‌دارد و
  برای درخواست API در Header استاندارد `Authorization: Bearer` می‌فرستد.
- هر دو به ApplicationSession دارای `sid` در SQL وابسته‌اند.
- حداکثر Session سه ساعت، Idle سی دقیقه و حداکثر Session هم‌زمان سه عدد است.
- وضعیت حساب AD حداکثر هر ۶۰ ثانیه بازاعتبارسنجی می‌شود.

## پیامدها

- Logout و Disabled User پیش از پایان عمر JWT قابل ابطال‌اند.
- JWT کاملاً Stateless نیست و Session Store بخشی از مرز امنیتی است.
- JWT به‌دلیل محدودیت Cookie و Policy سازمان در `localStorage` در دسترس JavaScript قرار دارد؛
  عمر پنج‌دقیقه‌ای، عدم ثبت Token در Log، پاک‌سازی هنگام Logout/انقضای Session و اعتبارسنجی
  `sid` سمت سرور کنترل‌های جبرانی این تصمیم‌اند. Application Cookie استاندارد Identity همچنان
  `HttpOnly` و مستقل از JWT باقی می‌ماند.
- Microsoft.Identity.Web استفاده نمی‌شود، زیرا Provider هدف Microsoft Entra است؛
  سامانه فعلی از AD DS داخلی، Windows Authentication و LDAPS استفاده می‌کند.
