using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Trafico.Peatones;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1338: deja limpio lo que una clase de pruebas de policía, patrullas o IA puede dejar vivo para la siguiente (las de Personaje con entrada simulada
    /// fallaban en masa si corrían después). Lo llama la base de Personaje al empezar y las clases de IA en su TearDown.
    /// </summary>
    public static class LimpiezaEntreClasesT1338
    {
        /// <summary>Quita a los policías, patrullas, drones, peatones y gestores sueltos, y suelta el tiempo, la pausa y el cursor.</summary>
        public static void Limpiar()
        {
            Time.timeScale = 1f;
            Pausa.Fijar(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            DestruirTodos<PandilleroAPie>();
            DestruirTodos<PatrullaMundo>();
            DestruirTodos<DronMundo>();
            DestruirTodos<PeatonMundo>();
            DestruirTodos<GestorPersecucion>();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPolicia.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
        }

        private static void DestruirTodos<T>() where T : MonoBehaviour
        {
            foreach (T objeto in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (objeto != null)
                {
                    Object.DestroyImmediate(objeto.gameObject);
                }
            }
        }
    }
}
