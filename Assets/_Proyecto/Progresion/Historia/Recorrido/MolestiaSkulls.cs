using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
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
        private Vector3 puntoSalida;

        // T-1652: caminan lento platicando, se le ponen enfrente (contacto a 0.8 m), lo empujan en el cuadro del contacto y se van corriendo.
        private const float RapidezPlatica = 1.4f;
        private const float RapidezPlantarse = 2.8f;
        private const float RapidezCorrer = 5.5f;
        private const float DistanciaPlantarse = 4.5f;
        private const float DistanciaContacto = 0.8f;
        private const float SegundosEntreFrases = 3.4f;
        private const float SegundosHastaContacto = 0.4f; // PJ01_Pandillero_Empujar (23 cuadros): la mano llega al pecho a ~0.4 s
        private const float SegundosMaximosPlatica = 45f;
        private const float AlturaBoca = 1.7f;
        private static readonly Color ColorSkull = new Color(1f, 0.25f, 0.85f);
        private static readonly string[] FrasesPlatica =
        {
            "¿Y ese quién es?", "Mira nomás, un pelado perdido.", "Ni se ha dado cuenta.", "Va solito, el valiente.", "Pa' mí que trae lana."
        };
        private static readonly string[] FrasesEnfrente = { "¿A dónde tan solito, pelado?", "Aquí se paga el paso." };

        /// <summary>Depuración: qué fase va ("platica", "plantan", "empujan", "huyen", "") para las pruebas y capturas.</summary>
        public string Fase { get; private set; } = string.Empty;

        /// <summary>
        /// Respaldo cuando no hay marcador Historia_CallejonSkulls: un punto a unos 15-20 m de <paramref name="inicio"/>, a un lado de la calle (perpendicular a
        /// <paramref name="adelante"/>), con piso debajo y sin colisionadores encima. Devuelve false si no encuentra hueco (quien llama usa otra cosa).
        /// </summary>
        public static bool BuscarSalidaRespaldo(Vector3 inicio, Vector3 adelante, out Vector3 punto)
        {
            adelante = Plano(adelante);
            adelante = adelante.sqrMagnitude > 0.0001f ? adelante.normalized : Vector3.forward;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            float[] distancias = { 17f, 15f, 20f, 18f };
            float[] avances = { 0f, 4f, -4f, 8f, -8f };
            foreach (float d in distancias)
            {
                foreach (float a in avances)
                {
                    for (int signo = -1; signo <= 1; signo += 2)
                    {
                        Vector3 candidato = inicio + lado * (d * signo) + adelante * a;
                        if (Physics.Raycast(candidato + Vector3.up * 8f, Vector3.down, out RaycastHit golpe, 20f, ~0, QueryTriggerInteraction.Ignore)
                            && !Physics.CheckCapsule(golpe.point + Vector3.up * 0.4f, golpe.point + Vector3.up * 1.7f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
                        {
                            punto = golpe.point + Vector3.up * 0.05f;
                            return true;
                        }
                    }
                }
            }
            punto = default;
            return false;
        }

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
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia) => Iniciar(prefabs, distancia, null);

        /// <summary>
        /// T-1652: como <see cref="Iniciar(IReadOnlyList{GameObject}, float)"/> pero los SKULLS salen de <paramref name="salida"/> (el callejón junto a la casa)
        /// y caminan hacia el jugador. Con null salen <paramref name="distancia"/> metros adelante de él, como antes.
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia, Vector3? salida)
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
                Vector3 baseSalida = salida.HasValue ? salida.Value : jugador.position + adelante * distancia;
                Vector3 donde = Suelo(baseSalida + lado * (i == 0 ? -0.8f : 0.8f));
                Vector3 mira = Plano(jugador.position - donde);
                Quaternion giro = mira.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(mira.normalized, Vector3.up) : Quaternion.LookRotation(-adelante, Vector3.up);
                GameObject objeto = Instantiate(prefab, donde, giro);
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
            puntoSalida = skulls[0].transform.position;
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
            // 1. Caminan lento hacia él desde el callejón, platicando entre ellos. El jugador sigue libre de moverse (no de pegar).
            Fase = "platica";
            float inicio = Time.time;
            float proximaFrase = Time.time + 0.5f;
            int frase = 0;
            while (Time.time - inicio < SegundosMaximosPlatica && DistanciaMinima() > DistanciaPlantarse)
            {
                bool apurar = Time.time - inicio > SegundosMaximosPlatica * 0.5f;
                foreach (PandilleroAPie p in Vivos())
                {
                    Vector3 hacia = Plano(jugador.position - p.transform.position);
                    p.MirarHacia(hacia, GradosGiroPorSegundo * 0.5f);
                    p.MoverPlano(hacia.normalized * (apurar ? RapidezPlantarse : RapidezPlatica));
                }
                if (Time.time >= proximaFrase)
                {
                    proximaFrase = Time.time + SegundosEntreFrases;
                    Platicar(frase++);
                }
                yield return null;
            }
            // 2. Se le ponen enfrente: lo detienen en seco, uno de frente a 0.8 m y el otro a un lado.
            Fase = "plantan";
            controlador.ActivarControl(false);
            List<PandilleroAPie> vivos = Vivos();
            PandilleroAPie empujador = vivos.Count > 0 ? vivos[0] : null;
            PandilleroAPie otro = vivos.Count > 1 ? vivos[1] : null;
            Vector3 frente = Plano(jugador.forward).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, frente);
            Vector3 puestoFrente = jugador.position + frente * DistanciaContacto;
            if (Physics.Raycast(jugador.position + Vector3.up * 0.9f, frente, DistanciaContacto + 0.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Pared de frente: el empujador se pone del lado por donde venía.
                Vector3 vino = empujador != null ? Plano(empujador.transform.position - jugador.position).normalized : -frente;
                puestoFrente = jugador.position + vino * DistanciaContacto;
            }
            Vector3 puestoOtro = jugador.position + (puestoFrente - jugador.position).normalized * 1.5f + lado * 1.1f;
            float limitePlantarse = Time.time + 6f;
            while (Time.time < limitePlantarse)
            {
                bool listos = IrAlPuesto(empujador, puestoFrente, 0.12f);
                listos &= IrAlPuesto(otro, puestoOtro, 0.3f);
                if (listos)
                {
                    break;
                }
                yield return null;
            }
            AlinearFrenteAlJugador();
            foreach (PandilleroAPie p in Vivos())
            {
                p.MoverPlano(Vector3.zero);
            }
            // 3. Le dicen algo.
            if (empujador != null)
            {
                Frase(empujador, FrasesEnfrente[0], VocesPandilla.Provocar);
            }
            yield return Esperar(SegundosInsulto);
            if (otro != null)
            {
                Frase(otro, FrasesEnfrente[1], VocesPandilla.Insulto);
                yield return Esperar(0.9f);
            }
            // 4. Lo empujan: el clip de empujar y, en el cuadro del contacto, el derribo del jugador (ni antes ni después).
            Fase = "empujan";
            Vector3 direccion = empujador != null ? Plano(jugador.position - empujador.transform.position) : jugador.forward;
            if (empujador != null)
            {
                AlinearFrenteAlJugador();
                if (empujador.Visual == null || !EstadosNpcT1560.Gestear(empujador.Visual, GestoNpcT1560.Empujar))
                {
                    empujador.Visual?.Golpear(false);
                }
                Sonidos.ReproducirEn(SonidoInsulto, empujador.transform.position);
                yield return Esperar(SegundosHastaContacto);
            }
            controlador.ActivarControl(controlAntes); // un instante, para que el empujón lo mueva; el derribo bloquea caminar y pegar
            Aviso(TablaRecorrido.AvisoSinAtacar);
            atropello.Derribar(direccion);
            yield return Esperar(0.7f);
            // 5. Se ríen un momento y se van CORRIENDO por donde llegaron.
            Fase = "huyen";
            foreach (PandilleroAPie p in Vivos())
            {
                Sonidos.ReproducirEn(SonidoRisa, p.transform.position);
                p.Visual?.Burlar();
            }
            yield return Esperar(SegundosRisa);
            float finHuida = Time.time + SegundosHuida;
            while (Time.time < finHuida)
            {
                foreach (PandilleroAPie p in Vivos())
                {
                    Vector3 salida = Plano(puntoSalida - p.transform.position);
                    if (salida.sqrMagnitude < 1f)
                    {
                        salida = Plano(p.transform.position - jugador.position);
                    }
                    salida.Normalize();
                    p.MirarHacia(salida, GradosGiroPorSegundo);
                    p.MoverPlano(salida * RapidezCorrer);
                }
                yield return null;
            }
            // 6. Espera a que se levante (AtropelloJugador lo levanta solo) y lo suelta.
            float limiteLevantar = Time.time + SegundosMaximosLevantarse;
            while (atropello != null && atropello.Derribado && Time.time < limiteLevantar)
            {
                yield return null;
            }
            Fase = string.Empty;
            escena = null;
            Limpiar();
            Terminada?.Invoke();
        }

        // Camina hacia el puesto girando de frente a donde va; true si ya llegó (o no existe).
        private bool IrAlPuesto(PandilleroAPie p, Vector3 puesto, float margen)
        {
            if (p == null)
            {
                return true;
            }
            Vector3 d = Plano(puesto - p.transform.position);
            if (d.magnitude <= margen)
            {
                p.MoverPlano(Vector3.zero);
                p.MirarHacia(Plano(jugador.position - p.transform.position), GradosGiroPorSegundo);
                return true;
            }
            p.MirarHacia(d, GradosGiroPorSegundo);
            p.MoverPlano(d.normalized * RapidezPlantarse);
            return false;
        }

        // Una frase de la plática: habla uno y luego el otro; voz SKU_AMBIENTE (si la cola lo deja) y subtítulo siempre.
        private void Platicar(int indice)
        {
            List<PandilleroAPie> vivos = Vivos();
            if (vivos.Count == 0)
            {
                return;
            }
            PandilleroAPie quien = vivos[indice % vivos.Count];
            Frase(quien, FrasesPlatica[indice % FrasesPlatica.Length], VocesPandilla.Ambiente);
        }

        private void Frase(PandilleroAPie quien, string texto, string idVoz)
        {
            if (quien == null)
            {
                return;
            }
            Subtitulos.Publicar("SKULL", texto, quien.transform.position + Vector3.up * AlturaBoca, 2.8f, ColorSkull);
            VocesPandilla.Decir(quien.transform, idVoz, true);
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
