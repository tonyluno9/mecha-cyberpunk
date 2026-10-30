using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Apariencia
{
    /// <summary>
    /// Viste al muñeco del jugador según <see cref="AparienciaJugador.Cara"/> del perfil (T-413, D-tony-246): 0 es A "CHAMARRA"
    /// (ModeloJugador) y 1 es B "MÁSCARA" (ModeloJugador_B). No cambia el modelo (el Animator y la mano del arma siguen): copia los
    /// materiales del prefab elegido por ruta y prende solo las piezas que ese prefab trae (el cabello de A, la máscara de B).
    /// Va en la escena (GTA_T413) y se repinta con <see cref="IPerfilJugador.Cambio"/>.
    /// </summary>
    public class ModeloSegunPerfil : MonoBehaviour
    {
        public const string NombreModelo = "ModeloJugador";

        [Tooltip("Un prefab por personaje, en el orden de Apariencia.Cara (0 = A CHAMARRA, 1 = B MÁSCARA).")]
        [SerializeField] private GameObject[] modelos = new GameObject[0];

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

        /// <summary>Viste ahora al muñeco del jugador con el modelo de su cara.</summary>
        public void Aplicar()
        {
            if (perfil == null || jugador == null || jugador.Raiz == null || modelos.Length == 0)
            {
                return;
            }
            GameObject fuente = modelos[Indice(perfil.Apariencia.Cara, modelos.Length)];
            Transform modelo = Buscar(jugador.Raiz, NombreModelo);
            if (fuente == null || modelo == null)
            {
                return;
            }
            Vestir(modelo, fuente.transform);
        }

        /// <summary>
        /// Copia a <paramref name="modelo"/> los materiales de cada malla de <paramref name="fuente"/> (por su ruta); la que falta se copia
        /// y la que la fuente no trae se apaga.
        /// </summary>
        public static void Vestir(Transform modelo, Transform fuente)
        {
            HashSet<string> rutas = new HashSet<string>();
            foreach (MeshRenderer origen in fuente.GetComponentsInChildren<MeshRenderer>(true))
            {
                string ruta = Ruta(origen.transform, fuente);
                rutas.Add(ruta);
                Transform destino = modelo.Find(ruta);
                if (destino == null)
                {
                    Transform padre = origen.transform.parent == fuente ? modelo : modelo.Find(Ruta(origen.transform.parent, fuente));
                    if (padre == null)
                    {
                        continue;
                    }
                    GameObject copia = Instantiate(origen.gameObject, padre);
                    copia.name = origen.name;
                    copia.transform.localPosition = origen.transform.localPosition;
                    copia.transform.localRotation = origen.transform.localRotation;
                    copia.transform.localScale = origen.transform.localScale;
                    destino = copia.transform;
                }
                destino.gameObject.SetActive(origen.gameObject.activeSelf);
                MeshRenderer render = destino.GetComponent<MeshRenderer>();
                if (render != null)
                {
                    render.sharedMaterials = origen.sharedMaterials;
                }
            }
            foreach (MeshRenderer render in modelo.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!rutas.Contains(Ruta(render.transform, modelo)))
                {
                    render.gameObject.SetActive(false);
                }
            }
        }

        private static string Ruta(Transform t, Transform raiz)
        {
            string ruta = t.name;
            for (Transform p = t.parent; p != null && p != raiz; p = p.parent)
            {
                ruta = p.name + "/" + ruta;
            }
            return ruta;
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
