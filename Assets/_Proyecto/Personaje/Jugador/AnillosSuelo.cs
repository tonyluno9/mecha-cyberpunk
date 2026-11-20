using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// T-1525: ondas de pisada sobre pisos mojados que no son un charco con el shader de agua (asfalto mojado, banquetas, decals). Reparte un número fijo de cuadros
    /// horizontales (se reutilizan) con el shader OndaPisadaSuelo, que dibuja el mismo anillo que el charco de T-1503. No toca el material del piso.
    /// </summary>
    public sealed class AnillosSuelo
    {
        public const string NombreShader = "MechaCyberpunk/Personaje/OndaPisadaSuelo";
        public const string NombreMaterial = "OndaPisadaSuelo"; // Resources/OndaPisadaSuelo.mat (lo crea el editor: OndasSueloT1525.Aplicar)
        public const int Cantidad = 6;
        public const float LadoCuadro = 8f; // m: cubre el radio máximo de la onda (3.3 m + 0.5 m de cresta) desde el centro
        public const float AlturaSobrePiso = 0.03f;

        private static readonly int IdOrigen = Shader.PropertyToID("_Origen");

        private readonly GameObject[] cuadros = new GameObject[Cantidad];
        private readonly Material[] materiales = new Material[Cantidad];
        private readonly float[] momentos = new float[Cantidad];
        private readonly bool[] vivos = new bool[Cantidad];
        private int siguiente;
        private Mesh malla;
        private Material baseMaterial;
        private bool sinShader;

        /// <summary>Cuántas ondas de piso están a la vista.</summary>
        public int Activos { get; private set; }

        /// <summary>Último origen pedido, para las pruebas.</summary>
        public Vector3 UltimaPosicion { get; private set; }

        public void Agregar(Vector3 posicion, float ahora)
        {
            if (!Preparar())
            {
                return;
            }
            int i = siguiente;
            siguiente = (siguiente + 1) % Cantidad;
            if (cuadros[i] == null)
            {
                cuadros[i] = CrearCuadro(i);
            }
            cuadros[i].transform.position = posicion + Vector3.up * AlturaSobrePiso;
            materiales[i].SetVector(IdOrigen, new Vector4(posicion.x, posicion.y, posicion.z, ahora));
            momentos[i] = ahora;
            vivos[i] = true;
            cuadros[i].SetActive(true);
            UltimaPosicion = posicion;
            Recontar();
        }

        /// <summary>Apaga las ondas ya vencidas; devuelve cuántas siguen a la vista.</summary>
        public int Actualizar(float ahora)
        {
            for (int i = 0; i < Cantidad; i++)
            {
                if (vivos[i] && ahora - momentos[i] > ReglaOndasCharco.Vida)
                {
                    vivos[i] = false;
                    cuadros[i].SetActive(false);
                }
            }
            Recontar();
            return Activos;
        }

        public void Destruir()
        {
            for (int i = 0; i < Cantidad; i++)
            {
                if (cuadros[i] != null)
                {
                    Eliminar(cuadros[i]);
                }
                if (materiales[i] != null)
                {
                    Eliminar(materiales[i]);
                }
                vivos[i] = false;
            }
            if (malla != null)
            {
                Eliminar(malla);
            }
            Activos = 0;
        }

        // Destroy no vale fuera de Play (las pruebas de EditMode).
        private static void Eliminar(Object objeto)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(objeto);
            }
            else
            {
                Object.DestroyImmediate(objeto);
            }
        }

        private void Recontar()
        {
            int n = 0;
            for (int i = 0; i < Cantidad; i++)
            {
                if (vivos[i])
                {
                    n++;
                }
            }
            Activos = n;
        }

        private bool Preparar()
        {
            if (baseMaterial != null)
            {
                return true;
            }
            if (sinShader)
            {
                return false;
            }
            baseMaterial = Resources.Load<Material>(NombreMaterial);
            if (baseMaterial == null)
            {
                Shader s = Shader.Find(NombreShader);
                if (s == null)
                {
                    sinShader = true;
                    Debug.LogWarning("[Personaje] T-1525: no existe el shader " + NombreShader + ": no hay ondas en el piso mojado.");
                    return false;
                }
                baseMaterial = new Material(s) { name = NombreMaterial };
            }
            malla = CrearMalla();
            return true;
        }

        private GameObject CrearCuadro(int i)
        {
            GameObject objeto = new GameObject("OndaPisada_" + i) { hideFlags = HideFlags.DontSave };
            objeto.SetActive(false);
            objeto.AddComponent<MeshFilter>().sharedMesh = malla;
            MeshRenderer render = objeto.AddComponent<MeshRenderer>();
            materiales[i] = new Material(baseMaterial);
            render.sharedMaterial = materiales[i];
            render.shadowCastingMode = ShadowCastingMode.Off;
            render.receiveShadows = false;
            return objeto;
        }

        // Cuadro horizontal centrado en el origen, mirando hacia arriba.
        private static Mesh CrearMalla()
        {
            float m = LadoCuadro * 0.5f;
            Mesh cuadro = new Mesh { name = "OndaPisadaCuadro" };
            cuadro.vertices = new[] { new Vector3(-m, 0f, -m), new Vector3(-m, 0f, m), new Vector3(m, 0f, m), new Vector3(m, 0f, -m) };
            cuadro.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            cuadro.bounds = new Bounds(Vector3.zero, new Vector3(LadoCuadro, 0.1f, LadoCuadro));
            return cuadro;
        }
    }
}
