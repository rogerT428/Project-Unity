using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TrashSorting
{
    /// <summary>Skor, timer, feedback edukasi, suara, dan restart.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Aturan")]
        public float timeLimit = 300f;   // 5 menit, santai untuk anak-anak
        public int correctPoints = 10;
        public int wrongPenalty = 0;     // tidak dikurangi, cukup diberi penjelasan

        [Header("UI")]
        public Text scoreText;
        public Text timerText;
        public Text remainingText;
        public Text feedbackText;
        public Text infoText;
        public GameObject gameOverPanel;
        public Text gameOverText;

        public bool IsPlaying { get; private set; }

        int _score, _correct, _wrong, _remaining;
        float _timeLeft, _feedbackTimer;
        AudioSource _audio;
        AudioClip _correctClip, _wrongClip, _winClip;

        void Awake()
        {
            Instance = this;
            _audio = gameObject.AddComponent<AudioSource>();
            _correctClip = MakeTone(new[] { 660f, 880f }, 0.12f);
            _wrongClip = MakeTone(new[] { 220f, 160f }, 0.18f);
            _winClip = MakeTone(new[] { 523f, 659f, 784f, 1046f }, 0.15f);
        }

        void Start()
        {
            _remaining = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
            _timeLeft = timeLimit;
            IsPlaying = true;
            if (gameOverPanel) gameOverPanel.SetActive(false);
            SetFeedback("Ambil sampah dan buang ke tempat yang benar!", Color.white, "");
            RefreshUI();
        }

        void Update()
        {
            if (!IsPlaying) return;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f) { _timeLeft = 0f; EndGame(false); }

            if (_feedbackTimer > 0f && (_feedbackTimer -= Time.deltaTime) <= 0f && feedbackText)
                feedbackText.text = "";
            RefreshUI();
        }

        public void ReportSort(TrashItem item, TrashType bin, bool correct)
        {
            if (correct)
            {
                _score += correctPoints; _correct++; _remaining--;
                _audio.PlayOneShot(_correctClip);
                SetFeedback($"BENAR! {item.displayName} = {item.type}", new Color(0.4f, 1f, 0.5f), item.info);
                if (_remaining <= 0) EndGame(true);
            }
            else
            {
                _score = Mathf.Max(0, _score - wrongPenalty); _wrong++;
                _audio.PlayOneShot(_wrongClip);
                SetFeedback($"SALAH! {item.displayName} bukan {bin}, tapi {item.type}", new Color(1f, 0.45f, 0.4f), item.info);
            }
            RefreshUI();
        }

        void EndGame(bool win)
        {
            IsPlaying = false;
            if (win) _audio.PlayOneShot(_winClip);
            if (gameOverPanel) gameOverPanel.SetActive(true);
            if (gameOverText)
                gameOverText.text = (win ? "SELAMAT! Semua sampah terpilah!" : "WAKTU HABIS!") +
                                    $"\n\nSkor: {_score}\nBenar: {_correct}   Salah: {_wrong}";
        }

        public void RestartGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        void SetFeedback(string msg, Color c, string info)
        {
            if (feedbackText) { feedbackText.text = msg; feedbackText.color = c; }
            if (infoText) infoText.text = info;
            _feedbackTimer = 4f;
        }

        void RefreshUI()
        {
            if (scoreText) scoreText.text = $"Skor: {_score}";
            if (remainingText) remainingText.text = $"Sisa sampah: {_remaining}";
            if (timerText)
            {
                int t = Mathf.CeilToInt(_timeLeft);
                timerText.text = $"Waktu: {t / 60:0}:{t % 60:00}";
            }
        }

        static AudioClip MakeTone(float[] notes, float noteLen)
        {
            const int rate = 44100;
            int per = (int)(rate * noteLen);
            var data = new float[per * notes.Length];
            for (int n = 0; n < notes.Length; n++)
                for (int i = 0; i < per; i++)
                {
                    float env = 1f - (float)i / per;
                    data[n * per + i] = Mathf.Sin(2 * Mathf.PI * notes[n] * i / rate) * 0.3f * env;
                }
            var clip = AudioClip.Create("tone", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
