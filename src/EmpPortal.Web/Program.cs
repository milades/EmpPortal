using System.Security.Claims;
using System.Threading.RateLimiting;
using EmpPortal.Application.Authorization;
using EmpPortal.Application.Configuration;
using EmpPortal.Application.Forms;
using EmpPortal.Application.Hr;
using EmpPortal.Application.Identity;
using EmpPortal.Application.Security;
using EmpPortal.Application.Tabular;
using EmpPortal.Infrastructure.Access;
using EmpPortal.Infrastructure.Configuration;
using EmpPortal.Infrastructure.Forms;
using EmpPortal.Infrastructure.Hr;
using EmpPortal.Infrastructure.Identity;
using EmpPortal.Infrastructure.Persistence;
using EmpPortal.Infrastructure.Persistence.Identity;
using EmpPortal.Infrastructure.Tabular;
using EmpPortal.Web.Authorization;
using EmpPortal.Web.Components;
using EmpPortal.Web.Components.Account;
using EmpPortal.Web.Security;
using EmpPortal.Web.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("PortalDatabase") ??
    throw new InvalidOperationException("Connection string 'PortalDatabase' was not configured.");
IReadOnlyDictionary<string, string?> runtimeSettings = RuntimeSettingsConfigurationLoader.Load(
    connectionString,
    required: !builder.Environment.IsDevelopment());
builder.Configuration.AddInMemoryCollection(runtimeSettings);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton(TimeProvider.System);

JwtOptions jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
    string.IsNullOrWhiteSpace(jwtOptions.Audience) ||
    jwtOptions.AccessTokenMinutes is < 1 or > 15)
{
    throw new InvalidOperationException(
        "JWT issuer, audience, and an access-token lifetime between 1 and 15 minutes are required.");
}

PortalSessionOptions sessionOptions = builder.Configuration
    .GetSection(PortalSessionOptions.SectionName)
    .Get<PortalSessionOptions>() ?? new PortalSessionOptions();
SessionPolicy sessionPolicy = new(
    TimeSpan.FromMinutes(sessionOptions.AbsoluteMinutes),
    TimeSpan.FromMinutes(sessionOptions.IdleMinutes),
    sessionOptions.MaxConcurrentPerUser,
    TimeSpan.FromMinutes(jwtOptions.AccessTokenMinutes),
    TimeSpan.FromSeconds(sessionOptions.AdRevalidationSeconds));
sessionPolicy.EnsureValid();
PortalAuthenticationOptions authenticationOptions = builder.Configuration
    .GetSection(PortalAuthenticationOptions.SectionName)
    .Get<PortalAuthenticationOptions>() ?? new PortalAuthenticationOptions();
if (!authenticationOptions.SsoEnabled && !authenticationOptions.ManualLoginEnabled)
{
    throw new InvalidOperationException("At least one portal authentication method must be enabled.");
}
LoginSecurityOptions loginSecurityOptions = builder.Configuration
    .GetSection(LoginSecurityOptions.SectionName)
    .Get<LoginSecurityOptions>() ?? new LoginSecurityOptions();
if (loginSecurityOptions.AttemptLimit is < 1 or > 20 ||
    loginSecurityOptions.AttemptWindowMinutes is < 1 or > 60)
{
    throw new InvalidOperationException("Login rate-limit settings are invalid.");
}

JwtSigningKeyProvider jwtSigningKeyProvider = new(builder.Environment, jwtOptions);
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton(sessionPolicy);
builder.Services.AddSingleton(authenticationOptions);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        bool isManualLoginPost = HttpMethods.IsPost(httpContext.Request.Method) &&
            httpContext.Request.Path.Equals("/account/login", StringComparison.OrdinalIgnoreCase);
        if (!isManualLoginPost)
        {
            return RateLimitPartition.GetNoLimiter("unlimited");
        }

        string partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = loginSecurityOptions.AttemptLimit,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(loginSecurityOptions.AttemptWindowMinutes)
            });
    });
});
builder.Services.AddSingleton(jwtSigningKeyProvider);
builder.Services.AddScoped<PortalAccessTokenService>();
builder.Services.AddScoped<PortalJwtBearerEvents>();

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
});
authenticationBuilder.AddIdentityCookies();
if (!builder.Environment.IsDevelopment())
{
    authenticationBuilder.AddNegotiate("WindowsSso", _ => { });
}
authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.MapInboundClaims = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = jwtSigningKeyProvider.SecurityKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = ClaimTypes.Name,
        RoleClaimType = ClaimTypes.Role
    };
    options.EventsType = typeof(PortalJwtBearerEvents);
});

builder.Services.AddDbContextFactory<PortalDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure()));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<PortalDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-EmpPortal.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = sessionPolicy.AbsoluteLifetime;
    options.SlidingExpiration = false;
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/access-denied";
    options.EventsType = typeof(PortalCookieAuthenticationEvents);
});
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = sessionPolicy.DirectoryRevalidationInterval);
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IdentityCookieOnly", policy =>
        policy.AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
            .RequireAuthenticatedUser());
    options.AddPolicy("ApiAccess", policy =>
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser());

    foreach (PortalResourceDefinition resource in PortalResources.All)
    {
        options.AddPolicy(
            PortalResources.PolicyName(resource.Key),
            policy => policy.Requirements.Add(new PortalResourceRequirement(resource.Key)));
    }
});
builder.Services.AddScoped<IAuthorizationHandler, PortalResourceAuthorizationHandler>();
builder.Services.AddScoped<IPortalSignInService, IdentityPortalSignInService>();
builder.Services.AddScoped<IPortalSignOutService, IdentityPortalSignOutService>();
builder.Services.AddScoped<IRuntimeSettingsService, RuntimeSettingsService>();
builder.Services.AddScoped<IFormManagementService, FormManagementService>();
builder.Services.AddScoped<IFormSubmissionService, FormSubmissionService>();
builder.Services.AddScoped<IFormReportingService, FormReportingService>();
builder.Services.AddScoped<IPortalAccessEvaluator, PortalAccessEvaluator>();
builder.Services.AddScoped<IPortalAccessAdministrationService, PortalAccessAdministrationService>();
builder.Services.AddScoped<IPayslipSettingsService, PayslipSettingsService>();
builder.Services.AddScoped<IPayslipReportService, PayslipReportService>();
builder.Services.AddScoped<IPersonnelFileService, PersonnelFileService>();
builder.Services.AddScoped<ICharityPledgeService, CharityPledgeService>();
builder.Services.AddScoped<IEmployeeTabularQuery, EmployeeTabularQuery>();
builder.Services.Configure<ExternalTabularSourceOptions>(
    builder.Configuration.GetSection(ExternalTabularSourceOptions.SectionName));
builder.Services.AddScoped<FormActorFactory>();
builder.Services.AddScoped<PortalActorFactory>();
builder.Services.AddScoped<IConfirmationService, SweetAlertConfirmationService>();
builder.Services.AddOptions<FormPdfOptions>()
    .BindConfiguration(FormPdfOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.RegularFontPath) &&
            !string.IsNullOrWhiteSpace(options.BoldFontPath),
        "مسیر فونت‌های PDF باید تنظیم شود.")
    .Validate(
        options => string.Equals(options.License, "Community", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.License, "Professional", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(options.License, "Enterprise", StringComparison.OrdinalIgnoreCase) ||
            builder.Environment.IsDevelopment() &&
            string.Equals(options.License, "Evaluation", StringComparison.OrdinalIgnoreCase),
        "Forms:Pdf:License باید Community، Professional یا Enterprise باشد؛ Evaluation فقط در Development مجاز است.")
    .ValidateOnStart();
builder.Services.AddOptions<PayslipReportOptions>()
    .BindConfiguration(PayslipReportOptions.SectionName)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.TemplateRelativePath),
        "Payslip:Report:TemplateRelativePath باید مسیر فایل .mrt را مشخص کند.")
    .ValidateOnStart();
builder.Services.AddScoped<PortalCookieAuthenticationEvents>();
builder.Services.AddScoped<CircuitHandler, SessionCircuitHandler>();
builder.Services.AddOptions<BootstrapAdministratorOptions>()
    .BindConfiguration(BootstrapAdministratorOptions.SectionName)
    .Validate(
        options => builder.Environment.IsDevelopment() ||
            !string.IsNullOrWhiteSpace(options.Upn),
        "BootstrapAdministrator:Upn must be configured outside Development.")
    .ValidateOnStart();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole();

    DirectoryInfo developmentKeyDirectory = Directory.CreateDirectory(
        Path.Combine(builder.Environment.ContentRootPath, ".local", "data-protection-keys"));
    builder.Services.AddDataProtection()
        .SetApplicationName("EmpPortal")
        .PersistKeysToFileSystem(developmentKeyDirectory);

    DevelopmentDirectoryAccount[] accounts = builder.Configuration
        .GetSection("DevelopmentIdentity:Accounts")
        .Get<DevelopmentDirectoryAccount[]>() ?? [];
    bool acceptAnyNonEmptyPassword = builder.Configuration
        .GetValue<bool>("DevelopmentIdentity:AcceptAnyNonEmptyPassword");

    builder.Services.AddSingleton<IEnterpriseIdentityProvider>(
        new DevelopmentEnterpriseIdentityProvider(accounts, acceptAnyNonEmptyPassword));
}
else
{
    ProductionDataProtectionOptions dataProtectionOptions = builder.Configuration
        .GetSection(ProductionDataProtectionOptions.SectionName)
        .Get<ProductionDataProtectionOptions>() ?? new ProductionDataProtectionOptions();
    if (string.IsNullOrWhiteSpace(dataProtectionOptions.KeyRingPath))
    {
        throw new InvalidOperationException("DataProtection:KeyRingPath must be configured.");
    }

    var dataProtectionCertificate = CertificateStoreLoader.LoadFromLocalMachine(
        dataProtectionOptions.CertificateThumbprint,
        requirePrivateKey: true,
        "DataProtection:CertificateThumbprint");
    DirectoryInfo productionKeyDirectory = Directory.CreateDirectory(
        dataProtectionOptions.KeyRingPath);
    builder.Services.AddSingleton(dataProtectionCertificate);
    builder.Services.AddDataProtection()
        .SetApplicationName("EmpPortal")
        .PersistKeysToFileSystem(productionKeyDirectory)
        .ProtectKeysWithCertificate(dataProtectionCertificate);

    builder.Services.AddOptions<ActiveDirectoryOptions>()
        .BindConfiguration(ActiveDirectoryOptions.SectionName)
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.DomainFqdn),
            "ActiveDirectory:DomainFqdn is required.")
        .Validate(
            options => !string.IsNullOrWhiteSpace(options.BaseDn),
            "ActiveDirectory:BaseDn is required.")
        .Validate(
            options => options.LdapsPort is > 0 and <= 65535,
            "ActiveDirectory:LdapsPort is invalid.")
        .Validate(
            options => options.OperationTimeoutSeconds is >= 1 and <= 60,
            "ActiveDirectory:OperationTimeoutSeconds must be between 1 and 60.")
        .ValidateOnStart();
    builder.Services.AddSingleton<IEnterpriseIdentityProvider, LdapEnterpriseIdentityProvider>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using AsyncServiceScope migrationScope = app.Services.CreateAsyncScope();
    IDbContextFactory<PortalDbContext> dbContextFactory =
        migrationScope.ServiceProvider.GetRequiredService<IDbContextFactory<PortalDbContext>>();
    await using PortalDbContext migrationDbContext = await dbContextFactory.CreateDbContextAsync();
    await migrationDbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.Use(async (httpContext, next) =>
{
    const string correlationHeader = "X-Correlation-ID";
    string? requestedCorrelationId = httpContext.Request.Headers[correlationHeader].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(requestedCorrelationId) && requestedCorrelationId.Length <= 100)
    {
        httpContext.TraceIdentifier = requestedCorrelationId;
    }

    httpContext.Response.Headers[correlationHeader] = httpContext.TraceIdentifier;
    await next();
});

app.Use(async (httpContext, next) =>
{
    bool allowsSameOriginFrame = httpContext.Request.Path.StartsWithSegments(
        "/api/payslip/pdf",
        StringComparison.OrdinalIgnoreCase);

    httpContext.Response.Headers.XContentTypeOptions = "nosniff";
    httpContext.Response.Headers.XFrameOptions = allowsSameOriginFrame ? "SAMEORIGIN" : "DENY";
    httpContext.Response.Headers.Append(
        "Referrer-Policy",
        "strict-origin-when-cross-origin");
    httpContext.Response.Headers.ContentSecurityPolicy = allowsSameOriginFrame
        ? "object-src 'none'; base-uri 'self'; frame-ancestors 'self'"
        : "object-src 'none'; base-uri 'self'; frame-ancestors 'none'";
    httpContext.Response.Headers.Append(
        "Permissions-Policy",
        "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();
app.MapGet("/health/ready", async (
    IDbContextFactory<PortalDbContext> dbContextFactory,
    CancellationToken cancellationToken) =>
{
    await using PortalDbContext dbContext = await dbContextFactory.CreateDbContextAsync(
        cancellationToken);
    bool canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "Healthy" })
        : Results.Json(
            new { status = "Unhealthy" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

app.MapGet("/api/auth/antiforgery", (
    IAntiforgery antiforgery,
    HttpContext httpContext) =>
{
    AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(httpContext);
    httpContext.Response.Headers.CacheControl = "no-store";
    return Results.Ok(new { requestToken = tokens.RequestToken });
}).RequireAuthorization("IdentityCookieOnly");

app.MapGet("/api/auth/session-state", (HttpContext httpContext) =>
{
    httpContext.Response.Headers.CacheControl = "no-store";
    return httpContext.User.Identity?.IsAuthenticated == true
        ? Results.NoContent()
        : Results.Unauthorized();
}).AllowAnonymous();

app.MapPost("/api/auth/token", async (
    IAntiforgery antiforgery,
    PortalAccessTokenService tokenService,
    HttpContext httpContext) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(httpContext);
    }
    catch (AntiforgeryValidationException)
    {
        return Results.BadRequest(new { error = "antiforgery_validation_failed" });
    }

    AccessTokenResponse? token = await tokenService.CreateAsync(
        httpContext.User,
        httpContext.RequestAborted);
    return token is null
        ? Results.Unauthorized()
        : Results.Ok(token);
}).RequireAuthorization("IdentityCookieOnly");

app.MapGet("/api/me", (ClaimsPrincipal principal) => Results.Ok(new
{
    userId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
    userName = principal.Identity?.Name,
    roles = principal.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray()
})).RequireAuthorization("ApiAccess");

app.MapGet("/api/forms", async (
    IFormSubmissionService formSubmissionService,
    ClaimsPrincipal principal,
    HttpContext httpContext) => Results.Ok(await formSubmissionService.GetAvailableFormsAsync(
        FormActorFactory.Create(principal, httpContext),
        httpContext.RequestAborted)))
    .RequireAuthorization("ApiAccess");

app.MapGet("/api/forms/{slug}", async (
    string slug,
    IFormSubmissionService formSubmissionService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    FormRuntimeData? form = await formSubmissionService.GetRuntimeAsync(
        slug,
        FormActorFactory.Create(principal, httpContext),
        httpContext.RequestAborted);
    return form is null ? Results.NotFound() : Results.Ok(form);
}).RequireAuthorization("ApiAccess");

app.MapPut("/api/form-submissions/draft", async (
    SaveSubmissionRequest request,
    IFormSubmissionService formSubmissionService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        SubmissionResult result = await formSubmissionService.SaveDraftAsync(
            request,
            FormActorFactory.Create(principal, httpContext),
            httpContext.RequestAborted);
        return Results.Ok(result);
    }
    catch (FormConcurrencyException exception)
    {
        return Results.Conflict(new { error = "concurrency_conflict", message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "invalid_submission", message = exception.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("ApiAccess");

app.MapPost("/api/form-submissions/submit", async (
    SaveSubmissionRequest request,
    IFormSubmissionService formSubmissionService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        SubmissionResult result = await formSubmissionService.SubmitAsync(
            request,
            FormActorFactory.Create(principal, httpContext),
            httpContext.RequestAborted);
        return Results.Ok(result);
    }
    catch (FormConcurrencyException exception)
    {
        return Results.Conflict(new { error = "concurrency_conflict", message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "invalid_submission", message = exception.Message });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization("ApiAccess");

app.MapGet("/api/forms/{formId:guid}/submissions/export.xlsx", async (
    Guid formId,
    string? search,
    string? status,
    string? from,
    string? to,
    IFormReportingService formReportingService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        SubmissionQuery query = new(
            1,
            200,
            search,
            ParseSubmissionStatus(status),
            ParseReportDate(from, endOfDay: false),
            ParseReportDate(to, endOfDay: true));
        byte[] content = await formReportingService.ExportExcelAsync(
            formId,
            query,
            FormActorFactory.Create(principal, httpContext),
            httpContext.RequestAborted);
        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"form-{formId:N}-submissions.xlsx");
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "export_failed", message = exception.Message });
    }
}).RequireAuthorization("IdentityCookieOnly");

app.MapGet("/api/submissions/{submissionId:guid}/export.pdf", async (
    Guid submissionId,
    IFormReportingService formReportingService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        byte[] content = await formReportingService.ExportPdfAsync(
            submissionId,
            FormActorFactory.Create(principal, httpContext),
            httpContext.RequestAborted);
        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.File(content, "application/pdf", $"submission-{submissionId:N}.pdf");
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound();
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "pdf_export_failed", message = exception.Message });
    }
}).RequireAuthorization("IdentityCookieOnly");

app.MapGet("/api/payslip/pdf", async (
    int year,
    int month,
    bool? download,
    IPayslipReportService payslipReportService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        PayslipPdfResult pdf = await payslipReportService.RenderMyPayslipPdfAsync(
            PortalActorFactory.Create(principal, httpContext),
            year,
            month,
            httpContext.RequestAborted);
        httpContext.Response.Headers.CacheControl = "no-store";
        if (download == true)
        {
            return Results.File(pdf.Content, pdf.ContentType, pdf.FileName);
        }

        return Results.File(pdf.Content, pdf.ContentType);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (ArgumentOutOfRangeException exception)
    {
        return Results.BadRequest(new { error = "invalid_period", message = exception.Message });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "payslip_render_failed", message = exception.Message });
    }
}).RequireAuthorization("IdentityCookieOnly");

app.MapGet("/api/charity/export.xlsx", async (
    ICharityPledgeService charityPledgeService,
    ClaimsPrincipal principal,
    HttpContext httpContext) =>
{
    try
    {
        byte[] content = await charityPledgeService.ExportExcelAndLockAsync(
            PortalActorFactory.Create(principal, httpContext),
            httpContext.RequestAborted);
        httpContext.Response.Headers.CacheControl = "no-store";
        return Results.File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"charity-pledges-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = "charity_export_failed", message = exception.Message });
    }
}).RequireAuthorization("IdentityCookieOnly");

if (app.Environment.IsDevelopment())
{
    app.MapGet("/auth/sso", async (
        string? returnUrl,
        IConfiguration configuration,
        PortalAuthenticationOptions portalAuthenticationOptions,
        IPortalSignInService signInService,
        HttpContext httpContext) =>
    {
        if (!portalAuthenticationOptions.SsoEnabled)
        {
            return Results.LocalRedirect("/account/login?ssoError=true");
        }

        string? developmentUpn = configuration["DevelopmentIdentity:SsoUpn"];
        if (string.IsNullOrWhiteSpace(developmentUpn))
        {
            return Results.LocalRedirect("/account/login?ssoError=true");
        }

        PortalSignInResult result = await signInService.SsoSignInAsync(
            developmentUpn,
            httpContext.RequestAborted);

        return Results.LocalRedirect(
            result.Succeeded
                ? GetSafeLocalReturnUrl(returnUrl)
                : "/account/login?ssoError=true");
    }).AllowAnonymous();
}
else
{
    app.MapGet("/auth/sso", async (
        string? returnUrl,
        PortalAuthenticationOptions portalAuthenticationOptions,
        IPortalSignInService signInService,
        HttpContext httpContext) =>
    {
        if (!portalAuthenticationOptions.SsoEnabled)
        {
            return Results.LocalRedirect("/account/login?ssoError=true");
        }

        string? loginName = httpContext.User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(loginName))
        {
            return Results.LocalRedirect("/account/login?ssoError=true");
        }

        PortalSignInResult result = await signInService.SsoSignInAsync(
            loginName,
            httpContext.RequestAborted);

        return Results.LocalRedirect(
            result.Succeeded
                ? GetSafeLocalReturnUrl(returnUrl)
                : "/account/login?ssoError=true");
    }).RequireAuthorization(new AuthorizeAttribute
    {
        AuthenticationSchemes = "WindowsSso"
    });
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetSafeLocalReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl) ||
        !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ||
        !returnUrl.StartsWith('/') ||
        returnUrl.StartsWith("//", StringComparison.Ordinal))
    {
        return "/";
    }

    return returnUrl;
}

static EmpPortal.Domain.Forms.FormSubmissionStatus? ParseSubmissionStatus(string? value) =>
    int.TryParse(value, out int parsed) &&
    Enum.IsDefined((EmpPortal.Domain.Forms.FormSubmissionStatus)parsed)
        ? (EmpPortal.Domain.Forms.FormSubmissionStatus)parsed
        : null;

static DateTimeOffset? ParseReportDate(string? value, bool endOfDay)
{
    if (!DateOnly.TryParse(value, out DateOnly date))
    {
        return null;
    }

    DateTime local = date.ToDateTime(endOfDay ? TimeOnly.MaxValue : TimeOnly.MinValue);
    return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local)).ToUniversalTime();
}
//Stimulsoft.Base.StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHl2AD0gPVknKsaW0un+3PuM6TTcPMUAWEURKXNso0e5OFPaZYasFtsxNoDemsFOXbvf7SIcnyAkFX/4u37NTfx7g+0IqLXw6QIPolr1PvCSZz8Z5wjBNakeCVozGGOiuCOQDy60XNqfbgrOjxgQ5y/u54K4g7R/xuWmpdx5OMAbUbcy3WbhPCbJJYTI5Hg8C/gsbHSnC2EeOCuyA9ImrNyjsUHkLEh9y4WoRw7lRIc1x+dli8jSJxt9C+NYVUIqK7MEeCmmVyFEGN8mNnqZp4vTe98kxAr4dWSmhcQahHGuFBhKQLlVOdlJ/OT+WPX1zS2UmnkTrxun+FWpCC5bLDlwhlslxtyaN9pV3sRLO6KXM88ZkefRrH21DdR+4j79HA7VLTAsebI79t9nMgmXJ5hB1JKcJMUAgWpxT7C7JUGcWCPIG10NuCd9XQ7H4ykQ4Ve6J2LuNo9SbvP6jPwdfQJB6fJBnKg4mtNuLMlQ4pnXDc+wJmqgw25NfHpFmrZYACZOtLEJoPtMWxxwDzZEYYfT";
Stimulsoft.Base.StiLicense.Key = "6vJhGtLLLz2GNviWmUTrhSqnOItdDwjBylQzQcAOiHkO46nMQvol4ASeg91in+mGJLnn2KMIpg3eSXQSgaFOm15+0lhekKip+wRGMwXsKpHAkTvorOFqnpF9rchcYoxHXtjNDLiDHZGTIWq6D/2q4k/eiJm9fV6FdaJIUbWGS3whFWRLPHWCBsWnalqTdZlP9knjaWclfjmUKf2Ksc5btMD6pmR7ZHQfHXfdgYK7tLR1rqtxYxBzOPq3LIBvd3spkQhKb07LTZQoyQ3vmRSMALmJSS6ovIS59XPS+oSm8wgvuRFqE1im111GROa7Ww3tNJTA45lkbXX+SocdwXvEZyaaq61Uc1dBg+4uFRxvyRWvX5WDmJz1X0VLIbHpcIjdEDJUvVAN7Z+FW5xKsV5ySPs8aegsY9ndn4DmoZ1kWvzUaz+E1mxMbOd3tyaNnmVhPZeIBILmKJGN0BwnnI5fu6JHMM/9QR2tMO1Z4pIwae4P92gKBrt0MqhvnU1Q6kIaPPuG2XBIvAWykVeH2a9EP6064e11PFCBX4gEpJ3XFD0peE5+ddZh+h495qUc1H2B";

public partial class Program;
