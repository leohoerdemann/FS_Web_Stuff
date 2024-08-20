using FS_Web_Stuff.Server.WebSocketHandlers;

namespace FS_Web_Stuff.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            ///}

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseWebSockets(
                new WebSocketOptions()
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(120),
                });

            var gameWS = new WebSocketHandlerGame();

            app.Map("/wsgame", async context =>
                {
                    await gameWS.HandleWebSocketAsync(context);
                }
            );

            //var twitchWS = new WebSocketHandlerTwitch();

            //app.Map("/wstwitch", twitchWS.HandleWebSocketAsync);

            app.MapGet("/playercounts", () =>
            {
                var random = new Random();
                var counties = new List<string> { "America", "Mexico", "Europe" };
                var results = new Dictionary<string, int>();

                foreach (var county in counties)
                {
                    var count = random.Next(1, 100);
                    results.Add(county, count);
                }

                return Results.Json(results);
            });

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
