using System.Web.Http;
using System.Web.Http.Cors;
using SmkcApi.Repositories;
using SmkcApi.Services;

namespace SmkcApi
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Configure dependency injection
            ConfigureDependencyInjection(config);

            // Enable CORS for specific origins only
            var cors = new EnableCorsAttribute(
                origins: "https://trusted-bank-domain.com", // Replace with actual bank domains
                headers: "*",
                methods: "GET,POST,PUT"
            );
            config.EnableCors(cors);

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // Remove XML formatter - JSON only
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Configure JSON formatting
            config.Formatters.JsonFormatter.SerializerSettings.DateFormatString = "yyyy-MM-ddTHH:mm:ssZ";
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;

            // Add custom message handlers for security
            config.MessageHandlers.Add(new Security.ApiKeyAuthenticationHandler());
        }

        private static void ConfigureDependencyInjection(HttpConfiguration config)
        {
            // Simple dependency injection setup
            // In production, consider using a proper DI container like Unity, Autofac, or Ninject
            config.DependencyResolver = new SimpleDependencyResolver();
        }
    }
}