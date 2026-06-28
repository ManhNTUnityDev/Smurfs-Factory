using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace MatchFactory.UI
{
    public class LoadingController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image loadingFillImage;

        [Header("Settings")]
        [SerializeField] private float simulatedLoadTime = 2f;
        [SerializeField] private string nextSceneName = "Gameplay";

        private void Start()
        {
            if (loadingFillImage != null)
            {
                loadingFillImage.fillAmount = 0f;
            }
            StartCoroutine(LoadingRoutine());
        }

        private IEnumerator LoadingRoutine()
        {
            float elapsedTime = 0f;
            
            // Bước 1: Giả lập load config, data, v.v. (tăng thanh tiến trình lên 90%)
            while (elapsedTime < simulatedLoadTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / simulatedLoadTime);
                
                if (loadingFillImage != null)
                {
                    loadingFillImage.fillAmount = progress * 0.9f;
                }
                
                yield return null;
            }

            // Bước 2: Load scene thực tế
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                // Khi progress đạt 0.9 tức là scene đã load xong, chờ được kích hoạt
                if (asyncLoad.progress >= 0.9f)
                {
                    if (loadingFillImage != null)
                    {
                        loadingFillImage.fillAmount = 1f;
                    }
                    // Chờ thêm 1 chút cho mượt nếu cần
                    yield return new WaitForSeconds(0.2f);
                    asyncLoad.allowSceneActivation = true;
                }
                else
                {
                    if (loadingFillImage != null)
                    {
                        loadingFillImage.fillAmount = 0.9f + (asyncLoad.progress * 0.1f);
                    }
                }
                yield return null;
            }
        }
    }
}
