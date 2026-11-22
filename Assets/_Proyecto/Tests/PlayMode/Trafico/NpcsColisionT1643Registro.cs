using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Peatones;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Trafico
{
    /// <summary>
    /// T-1643 (T23): deja correr 2 min de juego (timeScale 2) en Ciudad_Prologo y cuenta cuántas veces un NPC (peatón o personaje con CharacterController
    /// que no es el jugador) queda con la cápsula dentro de un collider sólido. Escribe docs/capturas/_tareas/T23/registro.txt y dos capturas. Explicit.
    /// </summary>
    public class NpcsColisionT1643Registro
    {
        private const string Carpeta = "docs/capturas/_tareas/T23";
        private const float SegundosDeJuego = 120f;
        private const float EscalaTiempo = 2f;
        private const float SegundosEntreMuestras = 0.5f;
        private const int MaximoEjemplos = 25;

        [UnityTest, Explicit("Genera docs/capturas/_tareas/T23/registro.txt y capturas (2 min a timeScale 2 en Ciudad_Prologo)")]
        public IEnumerator CiudadPrologo_CuentaNpcsDentroDeObjetos()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return EditorSceneManager.LoadSceneInPlayMode("Assets/_Proyecto/Escenas/Ciudad_Prologo.unity", new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 40; i++)
            {
                yield return null;
            }
            Directory.CreateDirectory(Carpeta);
            int muestras = 0, dentroPeatones = 0, dentroOtros = 0, maxPeatones = 0, maxOtros = 0;
            var ejemplos = new List<string>();
            Vector3? primerCaso = null;
            Time.timeScale = EscalaTiempo;
            float fin = Time.time + SegundosDeJuego;
            float proxima = 0f;
            while (Time.time < fin)
            {
                yield return null;
                if (Time.time < proxima)
                {
                    continue;
                }
                proxima = Time.time + SegundosEntreMuestras;
                muestras++;
                Physics.SyncTransforms();
                Transform jugador = Registro<IJugador>.Actual?.Raiz;
                int peatones = 0, otros = 0;
                foreach (PeatonMundo p in PeatonMundo.Activos)
                {
                    if (p == null || p.Muerto || !p.gameObject.activeInHierarchy || MovimientoSeguro.Libre(p.transform.position, p.transform))
                    {
                        continue;
                    }
                    peatones++;
                    Anotar(ejemplos, "peaton", p.name, p.transform.position, Time.time);
                    primerCaso = primerCaso ?? p.transform.position;
                }
                foreach (CharacterController cc in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
                {
                    if (!cc.enabled || (jugador != null && cc.transform.IsChildOf(jugador)) || cc.GetComponentInParent<PeatonMundo>() != null)
                    {
                        continue;
                    }
                    Vector3 pie = new Vector3(cc.bounds.center.x, cc.bounds.min.y, cc.bounds.center.z);
                    if (MovimientoSeguro.Libre(pie, cc.transform))
                    {
                        continue;
                    }
                    otros++;
                    Anotar(ejemplos, "cc", cc.name, pie, Time.time);
                    primerCaso = primerCaso ?? pie;
                }
                dentroPeatones += peatones;
                dentroOtros += otros;
                maxPeatones = Mathf.Max(maxPeatones, peatones);
                maxOtros = Mathf.Max(maxOtros, otros);
            }
            Time.timeScale = 1f;
            var sb = new StringBuilder();
            sb.AppendLine("T23 registro: " + SegundosDeJuego + " s de juego a timeScale " + EscalaTiempo + ", " + muestras + " muestras cada " + SegundosEntreMuestras + " s");
            sb.AppendLine("Peatones con la cápsula dentro de algo sólido: " + dentroPeatones + " (máximo a la vez " + maxPeatones + ")");
            sb.AppendLine("Otros NPC con CharacterController dentro de algo sólido: " + dentroOtros + " (máximo a la vez " + maxOtros + ")");
            foreach (string e in ejemplos)
            {
                sb.AppendLine("  " + e);
            }
            File.WriteAllText(Path.Combine(Carpeta, "registro.txt"), sb.ToString());

            GameObject camObjeto = new GameObject("Cam_T1643");
            Camera cam = camObjeto.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            PeatonMundo cualquiera = null;
            foreach (PeatonMundo p in PeatonMundo.Activos)
            {
                if (p != null && p.gameObject.activeInHierarchy)
                {
                    cualquiera = p;
                    break;
                }
            }
            if (cualquiera != null)
            {
                Foto(cam, cualquiera.transform.position, "peaton.png");
            }
            if (primerCaso.HasValue)
            {
                Foto(cam, primerCaso.Value, "primer-caso.png");
            }
            Object.Destroy(camObjeto);
            Debug.Log("[Trafico] T23 registro: peatones dentro=" + dentroPeatones + ", otros=" + dentroOtros);
        }

        private static void Anotar(List<string> ejemplos, string tipo, string nombre, Vector3 pos, float t)
        {
            if (ejemplos.Count < MaximoEjemplos)
            {
                ejemplos.Add(tipo + " " + nombre + " en " + pos.ToString("0.0") + " t=" + t.ToString("0.0"));
            }
        }

        private static void Foto(Camera cam, Vector3 foco, string nombre)
        {
            cam.transform.position = foco + new Vector3(2.5f, 2f, -3f);
            cam.transform.LookAt(foco + Vector3.up);
            CapturaPrueba.Guardar(cam, "_tareas/T23/" + nombre);
        }
    }
}
