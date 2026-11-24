using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR27: sacar y guardar armas con gestos. Mano derecha en la cadera derecha + grip derecho = sacar (o guardar) el arma corta equipada (pistola
    /// o revólver); mano derecha sobre el hombro derecho + grip derecho = el arma larga equipada (la última larga que se tuvo en la mano; si no, la
    /// primera que se tenga). Equipa con <see cref="ArmasAPie.Equipar"/> (lo mismo que la rueda de PC: suena el sacar del arma o el cambio a puños) y
    /// vibra. Las zonas están en <see cref="ReglaGestosArmaVR"/>. Un grip en la zona es del gesto: ese cuadro no agarra enemigos ni toma granadas
    /// (<see cref="GripTomado"/>). Lo agrega <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GestosArmaVR : MonoBehaviour
    {
        private const float AmplitudSacar = 0.5f;
        private const float SegundosSacar = 0.08f;
        private const float AmplitudNada = 0.15f;
        private const float SegundosNada = 0.04f;
        private const float SegundosEntreIntentosRueda = 0.5f;

        private static readonly ArmaAPie[] OrdenCortas = { ArmaAPie.Pistola, ArmaAPie.Revolver };
        private static readonly ArmaAPie[] OrdenLargas =
        {
            ArmaAPie.Fusil, ArmaAPie.Escopeta, ArmaAPie.Subfusil, ArmaAPie.Sniper, ArmaAPie.RiflePlasma, ArmaAPie.Lanzagranadas
        };

        private static int cuadroGripDerecho = -1;
        private static int cuadroGripIzquierdo = -1;

        private AdaptadorVR adaptador;
        private ArmasAPie armas;
        private ArmaAPie ultimaCorta = ArmaAPie.Pistola;
        private ArmaAPie ultimaLarga = ArmaAPie.Punos;
        private bool ruedaConX;
        private float siguienteIntentoRueda;

        /// <summary>Veces que el gesto sacó o guardó un arma (para las pruebas).</summary>
        public int Cambios { get; private set; }

        /// <summary>La última zona en que se pulsó el grip derecho (para las pruebas).</summary>
        public ZonaArmaVR UltimaZona { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            cuadroGripDerecho = -1;
            cuadroGripIzquierdo = -1;
        }

        /// <summary>True si el grip de esa mano lo usó este cuadro un gesto de armas (sacar arma o tomar cargador).</summary>
        public static bool GripTomado(bool derecha) => (derecha ? cuadroGripDerecho : cuadroGripIzquierdo) == Time.frameCount;

        /// <summary>Marca que el grip de esa mano es de un gesto de armas este cuadro.</summary>
        public static void TomarGrip(bool derecha)
        {
            if (derecha)
            {
                cuadroGripDerecho = Time.frameCount;
            }
            else
            {
                cuadroGripIzquierdo = Time.frameCount;
            }
        }

        /// <summary>La zona del cuerpo en que está la mano (relativa a la cámara del rig).</summary>
        public static ZonaArmaVR ZonaDe(AdaptadorVR adaptador, ManoVR mano)
        {
            if (adaptador == null || mano == null || adaptador.Camara == null)
            {
                return ZonaArmaVR.Ninguna;
            }
            Transform cabeza = adaptador.Camara.transform;
            Vector3 local = ReglaGestosArmaVR.EnCabeza(mano.transform.position, cabeza.position, cabeza.forward);
            return ReglaGestosArmaVR.Zona(local, mano.Derecha);
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            armas = GetComponent<ArmasAPie>();
            if (adaptador == null || armas == null)
            {
                Debug.LogWarning("[VR] GestosArmaVR sin AdaptadorVR o ArmasAPie en " + name + ".", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            if (!adaptador.Armado)
            {
                return;
            }
            AsegurarRuedaConX();
            Recordar(armas.ArmaActiva);
            if (Pausa.Activa || MenusVR.MenuAbierto || (adaptador.Jugador != null && adaptador.Jugador.Modo != ModoJugador.APie) || MechaCyberpunk.VR.Guia.GestosProtesisVR.EnPose || !EntradaVR.Pulso(EntradaVR.GripDerecho))
            {
                return;
            }
            ManoVR mano = adaptador.ManoDerecha;
            ZonaArmaVR zona = ZonaDe(adaptador, mano);
            UltimaZona = zona;
            if (zona == ZonaArmaVR.Ninguna)
            {
                return;
            }
            TomarGrip(true);
            ArmaAPie activa = armas.ArmaActiva;
            ArmaAPie destino = ReglaGestosArmaVR.QueSacar(zona, activa, Elegir(OrdenCortas, ultimaCorta), Elegir(OrdenLargas, ultimaLarga));
            if (destino != activa && armas.PuedeCambiar && armas.Equipar(destino))
            {
                Cambios++;
                mano.Vibrar(AmplitudSacar, SegundosSacar);
                return;
            }
            mano.Vibrar(AmplitudNada, SegundosNada); // no hay arma de esa clase: un toque corto para que se sepa que el gesto llegó
        }

        // VR27: X (mantener) abre la rueda. AdaptadorVR la pone al armarse, pero la acción Rueda puede encenderse después (la enciende la rueda en su
        // Start): se reintenta hasta que quede.
        private void AsegurarRuedaConX()
        {
            if (ruedaConX || Time.unscaledTime < siguienteIntentoRueda)
            {
                return;
            }
            siguienteIntentoRueda = Time.unscaledTime + SegundosEntreIntentosRueda;
            if (AdaptadorVR.BuscarAccionAPie(MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.NombreAccion) != null)
            {
                ruedaConX = AdaptadorVR.AgregarBinding(MechaCyberpunk.UI.Rueda.RuedaArmasObjetos.NombreAccion, EntradaVR.RutaRueda);
            }
        }

        private void Recordar(ArmaAPie activa)
        {
            if (ReglaGestosArmaVR.EsCorta(activa))
            {
                ultimaCorta = activa;
            }
            else if (ReglaGestosArmaVR.EsLarga(activa))
            {
                ultimaLarga = activa;
            }
        }

        // La última de esa clase si se sigue teniendo; si no, la primera que se tenga en el orden; Punos si ninguna.
        private ArmaAPie Elegir(ArmaAPie[] orden, ArmaAPie ultima)
        {
            if (ultima != ArmaAPie.Punos && armas.TieneArma(ultima))
            {
                return ultima;
            }
            foreach (ArmaAPie a in orden)
            {
                if (armas.TieneArma(a))
                {
                    return a;
                }
            }
            return ArmaAPie.Punos;
        }
    }
}
