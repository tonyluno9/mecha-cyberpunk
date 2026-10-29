using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.IA.Aliados
{
    /// <summary>
    /// Tu pandilla (T-405): hasta 3 aliados. Crea y guarda a los aliados (clave <c>aliados</c>), sube su nivel por vencidos, los trae de
    /// vuelta 45 s después de caer y alterna seguir/esperar con F.
    /// </summary>
    public class PandillaJugador : MonoBehaviour, IPandillaJugador, IGuardable
    {
        public const string Clave = "aliados";
        private const int MaximoAliados = 3;

        [Tooltip("Prefab del aliado (Aliado.prefab).")]
        [SerializeField] private GameObject prefabAliado;
        [Tooltip("Segundos que tarda en volver un aliado caído (s).")]
        [Min(1f)] [SerializeField] private float segundosVuelta = 45f;
        [Tooltip("Distancia detrás de ti a la que vuelve un aliado caído (m).")]
        [Min(1f)] [SerializeField] private float distanciaVuelta = 15f;

        private class Miembro
        {
            public string Nombre;
            public int Vencidos;
            public AliadoAPie Aliado;
            public float Regreso = -1f;
        }

        [Serializable]
        private class DatosGuardados
        {
            public string[] nombres = Array.Empty<string>();
            public int[] vencidos = Array.Empty<int>();
        }

        private readonly List<Miembro> miembros = new List<Miembro>();
        private readonly List<Transform> transforms = new List<Transform>();
        private bool siguiendo = true;

        public string ClaveGuardado => Clave;
        public int Cantidad => miembros.Count;
        public int Maximo => MaximoAliados;
        public bool Siguiendo => siguiendo;
        public IReadOnlyList<Transform> Aliados
        {
            get
            {
                transforms.Clear();
                foreach (Miembro m in miembros)
                {
                    if (m.Aliado != null && m.Aliado.gameObject.activeSelf)
                    {
                        transforms.Add(m.Aliado.transform);
                    }
                }
                return transforms;
            }
        }

        public event Action Cambio;
        public event Action<int> SubioDeNivel;

        public int Nivel(int indice) => ReglaAliado.Nivel(miembros[indice].Vencidos);

        public string Nombre(int indice) => miembros[indice].Nombre;

        public float Vida(int indice)
        {
            Miembro m = miembros[indice];
            return m.Aliado == null || !m.Aliado.gameObject.activeSelf ? 0f : m.Aliado.Actual / m.Aliado.Maxima;
        }

        public bool Tiene(string nombre) => miembros.Exists(m => m.Nombre == nombre);

        private void OnEnable()
        {
            Registro<IPandillaJugador>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
        }

        private void OnDisable()
        {
            Registro<IPandillaJugador>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
        }

        /// <summary>Suma un aliado a la pandilla. False si está llena o ya lo tienes.</summary>
        public bool Reclutar(string nombre)
        {
            if (miembros.Count >= MaximoAliados || Tiene(nombre))
            {
                return false;
            }
            miembros.Add(new Miembro { Nombre = nombre });
            Cambio?.Invoke();
            return true;
        }

        private void Update()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return;
            }
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.fKey.wasPressedThisFrame && miembros.Count > 0 && jugador.Modo == ModoJugador.APie)
            {
                siguiendo = !siguiendo;
                foreach (Miembro m in miembros)
                {
                    if (m.Aliado != null)
                    {
                        m.Aliado.Siguiendo = siguiendo;
                    }
                }
                Cambio?.Invoke();
            }
            for (int i = 0; i < miembros.Count; i++)
            {
                Miembro m = miembros[i];
                if (m.Aliado == null)
                {
                    Crear(m, jugador.Raiz.position + jugador.Raiz.right * (1.5f + i));
                }
                else if (m.Regreso >= 0f && Time.time >= m.Regreso)
                {
                    m.Regreso = -1f;
                    m.Aliado.Revivir(Suelo(jugador.Raiz.position - jugador.Raiz.forward * distanciaVuelta));
                    Cambio?.Invoke();
                }
            }
        }

        private void Crear(Miembro m, Vector3 posicion)
        {
            if (prefabAliado == null)
            {
                return;
            }
            GameObject objeto = Instantiate(prefabAliado, Suelo(posicion), Quaternion.identity);
            objeto.name = "Aliado_" + m.Nombre;
            m.Aliado = objeto.GetComponent<AliadoAPie>();
            m.Aliado.PonerNivel(ReglaAliado.Nivel(m.Vencidos));
            m.Aliado.Siguiendo = siguiendo;
            m.Aliado.Cambio += AlCambiar;
            m.Aliado.KO += () => AlCaer(m);
            m.Aliado.Vencio += _ => AlVencer(m);
            Cambio?.Invoke();
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 30f, Vector3.down, out RaycastHit piso, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                return piso.point + Vector3.up * 0.05f;
            }
            return punto;
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void AlCaer(Miembro m)
        {
            m.Regreso = Time.time + segundosVuelta;
            Cambio?.Invoke();
        }

        private void AlVencer(Miembro m)
        {
            int antes = ReglaAliado.Nivel(m.Vencidos);
            m.Vencidos++;
            int despues = ReglaAliado.Nivel(m.Vencidos);
            if (despues > antes)
            {
                m.Aliado.PonerNivel(despues);
                SubioDeNivel?.Invoke(miembros.IndexOf(m));
            }
            Cambio?.Invoke();
        }

        public string Guardar()
        {
            DatosGuardados d = new DatosGuardados
            {
                nombres = new string[miembros.Count],
                vencidos = new int[miembros.Count]
            };
            for (int i = 0; i < miembros.Count; i++)
            {
                d.nombres[i] = miembros[i].Nombre;
                d.vencidos[i] = miembros[i].Vencidos;
            }
            return JsonUtility.ToJson(d);
        }

        public void Cargar(string datos)
        {
            DatosGuardados d;
            try
            {
                d = JsonUtility.FromJson<DatosGuardados>(datos);
            }
            catch (Exception)
            {
                d = null;
            }
            if (d == null || d.nombres == null || d.vencidos == null || d.nombres.Length != d.vencidos.Length)
            {
                Debug.LogWarning("[IA] No se entendió el guardado de aliados.");
                return;
            }
            foreach (Miembro m in miembros)
            {
                if (m.Aliado != null)
                {
                    Destroy(m.Aliado.gameObject);
                }
            }
            miembros.Clear();
            for (int i = 0; i < d.nombres.Length && i < MaximoAliados; i++)
            {
                miembros.Add(new Miembro { Nombre = d.nombres[i], Vencidos = d.vencidos[i] });
            }
            Cambio?.Invoke();
        }
    }
}
