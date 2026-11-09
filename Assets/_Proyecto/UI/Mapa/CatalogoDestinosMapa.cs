using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Todos los lugares a los que se puede ir (T-667): casa, El Doc, Tino el inventor, el mecánico, el tianguis, el casino, el mercado y el bar de las
    /// misiones. Su posición sale, en este orden, de los lugares de Core (<see cref="ILugar"/>), de una marca de lugar de T-409 del mismo tipo o de un
    /// objeto de la escena con el nombre de su lugar (así no se edita la escena de Alan). Lo usan el mapa grande y el minimapa.
    /// </summary>
    public static class CatalogoDestinosMapa
    {
        private const float SegundosDeCache = 2f;

        private static readonly DestinoMapa[] todos =
        {
            new DestinoMapa(IdDestinoMapa.Casa, "CASA", string.Empty, IdLugar.Casa, TipoMarcaMapa.Casa, "Lugar_Casa"),
            new DestinoMapa(IdDestinoMapa.ElDoc, "I+D+I", "+", IdLugar.Clinica, TipoMarcaMapa.Clinica, "Clinica_Camilla", "Lugar_Clinica"),
            new DestinoMapa(IdDestinoMapa.Tino, "TINO", "T", IdLugar.Ninguno, TipoMarcaMapa.Cic, "Lugar_CIC", "TallerTino"), // el taller de Tino es el CIC
            new DestinoMapa(IdDestinoMapa.Mecanico, "MECÁNICO", "M", IdLugar.Ninguno, null, "Lugar_TallerMecanico"),
            new DestinoMapa(IdDestinoMapa.Tianguis, "TIANGUIS", "G", IdLugar.Ninguno, null, "Tianguis").ConEtiquetaArriba(),
            new DestinoMapa(IdDestinoMapa.Casino, "CASINO", "$", IdLugar.Casino, null, "Casino"),
            new DestinoMapa(IdDestinoMapa.Mercado, "MERCADO", IconosMapa.SimboloMercado, IdLugar.MercadoNegro, TipoMarcaMapa.Mercado, "Lugar_MercadoNegro"),
            new DestinoMapa(IdDestinoMapa.Misiones, "MISIONES", "!", IdLugar.Bar, null, "Bar"),
        };

        private static readonly Dictionary<string, Transform> objetos = new Dictionary<string, Transform>();
        private static readonly Dictionary<string, float> faltantes = new Dictionary<string, float>();
        // T-934: la última posición conocida de cada lugar: un lugar no se borra del mapa porque su interior esté apagado.
        private static readonly Dictionary<IdDestinoMapa, Vector3> ultimas = new Dictionary<IdDestinoMapa, Vector3>();

        public static IReadOnlyList<DestinoMapa> Todos => todos;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Limpiar()
        {
            objetos.Clear();
            faltantes.Clear();
            ultimas.Clear();
        }

        public static DestinoMapa De(IdDestinoMapa id)
        {
            foreach (DestinoMapa d in todos)
            {
                if (d.Id == id)
                {
                    return d;
                }
            }
            return null;
        }

        /// <summary>Dónde está el destino (m, mundo, altura 0). False si no se encuentra en la escena.</summary>
        public static bool Posicion(IdDestinoMapa id, out Vector3 posicion)
        {
            posicion = Vector3.zero;
            DestinoMapa destino = De(id);
            if (destino == null)
            {
                return false;
            }
            if (destino.Lugar != IdLugar.Ninguno)
            {
                IReadOnlyList<ILugar> lugares = Registro<ILugar>.Todos;
                for (int i = 0; i < lugares.Count; i++)
                {
                    if (lugares[i] != null && lugares[i].Id == destino.Lugar)
                    {
                        posicion = Plano(lugares[i].PosicionEntrada);
                        ultimas[id] = posicion;
                        return true;
                    }
                }
            }
            if (destino.Marca.HasValue && MarcaDe(destino.Marca.Value, out IMarcaMapa marca))
            {
                posicion = Plano(marca.Posicion);
                ultimas[id] = posicion;
                return true;
            }
            foreach (string nombre in destino.NombresDeObjeto)
            {
                Transform t = Objeto(nombre);
                if (t != null)
                {
                    posicion = PosicionDe(t);
                    ultimas[id] = posicion;
                    return true;
                }
            }
            return ultimas.TryGetValue(id, out posicion);
        }

        /// <summary>True si una marca de lugar de T-409 del mismo tipo ya dibuja este destino (para no repetir su icono).</summary>
        public static bool LoDibujaUnaMarca(IdDestinoMapa id) => De(id)?.Marca != null && MarcaDe(De(id).Marca.Value, out _);

        /// <summary>El destino más cercano a <paramref name="punto"/> (m) dentro de <paramref name="radio"/> m, o null.</summary>
        public static DestinoMapa MasCercano(Vector3 punto, float radio)
        {
            DestinoMapa mejor = null;
            float menor = radio;
            foreach (DestinoMapa d in todos)
            {
                if (Posicion(d.Id, out Vector3 p))
                {
                    float distancia = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(punto.x, punto.z));
                    if (distancia <= menor)
                    {
                        menor = distancia;
                        mejor = d;
                    }
                }
            }
            return mejor;
        }

        private static bool MarcaDe(TipoMarcaMapa tipo, out IMarcaMapa marca)
        {
            IReadOnlyList<IMarcaMapa> marcas = Registro<IMarcaMapa>.Todos;
            for (int i = 0; i < marcas.Count; i++)
            {
                if (marcas[i] != null && marcas[i].Tipo == tipo && marcas[i].Visible)
                {
                    marca = marcas[i];
                    return true;
                }
            }
            marca = null;
            return false;
        }

        // Busca el objeto por nombre una vez y lo recuerda; si no existe, no vuelve a buscarlo hasta pasados unos segundos (Find recorre toda la escena).
        private static Transform Objeto(string nombre)
        {
            if (objetos.TryGetValue(nombre, out Transform guardado) && guardado != null)
            {
                return guardado;
            }
            if (faltantes.TryGetValue(nombre, out float hasta) && Time.unscaledTime < hasta)
            {
                return null;
            }
            GameObject encontrado = GameObject.Find(nombre);
            Transform transformEncontrado = encontrado != null ? encontrado.transform : BuscarApagado(nombre);
            if (transformEncontrado == null)
            {
                faltantes[nombre] = Time.unscaledTime + SegundosDeCache;
                return null;
            }
            objetos[nombre] = transformEncontrado;
            faltantes.Remove(nombre);
            return transformEncontrado;
        }

        // T-934: GameObject.Find no ve lo apagado, y un interior apagado no debe quitar su lugar del mapa.
        private static Transform BuscarApagado(string nombre)
        {
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene escena = SceneManager.GetSceneAt(s);
                if (!escena.isLoaded)
                {
                    continue;
                }
                foreach (GameObject raiz in escena.GetRootGameObjects())
                {
                    foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == nombre)
                        {
                            return t;
                        }
                    }
                }
            }
            return null;
        }

        // Un objeto de un grupo que se quedó en el origen (como la raíz del Tianguis) no dice dónde está: se usa el centro de lo que dibuja.
        private static Vector3 PosicionDe(Transform t)
        {
            Vector3 p = t.position;
            if (new Vector2(p.x, p.z).sqrMagnitude > 0.01f)
            {
                return Plano(p);
            }
            Bounds limites = new Bounds(p, Vector3.zero);
            bool hay = false;
            foreach (Renderer r in t.GetComponentsInChildren<Renderer>())
            {
                if (hay)
                {
                    limites.Encapsulate(r.bounds);
                }
                else
                {
                    limites = r.bounds;
                    hay = true;
                }
            }
            return Plano(limites.center);
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
