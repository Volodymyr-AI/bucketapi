using System.Reflection;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Application;
using Order.Application.Database;
using Order.Application.Messaging;
using Order.Core.Entities.Models.Middleware.RateLimit;
using Order.Infrastructure.DbAccessPostgreSQL;
using Order.Infrastructure.Messaging;
using Order.WebApi.CustomMiddlewares.GlobalExceptionHandler;
using Order.WebApi.CustomMiddlewares.RateLimiter;

namespace Order.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            //Rate limiting
            builder.Services.AddRateLimiting(options =>
            {
                // Generale end-point rules
                options.GeneralRules = new List<RateLimitRule>
                {
                    new RateLimitRule
                    {
                        Name = "PerMinute",
                        Limit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    },
                    new RateLimitRule
                    {
                        Name = "PerHour",
                        Limit = 1000,
                        Window = TimeSpan.FromHours(1)
                    }
                };
                // Specific end-point rules
                options.EndpointRules = new Dictionary<string, List<RateLimitRule>>
                {
                    ["GET:/api/order"] = new List<RateLimitRule>
                    {
                        new RateLimitRule { Name = "ProductsPerMinute", Limit = 200, Window = TimeSpan.FromMinutes(1) }
                    }
                };
                
                // Pattern-based rules (used with regex)
                options.PatternRules = new Dictionary<string, List<RateLimitRule>>
                {
                    // All API endpoints beginning with /api/admin (адмін панель)
                    [@"^POST:/api/admin/.*"] = new List<RateLimitRule>
                    {
                        new RateLimitRule { Name = "AdminPerMinute", Limit = 30, Window = TimeSpan.FromMinutes(1) }
                    },

                    // Всі upload endpoint-и (файли)
                    [@"^POST:.*/upload$"] = new List<RateLimitRule>
                    {
                        new RateLimitRule { Name = "UploadPerMinute", Limit = 10, Window = TimeSpan.FromMinutes(1) },
                        new RateLimitRule { Name = "UploadPerHour", Limit = 50, Window = TimeSpan.FromHours(1) }
                    }
                };

                // Whitelist IP addresses (not for rate limiting)
                options.WhitelistedIPs = new HashSet<string>
                {
                    "127.0.0.1",           // localhost
                    "::1",                 // localhost IPv6
                    "10.0.0.0/8",          // inner IP 
                    "192.168.1.100"        // concrete IP
                };
            });
            
            //Kafka Section
            var kafkaBootstrapServers = builder.Configuration["Kafka:BootstrapServers"];
            if (string.IsNullOrWhiteSpace(kafkaBootstrapServers))
                throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = kafkaBootstrapServers,
                Acks = Acks.All,
                RetryBackoffMaxMs = 1000,
                EnableIdempotence = true,
                BatchSize = 16384,
                LingerMs = 5,
                CompressionType = CompressionType.Snappy
            };

            builder.Services.AddSingleton<IProducer<string, string>>(serviceProvider =>
            {
                return new ProducerBuilder<string, string>(producerConfig)
                    .SetErrorHandler((_, e) =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
                        logger.LogError("Kafka producer error: {Reason}", e.Reason);
                    })
                    .Build();
            });
            builder.Services.AddScoped<IEventPublisher, KafkaEventProducer>();
            
            // Database Connection Section
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                Console.WriteLine("Connection string is NULL or EMPTY.");
            }
            else
            {
                Console.WriteLine($"Connection string: {connectionString}");
            }
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());
            
            // Application layer DI Section
            builder.Services.AddApplication();

            // Use Controllers Section
            builder.Services.AddControllers();
            
            // Use user secrets Section
            builder.Configuration.AddUserSecrets<AssemblyName>();
            
            // Set up api versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });
            
            // Swagger Section
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            //builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            // Main pipeline
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseGlobalExceptionHandling();
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiting();
            app.UseCors("ProductionCors");

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("X-Frame-Options", "DENY");
                context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
                context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
                context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

                await next();
            });
            
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.MapControllers();
            app.MapHealthChecks("/health");
            
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStopping.Register(() =>
            {
                var producer = app.Services.GetRequiredService<IProducer<string, string>>();
                producer?.Flush(TimeSpan.FromSeconds(10));
                producer?.Dispose();
            });

            app.Run();
        }
    }
}
