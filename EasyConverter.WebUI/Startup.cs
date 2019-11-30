using System;
using System.Linq;
using EasyConverter.Shared;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;

namespace EasyConverter.WebUI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMediatR(typeof(Startup).Assembly);
            services.AddSingleton<MessageQueueService>();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider services)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            var mediator = services.GetService<IMediator>();
            var messageQueue = services.GetService<MessageQueueService>();

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(@"F:\converter\result"),
                RequestPath = "/result",
            });

            app.UseTus(context =>
            {
                return new DefaultTusConfiguration
                {
                    UrlPath = "/files",
                    Store = new TusDiskStore(@"F:\converter\tus"),
                    Events = new Events
                    {
                        OnFileCompleteAsync = async ctx =>
                        {
                            var file = await ctx.GetFileAsync();
                            var metaData = await file.GetMetadataAsync(default);

                            var fileName = metaData["name"].GetString(System.Text.Encoding.UTF8);
                            var convertTo = metaData["convertTo"].GetString(System.Text.Encoding.UTF8);

                            var id = ctx.FileId;

                            var job = new ConvertDocumentJob
                            {
                                FileId = ctx.FileId,
                                DesiredExtension = convertTo,
                                Name = $"Convert {file.Id} ({fileName}) to {convertTo}",
                                OriginalExtension = fileName.Split('.').Last()
                            };

                            messageQueue.QueueJob(job);
                        }
                    }
                };
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
