using Microsoft.AspNetCore.Mvc;

namespace Dfit.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            ViewBag.StatusCode = statusCode;

            switch (statusCode)
            {
                case 401:
                    ViewBag.ErrorMessage = "Acceso No Autorizado. Debes iniciar sesión para ingresar a esta zona de DFIT POWER.";
                    return View("401");
                case 403:
                    ViewBag.ErrorMessage = "Acceso Prohibido. No cuentas con los permisos de administrador requeridos.";
                    return View("403");
                case 404:
                    ViewBag.ErrorMessage = "La página o rutina de DFIT POWER que buscas no existe o fue movida.";
                    return View("404");
                default:
                    ViewBag.ErrorMessage = "Ocurrió un error inesperado en nuestro sistema de entrenamiento. Inténtalo más tarde.";
                    return View("500");
            }
        }
    }
}
