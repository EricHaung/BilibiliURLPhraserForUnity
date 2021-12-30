namespace BiliBili
{
    public enum FormatCode
    {
        p240 = 6, //240P 极速 (仅mp4方式支持)
        p360 = 16,//360P 流畅	
        p480 = 32, //480P 清晰
        p720 = 64, //720P 高清 (web端默认值，B站前端需要登录才能选择，但是直接发送请求可以不登录就拿到720P的取流地址，无720P时则为720P60)
        p720f60 = 74, //720P60 高帧率 (需要认证登录账号)
        p1080 = 80,//1080P 高清 (需要认证登录账号)
        p1080h = 112,//1080P+ 高码率 (大多情况需求认证大会员账号)
        p1080hf60 = 116, //1080P60 高帧率 (大多情况需求认证大会员账号)
        k4 = 120, //4K 超清 (大多情况需求认证大会员账号)
        hdr = 125, //HDR 真彩色 (仅支持dash方式 大多情况需求认证大会员账号)
        k64 = 30216, //64K音樂
        k132 = 30232,//132K音樂
        k192 = 30280 //192K音樂
    }

    public class VideoFormat
    {
        public FormatCode id;
        public string url;
        public string mimeType;
        public string codecs;
        public int width;
        public int height;
        public float frameRate;
        public int codecid; //7=AVC(H264)，12=HEVC(H265)，音樂: 0
                            //(flv格式 and mp4格式) 仅H.264编码 
    }

    public class VideoData
    {
        public int order; //段落編號
        public int cid;
        public int duration;//duration单位为秒，length单位为毫秒
        public VideoFormat[] video_formats;
        public VideoFormat[] audio_formats;
    }
}
