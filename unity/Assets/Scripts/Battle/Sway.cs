using UnityEngine;

namespace GameMaker.Battle
{
    /// <summary>바람에 살랑이는 흔들림 — 밑동(이 오브젝트의 위치)을 축으로 좌우 회전.</summary>
    public class Sway : MonoBehaviour
    {
        public float amplitude = 2f;  // 최대 각도
        public float speed = 1.2f;
        float phase;

        void Start() => phase = transform.position.x * 0.013f; // 위치 기반 위상차 (일렁임)

        void Update()
        {
            transform.localRotation = Quaternion.Euler(0, 0,
                Mathf.Sin(Time.time * speed + phase) * amplitude);
        }
    }

    /// <summary>느린 상하 출렁임 — 바다 수면 등.</summary>
    public class Bob : MonoBehaviour
    {
        public float amplitude = 8f;
        public float speed = 0.9f;
        Vector3 basePos;

        void Start() => basePos = transform.position;

        void Update() =>
            transform.position = basePos + new Vector3(0, Mathf.Sin(Time.time * speed) * amplitude, 0);
    }

    /// <summary>지면 잔디립의 물결 — 좌우로 천천히 밀렸다 돌아오며 바닥이 일렁이는 착각.</summary>
    public class LipWave : MonoBehaviour
    {
        public float amplitude = 8f;   // 좌우 이동폭(px)
        public float vertAmp = 1.5f;   // 상하 이동폭(px) — 바다는 크게
        public float speed = 1.3f;
        public float phase;
        Vector3 basePos;

        void Start() => basePos = transform.position;

        void Update()
        {
            float t = Time.time * speed + phase;
            transform.position = basePos + new Vector3(
                Mathf.Sin(t) * amplitude,
                Mathf.Sin(t * 2.1f) * vertAmp, 0);
        }
    }

    /// <summary>바람에 흘러가는 표류 — 왼쪽 밖으로 나가면 오른쪽 화면 바로 밖에서
    /// 높이/속도/크기를 바꿔 재등장 (새 구름이 계속 생기는 것처럼).</summary>
    public class Drift : MonoBehaviour
    {
        public float speed = 25f;      // px/초
        public float wrapLeft = -350f;
        public float wrapRight = 3050f; // 화면 오른쪽 가장자리 바로 밖
        public float baseY = 900f;

        void Update()
        {
            var p = transform.position;
            p.x -= speed * Time.deltaTime;
            if (p.x < wrapLeft)
            {
                // 화면 오른쪽 바로 밖에서 재등장 (작은 랜덤 지연) + 모습 랜덤 변경
                p.x = wrapRight + Random.Range(0f, 260f);
                p.y = baseY + Random.Range(-140f, 180f);
                speed = Random.Range(24f, 55f); // 너무 느리면 복귀가 오래 걸려 최저속도 상향
                float s = Random.Range(1.4f, 3.0f);
                transform.localScale = new Vector3(s, s, 1f);
            }
            transform.position = p;
        }
    }
}
