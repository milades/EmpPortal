# ADR-0004: قراردادهای پیاده‌سازی Blazor SSR

- وضعیت: پذیرفته‌شده
- تاریخ: 2026-08-03

## تصمیم Render Mode

- Account، Login، Logout و SSO Callback: Static SSR و Full Page Request.
- ناحیه داخلی پرتال: Interactive Server با Prerender پیش‌فرض.
- WebAssembly و Interactive Auto در فاز اول مجاز نیستند.

## Button و Event

- در Static SSR، رویدادهای `@onclick` و `@onchange` اجرا نمی‌شوند؛ عملیات با
  Submit Form یا Endpoint انجام می‌شود.
- Button ارسال فرم همیشه `type="submit"` و Button عملیاتی Interactive همیشه
  `type="button"` دارد.
- Logout یک POST ضد CSRF است و با Link GET یا `@onclick` انجام نمی‌شود.
- Handler تعاملی Async است، Double-submit را با State محلی کنترل می‌کند و Exception
  را به Error Boundary/پیام قابل فهم هدایت می‌کند.
- `AsyncActionButton` الگوی مرجع رویداد و جلوگیری از کلیک هم‌زمان است و فقط داخل ناحیه
  Interactive Server استفاده می‌شود؛ وجود `@onclick` آن را برای صفحه Static SSR مناسب نمی‌کند.

## Form و Binding

- Static SSR Form دارای `method="post"` و `FormName` یکتا است.
- مدل با `[SupplyParameterFromForm]` دریافت می‌شود و Entity دیتابیس نیست.
- `EditForm` فقط یکی از `Model` یا `EditContext` را می‌گیرد.
- `DataAnnotationsValidator`، `ValidationSummary` و `ValidationMessage` استفاده می‌شوند.
- `InputText` و سایر Input Componentها با `@bind-Value` به Input Model متصل می‌شوند.
- Login/Logout از Enhanced Navigation استفاده نمی‌کنند تا Cookie/Redirect در چرخه
  کامل HTTP اعمال شود.

## Lifecycle و State

- Side Effect در Initialization ممنوع است، چون Prerender و Interactive Render ممکن
  است Initialization را تکرار کنند.
- داده قابل انتقال با Persistent Component State بین Prerender و Circuit حفظ می‌شود.
- JS interop فقط در `OnAfterRenderAsync` و پس از `firstRender` انجام می‌شود.
- State تجاری، مجوز و Session در Component/Circuit منبع حقیقت نیستند.
- کارهای طولانی Cancellation و Component Disposal را رعایت می‌کنند.

## Data Access

- DbContext به Component Interactive تزریق و برای عمر Circuit نگه‌داری نمی‌شود.
- از `IDbContextFactory<TContext>` و یک Context به‌ازای Operation استفاده می‌شود.
- فرم به Command/Input Model متصل و از Application Service عبور می‌کند.

## Authentication State

- Authentication State از ASP.NET Core Identity و Cookie سرور می‌آید.
- Circuit با Identity Revalidating AuthenticationStateProvider در بازه حداکثر ۶۰
  ثانیه Security Stamp و فعال بودن User را بازاعتبارسنجی می‌کند.
- `[Authorize]`/Policy روی Page یا Endpoint مرز امنیتی است؛ `AuthorizeView` به‌تنهایی
  کنترل دسترسی محسوب نمی‌شود.
