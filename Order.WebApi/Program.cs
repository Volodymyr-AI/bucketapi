using System.Reflection;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Application;
using Order.Application.Database;
using Order.Application.Messaging;
using Order.Infrastructure.DbAccessPostgreSQL;
using Order.Infrastructure.Messaging;
using Order.WebApi.CustomMiddlewares.GlobalExceptionHandler;

namespace Order.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            
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
