namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;
    using Fusion;
    using Fusion.KCC;
    using UnityEngine.Windows;


    /// <summary>
    /// Base class for Simple and Advanced player implementations.
    /// Provides references to components and basic setup.
    /// </summary>
    [RequireComponent(typeof(KCC))]
	[OrderBefore(typeof(KCC))]
    public abstract class Player : NetworkKCCProcessor
    {
        // PUBLIC VARS

        // Provide public access to some private vars (these are getter/setter shortcuts)
        public KCC KCC => _kcc;
        public PlayerInput Input => _input;
        public Camera Camera => _camera;
        public GameObject Visual => _visual;
        public NetworkCulling Culling => _culling;

        public bool DisableCameraFollow { get; set; } = false;

        [Networked]
        public float SpeedMultiplier { get; set; } = 1.0f;


        // PROTECTED VARS

        protected Transform CameraPivot => _cameraPivot;

        protected Transform CameraPivotOffset => _cameraPivotOffset;

        protected Transform CameraHandle => _cameraHandle;
        protected float MaxCameraAngle => _maxCameraAngle;

        protected float MinCameraAngle => _minCameraAngle;
        //protected VariableSpeedKCCProcessor VariableSpeedProcessor => _variableSpeedProcessor;

 

        // PRIVATE VARS

        [SerializeField]
        private GameObject _visual;

        [SerializeField]
        private Transform _cameraPivot;

        [SerializeField]
        private Transform _cameraPivotOffset;

        [SerializeField]
        private Transform _cameraHandle;

        [SerializeField]
        private float _minCameraAngle;

        [SerializeField]
        private float _maxCameraAngle;

        [SerializeField]
        private float _areaOfInterestRadius;

        //[SerializeField]
        //private VariableSpeedKCCProcessor _variableSpeedProcessor;

        private KCC _kcc;
        private PlayerInput _input;
        private NetworkCulling _culling;
        private Camera _camera;


        private void Awake()
        {
            _kcc = gameObject.GetComponent<KCC>();
            _input = gameObject.GetComponent<PlayerInput>();

            // Establish network culling callback method
            _culling = gameObject.GetComponent<NetworkCulling>();
            _culling.Updated = OnCullingUpdated;
        }


        #region NetworkBehaviour overrides

        public override void Spawned()
        {
            //name = Object.InputAuthority.ToString();

            if (Object.HasInputAuthority == true)
            {
                // GetComponent used here is provided by custom SceneExtensions class in Utils
               _camera = Runner.SimulationUnityScene.GetComponent<Camera>();
            }

            // Explicit KCC initialization. This needs to be called before using API, otherwise changes could be overriden by implicit initialization from KCC.Start() or KCC.Spawned()
            _kcc.Initialize(EKCCDriver.Fusion);

            // Player itself can modify kinematic speed, registering to KCC
            _kcc.AddModifier(this);

        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _camera = null;
        }

        public override void FixedUpdateNetwork()
        {
            // By default we expect derived classes to process input in FixedUpdateNetwork().
            // The correct approach is to set input before KCC updates internally => we need to specify [OrderBefore(typeof(KCC))] attribute.

            // SimplePlayer runs input processing in FixedUpdateNetwork() as expected, but KCC runs its internal update after Player.FixedUpdateNetwork().
            // Following call sets AoI position to last fixed update KCC position. It should not be a problem in most cases, but some one-frame glitches after teleporting might occur.
            // This problem is solved in AdvancedPlayer which uses manual KCC update at the cost of slightly increased complexity.

            Runner.AddPlayerAreaOfInterest(Object.InputAuthority, _kcc.FixedData.TargetPosition, _areaOfInterestRadius);
        }

        #endregion


        #region NetworkKCCProcessor INTERFACE

        // Lowest priority => this processor will be executed last
        public override float Priority => float.MinValue;

        public override EKCCStages GetValidStages(KCC kcc, KCCData data)
        {
            // Only SetKinematicSpeed stage is used, rest are filtered out and corresponding method calls will be skipped
            return EKCCStages.SetKinematicSpeed;
        }

        public override void SetKinematicSpeed(KCC kcc, KCCData data)
        {
            // Applying multiplier.
            data.KinematicSpeed *= SpeedMultiplier;
        }


        #endregion


        // PRIVATE METHODS

        /// <summary>
        /// Callback method for network culling
        /// </summary>
        /// <param name="isCulled">Network culled flag</param>
        private void OnCullingUpdated(bool isCulled)
        {
            // Show/hide the game object based on AoI (Area of Interest)
            _visual.SetActive(isCulled == false);

            if (_kcc.Collider != null)
            {
                _kcc.Collider.gameObject.SetActive(isCulled == false);
            }
        }
    }
}
