using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Lugares;
using MechaCyberpunk.Mundo.Pruebas;
using MechaCyberpunk.Mundo.Reloj;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>Ayudas compartidas por las pruebas de puertas y lugares: Ciudad.unity, el reloj y la cámara libre frente a una puerta.</summary>
    public static class EscenaLugares
    {
        public const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad.unity";
        public const string RutaPrefabCamara = "Assets/_Proyecto/Mundo/Prefabs/CamaraLibre.prefab";
        public const float HoraMediodia = 12f;
        public const float HoraNoche = 22f;
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float AlturaApoyo = 0.05f;

        public static IEnumerator CargarCiudad()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaCiudad, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
        }

        public static void Descargar()
        {
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.DestroyImmediate(raiz);
            }
        }

        public static RelojMundo RelojPausado()
        {
            RelojMundo reloj = Object.FindFirstObjectByType<RelojMundo>();
            Assert.IsNotNull(reloj, "Ciudad debe traer un RelojMundo");
            reloj.Pausar(true);
            return reloj;
        }

        // Los primeros frames de PlayMode son lentos: se espera a que deltaTime se estabilice.
        public static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        public static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        public static ILugar Lugar(IdLugar id)
        {
            foreach (ILugar lugar in Registro<ILugar>.Todos)
            {
                if (lugar.Id == id)
                {
                    return lugar;
                }
            }
            Assert.Fail("No hay un ILugar registrado para " + id);
            return null;
        }

        public static IPuerta Puerta(IdLugar id)
        {
            foreach (IPuerta puerta in Registro<IPuerta>.Todos)
            {
                if (puerta.Lugar == id)
                {
                    return puerta;
                }
            }
            Assert.Fail("No hay una IPuerta registrada para " + id);
            return null;
        }

        /// <summary>La ZonaInterior del lugar (T-303: Ciudad ya trae la del bar y la del casino).</summary>
        public static ZonaInterior ZonaDe(IdLugar id)
        {
            foreach (ZonaInterior zona in Object.FindObjectsByType<ZonaInterior>(FindObjectsSortMode.None))
            {
                LugarMundo lugar = zona.GetComponentInParent<LugarMundo>();
                if (lugar != null && lugar.Id == id)
                {
                    return zona;
                }
            }
            Assert.Fail("No hay ZonaInterior para " + id);
            return null;
        }

        /// <summary>Dirección horizontal desde la entrada del lugar hacia su puerta (la que mira la cámara al llegar).</summary>
        public static Vector3 HaciaLaPuerta(IdLugar id)
        {
            Vector3 direccion = Puerta(id).Posicion - Lugar(id).PosicionEntrada;
            direccion.y = 0f;
            return direccion.normalized;
        }

        /// <summary>Instancia la cámara libre en la entrada del lugar, mirando a su puerta.</summary>
        public static CamaraLibre ColocarFrenteA(IdLugar id, out GameObject objeto)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabCamara);
            objeto = Object.Instantiate(prefab);
            CamaraLibre camara = objeto.GetComponent<CamaraLibre>();
            Vector3 posicion = Lugar(id).PosicionEntrada + Vector3.up * AlturaApoyo;
            camara.Colocar(new Pose(posicion, Quaternion.LookRotation(HaciaLaPuerta(id), Vector3.up)));
            return camara;
        }

        /// <summary>Cuántos metros pasó la cámara de la puerta hacia adentro (negativo: sigue en la calle).</summary>
        public static float MetrosDentro(CamaraLibre camara, IdLugar id)
        {
            return Vector3.Dot(camara.transform.position - Puerta(id).Posicion, HaciaLaPuerta(id));
        }
    }
}
