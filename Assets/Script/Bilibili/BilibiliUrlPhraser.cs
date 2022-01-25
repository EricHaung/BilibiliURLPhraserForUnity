using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace BiliBili
{
    public enum SearchCode
    {
        flv = 0, //flv格式 仅H.264编码 部分老视频存在分段现象 与mp4格式及dash格式互斥
        mp4 = 1,//mp4格式 仅H.264编码 不存在视频分段 与flv格式及dash格式互斥
        dash = 16, //dash格式 H.264编码或H.265编码 部分老视频的清晰度上限低于flv格式 与mp4格式及flv格式互斥
        hdr = 64, //HDR需求 必须为dash格式 && qn = 125 大多情况需求认证大会员账号
        k4 = 128, //4K需求 该值与fourk字段协同作用 && qn=120 大多情况需求认证大会员账号
        dolby = 256,//杜比音效需求 必须为dash格式 大多情况需求认证大会员账号
    }

    public class BilibiliUrlPhraser
    {
        public static Dictionary<string, Tuple<string, string, string, int>> urlDic = new Dictionary<string, Tuple<string, string, string, int>>();
        private string sourceUrl = ""; //https://www.bilibili.com/video/BV16Q4y1v7Ao?spm_id_from=333.851.b_62696c695f7265706f72745f646f756761.18
        private SearchCode[] searchCodes; //new SearchCode[] { SearchCode.mp4, SearchCode.dolby, SearchCode.hdr };
        private string getInfoUrl = "http://api.bilibili.com/x/web-interface/view";
        private string getVideoUrl = "http://api.bilibili.com/x/player/playurl";
        // private string getBangumiUrl = "http://api.bilibili.com/pgc/view/web/season";
        private ILoader loader;


        public BilibiliUrlPhraser(ILoader _loader)
        {
            loader = _loader;
        }

        public void GetURL(string _sourceUrl, SearchCode[] _searchCodes, Action<string, string, string, int> output)
        {
            sourceUrl = _sourceUrl;
            searchCodes = _searchCodes;
            if (urlDic.ContainsKey(sourceUrl))
            {
                if (GetExpireTime(urlDic[sourceUrl].Item1) > GetCurrentUNIXTime())
                {
                    Debug.Log("連結還沒過期 " + GetExpireTime(urlDic[sourceUrl].Item1));
                    output.Invoke(urlDic[sourceUrl].Item1, urlDic[sourceUrl].Item2, urlDic[sourceUrl].Item3, urlDic[sourceUrl].Item4);
                }
                else
                {
                    Debug.Log("連結過期惹");
                    urlDic.Remove(sourceUrl);
                    TryGetURLInfo(sourceUrl, (result) =>
                    {
                        if (result.errorCode != 0)
                        {
                            Debug.Log(result.errorCode);
                            Debug.Log(result.errorMsg);
                            output.Invoke("error", result.errorCode.ToString() + " " + result.errorMsg, "", 0);
                            return;
                        }

                        TryGetVideoUrl(0, searchCodes, result, (videoUrl, audioUrl, title, duration) =>
                        {
                            if (string.IsNullOrEmpty(videoUrl))
                            {
                                Debug.Log("無法取得需要的影片");
                                output.Invoke("", "", "", 0);
                                return;
                            }
                            urlDic.Add(sourceUrl, Tuple.Create(videoUrl, audioUrl, title, duration));
                            output.Invoke(videoUrl, audioUrl, title, duration);
                        });
                    });
                }
            }
            else
            {
                Debug.Log("是新的影片連結");
                TryGetURLInfo(sourceUrl, (result) =>
                {
                    if (result.errorCode != 0)
                    {
                        Debug.Log(result.errorCode);
                        Debug.Log(result.errorMsg);
                        output.Invoke("error", result.errorCode.ToString() + " " + result.errorMsg, "", 0);
                        return;
                    }

                    TryGetVideoUrl(0, searchCodes, result, (videoUrl, audioUrl, title, duration) =>
                     {
                         if (string.IsNullOrEmpty(videoUrl))
                         {
                             Debug.Log("無法取得需要的影片");
                             return;
                         }
                         urlDic.Add(sourceUrl, Tuple.Create(videoUrl, audioUrl, title, duration));
                         output.Invoke(videoUrl, audioUrl, title, duration);
                     });
                });
            }
        }

        private void TryGetURLInfo(string url, Action<BiliBiliData> callback)
        {
            BiliBiliURLData urlData = BilibiliUrlFilter(url);

            if (!string.IsNullOrEmpty(urlData.bvid) && urlData.type == URLType.video)
            {
                loader.SendWebRequest(getInfoUrl + "?bvid=" + urlData.bvid,
                (progressing) => { },
                (errorMsg) => { },
                (content) =>
                {
                    JObject result = JObject.Parse(content);

                    if ((int)(result["code"]) == 0)
                        callback?.Invoke(RawVideoInfoDataPhraser((JObject)result["data"]));
                    else
                        callback?.Invoke(new BiliBiliData() { errorCode = (int)(result["code"]), errorMsg = result["message"].ToString() });

                });
            }
            else
                callback?.Invoke(new BiliBiliData() { errorCode = -702, errorMsg = "網址錯誤" });
        }

        private BiliBiliURLData BilibiliUrlFilter(string url)
        {
            Match videoMatch;

            string videoPattern = @"www\.bilibili\.com\/video\/\w*";
            if ((videoMatch = Regex.Match(url, videoPattern)).Success)
            {
                return new BiliBiliURLData()
                {
                    type = URLType.video,
                    bvid = videoMatch.Groups[0].Value.Replace(@"www.bilibili.com/video/", "").Replace(@"?", "")
                };
            }

            Match liveMatch;
            string livePattern = @"live\.bilibili\.com\/\d*\?";
            if ((liveMatch = Regex.Match(url, livePattern)).Success)
            {
                Debug.Log("直播影片無法使用");
                return new BiliBiliURLData()
                {
                    type = URLType.live,
                    cid = liveMatch.Groups[0].Value.Replace(@"live.bilibili.com/", "").Replace(@"?", "")
                };
            }

            Match bangumiSeasonMatch;
            string bangumiSeasonPattern = @"www\.bilibili\.com\/bangumi\/play\/ss\d*";
            if ((bangumiSeasonMatch = Regex.Match(url, bangumiSeasonPattern)).Success)
            {
                Debug.Log("番劇影片無法使用");
                return new BiliBiliURLData()
                {
                    type = URLType.bangumi,
                    season_id = bangumiSeasonMatch.Groups[0].Value.Replace(@"www.bilibili.com/bangumi/play/ss", "")
                };
            }

            Match bangumiEpisodeMatch;
            string bangumiEpisodePattern = @"www\.bilibili\.com\/bangumi\/play\/ep\d*";
            if ((bangumiEpisodeMatch = Regex.Match(url, bangumiEpisodePattern)).Success)
            {
                Debug.Log("番劇影片無法使用");
                return new BiliBiliURLData()
                {
                    type = URLType.bangumi,
                    ep_id = bangumiEpisodeMatch.Groups[0].Value.Replace(@"www.bilibili.com/bangumi/play/ep", "")
                };
            }
            Debug.Log("非Bilibili影片");
            return new BiliBiliURLData() { type = URLType.none };
        }

        private BiliBiliData RawVideoInfoDataPhraser(JObject rawData)
        {
            JObject rightsJSON = (JObject)rawData["rights"];
            Rights m_rights = new Rights()
            {
                elec = (int)(rightsJSON["elec"]),
                download = (int)(rightsJSON["download"]),
                movie = (int)(rightsJSON["movie"]),
                pay = (int)(rightsJSON["pay"]),
                hd5 = (int)(rightsJSON["hd5"]),
                no_reprint = (int)(rightsJSON["no_reprint"]),
                autoplay = (int)(rightsJSON["autoplay"]),
                ugc_pay = (int)(rightsJSON["ugc_pay"]),
                is_stein_gate = (int)(rightsJSON["is_stein_gate"]),
                is_cooperation = (int)(rightsJSON["is_cooperation"]),
                is_360 = (int)(rightsJSON["is_360"])
            };

            JObject stateJSON = (JObject)rawData["stat"];
            State m_state = new State()
            {
                view = (int)(stateJSON["view"]),
                danmaku = (int)(stateJSON["danmaku"]),
                reply = (int)(stateJSON["reply"]),
                favorite = (int)(stateJSON["favorite"]),
                coin = (int)(stateJSON["coin"]),
                share = (int)(stateJSON["share"]),
                now_rank = (int)(stateJSON["now_rank"]),
                his_rank = (int)(stateJSON["his_rank"]),
                like = (int)(stateJSON["like"])
            };

            List<VideoData> videoDatas = new List<VideoData>();
            JArray pagesJSON = (JArray)rawData["pages"];
            foreach (var pageJSON in pagesJSON)
            {
                VideoData videoData = new VideoData()
                {
                    order = (int)(pageJSON["page"]),
                    cid = (int)(pageJSON["cid"]),
                    duration = (int)(pageJSON["duration"]),
                    video_formats = new VideoFormat[0],
                    audio_formats = new VideoFormat[0]
                };
                videoDatas.Add(videoData);
            }

            BiliBiliData data = new BiliBiliData()
            {
                bvid = rawData["bvid"].ToString(),
                avid = (int)(rawData["aid"]),
                copyright = (Copyright)(int)(rawData["copyright"]),
                coverImg = rawData["pic"].ToString(),
                title = rawData["title"].ToString(),
                description = rawData["desc_v2"] == null ? null : rawData["desc_v2"][0]["raw_text"].ToString(),
                width = (int)rawData["dimension"]["width"],
                height = (int)rawData["dimension"]["height"],
                rotate = (int)rawData["dimension"]["rotate"],
                rights = m_rights,
                videos = videoDatas.ToArray(),
                state = m_state
            };
            return data;
        }

        private void TryGetVideoUrl(int page, SearchCode[] searchCodes, BiliBiliData data, Action<string, string, string, int> result)
        {
            if (page > data.videos.Length)
            {
                Debug.Log("無效的段落要求");
                result?.Invoke("", "", "", 0);
                return;
            }
            else
                GetURLData(page, searchCodes, data, result);
        }

        private void GetURLData(int page, SearchCode[] searchCodes, BiliBiliData data, Action<string, string, string, int> result)
        {
            int searchCodeResult = 0;
            foreach (var searchCode in searchCodes)
                searchCodeResult = searchCodeResult | (int)searchCode;

            SearchCode typeCode = SearchCode.dash;

            if (searchCodeResult == (searchCodeResult | (int)SearchCode.mp4))
                typeCode = SearchCode.mp4;
            else if (searchCodeResult == (searchCodeResult | (int)SearchCode.dash))
                typeCode = SearchCode.dash;
            else
                typeCode = SearchCode.flv;

            loader.SendWebRequest(getVideoUrl + "?cid=" + data.videos[page].cid + "&bvid=" + data.bvid + "&fnval=" + searchCodeResult,
            (progressing) => { },
            (errorMsg) => { },
            (content) =>
            {
                JObject rawData = JObject.Parse(content);

                if ((int)(rawData["code"]) == 0)
                {
                    data = RawURLDataFormat(data, page, typeCode, (JObject)rawData["data"]);
                    Tuple<string, string, string, int> outputURLs = VideoFliter(data, page, typeCode);
                    result.Invoke(outputURLs.Item1, outputURLs.Item2, outputURLs.Item3, outputURLs.Item4);
                }
                else
                {
                    Debug.Log((int)rawData["code"]);
                    Debug.Log(rawData["message"]);
                    result?.Invoke("", "", "", 0);
                }
            });
        }

        private BiliBiliData RawURLDataFormat(BiliBiliData data, int page, SearchCode code, JObject rawData)
        {
            switch (code)
            {
                case SearchCode.mp4:
                    JArray mp4JSON = (JArray)rawData["durl"];
                    foreach (var video in data.videos)
                    {
                        if (video.order == page + 1)
                        {
                            List<VideoFormat> videoFormats = new List<VideoFormat>();

                            foreach (var videoJSON in mp4JSON)
                            {
                                VideoFormat videoFormat = new VideoFormat()
                                {
                                    id = (FormatCode)(int)(videoJSON["order"]),
                                    url = videoJSON["url"].ToString().Replace("\\u0026", "&"),
                                    mimeType = "mp4",
                                    codecs = "AVC(H264)",
                                    width = data.width,
                                    height = data.height,
                                    frameRate = -1,
                                    codecid = 7
                                };
                                videoFormats.Add(videoFormat);
                            }
                            data.videos[page].video_formats = videoFormats.ToArray();
                            data.videos[page].audio_formats = videoFormats.ToArray();
                        }
                    }
                    break;
                case SearchCode.dash:
                    JArray dashVideosJSON = (JArray)rawData["dash"]["video"];
                    foreach (var video in data.videos)
                    {
                        if (video.order == page + 1)
                        {
                            List<VideoFormat> videoFormats = new List<VideoFormat>();

                            foreach (var videoJSON in dashVideosJSON)
                            {
                                VideoFormat videoFormat = new VideoFormat()
                                {
                                    id = (FormatCode)(int)(videoJSON["id"]),
                                    url = videoJSON["baseUrl"].ToString().Replace("\\u0026", "&"),
                                    mimeType = videoJSON["mimeType"].ToString(),
                                    codecs = videoJSON["codecs"].ToString(),
                                    width = (int)videoJSON["width"],
                                    height = (int)videoJSON["height"],
                                    frameRate = (int)videoJSON["frameRate"],
                                    codecid = (int)videoJSON["codecid"]
                                };
                                videoFormats.Add(videoFormat);
                            }
                            data.videos[page].video_formats = videoFormats.ToArray();
                        }
                    }

                    JArray dashAudiosJSON = (JArray)rawData["dash"]["audio"];
                    foreach (var video in data.videos)
                    {
                        if (video.order == page + 1)
                        {
                            List<VideoFormat> audioFormats = new List<VideoFormat>();
                            foreach (var audioJSON in dashAudiosJSON)
                            {
                                VideoFormat audioFormat = new VideoFormat()
                                {
                                    id = (FormatCode)(int)(audioJSON["id"]),
                                    url = audioJSON["baseUrl"].ToString().Replace("\\u0026", "&"),
                                    mimeType = audioJSON["mimeType"].ToString(),
                                    codecs = audioJSON["codecs"].ToString(),
                                    width = (int)audioJSON["width"],
                                    height = (int)audioJSON["height"],
                                    frameRate = (int)audioJSON["frameRate"],
                                    codecid = (int)audioJSON["codecid"]
                                };
                                audioFormats.Add(audioFormat);
                            }
                            data.videos[page].audio_formats = audioFormats.ToArray();
                        }
                    }
                    break;
            }
            return data;
        }

        private Tuple<string, string, string, int> VideoFliter(BiliBiliData data, int page, SearchCode code)
        {
            string outputVideoUrl = "";
            string outputAudioUrl = "";
            int duration = 0;
            for (int index = 0; index < data.videos[page].video_formats.Length; index++)
            {
                if (data.videos[page].video_formats[index].codecid == 12)
                    continue;

                if (string.IsNullOrEmpty(data.videos[page].video_formats[index].url))
                    continue;

                outputVideoUrl = UnityHelper.ConvertUnicodeToString(data.videos[page].video_formats[index].url);
                duration = data.videos[page].duration;
                break;
            }

            for (int index = 0; index < data.videos[page].audio_formats.Length; index++)
            {
                if (data.videos[page].audio_formats[index].codecid != 0)
                    continue;

                if (string.IsNullOrEmpty(data.videos[page].audio_formats[index].url))
                    continue;

                outputAudioUrl = UnityHelper.ConvertUnicodeToString(data.videos[page].audio_formats[index].url);
                break;
            }

            return Tuple.Create(outputVideoUrl, outputAudioUrl, data.title, duration);
        }

        private int GetExpireTime(string url)
        {
            Match expireMatch;
            string expirePattern = @"deadline=\d*&";
            if ((expireMatch = Regex.Match(url, expirePattern)).Success)
            {
                return int.Parse(expireMatch.Groups[0].Value.Replace(@"deadline=", "").Replace(@"&", ""));
            }
            else
            {
                Debug.Log("影片連結找不到期限參數");
                return -1;
            }
        }

        private int GetCurrentUNIXTime()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
            return (int)(System.DateTime.UtcNow - epochStart).TotalSeconds;
        }

        public static bool IsBiliBiliUrl(string url)
        {
            Match videoMatch;
            string videoPattern = @"www\.bilibili\.com\/video\/\w*";
            return (videoMatch = Regex.Match(url, videoPattern)).Success;
        }

        public void GetCoverURL(Action<string> callback)
        {
            BiliBiliURLData urlData = BilibiliUrlFilter(sourceUrl);

            if (!string.IsNullOrEmpty(urlData.bvid) && urlData.type == URLType.video)
            {
                loader.SendWebRequest(getInfoUrl + "?bvid=" + urlData.bvid,
                (progressing) => { },
                (errorMsg) => { },
                (content) =>
                 {
                     JObject result = JObject.Parse(content);

                     if ((int)(result["code"]) == 0)
                         callback?.Invoke(result["data"]["pic"].ToString());
                     else
                         callback?.Invoke("");

                 });
            }
            else
                callback?.Invoke("");
        }
    }
}
