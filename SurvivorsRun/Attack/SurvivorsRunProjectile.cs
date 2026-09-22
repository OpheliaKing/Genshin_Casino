using UnityEngine;

namespace SHIN
{
    /// <summary>
    /// 투사체 런타임. DamageObject + StraightMover를 묶고, 수명·명중 소멸을 처리한다.
    /// </summary>
    [RequireComponent(typeof(SurvivorsRunDamageObject))]
    [RequireComponent(typeof(SurvivorsRunProjectileStraightMover))]
    public class SurvivorsRunProjectile : MonoBehaviour
    {
        [SerializeField] private float _lifetime = 2.5f;
        [SerializeField] private bool _destroyOnHit = true;

        private SurvivorsRunDamageObject _damageObject;
        private SurvivorsRunProjectileStraightMover _mover;
        private float _lifeRemaining;
        private bool _launched;
        private bool _destroying;

        private void Awake()
        {
            _damageObject = GetComponent<SurvivorsRunDamageObject>();
            _mover = GetComponent<SurvivorsRunProjectileStraightMover>();
        }

        private void OnEnable()
        {
            if (_damageObject != null)
                _damageObject.DamageApplied += OnDamageApplied;
        }

        private void OnDisable()
        {
            if (_damageObject != null)
                _damageObject.DamageApplied -= OnDamageApplied;
        }

        public void Launch(
            SurvivorsRunUnitBase owner,
            int damage,
            float hitCooldown,
            Vector2 direction,
            float speed,
            float lifetime = -1f)
        {
            if (_damageObject == null)
                _damageObject = GetComponent<SurvivorsRunDamageObject>();
            if (_mover == null)
                _mover = GetComponent<SurvivorsRunProjectileStraightMover>();

            _destroying = false;
            _lifeRemaining = lifetime > 0f ? lifetime : _lifetime;
            _mover.BindTimeOwner(owner);
            _damageObject.Setup(owner, damage, hitCooldown);
            _damageObject.SetDamageEnabled(true);
            _mover.Launch(direction, speed);
            _launched = true;
        }

        private void Update()
        {
            if (!_launched || _destroying)
                return;

            var scale = _damageObject != null && _damageObject.Owner != null && _damageObject.Owner.Manager != null
                ? _damageObject.Owner.Manager.TimeScale
                : 1f;
            if (scale <= 0f)
                return;

            _lifeRemaining -= Time.deltaTime * scale;
            if (_lifeRemaining <= 0f)
                Despawn();
        }

        private void OnDamageApplied(SurvivorsRunUnitBase target)
        {
            if (!_destroyOnHit || _destroying)
                return;

            Despawn();
        }

        private void Despawn()
        {
            if (_destroying)
                return;

            _destroying = true;
            _launched = false;
            if (_mover != null)
                _mover.Stop();
            if (_damageObject != null)
                _damageObject.SetDamageEnabled(false);

            Destroy(gameObject);
        }
    }
}
