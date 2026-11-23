using UnityEngine;

namespace MechaCyberpunk.UI.Clinica
{
    /// <summary>
    /// T-1658: estado compartido de la primera visita al Doc. Mientras <see cref="Pendiente"/> la camilla/silla no se puede usar (usa la misma tecla que
    /// hablar con el Doc y confundía): primero el Doc inyecta el suero y abre el menú de prótesis. Al terminar (el recorrido de la historia lo avisa con
    /// <see cref="Terminar"/>) la camilla se habilita y arranca el tutorial de las prótesis en la calle (<see cref="TutorialPendiente"/>).
    /// Estático y sin guardado: se recalcula en cada arranque con el paso de la historia. Se limpia al entrar a Play.
    /// </summary>
    public static class PrimeraVisitaClinica
    {
        /// <summary>True desde que el jugador tiene el dinero del maletín hasta que termina el menú de prótesis del suero: la camilla no se ofrece.</summary>
        public static bool Pendiente { get; private set; }

        /// <summary>True desde que termina la primera visita hasta que el jugador completa los pasos del tutorial de prótesis.</summary>
        public static bool TutorialPendiente { get; private set; }

        /// <summary>El paso del tutorial de prótesis en pantalla (para depurar y pruebas).</summary>
        public static PasoTutorialProtesis PasoTutorial { get; set; } = PasoTutorialProtesis.AbrirMenu;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            Pendiente = false;
            TutorialPendiente = false;
            PasoTutorial = PasoTutorialProtesis.AbrirMenu;
        }

        /// <summary>La primera visita empieza: la camilla se bloquea.</summary>
        public static void Iniciar() => Pendiente = true;

        /// <summary>La primera visita terminó: la camilla se habilita y sale el tutorial de prótesis.</summary>
        public static void Terminar()
        {
            if (!Pendiente)
            {
                return;
            }
            Pendiente = false;
            TutorialPendiente = true;
            PasoTutorial = PasoTutorialProtesis.AbrirMenu;
        }

        /// <summary>El tutorial de prótesis se completó (o se descarta).</summary>
        public static void TerminarTutorial() => TutorialPendiente = false;

        /// <summary>Solo pruebas y depuración: fija el estado sin pasar por la historia.</summary>
        public static void Fijar(bool pendiente, bool tutorialPendiente)
        {
            Pendiente = pendiente;
            TutorialPendiente = tutorialPendiente;
            PasoTutorial = PasoTutorialProtesis.AbrirMenu;
        }

        /// <summary>Depuración (invocable por reflexión en Play): estado de la primera visita y del tutorial.</summary>
        public static string Depurar() => "[UI] PrimeraVisita pendiente=" + Pendiente + " tutorialProtesis pendiente=" + TutorialPendiente + " paso=" + PasoTutorial;
    }
}
