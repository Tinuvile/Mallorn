using System.Text;
using System.Text.Encodings.Web;
using CampusTrade.API.Data;
using CampusTrade.API.Extensions;
using CampusTrade.API.Middleware;
using CampusTrade.API.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// 设置控制台编码为UTF-8，确保中文字符正确显示
Console.OutputEncoding = Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

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

// 添加 Oracle 数据库连接
builder.Services.AddDbContext<CampusTradeDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("DefaultConnection")));

// 添加JWT认证和Token服务
builder.Services.AddJwtAuthentication(builder.Configuration);

// 添加认证相关服务
builder.Services.AddAuthenticationServices();

// 添加文件管理服务
builder.Services.AddFileManagementServices(builder.Configuration);

// 配置 CORS
builder.Services.AddCorsPolicy(builder.Configuration);

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    // 在开发环境下，先配置Swagger，避免被其他中间件影响
    app.UseSwagger();
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

// 使用全局异常处理中间件
app.UseGlobalExceptionHandler();

// 使用安全检查中间件
app.UseSecurity();

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

app.Run();

// 使Program类可供测试访问
public partial class Program { }
