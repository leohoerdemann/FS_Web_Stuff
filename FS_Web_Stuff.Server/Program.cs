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

            app.Map("/wsgame/{clientId}", async context =>
                {
                    var clientId = context.Request.RouteValues["clientId"].ToString();
                    await gameWS.HandleWebSocketAsync(context, clientId);
                }
            )
            .WithName("GameWebSocket")
            .WithOpenApi();

            var twitchWS = new WebSocketHandlerTwitch(gameWS);

            app.Map("/wstwitch", async context =>
                {
                    await twitchWS.HandleWebSocketAsync(context);
                }
            )
            .WithName("TwitchWebSocket")
            .WithOpenApi();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
