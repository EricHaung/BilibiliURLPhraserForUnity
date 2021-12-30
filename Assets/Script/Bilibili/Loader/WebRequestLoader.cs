using System;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;

namespace BiliBili
{
    public class WebRequestLoader : MonoBehaviour, ILoader
    {
        UnityWebRequest www;
        public void SendWebRequest(string url, Action<float> OnProgressing, Action<string> OnError, Action<string> OnFinished)
        {
            StartCoroutine(Send(url, OnProgressing, OnError, OnFinished));
        }

        private IEnumerator Send(string url, Action<float> OnProgressing, Action<string> OnError, Action<string> OnFinished)
        {
            if (string.IsNullOrEmpty(url))
                OnError?.Invoke("Invalid URL");
            else
            {
                www = UnityWebRequest.Get(url);
                www.timeout = 60;
                www.SendWebRequest();

                while (!www.isDone)
                {
                    OnProgressing?.Invoke(www.downloadProgress);
                    yield return null;
                }

                if ((www.result != UnityWebRequest.Result.Success))
                {
                    OnError?.Invoke(www.error);
                }
                else
                {
                    OnFinished?.Invoke(www.downloadHandler.text);
                }
            }
        }
    }
}
