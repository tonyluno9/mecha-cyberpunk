using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Habilidades;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Apoyo
{
    /// <summary>
    /// T-866 (playtest 6-nov tarde): el VISOR (implante de ojos) deja ver a los enemigos a través de las cosas. Con el implante de ojos puesto,
    /// a cada enemigo vivo a menos de <see cref="Alcance"/> m (pandilleros, el jefe) se le suma el material de rayos X (Resources/VisorRayosX: solo
    /// se dibuja donde algo lo tapa, en naranja). Al quitarse el visor o al morir el enemigo, se le quita. Busca enemigos cada
    /// <see cref="CadaCuanto"/> s, no cada cuadro. Se crea solo al cargar la escena si está el material.
    /// T-992: ya no es siempre; marca solo mientras la habilidad CABEZA ([3]) está activa (<see cref="HabilidadesProtesis.CabezaActiva"/>) y hay implante de ojos.
    /// </summary>
    public class VisorRayosX : MonoBehaviour
    {
        public const string NombreMaterial = "VisorRayosX";
        public const float Alcance = 45f;
        public const float CadaCuanto = 0.75f;

        private static VisorRayosX actual;
        private Material material;
        private float siguiente;
        private readonly Dictionary<Renderer, Material[]> marcados = new Dictionary<Renderer, Material[]>();
        private readonly List<Renderer> quitar = new List<Renderer>();

        public static VisorRayosX Actual => actual;

        /// <summary>Cuántos renderers de enemigos se ven ahora a través de las cosas.</summary>
        public int Marcados => marcados.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => actual = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar() => Crear();

        public static VisorRayosX Crear()
        {
            if (actual != null)
            {
                return actual;
            }
            Material m = Resources.Load<Material>(NombreMaterial);
            if (m == null)
            {
                return null;
            }
            GameObject go = new GameObject("VisorRayosX");
            DontDestroyOnLoad(go);
            actual = go.AddComponent<VisorRayosX>();
            actual.material = m;
            return actual;
        }

        /// <summary>Para las pruebas: el material de rayos X.</summary>
        public void Configurar(Material rayosX) => material = rayosX;

        private void OnEnable() => HabilidadesProtesis.CabezaCambio += Actualizar;

        private void OnDisable() => HabilidadesProtesis.CabezaCambio -= Actualizar;

        private void OnDestroy()
        {
            Desmarcar();
            if (actual == this)
            {
                actual = null;
            }
        }

        /// <summary>True si el jugador trae el implante de ojos y la habilidad CABEZA está activa.</summary>
        public static bool VisorPuesto()
        {
            IImplantes implantes = Registro<IImplantes>.Actual;
            return implantes != null && implantes.Nivel(EspacioImplante.Ojos) > 0 && HabilidadesProtesis.CabezaActiva;
        }

        private void Update()
        {
            if (Time.unscaledTime < siguiente)
            {
                return;
            }
            siguiente = Time.unscaledTime + CadaCuanto;
            Actualizar();
        }

        /// <summary>Vuelve a decidir a quién se marca (también lo llaman las pruebas).</summary>
        public void Actualizar()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (!VisorPuesto() || jugador == null || jugador.Raiz == null)
            {
                Desmarcar();
                return;
            }
            Vector3 ojo = jugador.Raiz.position;
            HashSet<Renderer> deben = new HashSet<Renderer>();
            foreach (PandilleroAPie p in FindObjectsByType<PandilleroAPie>(FindObjectsSortMode.None))
            {
                if (!p.EstaKO && (p.transform.position - ojo).sqrMagnitude < Alcance * Alcance)
                {
                    foreach (Renderer r in p.GetComponentsInChildren<Renderer>())
                    {
                        if (r is SkinnedMeshRenderer || r is MeshRenderer)
                        {
                            deben.Add(r);
                        }
                    }
                }
            }
            quitar.Clear();
            foreach (Renderer r in marcados.Keys)
            {
                if (r == null || !deben.Contains(r))
                {
                    quitar.Add(r);
                }
            }
            foreach (Renderer r in quitar)
            {
                Restaurar(r);
            }
            foreach (Renderer r in deben)
            {
                if (!marcados.ContainsKey(r))
                {
                    Material[] antes = r.sharedMaterials;
                    Material[] con = new Material[antes.Length + 1];
                    antes.CopyTo(con, 0);
                    con[antes.Length] = material;
                    marcados[r] = antes;
                    r.sharedMaterials = con;
                }
            }
        }

        /// <summary>True si ese renderer trae ahora el material de rayos X.</summary>
        public bool EstaMarcado(Renderer r) => r != null && marcados.ContainsKey(r);

        private void Restaurar(Renderer r)
        {
            if (r != null && marcados.TryGetValue(r, out Material[] antes))
            {
                r.sharedMaterials = antes;
            }
            marcados.Remove(r);
        }

        private void Desmarcar()
        {
            foreach (Renderer r in new List<Renderer>(marcados.Keys))
            {
                Restaurar(r);
            }
            marcados.Clear();
        }
    }
}
