# ADR-0001: راهبرد Rendering

- وضعیت: پذیرفته‌شده
- تاریخ: 2026-08-03

## تصمیم

از Blazor Web App روی .NET 10 استفاده می‌شود. ناحیه Account/Auth به‌صورت Static
SSR و ناحیه داخلی پرتال به‌صورت Interactive Server اجرا می‌شود. WebAssembly و
Interactive Auto در فاز اول استفاده نمی‌شوند.

## پیامدها

- Login/Logout در چرخه کامل HTTP به Cookie دسترسی دارند.
- UI داخلی رفتار تعاملی یکنواخت دارد.
- ظرفیت SignalR/Circuit باید Load Test شود.
- State امنیتی یا تجاری نباید فقط در Circuit نگه‌داری شود.
- صفحات Static SSR از Post Form و Form Mapping استفاده می‌کنند و Eventهای C# مانند
  `@onclick` فقط در Interactive Server معتبرند.
- دسترسی داده در Componentهای Interactive با DbContextFactory و Context به‌ازای
  Operation انجام می‌شود.
- جزئیات اجرایی اجباری در ADR-0004 ثبت شده‌اند.
