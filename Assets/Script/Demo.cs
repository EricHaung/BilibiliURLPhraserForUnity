using BiliBili;
using UnityEngine;
using UnityEngine.Video;

public class Demo : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    private ILoader loader;
    private BilibiliUrlPhraser bilibiliUrlPhraser;

    void Start()
    {
        loader = this.gameObject.AddComponent<WebRequestLoader>();
        bilibiliUrlPhraser = new BilibiliUrlPhraser(loader);

        bilibiliUrlPhraser.GetURL("https://www.bilibili.com/video/BV1dS4y1M7bU?spm_id_from=333.5.b_686967685f656e65726779.1", new SearchCode[] { SearchCode.mp4, SearchCode.dolby, SearchCode.hdr }, (videoUrl, audioUrl, title, duartion) =>
        {
            Debug.Log(videoUrl);
            videoPlayer.url = videoUrl;
            videoPlayer.Play();
        });
    }
    
}
