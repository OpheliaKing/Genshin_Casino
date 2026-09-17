using UnityEngine;

namespace SHIN
{
    public class SurvivorsRunUnitBase : MonoBehaviour
    {
        private string _tid;
        private int _hp;
        private int _maxHp;
        private int _attack;
        private float _moveSpeed;
        private float _attackSpeed;
        /// <summary>
        /// 슬로우 등에 사용
        /// </summary>
        private float _characterSpeed;

        private SURVIVORSRUN_UNIT_TYPE _unitType;
        private SurvivorsRunManager _manager;
        private bool _isDead;

        public string Tid => _tid;
        public int Hp => _hp;
        public int MaxHp => _maxHp;
        public int Attack => _attack;
        public float MoveSpeed => _moveSpeed;
        public float AttackSpeed => _attackSpeed;
        public SURVIVORSRUN_UNIT_TYPE UnitType => _unitType;
        public SurvivorsRunManager Manager => _manager;
        public bool IsDead => _isDead || _hp <= 0;

        public void BindManager(SurvivorsRunManager manager)
        {
            _manager = manager;
        }

        public void Setup(
            string tid,
            SURVIVORSRUN_UNIT_TYPE unitType,
            int maxHp,
            int attack,
            float moveSpeed,
            float attackSpeed)
        {
            _tid = tid;
            _unitType = unitType;
            _maxHp = Mathf.Max(1, maxHp);
            _hp = _maxHp;
            _attack = Mathf.Max(0, attack);
            _moveSpeed = Mathf.Max(0f, moveSpeed);
            _attackSpeed = Mathf.Max(0f, attackSpeed);
            _characterSpeed = 1f;
            _isDead = false;
        }

        public void SetMoveSpeed(float moveSpeed)
        {
            _moveSpeed = Mathf.Max(0f, moveSpeed);
        }

        /// <summary>
        /// 플레이어 입력·AI 공통 이동.
        /// direction이 zero면 정지, 아니면 정규화 후 상하좌우(대각 포함)로 이동한다.
        /// </summary>
        public void Move(Vector2 direction)
        {
            if (IsDead || _moveSpeed <= 0f)
                return;

            if (direction.sqrMagnitude <= 0.0001f)
                return;

            var scale = _manager != null ? _manager.TimeScale : 1f;
            var delta = direction.normalized * (_moveSpeed * Time.deltaTime * scale);
            var pos = transform.position;
            pos.x += delta.x;
            pos.y += delta.y;
            transform.position = pos;
        }

        /// <summary>
        /// 피해 적용. 가급적 <see cref="SurvivorsRunCombat.TryApplyDamage"/> 경유로 호출한다.
        /// </summary>
        public bool TakeDamage(int damage, SurvivorsRunUnitBase attacker = null)
        {
            if (IsDead || damage <= 0)
                return false;

            _hp = Mathf.Max(0, _hp - damage);
            Debug.Log(
                $"[SurvivorsRun] HP {_tid ?? name}: -{damage} → {_hp}/{_maxHp}" +
                (attacker != null ? $" (by {attacker.Tid ?? attacker.name})" : string.Empty),
                this);
            OnDamaged(damage, attacker);

            if (_hp <= 0)
                Die(attacker);

            return true;
        }

        protected virtual void OnDamaged(int damage, SurvivorsRunUnitBase attacker)
        {
        }

        protected virtual void Die(SurvivorsRunUnitBase killer)
        {
            if (_isDead)
                return;

            _isDead = true;
            _hp = 0;

            if (_unitType == SURVIVORSRUN_UNIT_TYPE.PLAYER)
            {
                Debug.Log(
                    $"[SurvivorsRun] Player death: {_tid ?? name}" +
                    (killer != null ? $" (killed by {killer.Tid ?? killer.name})" : string.Empty),
                    this);
            }

            OnDied(killer);
            gameObject.SetActive(false);
        }

        protected virtual void OnDied(SurvivorsRunUnitBase killer)
        {
        }
    }

    public enum SURVIVORSRUN_UNIT_TYPE
    {
        NONE,
        PLAYER,
        ENEMY,
    }
}
