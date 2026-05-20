namespace TommyLogistic.Shared.Models;

public class TourStep
{
    public string ElementId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}


/* ═══════════════════════════════════════════════
   EJEMPLO DE USO EN UNA PÁGINA .razor
   ═══════════════════════════════════════════════

@page "/mi-pagina"
@using TuApp.Models

<button class="btn btn-primary" @onclick="IniciarTour">
    Iniciar tour guiado
</button>

<TourGuide Steps="_pasos"
           IsActive="_tourActivo"
           OnClose="() => _tourActivo = false"
           CloseOnOverlayClick="false" />

<section>
    <h3 id="seccion-intro">Introducción al proyecto</h3>
    <p>Contenido de la sección...</p>
</section>

<section>
    <h3 id="seccion-config">Configuración inicial</h3>
    <p>Contenido de la sección...</p>
</section>

<section>
    <h3 id="seccion-resultados">Resultados y métricas</h3>
    <p>Contenido de la sección...</p>
</section>

@code {
    private bool _tourActivo = false;

    private List<TourStep> _pasos = new()
    {
        new TourStep
        {
            ElementId = "seccion-intro",
            Title     = "Introducción",
            Message   = "Aquí encontrarás el propósito general y el alcance del proyecto."
        },
        new TourStep
        {
            ElementId = "seccion-config",
            Title     = "Configuración",
            Message   = "Define los parámetros antes de comenzar. Todo lo esencial está aquí."
        },
        new TourStep
        {
            ElementId = "seccion-resultados",
            Title     = "Resultados",
            Message   = "Revisa métricas y resultados para tomar decisiones informadas."
        }
    };

    private void IniciarTour() => _tourActivo = true;
}

═══════════════════════════════════════════════
   REGISTRO DEL SCRIPT EN index.html
   (dentro de wwwroot/index.html, antes de </body>)
═══════════════════════════════════════════════

<script src="js/tourGuide.js"></script>

*/