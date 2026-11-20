using System.Collections.Generic;
using MechaCyberpunk.UI.Datos;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// T-1529: pone la tiendita de la esquina al cargar la escena (la llama <see cref="ColocadorExpendedoras"/>, con la misma pantalla de compra de las
    /// máquinas): el edificio ED02, un mostrador a su entrada y el tendero detrás. La compra de los consumibles es con E en el mostrador
    /// (<see cref="Expendedora"/> con la tienda de la tiendita). Usa el primer sitio libre de <see cref="DatosExpendedoras.SitiosTiendita"/>: no se
    /// edita la escena. Cada paso escribe "[T-1529] ..." en el log para ver dónde quedó.
    /// </summary>
    public static class ColocadorTiendita
    {
        public const string NombreRaiz = "Tiendita";
        private const float AlturaRevisionMin = 0.8f;
        private const float AlturaRevisionMax = 3f;

        /// <summary>Pone la tiendita bajo <paramref name="padre"/>. Devuelve su raíz, o null si no hay datos o sitios.</summary>
        public static Transform Colocar(Scene escena, DatosExpendedoras datos, Transform padre, PantallaTienda pantalla)
        {
            if (datos == null || datos.Tiendita == null || datos.TiendaTiendita == null || datos.Catalogo == null || pantalla == null)
            {
                return null;
            }
            var sitios = new List<DatosExpendedoras.Posicion>();
            var posiciones = new List<Vector3>();
            foreach (DatosExpendedoras.Posicion p in datos.SitiosTiendita)
            {
                if (p.escena == escena.name && ColocadorExpendedoras.ResolverPosicion(p, escena, out Vector3 donde))
                {
                    sitios.Add(p);
                    posiciones.Add(donde);
                }
            }
            if (sitios.Count == 0)
            {
                Debug.LogWarning("[T-1529] Tiendita: ningún sitio resuelve en la escena " + escena.name + " (falta marcador o lugar); no se coloca.");
                return null;
            }

            var raiz = new GameObject(NombreRaiz);
            raiz.transform.SetParent(padre, false);
            GameObject edificio = null;
            Bounds medidas = default;
            var libres = new bool[sitios.Count];
            int elegido = -1;
            // Se prueba cada sitio con el edificio puesto (para medirlo de verdad) y se queda el primero libre; si ninguno, el primero con aviso.
            for (int i = 0; i < sitios.Count; i++)
            {
                if (edificio != null)
                {
                    Object.DestroyImmediate(edificio);
                    edificio = null;
                }
                edificio = Object.Instantiate(datos.Tiendita, posiciones[i], Quaternion.Euler(0f, sitios[i].giro, 0f), raiz.transform);
                edificio.name = "Edificio";
                medidas = Medir(edificio);
                libres[i] = EstaLibre(medidas, edificio.transform, ReglaTiendita.Frente(sitios[i].giro), posiciones[i].y);
                Debug.Log("[T-1529] Tiendita: sitio " + i + " (" + sitios[i].nota + ") en " + posiciones[i] + " libre=" + libres[i] + " medidas=" + medidas.size);
                if (libres[i])
                {
                    elegido = i;
                    break;
                }
            }
            if (elegido < 0)
            {
                elegido = ReglaTiendita.ElegirSitio(libres);
                Debug.LogWarning("[T-1529] Tiendita: ningún sitio estaba libre; se usa el primero (" + sitios[elegido].nota + "). Conviene un marcador 'Marcador_Tiendita'.");
                if (edificio != null)
                {
                    Object.DestroyImmediate(edificio);
                }
                edificio = Object.Instantiate(datos.Tiendita, posiciones[elegido], Quaternion.Euler(0f, sitios[elegido].giro, 0f), raiz.transform);
                edificio.name = "Edificio";
                medidas = Medir(edificio);
            }

            Vector3 frente = ReglaTiendita.Frente(sitios[elegido].giro);
            float suelo = posiciones[elegido].y;
            Quaternion mira = Quaternion.LookRotation(frente, Vector3.up);
            Vector3 mostradorPos = ReglaTiendita.PosicionMostrador(medidas.center, frente, medidas.extents, suelo);
            Vector3 tenderoPos = ReglaTiendita.PosicionTendero(medidas.center, frente, medidas.extents, suelo);

            var puesto = new GameObject("Mostrador");
            puesto.transform.SetParent(raiz.transform, false);
            puesto.transform.SetPositionAndRotation(mostradorPos, mira);
            if (datos.Mostrador != null)
            {
                GameObject modelo = Object.Instantiate(datos.Mostrador, puesto.transform);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;
            }
            if (puesto.GetComponentInChildren<Collider>() == null)
            {
                var caja = puesto.AddComponent<BoxCollider>();
                caja.center = new Vector3(0f, 0.55f, 0f);
                caja.size = new Vector3(2.4f, 1.1f, 0.8f);
            }
            puesto.AddComponent<Expendedora>().Iniciar(datos.TiendaTiendita, datos.Catalogo, pantalla);

            if (datos.Tendero != null)
            {
                GameObject persona = Object.Instantiate(datos.Tendero, raiz.transform);
                persona.name = "Tendero";
                persona.transform.SetPositionAndRotation(tenderoPos, mira);
            }
            Debug.Log("[T-1529] Tiendita puesta: sitio '" + sitios[elegido].nota + "' edificio=" + medidas.center + " mostrador=" + mostradorPos + " tendero=" + tenderoPos);
            return raiz.transform;
        }

        private static Bounds Medir(GameObject objeto)
        {
            Renderer[] renders = objeto.GetComponentsInChildren<Renderer>();
            if (renders.Length == 0)
            {
                return new Bounds(objeto.transform.position + Vector3.up * 3f, new Vector3(6f, 6f, 6f));
            }
            Bounds b = renders[0].bounds;
            for (int i = 1; i < renders.Length; i++)
            {
                b.Encapsulate(renders[i].bounds);
            }
            return b;
        }

        // Libre: ningún collider ajeno al edificio dentro de su caja (sin el piso) ni de la zona del mostrador.
        private static bool EstaLibre(Bounds medidas, Transform edificio, Vector3 frente, float suelo)
        {
            Vector3 mitad = medidas.extents;
            Vector3 centro = new Vector3(medidas.center.x, suelo + (AlturaRevisionMin + AlturaRevisionMax) * 0.5f, medidas.center.z);
            Vector3 caja = new Vector3(mitad.x + ReglaTiendita.MargenLibre, (AlturaRevisionMax - AlturaRevisionMin) * 0.5f, mitad.z + ReglaTiendita.MargenLibre);
            if (Ocupado(centro, caja, edificio))
            {
                return false;
            }
            Vector3 mostrador = ReglaTiendita.PosicionMostrador(medidas.center, frente, mitad, suelo);
            return !Ocupado(mostrador + Vector3.up * (AlturaRevisionMin + 0.2f), new Vector3(1.4f, 0.3f, 1.0f), edificio);
        }

        private static bool Ocupado(Vector3 centro, Vector3 mitad, Transform edificio)
        {
            Collider[] tocados = Physics.OverlapBox(centro, mitad, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < tocados.Length; i++)
            {
                if (tocados[i] != null && !tocados[i].transform.IsChildOf(edificio))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
