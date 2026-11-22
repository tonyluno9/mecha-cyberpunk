using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10, paso 3): dos SKULLS le salen al paso al jugador, lo insultan, lo empujan y lo tiran; se ríen y se van, y él se levanta con la
    /// animación de levantarse de <see cref="AtropelloJugador"/> (la misma del atropello). Durante la escena el jugador NO puede atacar: puños y armas
    /// quedan apagados y el intento de pegar saca un aviso discreto. Los SKULLS se llevan con <see cref="PandilleroAPie.Acosando"/> (la IA solo les pone
    /// la gravedad) y se mueven con <see cref="PandilleroAPie.MoverPlano"/>; se destruyen lejos de la vista.
    /// </summary>
    public class MolestiaSkulls : MonoBehaviour
    {
        public const string SonidoInsulto = "SKU_INSULTO";
        public const string SonidoRisa = "SKU_RISA_BURLA";
        public const string SonidoProvocar = "SKU_PROVOCAR";
        private const float RapidezCaminar = 2.4f;
        private const float RapidezHuir = 3.2f;
        private const float DistanciaFrente = 1.8f;
        private const float SegundosInsulto = 1.6f;
        private const float SegundosRisa = 2.2f;
        private const float SegundosHuida = 5f;
        private const float SegundosMaximosAcercarse = 9f;
        private const float SegundosMaximosLevantarse = 7f;
        private const float SegundosAvisoRepetido = 3f;
        private const float GradosGiroPorSegundo = 360f;

        private static readonly string[] GritosInsulto = { "¡Ey, pelado!", "¿A dónde vas?", "¡Mírenlo, qué valiente!" };

        private readonly List<PandilleroAPie> skulls = new List<PandilleroAPie>();
        private readonly List<Behaviour> apagados = new List<Behaviour>();
        private ControladorAPie controlador;
        private AtropelloJugador atropello;
        private Transform jugador;
        private bool controlAntes;
        private float ultimoAviso = -100f;
        private Coroutine escena;

        /// <summary>True mientras la escena corre.</summary>
        public bool EnCurso => escena != null;

        /// <summary>True si los puños y las armas del jugador están apagados por la escena.</summary>
        public bool AtaqueBloqueado => apagados.Count > 0;

        /// <summary>Los SKULLS de la escena (para pruebas).</summary>
        public IReadOnlyList<PandilleroAPie> Skulls => skulls;

        /// <summary>Se avisa al terminar (el jugador ya se levantó y los SKULLS se fueron).</summary>
        public event Action Terminada;

        /// <summary>
        /// Arranca la escena. <paramref name="prefabs"/> trae uno o dos prefabs de SKULL; <paramref name="distancia"/> es qué tan adelante del jugador salen.
        /// Devuelve false (sin hacer nada) si falta el jugador o los prefabs: quien llama sigue con la historia.
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia)
        {
            IJugador j = Registro<IJugador>.Actual;
            if (escena != null || j == null || j.Raiz == null || prefabs == null || prefabs.Count == 0 || prefabs[0] == null)
            {
                return false;
            }
            jugador = j.Raiz;
            controlador = jugador.GetComponentInChildren<ControladorAPie>(true);
            atropello = jugador.GetComponent<AtropelloJugador>();
            if (atropello == null && controlador != null)
            {
                atropello = jugador.gameObject.AddComponent<AtropelloJugador>();
            }
            if (controlador == null || atropello == null)
            {
                return false;
            }
            Vector3 adelante = Vector3.ProjectOnPlane(jugador.forward, Vector3.up).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            for (int i = 0; i < 2; i++)
            {
                GameObject prefab = prefabs[Mathf.Min(i, prefabs.Count - 1)] != null ? prefabs[Mathf.Min(i, prefabs.Count - 1)] : prefabs[0];
                Vector3 donde = Suelo(jugador.position + adelante * distancia + lado * (i == 0 ? -1.2f : 1.2f));
                GameObject objeto = Instantiate(prefab, donde, Quaternion.LookRotation(-adelante, Vector3.up));
                objeto.name = "SkullRecorrido_" + (i + 1);
                PandilleroAPie p = objeto.GetComponent<PandilleroAPie>() ?? objeto.GetComponentInChildren<PandilleroAPie>(true);
                if (p == null)
                {
                    Destroy(objeto);
                    continue;
                }
                p.Acosando = true;
                skulls.Add(p);
            }
            if (skulls.Count == 0)
            {
                return false;
            }
            escena = StartCoroutine(Correr());
            return true;
        }

        /// <summary>Corta la escena (salto de paso o fin de partida): quita a los SKULLS y devuelve ataque y control.</summary>
        public void Cancelar()
        {
            if (escena != null)
            {
                StopCoroutine(escena);
                escena = null;
            }
            Limpiar();
        }

        private void OnDestroy() => Cancelar();

        private IEnumerator Correr()
        {
            BloquearAtaque();
            controlAntes = controlador.ControlActivo;
            // 1. Se le acercan: lo detienen en seco.
            controlador.ActivarControl(false);
            float limite = Time.time + SegundosMaximosAcercarse;
            while (Time.time < limite && DistanciaMinima() > DistanciaFrente)
            {
                foreach (PandilleroAPie p in Vivos())
                {
                    Vector3 hacia = Plano(jugador.position - p.transform.position);
                    p.MirarHacia(hacia, GradosGiroPorSegundo);
                    p.MoverPlano(hacia.normalized * RapidezCaminar);
                }
                yield return null;
            }
            // 2. Lo insultan.
            AlinearFrenteAlJugador();
            for (int i = 0; i < skulls.Count; i++)
            {
                PandilleroAPie p = skulls[i];
                if (p == null)
                {
                    continue;
                }
                p.MoverPlano(Vector3.zero);
                p.Gritar(GritosInsulto[i % GritosInsulto.Length]);
                Sonidos.ReproducirEn(i == 0 ? SonidoInsulto : SonidoProvocar, p.transform.position);
            }
            yield return Esperar(SegundosInsulto);
            // 3. Lo empujan y lo tiran. El control vuelve un instante para que el empujón lo mueva; el derribo bloquea caminar y pegar.
            PandilleroAPie empujador = Vivos().Count > 0 ? Vivos()[0] : null;
            Vector3 direccion = empujador != null ? Plano(jugador.position - empujador.transform.position) : jugador.forward;
            if (empujador != null && empujador.Visual != null)
            {
                empujador.Visual.Golpear(false);
            }
            controlador.ActivarControl(controlAntes);
            Aviso(TablaRecorrido.AvisoSinAtacar);
            atropello.Derribar(direccion);
            if (empujador != null)
            {
                Sonidos.ReproducirEn(SonidoInsulto, empujador.transform.position);
            }
            yield return Esperar(0.6f);
            // 4. Se ríen y se van.
            foreach (PandilleroAPie p in Vivos())
            {
                Sonidos.ReproducirEn(SonidoRisa, p.transform.position);
                if (p.Visual != null)
                {
                    p.Visual.Burlar();
                }
            }
            yield return Esperar(SegundosRisa);
            Vector3 salida = Plano(Vivos().Count > 0 ? Vivos()[0].transform.position - jugador.position : -jugador.forward).normalized;
            float finHuida = Time.time + SegundosHuida;
            while (Time.time < finHuida)
            {
                foreach (PandilleroAPie p in Vivos())
                {
                    p.MirarHacia(salida, GradosGiroPorSegundo);
                    p.MoverPlano(salida * RapidezHuir);
                }
                yield return null;
            }
            // 5. Espera a que se levante (AtropelloJugador lo levanta solo) y lo suelta.
            float limiteLevantar = Time.time + SegundosMaximosLevantarse;
            while (atropello != null && atropello.Derribado && Time.time < limiteLevantar)
            {
                yield return null;
            }
            escena = null;
            Limpiar();
            Terminada?.Invoke();
        }

        private void Update()
        {
            // Un clic mientras no puede pegar: aviso discreto.
            if (apagados.Count > 0 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Aviso(TablaRecorrido.AvisoSinAtacar);
            }
        }

        private void Aviso(string texto)
        {
            if (Time.time - ultimoAviso < SegundosAvisoRepetido)
            {
                return;
            }
            ultimoAviso = Time.time;
            MechaCyberpunk.UI.Hud.ColaAvisos.Publicar(texto, MechaCyberpunk.UI.Hud.PrioridadAviso.Normal, MechaCyberpunk.UI.Hud.TipoAviso.Malo, 3f);
        }

        // Puños y armas (a pie) apagados mientras dura la escena; se vuelven a prender igual.
        private void BloquearAtaque()
        {
            apagados.Clear();
            Transform raiz = jugador;
            foreach (GolpeAPie g in raiz.GetComponentsInChildren<GolpeAPie>(true))
            {
                Apagar(g);
            }
            foreach (PistolaAPie g in raiz.GetComponentsInChildren<PistolaAPie>(true))
            {
                Apagar(g);
            }
            foreach (ArmasEspecialesAPie g in raiz.GetComponentsInChildren<ArmasEspecialesAPie>(true))
            {
                Apagar(g);
            }
        }

        private void Apagar(Behaviour componente)
        {
            if (componente != null && componente.enabled)
            {
                componente.enabled = false;
                apagados.Add(componente);
            }
        }

        private void Limpiar()
        {
            foreach (Behaviour b in apagados)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
            apagados.Clear();
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    Destroy(p.gameObject);
                }
            }
            skulls.Clear();
            if (controlador != null && !controlador.ControlActivo && controlAntes)
            {
                controlador.ActivarControl(true);
            }
        }

        private List<PandilleroAPie> Vivos()
        {
            var lista = new List<PandilleroAPie>(skulls.Count);
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    lista.Add(p);
                }
            }
            return lista;
        }

        private float DistanciaMinima()
        {
            float menor = float.MaxValue;
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    menor = Mathf.Min(menor, Plano(jugador.position - p.transform.position).magnitude);
                }
            }
            return menor;
        }

        private void AlinearFrenteAlJugador()
        {
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    Vector3 hacia = Plano(jugador.position - p.transform.position);
                    if (hacia.sqrMagnitude > 0.0001f)
                    {
                        p.transform.rotation = Quaternion.LookRotation(hacia.normalized, Vector3.up);
                    }
                }
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 6f, Vector3.down, out RaycastHit golpe, 14f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point + Vector3.up * 0.05f;
            }
            return punto;
        }
    }
}
