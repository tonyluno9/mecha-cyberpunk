using MechaCyberpunk.UI.Rueda;
using UnityEngine;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR46: la rueda de armas y objetos de PC (<see cref="RuedaArmasObjetos"/>) en VR. Mantener X la abre (sin fondo ni marco: solo la rueda
    /// flotando en el HUD de casco). El selector no se mueve hasta apretar el GRIP izquierdo; con el grip apretado, mover la mano izquierda
    /// recorre la rueda y resalta lo que queda bajo el selector (vibra al cambiar); al SOLTAR el grip se elige: equipa el arma o la granada, o usa
    /// el consumible (papitas, bebida, café... con <c>ConsumiblesJugador.Usar</c>, lo mismo que en PC). Soltar sin nada resaltado deja la rueda
    /// abierta con el selector al centro; soltar X sin el grip cierra sin elegir. Ni el ratón ni el stick izquierdo mueven el selector en VR.
    /// Mientras la rueda está abierta el grip izquierdo es de la rueda: no agarra enemigos (<see cref="GestosArmaVR.TomarGrip"/>).
    /// La lógica está en <see cref="ReglaRuedaGripVR"/>. Lo agrega <see cref="ArranqueCombateVR"/>. Corre antes que la rueda (orden 0) para que
    /// lo resaltado sea el de este cuadro.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class RuedaArmasVR : MonoBehaviour
    {
        /// <summary>El texto del centro de la rueda sin nada resaltado, en VR.</summary>
        public const string TextoCentroVR = "GRIP IZQ. Y MUEVE LA MANO";

        private const float AmplitudCambio = 0.2f;
        private const float SegundosCambio = 0.03f;
        private const float AmplitudElegir = 0.5f;
        private const float SegundosElegir = 0.06f;

        private readonly ReglaRuedaGripVR regla = new ReglaRuedaGripVR();
        private AdaptadorVR adaptador;
        private RuedaArmasObjetos rueda;
        private bool estabaAbierta;
        private int resaltadaAntes = -1;

        /// <summary>Veces que se eligió algo soltando el grip (para las pruebas).</summary>
        public int Elegidas { get; private set; }

        /// <summary>El selector que pide la mano (unidades de la rueda).</summary>
        public Vector2 Cursor => regla.Cursor;

        /// <summary>True mientras el grip izquierdo mueve el selector.</summary>
        public bool Tomado => regla.Tomado;

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            if (adaptador == null)
            {
                Debug.LogWarning("[VR] RuedaArmasVR sin AdaptadorVR en " + name + ".", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Soltar();
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
            Vincular();
            if (rueda == null)
            {
                return;
            }
            bool abierta = RuedaArmasObjetos.Abierta && RuedaArmasObjetos.Actual == rueda;
            bool grip = EntradaVR.Sostenido(EntradaVR.GripIzquierdo);
            if (!abierta)
            {
                estabaAbierta = false;
                return;
            }
            if (!estabaAbierta)
            {
                estabaAbierta = true;
                regla.Abrir(grip);
                resaltadaAntes = -1;
            }
            GestosArmaVR.TomarGrip(false); // con la rueda abierta el grip izquierdo es de la rueda
            EventoRuedaGripVR evento = regla.Avanzar(grip, ManoEnRig(), EjeDerecha(), Vector3.up, ReglaRuedaArmas.RadioMaximo);
            ManoVR mano = adaptador.ManoIzquierda;
            if (evento == EventoRuedaGripVR.Solto)
            {
                if (rueda.Resaltada >= 0)
                {
                    estabaAbierta = false;
                    Elegidas++;
                    Vibrar(mano, AmplitudElegir, SegundosElegir);
                    rueda.ElegirYCerrar();
                    return;
                }
                regla.Centrar(); // soltó sin nada debajo: la rueda sigue abierta y se puede volver a intentar
            }
            rueda.PonerCursor(regla.Cursor);
            if (rueda.Resaltada >= 0 && rueda.Resaltada != resaltadaAntes)
            {
                Vibrar(mano, AmplitudCambio, SegundosCambio);
            }
            resaltadaAntes = rueda.Resaltada;
        }

        // La rueda del jugador: en VR el ratón y el stick no la mueven, y el centro dice cómo se usa.
        private void Vincular()
        {
            RuedaArmasObjetos actual = RuedaArmasObjetos.Actual;
            if (actual == rueda)
            {
                return;
            }
            Soltar();
            rueda = actual;
            if (rueda != null)
            {
                rueda.CursorExterno = true;
                rueda.TextoSinElegir = TextoCentroVR;
            }
        }

        private void Soltar()
        {
            if (rueda != null)
            {
                rueda.CursorExterno = false;
                rueda.TextoSinElegir = null;
            }
            rueda = null;
            estabaAbierta = false;
        }

        // La mano izquierda en el espacio del rig (el rig se mueve con el jugador; con la rueda abierta no camina).
        private Vector3 ManoEnRig()
        {
            Transform rig = adaptador.Rig;
            ManoVR mano = adaptador.ManoIzquierda;
            if (mano == null)
            {
                return Vector3.zero;
            }
            return rig != null ? rig.InverseTransformPoint(mano.transform.position) : mano.transform.position;
        }

        // La derecha de la cabeza, horizontal, en el espacio del rig (la rueda va en el HUD de casco, frente a la cara).
        private Vector3 EjeDerecha()
        {
            Transform rig = adaptador.Rig;
            Transform cabeza = adaptador.Camara != null ? adaptador.Camara.transform : null;
            if (cabeza == null)
            {
                return Vector3.right;
            }
            Vector3 derecha = rig != null ? rig.InverseTransformDirection(cabeza.right) : cabeza.right;
            derecha.y = 0f;
            return derecha.sqrMagnitude > 1e-6f ? derecha.normalized : Vector3.right;
        }

        private static void Vibrar(ManoVR mano, float amplitud, float segundos)
        {
            if (mano != null)
            {
                mano.Vibrar(amplitud, segundos);
            }
        }
    }
}
