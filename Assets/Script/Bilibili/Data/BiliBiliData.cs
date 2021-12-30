namespace BiliBili
{
    public class BiliBiliURLData
    {
        public URLType type;
        public string bvid;
        public string cid;
        public string season_id;
        public string ep_id;
    }

    public enum URLType
    {
        none,
        video, //一般的影片
        bangumi, //番劇 (Bangumi 番組計劃)
        live, //直播(目前不支援
        comic//漫畫(目前不支援
    }

    public enum Copyright
    {
        Origin = 1,
        Reprint = 2
    }

    public class Rights
    {
        public int elec; //是否支持充电
        public int download; //是否允许下载
        public int movie; //是否电影
        public int pay; //是否PGC付费
        public int hd5; //是否有高码率
        public int no_reprint; //是否显示“禁止转载“标志
        public int autoplay; //是否自动播放
        public int ugc_pay; //是否UGC付费
        public int is_stein_gate; //是否为互动视频
        public int is_cooperation; //是否为联合投稿
        public int is_360; //是否为360影片
    }

    public class State
    {
        public int view; //播放數
        public int danmaku;//弹幕数
        public int reply;//评论数
        public int favorite;//收藏数
        public int coin;//投币数
        public int share;//分享数
        public int now_rank;//当前排名
        public int his_rank;//历史最高排行
        public int like;//获赞数
    }

    public class BiliBiliData
    {
        public int errorCode;
        public string errorMsg;
        public string bvid;
        public int avid;
        public Copyright copyright;
        public string coverImg;
        public string title;
        public string description;
        public int width;
        public int height;
        public int rotate; //是否将宽高对换 0：正常 1：对换
        public Rights rights;
        public VideoData[] videos;
        public State state;
    }
}