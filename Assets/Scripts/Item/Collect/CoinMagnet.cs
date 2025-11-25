using UnityEngine;

public class CoinMagnet : MonoBehaviour
{
    [SerializeField] private float detectRadius = 4f;
    [SerializeField] private float pullSpeed = 9f;
    [SerializeField] private AnimationCurve speedCurve;

    private Transform _playerTransform;
    private bool _isPulled = false;
    private float _timePulled = 0f;

    private void Awake()
    {
        _playerTransform = FindFirstObjectByType<Player>()?.transform;
    }

    private void OnEnable()
    {
        _isPulled = false;
        _timePulled = 0f;

        if (_playerTransform == null)
        {
            _playerTransform = FindFirstObjectByType<Player>()?.transform;
        }
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        // เริ่มดึงเมื่อเข้าในรัศมี
        if (!_isPulled)
        {
            float distance = Vector2.Distance(transform.position, _playerTransform.position);

            if (distance <= detectRadius)
            {
                _isPulled = true;
                _timePulled = 0f;
            }
            else
            {
                return;
            }
        }

        // คำนวณความเร็วตามเส้นโค้ง
        _timePulled += Time.deltaTime;
        float evaluatedTime = Mathf.Clamp01(_timePulled);
        float spd = pullSpeed * speedCurve.Evaluate(evaluatedTime);

        // ดึงเข้าหาผู้เล่น (ไม่มีการ Collect)
        transform.position = Vector2.MoveTowards(
            transform.position,
            _playerTransform.position,
            spd * Time.deltaTime
        );
    }

}
