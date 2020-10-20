using System;
using System.Collections.Generic;
//using System.IO;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
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
       


        public enum Mode
        {
            /// <summary>
            /// Draw all the feature points from the start of the session
            /// </summary>
            All,

            /// <summary>
            /// Only draw the feature points from the current frame
            /// </summary>
            CurrentFrame,
        }

        [SerializeField]
        [Tooltip("Whether to draw all the feature points or only the ones from the current frame.")]
        Mode m_Mode;

        public Mode mode
        {
            get => m_Mode;
            set
            {
                m_Mode = value;
                RenderPoints();
            }
        }


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
                    RaycastHit hit;               

                //Shoot rays to check if there is collider to hit the CAD Model, if yes, store the points.
                for (int k = -2; k < 2; k++)
                {
                    for (int j = -2; j < 2; j++)
                    {
                        float screenX = 0 + j;
                        float screenY = 0 + k;
                        Vector3 forward = Camera.main.transform.TransformDirection(screenX, screenY, 100);
                                          
                        if (Physics.Raycast(Camera.main.transform.position, forward, out hit))
                        {
                            for (int i = 0; i < positions.Length; i++)
                            {
                                var dis = Vector3.Distance(hit.point, positions[i]);
                                if (dis < 0.01)
                                {                                    
                                    m_Points[identifiers[i]] = positions[i];                                                                        
                                    pointCloudPosition.Add(m_Points[identifiers[i]]);
                                    colliderHitName.Add(hit.collider.name);
                                }
                            }
                        }
                    }
                }

            }


                // Make sure we have enough particles to store all the ones we want to draw
                int numParticles = (mode == Mode.All) ? m_Points.Count : positions.Length;
                if (m_Particles == null || m_Particles.Length < numParticles)
                {
                    m_Particles = new ParticleSystem.Particle[numParticles];
                }

                switch (mode)
                {
                    case Mode.All:
                        {
                            // Draw all the particles
                            int particleIndex = 0;
                            foreach (var kvp in m_Points)
                            {
                                SetParticlePosition(particleIndex++, kvp.Value);
                                
                        }
                            break;
                        }
                    case Mode.CurrentFrame:
                        {
                            // Only draw the particles in the current frame
                            for (int i = 0; i < positions.Length; ++i)
                            {
                                SetParticlePosition(i, positions[i]);
                            }
                            break;
                        }
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

        

        public ARPointCloud m_PointCloud;

        ParticleSystem m_ParticleSystem;

        ParticleSystem.Particle[] m_Particles;       

        int m_NumParticles;

        public Dictionary<ulong, Vector3> m_Points = new Dictionary<ulong, Vector3>();
    }

}
