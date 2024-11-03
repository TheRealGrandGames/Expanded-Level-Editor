using UnityEngine;

namespace PlusLevelLoader.Custom
{
    internal class EditorBalloon : Balloon
    {
        private EnvironmentController ec;

        [SerializeField]
        private Entity entity;

        [SerializeField]
        private float radius = 1f;

        [SerializeField]
        private float minDirectionTime = 2.5f;

        [SerializeField]
        private float maxDirectionTime = 10f;

        private float directionTime;

        private Vector3 direction;

        private Vector3 _position;

        public float speed = 3f; //Was 10f and then after that 5f

        public float minSpeed = 1f;

        [SerializeField]
        private bool changeWhenSlow;

        public Entity Entity => entity;

        private void Start()
        {
            entity = gameObject.GetComponent<Entity>();
            Initialize(transform.parent.parent.gameObject.GetComponent<RoomController>());
            ChangeDirection();
            directionTime = Random.Range(minDirectionTime, maxDirectionTime);
            entity.OnEntityMoveInitialCollision += OnEntityMoveCollision;
        }

        private void Update()
        {
            directionTime -= Time.deltaTime;
            if (directionTime <= 0f || (changeWhenSlow && entity.Velocity.magnitude < minSpeed))
            {
                ChangeDirection();
                directionTime = Random.Range(minDirectionTime, maxDirectionTime);
            }

            entity.UpdateInternalMovement(direction * speed * ec.EnvironmentTimeScale);
        }

        public void Initialize(RoomController rc)
        {
            ec = rc.ec;
            base.transform.position = rc.RandomEventSafeCellNoGarbage().CenterWorldPosition;
            entity.Initialize(ec, base.transform.position);
        }

        public void EntityTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == 13 || other.gameObject.layer == 18)
            {
                direction = Vector3.Reflect(direction, (base.transform.position - other.transform.position).normalized);
                directionTime = Random.Range(minDirectionTime, maxDirectionTime);
            }
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        public void EntityTriggerExit(Collider other)
        {
        }

        private void OnEntityMoveCollision(RaycastHit hit)
        {
            direction = Vector3.Reflect(direction, hit.normal);
            Random.Range(minDirectionTime, maxDirectionTime);
        }

        private void ChangeDirection()
        {
            direction = Random.insideUnitCircle;
            direction.z = direction.y;
            direction.y = 0f;
            direction = direction.normalized;
        }

        public void Stop()
        {
            speed = 0f;
            base.enabled = false;
        }
    }
}