using System;

namespace BiliBili
{
    public interface ILoader
    {
        void SendWebRequest(string url, Action<float> OnProgressing, Action<string> OnError, Action<string> OnFinished);
    }
}