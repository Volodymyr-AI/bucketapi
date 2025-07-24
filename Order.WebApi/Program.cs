using System.Reflection;
using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Order.Application;
using Order.Application.Database;
using Order.Application.Messaging;
using Order.Infrastructure.DbAccessPostgreSQL;
using Order.Infrastructure.Messaging;

namespace Order.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            //Kafka
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
            
            builder.Services.AddDbContext<OrderDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IOrderDbContext>(provider => provider.GetRequiredService<OrderDbContext>());
            builder.Services.AddApplication();

            builder.Services.AddControllers();
            
            builder.Configuration.AddUserSecrets<AssemblyName>();
            
            builder.Services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            //builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.MapControllers();
            
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
