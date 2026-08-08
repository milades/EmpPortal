# گردش‌کار توسعه EmpPortal

## محیط‌ها

1. **Local Development**: Fake AD، SQL Server 2022 Developer و Secretهای محلی.
2. **Integration**: IIS، Domain آزمایشگاهی، چند حساب AD و LDAPS واقعی.
3. **UAT**: توپولوژی نزدیک Production و داده غیرواقعی/ماسک‌شده.
4. **Production**: IIS و SQL Server 2022 داخل اینترانت.

هیچ اتصال مستقیم از محیط توسعه به AD یا SQL عملیاتی مجاز نیست.

## جریان هر تغییر

```text
Work Item
  → Definition of Ready
  → ADR در صورت تغییر معماری
  → Branch کوتاه‌عمر
  → Code + Tests + Documentation
  → Local Quality Gate
  → Pull Request و Review
  → CI Gate
  → Merge به main
  → Artifact نسخه‌دار
  → Integration/UAT
  → Release Approval
  → Offline Deployment
  → Smoke Test و Monitoring
```

## مدیریت شاخه‌ها

- `main` همیشه قابل انتشار و محافظت‌شده است.
- شاخه‌های `feature/<work-item>-<summary>`، `fix/...` و `chore/...` کوتاه‌عمرند.
- Commit مستقیم روی `main` ممنوع است.
- تغییرات کوچک و قابل Review نگه داشته می‌شوند.
- Merge پس از حداقل یک Review و عبور همه Gateها انجام می‌شود.
- Release با Tag نسخه معنایی مانند `v1.0.0` مشخص می‌شود.

تا زمان ایجاد Remote داخلی، همین قرارداد روی مخزن محلی رعایت و سپس بدون بازنویسی
History به Git Server سازمان منتقل می‌شود.

## Definition of Ready

- هدف کسب‌وکار و Acceptance Criteria روشن است.
- اثر امنیتی و داده‌ای مشخص است.
- وابستگی خارجی، Migration و تغییر تنظیمات شناسایی شده‌اند.
- Mockup یا رفتار UI برای تغییرات ظاهری مشخص است.
- روش تست و Rollback تعریف شده است.

## Definition of Done

- کد، تست و مستندات تکمیل شده‌اند.
- Build بدون Warning و تست‌ها موفق‌اند.
- داده حساس وارد Log، Source یا Artifact نشده است.
- UI فارسی، RTL، ریسپانسیو و Keyboard-accessible است.
- Migration و Script بازگشت/Forward-fix بررسی شده‌اند.
- Observability و Audit متناسب اضافه شده است.
- Acceptance Criteria در Integration تأیید شده‌اند.

## Quality Gate محلی و CI

پس از ایجاد Lock Fileها، ترتیب استاندارد به شکل زیر است:

```powershell
dotnet restore --locked-mode
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
dotnet format --verify-no-changes
dotnet publish src/EmpPortal.Web --no-build --configuration Release --runtime win-x64 --no-self-contained
```

Pipeline باید علاوه بر موارد بالا این کنترل‌ها را انجام دهد:

- Architecture dependency tests
- Dependency vulnerability scan از Feed/Database داخلی
- Secret scan
- تولید SBOM و Hash Artifact
- بررسی عدم وجود URL/CDN خارجی در خروجی Web
- نگه‌داری Test Result و Release Metadata

## تست‌ها

- **Unit**: قواعد Domain و Application بدون SQL/AD/IIS.
- **Architecture**: جهت Dependencyها و ممنوعیت وابستگی Domain به Infrastructure.
- **Integration**: SQL، EF Core، Session Store و Fake/Real AD Adapter.
- **E2E**: Login، SSO، Manual، Logout، RBAC، Settings و Dashboard.
- **Security**: CSRF، XSS، Rate Limit، Session Fixation، Replay و Revocation.
- **Performance**: Circuit، LDAP revalidation و ظرفیت SQL.

تست Windows/Kerberos فقط در Integration Domain معتبر است و با TestServer یا Fake AD
جایگزین نمی‌شود.

## Workflow دیتابیس

1. تغییر مدل همراه Migration در همان Pull Request ثبت می‌شود.
2. SQL تولیدشده Review می‌شود؛ Migration مخرب نیازمند برنامه تبدیل داده است.
3. برنامه در Startup هیچ Migration خودکاری اجرا نمی‌کند.
4. Deployment Identity جداگانه Migration را اجرا می‌کند.
5. پیش از تغییر Production، Backup و Restore Point بررسی می‌شود.
6. Rollback ترجیحاً با Forward-fix انجام می‌شود؛ بازگشت مخرب بدون Runbook ممنوع است.

## Workflow امنیت و احراز هویت

- تغییر Scheme، Cookie، JWT، LDAP، Session یا Role نیازمند Security Review است.
- User/Role/Claim/Tokenهای Identity فقط با `UserManager`، `RoleManager` و
  `SignInManager` تغییر می‌کنند؛ دست‌کاری مستقیم جداول Identity ممنوع است.
- تولید دستی Principal یا Authentication Cookie در کد برنامه ممنوع است.
- AD Provider فقط Credential/Identity را Verify می‌کند و مجاز به ایجاد Session نیست.
- Password فقط در حافظه مسیر Login و تا پایان Bind حضور دارد.
- Log کردن Headerهای Authorization، Cookie و Body ورود ممنوع است.
- تغییر Role/Permission باید `AuthorizationVersion` را افزایش دهد.
- تغییر Domain یا Auth Policy باید Sessionهای متأثر را باطل کند.
- Production با Fake Provider یا کلید توسعه باید Fail-fast شود.

## Workflow UI و Tailwind

- Bootstrap RTL مسئول Grid و Component پایه است.
- Tailwind فقط برای Utilityهای تعریف‌شده و بدون Reset متداخل استفاده می‌شود.
- Prefix/Namespace و ترتیب CSS در Design System ثابت است.
- Tailwind در Development/CI Build و Purge می‌شود؛ Node روی IIS نصب نمی‌شود.
- فونت، Icon، CSS و JavaScript از مسیر محلی ارائه می‌شوند.

## قرارداد اجباری Blazor SSR

- صفحات Account/Auth/Logout از Static SSR و Full Request استفاده می‌کنند.
- فرم Static SSR باید `method="post"`، `FormName` یکتا، `EditForm`، مدل ورودی
  مستقل و `[SupplyParameterFromForm]` داشته باشد.
- Login/Logout در Static SSR با `<button type="submit">` یا Link/Endpoint انجام
  می‌شود؛ `@onclick` در Static SSR برای منطق C# معتبر نیست.
- `@onclick`، `@onchange` و سایر Eventهای C# فقط در Component دارای Interactive
  Server Render Mode استفاده می‌شوند.
- نوع Button همیشه صریح است: `submit` برای ارسال فرم و `button` برای عملیات تعاملی.
- Entityهای EF/Identity مستقیماً به فرم Bind نمی‌شوند؛ Input Model اختصاصی لازم است.
- `HttpContext` فقط در چرخه Static SSR/Endpoint استفاده می‌شود و در Circuit به آن
  اتکا نمی‌شود.
- Component تعاملی نباید DbContext Scoped را نگه دارد؛ Context از Factory و به‌ازای
  هر Operation ساخته و Dispose می‌شود.
- Side Effect در `OnInitialized{Async}` ممنوع است؛ Prerender می‌تواند Initialization
  را بیش از یک بار اجرا کند. برای داده پرهزینه از Persistent Component State استفاده می‌شود.
- JS interop و `ElementReference` فقط پس از `OnAfterRenderAsync(firstRender: true)` مجازند.
- Handlerهای Event و Resourceها در `Dispose`/`DisposeAsync` آزاد می‌شوند.
- `AuthorizeView` فقط نمایش UI را کنترل می‌کند؛ مجوز واقعی در Endpoint/Application
  Policy نیز اعمال می‌شود.

### قرارداد فرم‌ساز فاز ۲

- طراح و فرم Runtime به دلیل Drag & Drop، binding پویا و eventهای C# با Interactive Server اجرا می‌شوند.
- schema و پاسخ از Component به Application Service تحویل می‌شوند و در مرز سرور دوباره validate می‌شوند.
- نسخه منتشرشده immutable است؛ هر تغییر روی Draft بعدی انجام می‌شود و Submission به نسخه دقیق متصل است.
- HTML/JavaScript دلخواه، `MarkupString` برای محتوای مدیر و `eval` در شرط/محاسبه مجاز نیست.
- Workflow تأیید، فایل/امضا و نمودار تا فاز مربوط نباید به schema فاز ۲ به‌صورت ad-hoc افزوده شوند.

## Workflow Release آفلاین

1. CI یک Artifact غیرقابل‌تغییر و نسخه‌دار می‌سازد.
2. Artifact شامل App، Migration Script، Manifest، SBOM، Hash و Release Notes است.
3. Hash پس از انتقال به شبکه عملیاتی مجدداً کنترل می‌شود.
4. Deployment از حساب مجزا انجام و Application Pool کنترل‌شده متوقف/راه‌اندازی می‌شود.
5. Migration و سپس App Deploy اجرا می‌شوند.
6. Smoke Test شامل Health، Login، SSO، Manual، Logout، DB و Audit است.
7. در صورت شکست، Runbook بازگشت اجرا و Incident ثبت می‌شود.

## سیاست Review

- PR امنیت/Authentication/Authorization: حداقل یک Reviewer ارشد یا امنیت.
- PR دیتابیس: Reviewer آشنا با SQL و Backup/Restore.
- PR UI: کنترل RTL، Responsive و Accessibility.
- Author نمی‌تواند تنها Approver تغییر خودش باشد.
