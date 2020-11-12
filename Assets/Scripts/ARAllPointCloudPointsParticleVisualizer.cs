using System;
using System.Collections.Generic;
//using System.IO;
using UnityEngine.XR.ARSubsystems;
//using System.Linq;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Renders all points in an <see cref="ARPointCloud"/> as a <c>ParticleSystem</c>, persisting them all.
    /// </summary>
    [RequireComponent(typeof(ARPointCloud))]
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ARAllPointCloudPointsParticleVisualizer : MonoBehaviour
    {
        //added lines                        
        public List<ARPoint> addedPoints = new List<ARPoint>();
        public List<Vector3> pointCloudPosition = new List<Vector3>();
        public List<Vector3> addedPointsGT = new List<Vector3>();
        public List<string> colliderHitName = new List<string>();
        public List<string> gtHitName = new List<string>();
        public List<string> PCHitName = new List<string>();
        public Dictionary<string, List<Vector3>> GTParts = new Dictionary<string, List<Vector3>>();
        public Dictionary<string, int> GTHit = new Dictionary<string, int>();
        public Dictionary<string, int> PCHit = new Dictionary<string, int>();
        bool effectsOn;




        public int totalPointCount => m_Points.Count;
        public ulong pointCloudIdentifier;


        void OnPointCloudChanged(ARPointCloudUpdatedEventArgs eventArgs)
        {
            RenderPoints();
        }

        void SetParticlePosition(int index, Vector3 position)
        {
            m_Particles[index].startColor = m_ParticleSystem.main.startColor.color;
            m_Particles[index].startSize = m_ParticleSystem.main.startSize.constant;
            m_Particles[index].position = position;
            m_Particles[index].remainingLifetime = 1f;
        }


        void RenderPoints()
        {

            if (!m_PointCloud.positions.HasValue)
                return;

            var positions = m_PointCloud.positions.Value;

            //Store all the positions over time associated with their unique identifiers
            if (m_PointCloud.identifiers.HasValue)
            {
                var identifiers = m_PointCloud.identifiers.Value;

                for (int x = -2; x < 2; x++)
                {
                    for (int y = -2; y < 2; y++)
                    {
                        float screenX = 0 + x;
                        float screenY = 0 + y;

                        Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);
                        RaycastHit[] hits;
                        hits = Physics.RaycastAll(Camera.main.transform.position, forward);

                        for (int i = 0; i < hits.Length; i++)
                        {
                            gtHitName.Add(hits[i].collider.name);

                            for (int j = 0; j < positions.Length; j++)
                            {
                                var dis = Vector3.Distance(hits[i].point, positions[j]);
                                if (dis < 0.01)
                                {
                                    m_Points[identifiers[j]] = positions[j];
                                    pointCloudPosition.Add(m_Points[identifiers[j]]); //Dictionary add position with key identifier                                    
                                    colliderHitName.Add(hits[i].collider.name);                                    
                                }
                            }
                        }
                    }
                }
            }


            // Make sure we have enough particles to store all the ones we want to draw
            int numParticles = m_Points.Count;
            if (m_Particles == null || m_Particles.Length < numParticles)
            {
                m_Particles = new ParticleSystem.Particle[numParticles];
            }
            // Draw all the particles
            int particleIndex = 0;
            foreach (var kvp in m_Points)
            {
                SetParticlePosition(particleIndex++, kvp.Value);
            }

            // Remove any existing particles by setting remainingLifetime
            // to a negative value.
            for (int i = numParticles; i < m_NumParticles; ++i)
            {
                m_Particles[i].remainingLifetime = -1f;
            }

            m_ParticleSystem.SetParticles(m_Particles, Math.Max(numParticles, m_NumParticles));
            m_NumParticles = numParticles;

        }


        void Awake()
        {
            m_PointCloud = GetComponent<ARPointCloud>();
            m_ParticleSystem = GetComponent<ParticleSystem>();
        }

        void OnEnable()
        {
            m_PointCloud.updated += OnPointCloudChanged;
            UpdateVisibility();
        }

        void OnDisable()
        {
            m_PointCloud.updated -= OnPointCloudChanged;
            UpdateVisibility();
        }

        void Update()
        {
            UpdateVisibility();
        }

        void UpdateVisibility()
        {
            SetVisible(enabled && (m_PointCloud.trackingState != TrackingState.None));
        }

        void SetVisible(bool visible)
        {
            if (m_ParticleSystem == null)
                return;

            var renderer = m_ParticleSystem.GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = visible;
        }

        public void EffectsOn(bool value)
        {
            effectsOn = value;
        }           

        public ARPointCloud m_PointCloud;

        ParticleSystem m_ParticleSystem;

        ParticleSystem.Particle[] m_Particles;

        int m_NumParticles;

        public Dictionary<ulong, Vector3> m_Points = new Dictionary<ulong, Vector3>();
    }

}
