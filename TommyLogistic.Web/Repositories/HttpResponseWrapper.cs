using System.Net;

namespace TommyLogistic.Web.Repositories;

public class HttpResponseWrapper<T>(T? response, bool error, HttpResponseMessage httpResponseMessage)
{
    public bool Error { get; set; } = error;
    public T? Response { get; set; } = response;
    public HttpResponseMessage HttpResponseMessage { get; set; } = httpResponseMessage;

    public async Task<string?> GetErrorMessageAsync()
    {
        if (!Error)
        {
            return null;
        }
        var codigoEstatus = HttpResponseMessage.StatusCode;
        if (codigoEstatus == HttpStatusCode.NotFound)
        {
            return "Recurso  no  encontrado";
        }
        else if (codigoEstatus == HttpStatusCode.BadRequest)
        {
            return await HttpResponseMessage.Content.ReadAsStringAsync();
        }
        else if (codigoEstatus == HttpStatusCode.Unauthorized)
        {
            return "Tienes  que  logearte  para  hacer  esta  operación";
        }
        else if (codigoEstatus == HttpStatusCode.Forbidden)
        {
            return "No  tienes  permisos  para  hacer  esta  operación";
        }
        return "Ha  ocurrido  un  error  inesperado";
    }
}
