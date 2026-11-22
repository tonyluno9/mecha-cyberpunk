using System;
using System.Collections.Generic;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mundo.Datos;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Mundo.Ambiente
{
    /// <summary>
    /// T14 (T-1605): aplica el nivel de calidad (Bajo/Medio/Alto) a lo pesado del realismo: SSAO (features de URP), halos y conos de luz, partículas de
    /// aire y vapor, y sombras de luces adicionales. El nivel lo deja Opciones (ServicioOpciones) en PlayerPrefs con la llave
    /// <see cref="CalculoCalidadRealismo.LlaveNivel"/>; este componente lo lee al nacer y cada vez que Opciones avisa un cambio. Al destruirse devuelve el SSAO
    /// a como estaba.
    /// </summary>
    public class CalidadRealismo : MonoBehaviour
    {
        private const string NombreShaderHalo = "MechaCyberpunk/Mundo/HaloLuz";
        private const string CampoSombrasAdicionales = "m_AdditionalLightShadowsSupported";
        private const float SegundosEntreBusquedas = 1f;

        [SerializeField] private ConfigRealismo config;

        private readonly List<bool> ssaoOriginal = new List<bool>();
        private readonly List<Renderer> halos = new List<Renderer>();
        private readonly List<ParticleSystem> particulas = new List<ParticleSystem>();
        private IOpciones opciones;
        private float proximaBusqueda;
        private bool sombrasOriginalGuardadas;
        private bool sombrasOriginal;

        /// <summary>Nivel aplicado la última vez.</summary>
        public NivelCalidadRealismo Nivel { get; private set; } = NivelCalidadRealismo.Medio;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Mundo] CalidadRealismo en '" + name + "' necesita su ConfigRealismo.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            AplicarGuardado();
        }

        private void OnDisable()
        {
            Desuscribir();
        }

        private void OnDestroy()
        {
            RestaurarSsao();
            if (sombrasOriginalGuardadas)
            {
                PonerSombrasAdicionales(true);
            }
        }

        private void LateUpdate()
        {
            // Opciones puede nacer después que este componente: se engancha cuando aparezca.
            if (opciones == null && Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosEntreBusquedas;
                IOpciones actual = Registro<IOpciones>.Actual;
                if (actual != null)
                {
                    opciones = actual;
                    opciones.Cambio += AplicarGuardado;
                    AplicarGuardado();
                }
            }
        }

        private void Desuscribir()
        {
            if (opciones != null)
            {
                opciones.Cambio -= AplicarGuardado;
                opciones = null;
            }
        }

        private void AplicarGuardado()
        {
            Aplicar(CalculoCalidadRealismo.Nivel(PlayerPrefs.GetInt(CalculoCalidadRealismo.LlaveNivel, (int)NivelCalidadRealismo.Medio)));
        }

        /// <summary>Aplica un nivel: busca de nuevo halos y partículas (pueden haber nacido o morido) y los prende o apaga.</summary>
        public void Aplicar(NivelCalidadRealismo nivel)
        {
            Nivel = nivel;
            AjustesRealismo ajustes = CalculoCalidadRealismo.Ajustes(nivel);
            PonerSsao(ajustes.OclusionAmbiental);
            PonerHalos(ajustes.Halos);
            PonerParticulas(ajustes.Particulas);
            PonerSombrasAdicionales(ajustes.SombrasLucesAdicionales);
        }

        private void PonerSsao(bool prendido)
        {
            ScriptableRendererFeature[] features = config.OclusionAmbiental;
            if (ssaoOriginal.Count != features.Length)
            {
                ssaoOriginal.Clear();
                for (int i = 0; i < features.Length; i++)
                {
                    ssaoOriginal.Add(features[i] != null && features[i].isActive);
                }
            }
            for (int i = 0; i < features.Length; i++)
            {
                if (features[i] != null)
                {
                    features[i].SetActive(prendido);
                }
            }
        }

        private void RestaurarSsao()
        {
            ScriptableRendererFeature[] features = config != null ? config.OclusionAmbiental : null;
            if (features == null || ssaoOriginal.Count != features.Length)
            {
                return;
            }
            for (int i = 0; i < features.Length; i++)
            {
                if (features[i] != null)
                {
                    features[i].SetActive(ssaoOriginal[i]);
                }
            }
        }

        private void PonerHalos(bool prendido)
        {
            halos.Clear();
            MeshRenderer[] mallas = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < mallas.Length; i++)
            {
                Material material = mallas[i].sharedMaterial;
                if (material != null && material.shader != null && material.shader.name == NombreShaderHalo)
                {
                    halos.Add(mallas[i]);
                }
            }
            for (int i = 0; i < halos.Count; i++)
            {
                halos[i].enabled = prendido;
            }
        }

        private void PonerParticulas(bool prendido)
        {
            particulas.Clear();
            string[] prefijos = config.PrefijosParticulas;
            ParticleSystem[] todos = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < todos.Length; i++)
            {
                if (EmpiezaConAlguno(todos[i].gameObject.name, prefijos))
                {
                    particulas.Add(todos[i]);
                }
            }
            for (int i = 0; i < particulas.Count; i++)
            {
                ParticleSystem.EmissionModule emision = particulas[i].emission;
                emision.enabled = prendido;
                ParticleSystemRenderer render = particulas[i].GetComponent<ParticleSystemRenderer>();
                if (render != null)
                {
                    render.enabled = prendido;
                }
            }
        }

        private static bool EmpiezaConAlguno(string nombre, string[] prefijos)
        {
            for (int i = 0; i < prefijos.Length; i++)
            {
                if (nombre.StartsWith(prefijos[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        // URP 17 no deja cambiar esto por API pública en ejecución: se escribe el campo del asset activo (la copia que hace Opciones, no el del disco).
        private void PonerSombrasAdicionales(bool prendido)
        {
            UniversalRenderPipelineAsset asset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (asset == null)
            {
                asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            }
            if (asset == null)
            {
                return;
            }
            FieldInfo campo = typeof(UniversalRenderPipelineAsset).GetField(CampoSombrasAdicionales, BindingFlags.Instance | BindingFlags.NonPublic);
            if (campo == null)
            {
                return;
            }
            if (!sombrasOriginalGuardadas)
            {
                sombrasOriginal = (bool)campo.GetValue(asset);
                sombrasOriginalGuardadas = true;
            }
            campo.SetValue(asset, prendido && sombrasOriginal);
        }

        /// <summary>Para pruebas: cuántos halos y partículas encontró en la última aplicación.</summary>
        public int CantidadHalos => halos.Count;

        public int CantidadParticulas => particulas.Count;
    }
}
