using System.Text;
using System.Text.Encodings.Web;
using CampusTrade.API.Data;
using CampusTrade.API.Infrastructure.Extensions;
using CampusTrade.API.Infrastructure.Middleware;
using CampusTrade.API.Options;
using CampusTrade.API.Services.Auth;
using CampusTrade.API.Services.Background;
using CampusTrade.API.Services.Cache;
using CampusTrade.API.Services.Interfaces;
using CampusTrade.API.Services.Review;
using CampusTrade.API.Services.ScheduledTasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

try
{
    // 设置控制台编码为UTF-8，确保中文字符正确显示
    Console.OutputEncoding = Encoding.UTF8;

    var builder = WebApplication.CreateBuilder(args);

    // 配置 Serilog 从 appsettings.json 读取配置
    builder.Host.UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: context.Configuration["Serilog:WriteTo:1:Args:path"] ?? "logs/campus-trade-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: context.Configuration["Serilog:WriteTo:2:Args:path"] ?? "logs/errors/error-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 31,
                restrictedToMinimumLevel: LogEventLevel.Warning,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
    });

    Log.Information("正在启动 Campus Trade API...");

    // 添加服务到容器中
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
        });

    // 添加API文档
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Campus Trade API",
            Version = "v1.0",
            Description = "校园交易平台后端API文档",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Campus Trade Team",
                Email = "admin@campustrade.com"
            }
        });

        // 为Swagger添加JWT身份验证
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

        // 使用XML注释
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // 注册数据库性能拦截器
    builder.Services.AddScoped<DatabasePerformanceInterceptor>();

    // 添加 Oracle 数据库连接以及拦截器
    builder.Services.AddDbContext<CampusTradeDbContext>(options =>
    {
        var interceptor = builder.Services.BuildServiceProvider()
            .GetRequiredService<DatabasePerformanceInterceptor>();
        options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection"))
               .AddInterceptors(interceptor);
    });

    // 添加Repository层服务
    builder.Services.AddRepositoryServices();

    // 添加日志清理后台服务
    builder.Services.AddHostedService<CampusTrade.API.Services.LogCleanupService>();

    // 添加JWT认证和Token服务
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // 添加后台服务
    builder.Services.AddBackgroundServices();

    // 添加SignalR支持
    builder.Services.AddSignalRSupport();

    // 注册定时任务服务
    builder.Services.AddHostedService<TokenCleanupTask>();
    builder.Services.AddHostedService<LogCleanupTask>();
    builder.Services.AddHostedService<ProductManagementTask>();
    builder.Services.AddHostedService<OrderProcessingTask>();
    builder.Services.AddHostedService<UserCreditScoreCalculationTask>();
    builder.Services.AddHostedService<StatisticalAnalysisTask>();
    builder.Services.AddHostedService<NotificationPushTask>();

    // 添加认证相关服务
    builder.Services.AddAuthenticationServices(builder.Configuration);

    // 添加举报相关服务
    builder.Services.AddReportServices();

    // 添加文件管理服务
    builder.Services.AddFileManagementServices(builder.Configuration);

    // 添加订单服务
    builder.Services.AddOrderServices();

    // 添加商品服务
    builder.Services.AddProductServices();

    // 配置 CORS
    builder.Services.AddCorsPolicy(builder.Configuration);

    // 配置缓存选项
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

    // 注册缓存基础服务
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ICacheService, CacheService>();

    // 注册后台服务
    builder.Services.AddHostedService<CacheRefreshBackgroundService>();

    // 注册缓存服务
    builder.Services.AddScoped<ICategoryCacheService, CategoryCacheService>();
    builder.Services.AddScoped<IProductCacheService, ProductCacheService>();
    builder.Services.AddScoped<ISystemConfigCacheService, SystemConfigCacheService>();
    builder.Services.AddScoped<IUserCacheService, UserCacheService>();

    // 注册邮箱验证服务
    builder.Services.AddScoped<EmailVerificationService>();

    // 注册议价服务
    builder.Services.AddBargainServices();

    // 注册换物服务
    builder.Services.AddExchangeServices();

    // 注册评价服务
    builder.Services.AddReviewServices();

    var app = builder.Build();

    // 配置HTTP请求管道
    if (app.Environment.IsDevelopment())
    {
        // 开发环境下，先配置Swagger，避免被其他中间件影响
        app.UseSwagger();

        app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Campus Trade API v1.0");
    c.RoutePrefix = string.Empty; // 可选：设置 Swagger 为根路径
});
    }
    /*
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Campus Trade API v1.0");
                c.RoutePrefix = string.Empty; // 将Swagger UI设置为根路径
                c.DocumentTitle = "Campus Trade API Documentation";
                c.DefaultModelsExpandDepth(-1); // 隐藏模型
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // 默认折叠所有操作
         // 自定义HTML模板
                c.IndexStream = () =>
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(@"
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset='UTF-8'>
         <meta http-equiv='X-UA-Compatible' content='IE=edge'>
         <meta name='viewport' content='width=device-width, initial-scale=1.0'>
        <title>Campus Trade API Documentation</title>
        <link rel='stylesheet' type='text/css' href='./swagger-ui.css' />
        <style>
            html { box-sizing: border-box; overflow: -moz-scrollbars-vertical; overflow-y: scroll; }
            *, *:before, *:after { box-sizing: inherit; }
            body { margin:0; background: #fafafa; }
        </style>
    </head>
    <body>
        <div id='swagger-ui'></div>
         <script>
             // Polyfill for Object.hasOwn (ES2022) to support older browsers
             if (!Object.hasOwn) {
                 Object.hasOwn = function(obj, prop) {
                     return Object.prototype.hasOwnProperty.call(obj, prop);
                 };
             }
         </script>
        <script src='./swagger-ui-bundle.js'></script>
        <script src='./swagger-ui-standalone-preset.js'></script>
        <script>
            window.onload = function() {
                const ui = SwaggerUIBundle({
                    url: '/swagger/v1/swagger.json',
                    dom_id: '#swagger-ui',
                    deepLinking: true,
                    presets: [
                        SwaggerUIBundle.presets.apis,
                        SwaggerUIStandalonePreset
                    ],
                    plugins: [
                        SwaggerUIBundle.plugins.DownloadUrl
                    ],
                    layout: 'StandaloneLayout'
                });
            }
        </script>
    </body>
    </html>"));
                };
            });
        }
        else
        {
            app.UseHsts();
        }
    */
    // 使用全局异常处理中间件
    app.UseGlobalExceptionHandler();

    // 使用安全头中间件
    app.UseSecurityHeaders();

    // 使用安全检查中间件
    app.UseSecurity();

    app.UseMiddleware<PerformanceMiddleware>();

    // 启用静态文件访问（用于文件下载和预览）
    app.UseStaticFiles();

    // 配置Storage目录的静态文件服务
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Storage")),
        RequestPath = "/files"
    });

    // 在开发环境下禁用HTTPS重定向，避免影响Swagger
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // 启用路由匹配中间件
    app.UseRouting();

    // 启用 CORS - 在开发环境使用宽松的CORS策略
    if (app.Environment.IsDevelopment())
    {
        app.UseCors("DevelopmentCors");
    }
    else
    {
        app.UseCors("CampusTradeCors");
    }

    // 启用JWT验证中间件（在认证之前）
    app.UseJwtValidation();

    // 启用认证和授权
    app.UseAuthentication();
    app.UseAuthorization();

    // 映射控制器端点
    app.MapControllers();

    // 映射SignalR Hub
    app.MapHub<CampusTrade.API.Infrastructure.Hubs.NotificationHub>("/api/notification-hub");

    Log.Information("Campus Trade API 启动完成");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Campus Trade API 启动失败");
    throw;
}
finally
{
    Log.Information("正在关闭 Campus Trade API...");
    Log.CloseAndFlush();
}

// 使Program类可供测试访问
public partial class Program { }

