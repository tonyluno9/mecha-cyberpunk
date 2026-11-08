using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Apariencia
{
    /// <summary>
    /// Pone al jugador el modelo de su personaje (T-520, antes T-413): <see cref="AparienciaJugador.Cara"/> 0 es la mujer (Visual_woman, la que trae
    /// el prefab) y 1 es el hombre (Visual_man). Al cambiar, instancia el visual elegido en lugar del actual, pasa las armas de la mano al nuevo
    /// y avisa a los <see cref="IModeloCambiable"/> del jugador (animación, ocultar en el carro, agacharse y rodar).
    /// Va en la escena (GTA_T413) y se repinta con <see cref="IPerfilJugador.Cambio"/>.
    /// </summary>
    public class ModeloSegunPerfil : MonoBehaviour
    {
        public const string NombreModelo = "ModeloJugador";
        public const string NombreMano = "ManoDer";

        [Tooltip("Un prefab Visual_<nombre> por personaje, en el orden de Apariencia.Cara (0 = mujer, 1 = hombre). El prefab del jugador ya trae el 0.")]
        [SerializeField] private GameObject[] modelos = new GameObject[0];

        private int indiceActual;

        private IPerfilJugador perfil;
        private IJugador jugador;

        public IReadOnlyList<GameObject> Modelos => modelos;

        /// <summary>Para el creador de la escena.</summary>
        public void Configurar(GameObject[] nuevos) => modelos = nuevos ?? new GameObject[0];

        /// <summary>El índice de modelo para una cara (fuera de rango, el 0).</summary>
        public static int Indice(int cara, int cantidad) => cara >= 0 && cara < cantidad ? cara : 0;

        private void OnEnable()
        {
            Registro<IPerfilJugador>.Agregado += AlAgregarPerfil;
            Registro<IJugador>.Agregado += AlAgregarJugador;
        }

        private void OnDisable()
        {
            Registro<IPerfilJugador>.Agregado -= AlAgregarPerfil;
            Registro<IJugador>.Agregado -= AlAgregarJugador;
            Conectar(null);
        }

        private void Start()
        {
            Conectar(Registro<IPerfilJugador>.Actual);
            jugador = Registro<IJugador>.Actual;
            Aplicar();
        }

        private void AlAgregarPerfil(IPerfilJugador nuevo)
        {
            Conectar(nuevo);
            Aplicar();
        }

        private void AlAgregarJugador(IJugador nuevo)
        {
            jugador = nuevo;
            Aplicar();
        }

        private void Conectar(IPerfilJugador nuevo)
        {
            if (ReferenceEquals(nuevo, perfil))
            {
                return;
            }
            if (perfil != null)
            {
                perfil.Cambio -= Aplicar;
            }
            perfil = nuevo;
            if (perfil != null)
            {
                perfil.Cambio += Aplicar;
            }
        }

        /// <summary>Pone ahora al jugador el modelo de su cara.</summary>
        public void Aplicar()
        {
            if (perfil == null || jugador == null || jugador.Raiz == null || modelos.Length == 0)
            {
                return;
            }
            int indice = Indice(perfil.Apariencia.Cara, modelos.Length);
            GameObject fuente = modelos[indice];
            Transform actual = Buscar(jugador.Raiz, NombreModelo);
            if (fuente == null || actual == null || indice == indiceActual)
            {
                return;
            }
            Animator animadorViejo = actual.GetComponentInChildren<Animator>(true);
            bool visible = animadorViejo == null || animadorViejo.gameObject.activeSelf;
            GameObject nuevo = Instantiate(fuente, actual.parent);
            nuevo.name = NombreModelo;
            nuevo.transform.SetPositionAndRotation(actual.position, actual.rotation);
            Animator animadorNuevo = nuevo.GetComponentInChildren<Animator>(true);
            if (animadorNuevo != null)
            {
                animadorNuevo.gameObject.SetActive(visible);
            }
            PasarArmas(actual, nuevo.transform);
            actual.gameObject.SetActive(false);
            Destroy(actual.gameObject);
            indiceActual = indice;
            foreach (IModeloCambiable c in jugador.Raiz.GetComponentsInChildren<IModeloCambiable>(true))
            {
                c.AlCambiarModelo(animadorNuevo);
            }
        }

        /// <summary>Mueve lo que cuelga de la mano derecha del modelo viejo (las armas) a la mano del nuevo, conservando su posición local.</summary>
        public static void PasarArmas(Transform viejo, Transform nuevo)
        {
            Transform desde = Buscar(viejo, NombreMano);
            Transform hacia = Buscar(nuevo, NombreMano);
            if (desde == null || hacia == null)
            {
                return;
            }
            string mano = hacia.parent != null ? hacia.parent.name : string.Empty;
            for (int i = desde.childCount - 1; i >= 0; i--)
            {
                Transform arma = desde.GetChild(i);
                arma.SetParent(hacia, false);
                // T-736: cada modelo tiene sus ejes en la mano; el arma toma el agarre de la mano a la que pasa (PJ01: agarre.txt).
                if (AgarresArmas.TryObtener(arma.name, mano, out AgarreArma agarre))
                {
                    arma.localPosition = agarre.Posicion;
                    arma.localRotation = Quaternion.Euler(agarre.Rotacion);
                }
            }
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            if (raiz.name == nombre)
            {
                return raiz;
            }
            foreach (Transform hijo in raiz)
            {
                Transform encontrado = Buscar(hijo, nombre);
                if (encontrado != null)
                {
                    return encontrado;
                }
            }
            return null;
        }
    }
}
