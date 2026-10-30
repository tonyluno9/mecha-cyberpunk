using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Guardado;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Partida
{
    /// <summary>
    /// El AUTOGUARDADO (T-412, D-tony-244): escribe la ranura AUTO al conquistar una zona (<see cref="ITerritorio.Conquistada"/>, un cuadro
    /// después para que la historia ya haya avanzado) y cuando lo pide la cama al dormir (<see cref="Escribir"/>). El aviso "GUARDADO" lo
    /// pone la UI al oír <see cref="ServicioGuardado.RanuraEscrita"/>. Va en la raíz GTA_T412 de Ciudad_Prologo.
    /// </summary>
    public class Autoguardado : MonoBehaviour
    {
        private ITerritorio territorio;

        /// <summary>Escribe la ranura AUTO con el servicio de la escena. False si no hay servicio o no se pudo.</summary>
        public static bool Escribir()
        {
            ServicioGuardado servicio = ServicioGuardado.Actual;
            if (servicio == null)
            {
                Debug.LogWarning("[Progresion] Autoguardado sin ServicioGuardado en la escena.");
                return false;
            }
            return servicio.Guardar(ServicioGuardado.RanuraAuto);
        }

        private void OnEnable()
        {
            Registro<ITerritorio>.Agregado += Conectar;
            Conectar(Registro<ITerritorio>.Actual);
        }

        private void OnDisable()
        {
            Registro<ITerritorio>.Agregado -= Conectar;
            Desconectar();
        }

        private void Conectar(ITerritorio nuevo)
        {
            if (nuevo == null || ReferenceEquals(nuevo, territorio))
            {
                return;
            }
            Desconectar();
            territorio = nuevo;
            territorio.Conquistada += AlConquistar;
        }

        private void Desconectar()
        {
            if (territorio != null)
            {
                territorio.Conquistada -= AlConquistar;
                territorio = null;
            }
        }

        private void AlConquistar(string zona, IdPandilla anterior)
        {
            StartCoroutine(EscribirDespues());
        }

        private static IEnumerator EscribirDespues()
        {
            yield return null;
            Escribir();
        }
    }
}
