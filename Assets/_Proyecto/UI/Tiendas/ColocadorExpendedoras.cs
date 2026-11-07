using System.Collections.Generic;
using MechaCyberpunk.UI.Datos;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// Pone las máquinas expendedoras (T-685) al cargar una escena, leyendo la lista de <see cref="DatosExpendedoras"/>
    /// (Resources/DatosExpendedoras_Ciudad). No hace falta editar ninguna escena. Cada máquina es un objeto "Expendedora_N"
    /// con el modelo de Arte, un collider y el componente <see cref="Expendedora"/>; todas comparten una pantalla de tienda.
    /// </summary>
    public static class ColocadorExpendedoras
    {
        public const string NombreDatos = "DatosExpendedoras_Ciudad";
        public const string NombreRaiz = "Expendedoras";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AlArrancar()
        {
            SceneManager.sceneLoaded -= AlCargarEscena;
            SceneManager.sceneLoaded += AlCargarEscena;
            Colocar(SceneManager.GetActiveScene());
        }

        private static void AlCargarEscena(Scene escena, LoadSceneMode modo)
        {
            if (modo == LoadSceneMode.Single)
            {
                Colocar(escena);
            }
        }

        /// <summary>Coloca las máquinas de la escena dada. Devuelve la raíz creada, o null si no hay nada que poner o ya estaban.</summary>
        public static Transform Colocar(Scene escena)
        {
            DatosExpendedoras datos = Resources.Load<DatosExpendedoras>(NombreDatos);
            return datos != null ? Colocar(escena, datos) : null;
        }

        public static Transform Colocar(Scene escena, DatosExpendedoras datos)
        {
            if (!escena.IsValid() || !escena.isLoaded || datos == null || datos.Modelo == null || datos.Tienda == null
                || datos.Catalogo == null || datos.Pantalla == null)
            {
                return null;
            }
            var lugares = new List<DatosExpendedoras.Posicion>();
            foreach (DatosExpendedoras.Posicion p in datos.Posiciones)
            {
                if (p.escena == escena.name)
                {
                    lugares.Add(p);
                }
            }
            if (lugares.Count == 0)
            {
                return null;
            }
            foreach (GameObject raizEscena in escena.GetRootGameObjects())
            {
                if (raizEscena.name == NombreRaiz)
                {
                    return null;
                }
            }
            var raiz = new GameObject(NombreRaiz);
            SceneManager.MoveGameObjectToScene(raiz, escena);
            PantallaTienda pantalla = Object.Instantiate(datos.Pantalla, raiz.transform);
            pantalla.name = "PantallaExpendedoras";
            for (int i = 0; i < lugares.Count; i++)
            {
                DatosExpendedoras.Posicion p = lugares[i];
                var maquina = new GameObject("Expendedora_" + (i + 1));
                maquina.transform.SetParent(raiz.transform, false);
                maquina.transform.SetPositionAndRotation(p.posicion, Quaternion.Euler(0f, p.giro, 0f));
                GameObject modelo = Object.Instantiate(datos.Modelo, maquina.transform);
                modelo.name = "Modelo";
                modelo.transform.localPosition = Vector3.zero;
                modelo.transform.localRotation = Quaternion.identity;
                if (maquina.GetComponentInChildren<Collider>() == null)
                {
                    var caja = maquina.AddComponent<BoxCollider>();
                    caja.center = new Vector3(0f, 1f, 0f);
                    caja.size = new Vector3(1.64f, 2f, 0.98f);
                }
                maquina.AddComponent<Expendedora>().Iniciar(datos.Tienda, datos.Catalogo, pantalla);
            }
            return raiz.transform;
        }
    }
}
