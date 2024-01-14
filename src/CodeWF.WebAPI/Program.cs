WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.ConfigureExtraServices(new InitializerOptions
{
    EventBusQueueName = "CodeWF.WebAPI",
    LogFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}CodeWF.WebAPI.log"
});
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeWF.WebAPI", Version = "v1" });
    //c.AddAuthenticationHeader();
});
builder.Services.AddDataProtection();
//��¼��ע�����Ŀ����Ҫ����WebApplicationBuilderExtensions�еĳ�ʼ��֮�⣬��Ҫ���µĳ�ʼ��
//��Ҫ��AddIdentity��������AddIdentityCore
//��Ϊ��AddIdentity�ᵼ��JWT���Ʋ������ã�AddJwtBearer�лص����ᱻִ�У��������AuthenticationУ��ʧ��
//https://github.com/aspnet/Identity/issues/1376
IdentityBuilder idBuilder = builder.Services.AddIdentityCore<User>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        //�����趨RequireUniqueEmail��������������Ϊ��
        //options.User.RequireUniqueEmail = true;
        //�������У���GenerateEmailConfirmationTokenAsync��֤������
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
        options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
    }
);
idBuilder = new IdentityBuilder(idBuilder.UserType, typeof(Role), builder.Services);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
idBuilder.AddEntityFrameworkStores<CodeWFDbContext>().AddDefaultTokenProviders()
    .AddRoleValidator<RoleValidator<Role>>()
    .AddRoleManager<RoleManager<Role>>()
    .AddUserManager<IdUserManager>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IEmailSender, MockEmailSender>();
    builder.Services.AddScoped<ISmsSender, MockSmsSender>();
}
else
{
    builder.Services.AddScoped<IEmailSender, SendCloudEmailSender>();
    builder.Services.AddScoped<ISmsSender, SendCloudSmsSender>();
}

builder.Services.InstallCaptcha(builder.Configuration); // ע��ͼ����֤��

builder.Services.AddScoped<ISeedService, SeedService>();

WebApplication app = builder.Build();

// ����ʱ��ʼ�����ݿ���ṹ�������������ݵ�
using (IServiceScope serviceScope = app.Services.CreateScope())
{
    ISeedService seedService = serviceScope.ServiceProvider.GetRequiredService<ISeedService>();
    await seedService.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeWF.WebAPI v1"));
}

app.UseCodeWFDefault();
app.MapControllers();
app.Run();