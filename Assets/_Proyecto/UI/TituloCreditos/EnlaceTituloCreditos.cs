using System;
using MechaCyberpunk.UI.Creditos;
using MechaCyberpunk.UI.Titulo;
using UnityEngine;

namespace MechaCyberpunk.UI.TituloCreditos
{
    /// <summary>
    /// Une el botón CRÉDITOS del título con la pantalla de créditos (T-440). Vive en un ensamblado aparte porque UI.Creditos depende de UI
    /// y UI no puede depender de él (ciclo). Al iniciar el juego se engancha al evento estático del título.
    /// </summary>
    public static class EnlaceTituloCreditos
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Enlazar()
        {
            PantallaTitulo.PidioCreditos -= Abrir;
            PantallaTitulo.PidioCreditos += Abrir;
        }

        private static void Abrir(Action alCerrar)
        {
            PantallaCreditos.Abrir(alCerrar);
        }
    }
}
