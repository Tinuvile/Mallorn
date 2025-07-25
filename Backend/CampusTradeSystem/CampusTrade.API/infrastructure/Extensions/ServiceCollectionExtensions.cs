using CampusTrade.API.Infrastructure.Utils.Security;
using CampusTrade.API.Options;
using CampusTrade.API.Repositories.Implementations;
using CampusTrade.API.Repositories.Interfaces;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Background;
using CampusTrade.API.Services.File;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace CampusTrade.API.Infrastructure.Extensions;

/// <summary>
/// 服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加JWT认证服务
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 配置JWT选项
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        // 2. 验证JWT配置
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        // 3. 注册Token服务
        services.AddScoped<ITokenService, TokenService>();

        // 4. 配置JWT认证
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        if (jwtOptions == null)
            throw new InvalidOperationException("JWT配置未找到，请检查appsettings.json中的Jwt配置节");
        if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
            throw new InvalidOperationException("JWT密钥配置无效: 密钥不能为空且长度至少32个字符");

        var tokenValidationParameters = TokenHelper.CreateTokenValidationParameters(jwtOptions);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = jwtOptions.RequireHttpsMetadata;
            options.SaveToken = jwtOptions.SaveToken;
            options.TokenValidationParameters = tokenValidationParameters;
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Logger.Warning("JWT认证失败: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var tokenService = context.HttpContext.RequestServices.GetRequiredService<ITokenService>();
                    var jti = context.Principal?.FindFirst("jti")?.Value;
                    if (!string.IsNullOrEmpty(jti) && await tokenService.IsTokenBlacklistedAsync(jti))
                    {
                        context.Fail("Token已被撤销");
                    }
                },
                OnChallenge = context =>
                {
                    Log.Logger.Warning("JWT认证质询: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        // 5. 添加授权策略
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAuthenticatedUser", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
            options.AddPolicy("RequireActiveUser", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("IsActive", "True");
            });
            options.AddPolicy("RequireEmailVerified", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("EmailVerified", "True");
            });
        });

        return services;
    }

    /// <summary>
    /// 添加Repository和数据访问服务
    /// </summary>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    /// <summary>
    /// 添加认证相关服务
    /// </summary>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();

        // 注册通知服务
        services.AddScoped<Services.Auth.NotifiService>();
        services.AddScoped<Services.Auth.NotifiSenderService>();

        // 注册邮件服务
        services.AddScoped<Services.Email.EmailService>();

        // 添加内存缓存（用于Token黑名单）
        services.AddMemoryCache();
        services.AddHttpContextAccessor();
        return services;
    }

    /// <summary>
    /// 添加SignalR支持
    /// </summary>
    public static IServiceCollection AddSignalRSupport(this IServiceCollection services)
    {
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
            options.StreamBufferCapacity = 10;
        });

        return services;
    }

    /// <summary>
    /// 添加CORS策略
    /// </summary>
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("CampusTradeCors", policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000", "http://localhost:5173" };
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(30));
            });
            // 开发环境宽松策略
            options.AddPolicy("DevelopmentCors", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        return services;
    }

    /// <summary>
    /// 添加后台服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
    {
        // 注册通知发送后台服务
        services.AddHostedService<NotificationBackgroundService>();

        return services;
    }

    /// <summary>
    /// 添加文件管理服务
    /// </summary>
    public static IServiceCollection AddFileManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IThumbnailService, ThumbnailService>();
        services.AddSingleton<IValidateOptions<FileStorageOptions>, FileStorageOptionsValidator>();
        return services;
    }
}

/// <summary>
/// JWT选项验证器
/// </summary>
public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var errors = options.GetValidationErrors().ToList();
        if (errors.Any())
            return ValidateOptionsResult.Fail(errors);
        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// 文件存储选项验证器
/// </summary>
public class FileStorageOptionsValidator : IValidateOptions<FileStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, FileStorageOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.UploadPath))
            errors.Add("上传路径不能为空");
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            errors.Add("基础URL不能为空");
        if (options.MaxFileSize <= 0)
            errors.Add("最大文件大小必须大于0");
        if (options.ThumbnailWidth <= 0 || options.ThumbnailHeight <= 0)
            errors.Add("缩略图尺寸必须大于0");
        if (options.ThumbnailQuality < 1 || options.ThumbnailQuality > 100)
            errors.Add("缩略图质量必须在1-100之间");
        if (errors.Any())
            return ValidateOptionsResult.Fail(errors);
        return ValidateOptionsResult.Success;
    }
}
